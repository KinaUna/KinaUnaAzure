using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaWeb.Controllers
{
    public class KanbanItemsController(IKanbanItemsHttpClient kanbanItemsHttpClient) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> GetKanbanItem(int kanbanItemId)
        {
            KanbanItem kanbanItem = await kanbanItemsHttpClient.GetKanbanItem(kanbanItemId);
            if (kanbanItem == null)
            {
                return NotFound();
            }

            return Json(kanbanItem);
        }

        public async Task<IActionResult> GetKanbanItemsForBoard(int kanbanBoardId)
        {
            List<KanbanItem> kanbanItems = await kanbanItemsHttpClient.GetKanbanItemsForBoard(kanbanBoardId);
            
            return Json(kanbanItems);
        }
    }
}
