using KinaUna.Data.Extensions;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    public class KanbanItemsController(IKanbanItemsHttpClient kanbanItemsHttpClient,
        IKanbanBoardsHttpClient kanbanBoardsHttpClient,
        ITodoItemsHttpClient todoItemsHttpClient,
        IViewModelSetupService viewModelSetupService,
        IProgenyHttpClient progenyHttpClient) : Controller
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

        public async Task<IActionResult> AddKanbanItem(int kanbanBoardId, int columnId, int rowIndex)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            KanbanItemViewModel model = new(baseModel);
            
            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserId != null)
            {
                model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
                model.SetProgenyList();
            }

            model.KanbanItem.TodoItem = new TodoItem();
            model.KanbanItem.KanbanBoardId = kanbanBoardId;
            model.KanbanItem.ColumnIndex = columnId;
            model.KanbanItem.RowIndex = rowIndex;
            model.KanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanBoardId);
            
            model.SetAccessLevelList();
            model.SetStatusList(0);
            return PartialView("_AddKanbanItemPartial", model);
        }

        [HttpPost]
        public async Task<IActionResult> AddKanbanItem([FromForm] KanbanItemViewModel model)
        {
            KanbanItem kanbanItem = new();
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.KanbanBoard.ProgenyId);
            model.SetBaseProperties(baseModel);

            List<Progeny> progAdminList = await progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);
            if (progAdminList.Count == 0)
            {
                // Todo: Show that no children are available to add kanban for.
                return RedirectToAction("Index", "Kanbans");
            }
            TodoItem todoItem = await todoItemsHttpClient.AddTodoItem(model.KanbanItem.TodoItem);
            if (todoItem == null || todoItem.TodoItemId == 0) return Json(kanbanItem);

            model.KanbanItem.TodoItemId = todoItem.TodoItemId;
            model.KanbanItem.TodoItem = todoItem;
                
            KanbanItem addedKanbanItem = await kanbanItemsHttpClient.AddKanbanItem(model.KanbanItem);
            if (addedKanbanItem == null || addedKanbanItem.KanbanItemId == 0) return Json(kanbanItem);
            addedKanbanItem.TodoItem = todoItem;
            kanbanItem = addedKanbanItem;

            return Json(kanbanItem);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateKanbanItem([FromBody] KanbanItem kanbanItem)
        {
            KanbanItem existingKanbanItem = await kanbanItemsHttpClient.GetKanbanItem(kanbanItem.KanbanItemId);
            if (existingKanbanItem == null || existingKanbanItem.KanbanItemId == 0) return Json(new KanbanItem());
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), existingKanbanItem.TodoItem.ProgenyId);
            KanbanItemViewModel model = new(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return Json(new KanbanItem());
            }

            existingKanbanItem.ColumnIndex = kanbanItem.ColumnIndex;
            existingKanbanItem.RowIndex = kanbanItem.RowIndex;
            existingKanbanItem.ModifiedBy = model.CurrentUser.UserEmail;
            existingKanbanItem.ModifiedTime = System.DateTime.UtcNow;

            KanbanItem updatedKanbanItem = await kanbanItemsHttpClient.UpdateKanbanItem(existingKanbanItem);
            if (updatedKanbanItem == null || updatedKanbanItem.KanbanItemId == 0) return Json(new KanbanItem());

            return Json(updatedKanbanItem);
        }
    }
}
