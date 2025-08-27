using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services.KanbanServices;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUnaProgenyApi.Services;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class KanbanBoardsController(IKanbanBoardsService kanbanBoardsService, IUserAccessService userAccessService, IProgenyService progenyService) : ControllerBase
    {
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> GetKanbanBoard(int kanbanBoardId)
        {
            KanbanBoard kanbanBoard = await kanbanBoardsService.GetKanbanBoardById(kanbanBoardId);
            if (kanbanBoard == null)
            {
                return NotFound();
            }
            
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(kanbanBoard.ProgenyId, userEmail, kanbanBoard.AccessLevel);

            if (accessLevelResult.IsSuccess) return Ok(kanbanBoard);

            return accessLevelResult.ToActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] KanbanBoard kanbanBoard)
        {
            if (kanbanBoard == null)
            {
                return BadRequest();
            }

            Progeny progeny = await progenyService.GetProgeny(kanbanBoard.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (progeny != null)
            {

                if (!progeny.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return BadRequest();
            }

            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(kanbanBoard.ProgenyId, userEmail, kanbanBoard.AccessLevel);
            if (!accessLevelResult.IsSuccess) return accessLevelResult.ToActionResult();

            KanbanBoard savedKanbanItem = await kanbanBoardsService.AddKanbanBoard(kanbanBoard);

            return Ok(savedKanbanItem);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] KanbanBoard kanbanBoard)
        {
            if (kanbanBoard == null || id != kanbanBoard.KanbanBoardId)
            {
                return BadRequest();
            }

            KanbanBoard existingKanbanBoard = await kanbanBoardsService.GetKanbanBoardById(id);
            if (existingKanbanBoard == null)
            {
                return BadRequest();
            }

            Progeny progeny = await progenyService.GetProgeny(existingKanbanBoard.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (progeny != null)
            {

                if (!progeny.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return BadRequest();
            }

            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(kanbanBoard.ProgenyId, userEmail, kanbanBoard.AccessLevel);
            if (!accessLevelResult.IsSuccess) return accessLevelResult.ToActionResult();

            kanbanBoard.ModifiedBy = User.GetUserId();
            kanbanBoard.ModifiedTime = DateTime.UtcNow;

            KanbanBoard resultKanbanItem = await kanbanBoardsService.UpdateKanbanBoard(kanbanBoard);
            return Ok(resultKanbanItem);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            KanbanBoard existingKanbanBoard = await kanbanBoardsService.GetKanbanBoardById(id);
            if (existingKanbanBoard == null)
            {
                return NotFound();
            }
            
            Progeny progeny = await progenyService.GetProgeny(existingKanbanBoard.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (progeny != null)
            {

                if (!progeny.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return BadRequest();
            }

            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(existingKanbanBoard.ProgenyId, userEmail, existingKanbanBoard.AccessLevel);
            if (!accessLevelResult.IsSuccess) return accessLevelResult.ToActionResult();

            KanbanBoard deletedKanbanItem = await kanbanBoardsService.DeleteKanbanBoard(existingKanbanBoard);
            return Ok(deletedKanbanItem);
        }
    }
}
