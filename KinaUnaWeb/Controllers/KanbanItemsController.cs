using KinaUna.Data.Extensions;
using KinaUna.Data.Models.DTOs;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    public class KanbanItemsController(
        IKanbanItemsHttpClient kanbanItemsHttpClient,
        IKanbanBoardsHttpClient kanbanBoardsHttpClient,
        ITodoItemsHttpClient todoItemsHttpClient,
        IViewModelSetupService viewModelSetupService,
        IProgenyHttpClient progenyHttpClient,
        IUserInfosHttpClient userInfosHttpClient,
        ISubtasksHttpClient subtasksHttpClient) : Controller
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

        [HttpGet]
        public async Task<IActionResult> KanbanItemDetails(int kanbanItemId)
        {
            KanbanItem kanbanItem = await kanbanItemsHttpClient.GetKanbanItem(kanbanItemId);
            if (kanbanItem == null)
            {
                return NotFound();
            }

            TodoItem todoItem = await todoItemsHttpClient.GetTodoItem(kanbanItem.TodoItemId);
            if (todoItem == null)
            {
                return NotFound();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), todoItem.ProgenyId);
            KanbanItemViewModel model = new(baseModel)
            {
                KanbanItem = kanbanItem
            };

            
            model.KanbanItem.TodoItem = todoItem;
            model.KanbanItem.TodoItem.Progeny = await progenyHttpClient.GetProgeny(model.KanbanItem.TodoItem.ProgenyId);
            model.KanbanItem.TodoItem.Progeny.PictureLink = model.KanbanItem.TodoItem.Progeny.GetProfilePictureUrl();
            UserInfo todoUserInfo = await userInfosHttpClient.GetUserInfoByUserId(model.KanbanItem.TodoItem.CreatedBy);
            model.KanbanItem.TodoItem.CreatedBy = todoUserInfo.FullName();
            model.SetStatusList(model.KanbanItem.TodoItem.Status);

            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserId != null)
            {
                model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
                model.SetProgenyList();
            }

            model.SetAccessLevelList();
            model.SetStatusList(kanbanItem.TodoItem.Status);

            if (model.KanbanItem.TodoItem.ParentTodoItemId != 0)
            {
                model.ParentTodoItem = await todoItemsHttpClient.GetTodoItem(model.KanbanItem.TodoItem.ParentTodoItemId);
            }

            return PartialView("_KanbanItemDetailsPartial", model);

        }


        public async Task<IActionResult> GetKanbanItemsForBoard(int kanbanBoardId)
        {
            List<KanbanItem> kanbanItems = await kanbanItemsHttpClient.GetKanbanItemsForBoard(kanbanBoardId);
            foreach (KanbanItem kanbanItem in kanbanItems)
            {
                TodoItem todoItem = await todoItemsHttpClient.GetTodoItem(kanbanItem.TodoItemId);
                kanbanItem.TodoItem = todoItem;
                if (kanbanItem.TodoItem != null)
                {
                    kanbanItem.TodoItem.Progeny = await progenyHttpClient.GetProgeny(kanbanItem.TodoItem.ProgenyId);
                    if (kanbanItem.TodoItem.Progeny != null)
                    {
                        kanbanItem.TodoItem.Progeny.PictureLink = kanbanItem.TodoItem.Progeny.GetProfilePictureUrl();
                    }

                    UserInfo todoUserInfo = await userInfosHttpClient.GetUserInfoByUserId(kanbanItem.TodoItem.CreatedBy);
                    kanbanItem.TodoItem.CreatedBy = todoUserInfo.FullName();
                }
            }

            return Json(kanbanItems);
        }

        public async Task<IActionResult> AddKanbanItemFromTodoItem([FromBody] KanbanItem kanbanItem)
        {
            TodoItem todoItem = await todoItemsHttpClient.GetTodoItem(kanbanItem.TodoItemId);
            if (todoItem == null || todoItem.TodoItemId == 0)
            {
                return Json(new KanbanItem());
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), todoItem.ProgenyId);
            KanbanItemViewModel model = new(baseModel);
            List<Progeny> progAdminList = await progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);
            if (progAdminList.Count == 0)
            {
                return Json(new KanbanItem());
            }

            KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanItem.KanbanBoardId);
            kanbanBoard.SetColumnsListFromColumns();
            KanbanBoardColumn column = kanbanBoard.ColumnsList.SingleOrDefault(k => k.ColumnIndex == 0);
            if (column != null)
            {
                kanbanItem.ColumnId = column.Id;

            }

            foreach (KanbanBoardColumn columnItem in kanbanBoard.ColumnsList)
            {
                if (columnItem.SetStatus != todoItem.Status) continue;
                kanbanItem.ColumnId = columnItem.Id;
                break;
            }

            kanbanItem.RowIndex = -1;
            kanbanItem.TodoItem = todoItem;
            KanbanItem addedKanbanItem = await kanbanItemsHttpClient.AddKanbanItem(kanbanItem);
            if (addedKanbanItem == null || addedKanbanItem.KanbanItemId == 0) return Json(new KanbanItem());
            addedKanbanItem.TodoItem = todoItem;
            addedKanbanItem.TodoItem.Progeny = await progenyHttpClient.GetProgeny(addedKanbanItem.TodoItem.ProgenyId);
            if (addedKanbanItem.TodoItem.Progeny != null)
            {
                addedKanbanItem.TodoItem.Progeny.PictureLink = addedKanbanItem.TodoItem.Progeny.GetProfilePictureUrl();
            }
            UserInfo todoUserInfo = await userInfosHttpClient.GetUserInfoByUserId(addedKanbanItem.TodoItem.CreatedBy);
            addedKanbanItem.TodoItem.CreatedBy = todoUserInfo.FullName();
            return Json(addedKanbanItem);
        }

        public async Task<IActionResult> CopyKanbanItemToKanbanBoard(int kanbanItemId)
        {
            KanbanItem kanbanItem = await kanbanItemsHttpClient.GetKanbanItem(kanbanItemId);
            if (kanbanItem == null || kanbanItem.KanbanItemId == 0)
            {
                return NotFound();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), kanbanItem.TodoItem.ProgenyId);
            KanbanItemViewModel model = new(baseModel)
            {
                KanbanItem = kanbanItem
            };
            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserId != null)
            {
                model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
                model.SetProgenyList();
                model.KanbanItem.TodoItem = await todoItemsHttpClient.GetTodoItem(model.KanbanItem.TodoItemId);
                model.KanbanItem.TodoItem.Progeny = model.CurrentProgeny;
            }

            model.SetAccessLevelList();
            model.SetStatusList(model.KanbanItem.TodoItem.Status);

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
            model.SetProgenyList();

            List<int> progenyIds = [];
            progenyIds.AddRange(model.ProgenyList.ConvertAll(p => int.Parse(p.Value)));
            KanbanBoardsRequest kanbanBoardsRequest = new()
            {
                ProgenyIds = progenyIds,
                IncludeDeleted = false,
                Skip = 0,
                NumberOfItems = 0 // Get all.
            };
            KanbanBoardsResponse kanbanBoardsResponse = await kanbanBoardsHttpClient.GetProgeniesKanbanBoardsList(kanbanBoardsRequest);
            model.KanbanBoards = kanbanBoardsResponse.KanbanBoards;
            model.KanbanBoardsList = new List<SelectListItem>();
            
            foreach (KanbanBoard kanbanBoard in model.KanbanBoards)
            {
                List<KanbanItem> kanbanItems = await kanbanItemsHttpClient.GetKanbanItemsForBoard(kanbanBoard.KanbanBoardId);
                // If there is a KanbanItem with the KanbanBoardId for this TodoItem already, don't include it.
                if (kanbanItems.Exists(k => k.KanbanItemId == kanbanItemId))
                {
                    continue;
                }

                kanbanBoard.Progeny = await progenyHttpClient.GetProgeny(kanbanBoard.ProgenyId);
                SelectListItem item = new()
                {
                    Text = kanbanBoard.Title + " (" + kanbanBoard.Progeny.NickName + ")",
                    Value = kanbanBoard.KanbanBoardId.ToString()
                };
                model.KanbanBoardsList.Add(item);
            }

            return PartialView("_CopyKanbanItemToKanbanBoardPartial", model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> CopyKanbanItemToKanbanBoard([FromForm] KanbanItemViewModel model)
        {
            int kanbanBoardId = model.KanbanItem.KanbanBoardId;
            KanbanItem kanbanItem = await kanbanItemsHttpClient.GetKanbanItem(model.KanbanItem.KanbanItemId);
            if (kanbanItem == null || kanbanItem.KanbanItemId == 0)
            {
                return NotFound();
            }

            TodoItem existingTodoItem = await todoItemsHttpClient.GetTodoItem(kanbanItem.TodoItemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), existingTodoItem.ProgenyId);
            model.SetBaseProperties(baseModel);
            List<Progeny> progAdminList = await progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);
            if (progAdminList.Count == 0)
            {
                return BadRequest();
            }

            if (model.TodoItemReference == 1)
            {
                // Create a new TodoItem based on the existing one.
                model.KanbanItem.TodoItem.TodoItemId = 0;
                model.KanbanItem.TodoItem.CreatedBy = model.CurrentUser.UserEmail;
                model.KanbanItem.TodoItem.CreatedTime = System.DateTime.UtcNow;
                model.KanbanItem.TodoItem.ModifiedBy = model.CurrentUser.UserEmail;
                model.KanbanItem.TodoItem.ModifiedTime = System.DateTime.UtcNow;
                TodoItem newTodoItem = await todoItemsHttpClient.AddTodoItem(model.KanbanItem.TodoItem);
                kanbanItem.TodoItem = newTodoItem;
                kanbanItem.TodoItemId = newTodoItem.TodoItemId;

                // If the existing TodoItem has subtasks, copy them as well.
                SubtasksRequest subtasksRequest = new()
                {
                    ParentTodoItemId = existingTodoItem.TodoItemId,
                    ProgenyId = existingTodoItem.ProgenyId,
                    Skip = 0,
                    NumberOfItems = 0 // Get all.
                };
                SubtasksResponse subtasksResponse = await subtasksHttpClient.GetSubtasksList(subtasksRequest);
                foreach (TodoItem subTask in subtasksResponse.Subtasks)
                {
                    subTask.TodoItemId = 0;
                    subTask.ParentTodoItemId = newTodoItem.TodoItemId;

                    _ = await subtasksHttpClient.AddSubtask(subTask);
                }
            }

            KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanBoardId);
            kanbanBoard.SetColumnsListFromColumns();
            KanbanBoardColumn column = kanbanBoard.ColumnsList.SingleOrDefault(k => k.ColumnIndex == 0);
            if (column != null)
            {
                kanbanItem.ColumnId = column.Id;

            }

            foreach (KanbanBoardColumn columnItem in kanbanBoard.ColumnsList)
            {
                if (columnItem.SetStatus != existingTodoItem.Status) continue;
                kanbanItem.ColumnId = columnItem.Id;
                break;
            }

            KanbanItem newKanbanItem = new()
            {
                KanbanBoardId = kanbanBoard.KanbanBoardId,
                ColumnId = kanbanItem.ColumnId,
                RowIndex = -1,
                TodoItemId = kanbanItem.TodoItemId,
                CreatedBy = model.CurrentUser.UserEmail,
                CreatedTime = System.DateTime.UtcNow
            };

            KanbanItem addedKanbanItem = await kanbanItemsHttpClient.AddKanbanItem(newKanbanItem);
            
            return Json(addedKanbanItem);
        }

        public async Task<IActionResult> MoveKanbanItemToKanbanBoard(int kanbanItemId)
        {
            KanbanItem kanbanItem = await kanbanItemsHttpClient.GetKanbanItem(kanbanItemId);
            if (kanbanItem == null || kanbanItem.KanbanItemId == 0)
            {
                return NotFound();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), kanbanItem.TodoItem.ProgenyId);
            KanbanItemViewModel model = new(baseModel)
            {
                KanbanItem = kanbanItem
            };
            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserId != null)
            {
                model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
                model.SetProgenyList();
                model.KanbanItem.TodoItem = await todoItemsHttpClient.GetTodoItem(model.KanbanItem.TodoItemId);
                model.KanbanItem.TodoItem.Progeny = model.CurrentProgeny;
            }

            model.SetAccessLevelList();
            model.SetStatusList(model.KanbanItem.TodoItem.Status);

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
            model.SetProgenyList();

            List<int> progenyIds = [];
            progenyIds.AddRange(model.ProgenyList.ConvertAll(p => int.Parse(p.Value)));
            KanbanBoardsRequest kanbanBoardsRequest = new()
            {
                ProgenyIds = progenyIds,
                IncludeDeleted = false,
                Skip = 0,
                NumberOfItems = 0 // Get all.
            };
            KanbanBoardsResponse kanbanBoardsResponse = await kanbanBoardsHttpClient.GetProgeniesKanbanBoardsList(kanbanBoardsRequest);
            model.KanbanBoards = kanbanBoardsResponse.KanbanBoards;
            model.KanbanBoardsList = new List<SelectListItem>();

            foreach (KanbanBoard kanbanBoard in model.KanbanBoards)
            {
                List<KanbanItem> kanbanItems = await kanbanItemsHttpClient.GetKanbanItemsForBoard(kanbanBoard.KanbanBoardId);
                // If there is a KanbanItem with the KanbanBoardId for this TodoItem already, don't include it.
                if (kanbanItems.Exists(k => k.KanbanItemId == kanbanItemId))
                {
                    continue;
                }

                kanbanBoard.Progeny = await progenyHttpClient.GetProgeny(kanbanBoard.ProgenyId);
                SelectListItem item = new()
                {
                    Text = kanbanBoard.Title + " (" + kanbanBoard.Progeny.NickName + ")",
                    Value = kanbanBoard.KanbanBoardId.ToString()
                };
                model.KanbanBoardsList.Add(item);
            }

            return PartialView("_MoveKanbanItemToKanbanBoardPartial", model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> MoveKanbanItemToKanbanBoard([FromForm] KanbanItemViewModel model)
        {
            int kanbanBoardId = model.KanbanItem.KanbanBoardId;
            KanbanItem kanbanItem = await kanbanItemsHttpClient.GetKanbanItem(model.KanbanItem.KanbanItemId);
            if (kanbanItem == null || kanbanItem.KanbanItemId == 0)
            {
                return NotFound();
            }

            TodoItem existingTodoItem = await todoItemsHttpClient.GetTodoItem(kanbanItem.TodoItemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), existingTodoItem.ProgenyId);
            model.SetBaseProperties(baseModel);
            List<Progeny> progAdminList = await progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);
            if (progAdminList.Count == 0)
            {
                return BadRequest();
            }

            KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanBoardId);
            kanbanBoard.SetColumnsListFromColumns();
            KanbanBoardColumn column = kanbanBoard.ColumnsList.SingleOrDefault(k => k.ColumnIndex == 0);
            if (column != null)
            {
                kanbanItem.ColumnId = column.Id;

            }
            kanbanItem.KanbanBoardId = kanbanBoardId;
            kanbanItem.TodoItem = existingTodoItem;
            foreach (KanbanBoardColumn columnItem in kanbanBoard.ColumnsList)
            {
                if (columnItem.SetStatus != existingTodoItem.Status) continue;
                kanbanItem.ColumnId = columnItem.Id;
                break;
            }

            KanbanItem movedKanbanItem = await kanbanItemsHttpClient.UpdateKanbanItem(kanbanItem);
            
            return Json(movedKanbanItem);
        }


        public async Task<IActionResult> AddKanbanItem(int kanbanBoardId, int columnId, int rowIndex)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            KanbanItemViewModel model = new(baseModel);

            List<Progeny> progAdminList = await progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);
            if (progAdminList.Count == 0)
            {
                // Todo: Show that no children are available to add KanbanItem for.
                return RedirectToAction("Index", "Kanbans");
            }

            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserId != null)
            {
                model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
                model.SetProgenyList();
            }

            model.KanbanItem.TodoItem = new TodoItem();
            model.KanbanItem.KanbanBoardId = kanbanBoardId;
            model.KanbanItem.ColumnId = columnId;
            model.KanbanItem.RowIndex = rowIndex;
            model.KanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanBoardId);

            // If the column has a set status, set the initial TodoItem status to that.
            model.KanbanBoard.SetColumnsListFromColumns();
            KanbanBoardColumn column = model.KanbanBoard.ColumnsList.SingleOrDefault(k => k.Id == columnId);
            if (column != null)
            {
                model.KanbanItem.TodoItem.Status = column.SetStatus;
            }
            else
            {
                model.KanbanItem.TodoItem.Status = 0;
            }

            model.SetAccessLevelList();
            model.SetStatusList(model.KanbanItem.TodoItem.Status);
            return PartialView("_AddKanbanItemPartial", model);
        }

        [ValidateAntiForgeryToken]
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

            // If the there is a column with a set status matching the TodoItem status, set the ColumnId to that column.
            KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(model.KanbanItem.KanbanBoardId);
            kanbanBoard.SetColumnsListFromColumns();
            foreach (KanbanBoardColumn kanbanBoardColumn in kanbanBoard.ColumnsList)
            {
                if (model.KanbanItem.TodoItem.Status != kanbanBoardColumn.SetStatus) continue;
                model.KanbanItem.ColumnId = kanbanBoardColumn.Id;
                break;
            }

            KanbanItem addedKanbanItem = await kanbanItemsHttpClient.AddKanbanItem(model.KanbanItem);
            if (addedKanbanItem == null || addedKanbanItem.KanbanItemId == 0) return Json(kanbanItem);
            addedKanbanItem.TodoItem = todoItem;
            addedKanbanItem.TodoItem.Progeny = await progenyHttpClient.GetProgeny(addedKanbanItem.TodoItem.ProgenyId);
            if (addedKanbanItem.TodoItem.Progeny != null)
            {
                addedKanbanItem.TodoItem.Progeny.PictureLink = addedKanbanItem.TodoItem.Progeny.GetProfilePictureUrl();
            }
            UserInfo todoUserInfo = await userInfosHttpClient.GetUserInfoByUserId(addedKanbanItem.TodoItem.CreatedBy);
            addedKanbanItem.TodoItem.CreatedBy = todoUserInfo.FullName();
            
            return Json(addedKanbanItem);
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

            TodoItem todoItem = await todoItemsHttpClient.GetTodoItem(existingKanbanItem.TodoItemId);
            existingKanbanItem.TodoItem = todoItem;
            if (existingKanbanItem.ColumnId != kanbanItem.ColumnId)
            {
                // If the column has a set status and the TodoItem status is different, update the TodoItem status to that.
                KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanItem.KanbanBoardId);
                kanbanBoard.SetColumnsListFromColumns();
                KanbanBoardColumn column = kanbanBoard.ColumnsList.SingleOrDefault(k => k.Id == kanbanItem.ColumnId);
                if (column != null && column.SetStatus > -1)
                {
                    
                    if (todoItem != null && todoItem.Status != column.SetStatus)
                    {
                        todoItem.Status = column.SetStatus;
                        existingKanbanItem.TodoItem = await todoItemsHttpClient.UpdateTodoItem(todoItem);
                    }
                }
            }

            existingKanbanItem.ColumnId = kanbanItem.ColumnId;
            existingKanbanItem.RowIndex = kanbanItem.RowIndex;
            existingKanbanItem.ModifiedBy = model.CurrentUser.UserEmail;
            existingKanbanItem.ModifiedTime = System.DateTime.UtcNow;
            
            KanbanItem updatedKanbanItem = await kanbanItemsHttpClient.UpdateKanbanItem(existingKanbanItem);
            if (updatedKanbanItem == null || updatedKanbanItem.KanbanItemId == 0) return Json(new KanbanItem());

            updatedKanbanItem.TodoItem = await todoItemsHttpClient.GetTodoItem(updatedKanbanItem.TodoItemId);
            updatedKanbanItem.TodoItem.Progeny = await progenyHttpClient.GetProgeny(updatedKanbanItem.TodoItem.ProgenyId);
            if (updatedKanbanItem.TodoItem.Progeny != null)
            {
                updatedKanbanItem.TodoItem.Progeny.PictureLink = updatedKanbanItem.TodoItem.Progeny.GetProfilePictureUrl();
            }
            UserInfo todoUserInfo = await userInfosHttpClient.GetUserInfoByUserId(updatedKanbanItem.TodoItem.CreatedBy);
            updatedKanbanItem.TodoItem.CreatedBy = todoUserInfo.FullName();

            return Json(updatedKanbanItem);
        }

        public async Task<IActionResult> EditKanbanItem(int kanbanItemId)
        {
            KanbanItem kanbanItem = await kanbanItemsHttpClient.GetKanbanItem(kanbanItemId);
            if (kanbanItem == null || kanbanItem.KanbanItemId == 0)
            {
                return NotFound();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), kanbanItem.TodoItem.ProgenyId);
            KanbanItemViewModel model = new(baseModel)
            {
                KanbanItem = kanbanItem
            };
            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserId != null)
            {
                model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
                model.SetProgenyList();
                model.KanbanItem.TodoItem = await todoItemsHttpClient.GetTodoItem(model.KanbanItem.TodoItemId);
                model.KanbanItem.TodoItem.Progeny = model.CurrentProgeny;
            }

            model.SetAccessLevelList();
            model.SetStatusList(model.KanbanItem.TodoItem.Status);

            return PartialView("_EditKanbanItemPartial", model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> EditKanbanItem([FromForm] KanbanItemViewModel model)
        {
            KanbanItem kanbanItem = await kanbanItemsHttpClient.GetKanbanItem(model.KanbanItem.KanbanItemId);
            if (kanbanItem == null || kanbanItem.KanbanItemId == 0)
            {
                return NotFound();
            }

            Progeny progeny = await progenyHttpClient.GetProgeny(model.KanbanItem.TodoItem.ProgenyId);
            
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), progeny.Id);
            model.SetBaseProperties(baseModel);

            List<Progeny> progAdminList = await progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);
            if (progAdminList.Count == 0)
            {
                // Todo: Show that no children are available to add kanban for.
                return RedirectToAction("Index", "Kanbans");
            }

            // If the there is a column with a set status matching the TodoItem status, set the ColumnId to that column.
            KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(model.KanbanItem.KanbanBoardId);
            kanbanBoard.SetColumnsListFromColumns();
            foreach (KanbanBoardColumn kanbanBoardColumn in kanbanBoard.ColumnsList)
            {
                if (model.KanbanItem.TodoItem.Status != kanbanBoardColumn.SetStatus) continue;
                model.KanbanItem.ColumnId = kanbanBoardColumn.Id;
                break;
            }

            KanbanItem updatedKanbanItem = await kanbanItemsHttpClient.UpdateKanbanItem(model.KanbanItem);
            if (updatedKanbanItem == null || updatedKanbanItem.KanbanItemId == 0) return Json(kanbanItem);
            TodoItem editedTodoItem = model.CreateTodoItem();
            TodoItem updatedTodoItem = await todoItemsHttpClient.UpdateTodoItem(editedTodoItem);
           
            if (updatedTodoItem != null)
            {
                UserInfo todoUserInfo = await userInfosHttpClient.GetUserInfoByUserId(updatedTodoItem.CreatedBy);
                updatedTodoItem.CreatedBy = todoUserInfo.FullName();
                updatedTodoItem.Progeny = progeny;
                updatedTodoItem.Progeny.PictureLink = updatedTodoItem.Progeny.GetProfilePictureUrl();
            }

            updatedKanbanItem.TodoItem = updatedTodoItem;

            return Json(updatedKanbanItem);
        }

        public async Task<IActionResult> RemoveKanbanItem(int kanbanItemId)
        {
            KanbanItem kanbanItem = await kanbanItemsHttpClient.GetKanbanItem(kanbanItemId);
            if (kanbanItem == null || kanbanItem.KanbanItemId == 0)
            {
                return NotFound();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), kanbanItem.TodoItem.ProgenyId);
            KanbanItemViewModel model = new(baseModel)
            {
                KanbanItem = kanbanItem
            };
            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserId != null)
            {
                model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
                model.SetProgenyList();
                model.KanbanItem.TodoItem = await todoItemsHttpClient.GetTodoItem(model.KanbanItem.TodoItemId);
                model.KanbanItem.TodoItem.Progeny = model.CurrentProgeny;
                model.KanbanItem.TodoItem.Progeny.PictureLink = model.KanbanItem.TodoItem.Progeny.GetProfilePictureUrl();
            }

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index", "Kanbans");
            }

            model.SetAccessLevelList();
            model.SetStatusList(model.KanbanItem.TodoItem.Status);

            return PartialView("_RemoveKanbanItemPartial", model);

        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> RemoveKanbanItem([FromForm] KanbanItemViewModel model)
        {
            KanbanItem kanbanItem = await kanbanItemsHttpClient.GetKanbanItem(model.KanbanItem.KanbanItemId);
            if (kanbanItem == null || kanbanItem.KanbanItemId == 0)
            {
                return NotFound();
            }

            TodoItem existingTodoItem = await todoItemsHttpClient.GetTodoItem(kanbanItem.TodoItemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), existingTodoItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index", "Kanbans");
            }

            KanbanItem deletedKanbanItem = await kanbanItemsHttpClient.DeleteKanbanItem(model.KanbanItem);
            if (deletedKanbanItem == null || deletedKanbanItem.KanbanItemId == 0)
            {
                return Json(new KanbanItem());
            }
            // Todo: Also delete the TodoItem if the user wants to.

            return Json(deletedKanbanItem);
        }
    }
}
