using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using KinaUnaProgenyApi.Data;
using KinaUnaProgenyApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class NotesController : ControllerBase
    {
        private readonly ProgenyDbContext _context;

        public NotesController(ProgenyDbContext context)
        {
            _context = context;

        }
        // GET api/notes
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            List<Note> resultList = await _context.NotesDb.AsNoTracking().ToListAsync();

            return Ok(resultList);
        }

        // GET api/notes/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            List<Note> notesList = await _context.NotesDb.AsNoTracking().Where(n => n.ProgenyId == id && n.AccessLevel >= accessLevel).ToListAsync();
            if (notesList.Any())
            {
                return Ok(notesList);
            }
            else
            {
                return NotFound();
            }

        }

        // GET api/notes/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNoteItem(int id)
        {
            Note result = await _context.NotesDb.AsNoTracking().SingleOrDefaultAsync(n => n.NoteId == id);

            return Ok(result);
        }

        // POST api/notes
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Note value)
        {
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

            return Ok(noteItem);
        }

        // PUT api/notes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Note value)
        {
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

            return Ok(noteItem);
        }

        // DELETE api/notes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Note noteItem = await _context.NotesDb.SingleOrDefaultAsync(n => n.NoteId == id);
            if (noteItem != null)
            {
                _context.NotesDb.Remove(noteItem);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetNoteMobile(int id)
        {
            Note result = await _context.NotesDb.AsNoTracking().SingleOrDefaultAsync(n => n.NoteId == id);

            return Ok(result);
        }
        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> SyncAll()
        {
            
            HttpClient notesHttpClient = new HttpClient();
            
            notesHttpClient.BaseAddress = new Uri("https://kinauna.com");
            notesHttpClient.DefaultRequestHeaders.Accept.Clear();
            notesHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string notesApiPath = "/api/azureexport/notesexport";
            var notesUri = "https://kinauna.com" + notesApiPath;

            var notesResponseString = await notesHttpClient.GetStringAsync(notesUri);

            List<Note> notesList = JsonConvert.DeserializeObject<List<Note>>(notesResponseString);
            List<Note> notesItems = new List<Note>();
            foreach (Note value in notesList)
            {
                Note noteItem = new Note();
                noteItem.AccessLevel = value.AccessLevel;
                noteItem.Owner = value.Owner;
                noteItem.Content = value.Content;
                noteItem.Category = value.Category;
                noteItem.ProgenyId = value.ProgenyId;
                noteItem.Title = value.Title;
                noteItem.CreatedDate = value.CreatedDate;
                await _context.NotesDb.AddAsync(noteItem);
                notesItems.Add(noteItem);
                
            }
            await _context.SaveChangesAsync();

            return Ok(notesItems);
        }
    }
}
