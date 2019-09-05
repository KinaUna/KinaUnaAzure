using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class NotesController : ControllerBase
    {
        private readonly ProgenyDbContext _context;
        private readonly IDataService _dataService;

        public NotesController(ProgenyDbContext context, IDataService dataService)
        {
            _context = context;
            _dataService = dataService;
        }
        
        // GET api/notes/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(id, userEmail); // _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == id && u.UserId.ToUpper() == userEmail.ToUpper());
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Note> notesList = await _dataService.GetNotesList(id); // await _context.NotesDb.AsNoTracking().Where(n => n.ProgenyId == id && n.AccessLevel >= accessLevel).ToListAsync();
                notesList = notesList.Where(n => n.AccessLevel >= accessLevel).ToList();
                if (notesList.Any())
                {
                    return Ok(notesList);
                }
                return NotFound();
            }

            return Unauthorized();
        }

        // GET api/notes/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNoteItem(int id)
        {
            Note result = await _dataService.GetNote(id); // await _context.NotesDb.AsNoTracking().SingleOrDefaultAsync(n => n.NoteId == id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail); // _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            return Unauthorized();
        }

        // POST api/notes
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Note value)
        {
            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to add notes for this child.

                if (!prog.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            Note noteItem = new Note();
            noteItem.AccessLevel = value.AccessLevel;
            noteItem.Owner = value.Owner;
            noteItem.Content = value.Content;
            noteItem.Category = value.Category;
            noteItem.ProgenyId = value.ProgenyId;
            noteItem.Title = value.Title;
            noteItem.CreatedDate = DateTime.UtcNow;

            _context.NotesDb.Add(noteItem);
            await _context.SaveChangesAsync();
            await _dataService.SetNote(noteItem.NoteId);

            // Add to Timeline.
            TimeLineItem tItem = new TimeLineItem();
            tItem.ProgenyId = noteItem.ProgenyId;
            tItem.AccessLevel = noteItem.AccessLevel;
            tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Note;
            tItem.ItemId = noteItem.NoteId.ToString();
            UserInfo userinfo = _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == userEmail.ToUpper());
            if (userinfo != null)
            {
                tItem.CreatedBy = userinfo.UserId;
            }
            tItem.CreatedTime = noteItem.CreatedDate;
            tItem.ProgenyTime = noteItem.CreatedDate;

            await _context.TimeLineDb.AddAsync(tItem);
            await _context.SaveChangesAsync();
            await _dataService.SetTimeLineItem(tItem.TimeLineId);

            return Ok(noteItem);
        }

        // PUT api/notes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Note value)
        {
            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to edit notes for this child.
                if (!prog.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            Note noteItem = await _context.NotesDb.SingleOrDefaultAsync(n => n.NoteId == id);
            if (noteItem == null)
            {
                return NotFound();
            }

            noteItem.AccessLevel = value.AccessLevel;
            noteItem.Owner = value.Owner;
            noteItem.Content = value.Content;
            noteItem.Category = value.Category;
            noteItem.ProgenyId = value.ProgenyId;
            noteItem.Title = value.Title;
            noteItem.CreatedDate = value.CreatedDate;

            _context.NotesDb.Update(noteItem);
            await _context.SaveChangesAsync();
            await _dataService.SetNote(noteItem.NoteId);

            // Update Timeline.
            TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                t.ItemId == noteItem.NoteId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Note);
            if (tItem != null)
            {
                tItem.ProgenyTime = noteItem.CreatedDate;
                tItem.AccessLevel = noteItem.AccessLevel;
                _context.TimeLineDb.Update(tItem);
                await _context.SaveChangesAsync();
                await _dataService.SetTimeLineItem(tItem.TimeLineId);
            }

            return Ok(noteItem);
        }

        // DELETE api/notes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Note noteItem = await _context.NotesDb.SingleOrDefaultAsync(n => n.NoteId == id);
            if (noteItem != null)
            {
                // Check if child exists.
                Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == noteItem.ProgenyId);
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                if (prog != null)
                {
                    // Check if user is allowed to delete notes for this child.
                    if (!prog.IsInAdminList(userEmail))
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    return NotFound();
                }

                TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                    t.ItemId == noteItem.NoteId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Note);
                if (tItem != null)
                {
                    _context.TimeLineDb.Remove(tItem);
                    await _context.SaveChangesAsync();
                    await _dataService.RemoveTimeLineItem(tItem.TimeLineId, tItem.ItemType, tItem.ProgenyId);
                }

                _context.NotesDb.Remove(noteItem);
                await _context.SaveChangesAsync();
                await _dataService.RemoveNote(noteItem.NoteId, noteItem.ProgenyId);

                return NoContent();
            }

            return NotFound();
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetNoteMobile(int id)
        {
            Note result = await _dataService.GetNote(id); // await _context.NotesDb.AsNoTracking().SingleOrDefaultAsync(n => n.NoteId == id);

            if (result != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail); // _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());

                if (userAccess != null || result.ProgenyId == Constants.DefaultChildId)
                {
                    return Ok(result);
                }

                return Unauthorized();
            }

            return NotFound();
        }
    }
}
