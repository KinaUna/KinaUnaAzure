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
using KinaUna.Data.Models.AccessManagement;

namespace KinaUnaWeb.Controllers
{
    public class KanbanItemsController(
        IKanbanItemsHttpClient kanbanItemsHttpClient,
        IKanbanBoardsHttpClient kanbanBoardsHttpClient,
        ITodoItemsHttpClient todoItemsHttpClient,
        IViewModelSetupService viewModelSetupService,
        IProgenyHttpClient progenyHttpClient,
        IFamiliesHttpClient familiesHttpClient,
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

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), todoItem.ProgenyId, todoItem.FamilyId, false);
            KanbanItemViewModel model = new(baseModel)
            {
                KanbanItem = kanbanItem
            };

            
            model.KanbanItem.TodoItem = todoItem;
            if (model.KanbanItem.TodoItem.ProgenyId > 0)
            {
                model.KanbanItem.TodoItem.Progeny = await progenyHttpClient.GetProgeny(model.KanbanItem.TodoItem.ProgenyId);
            }

            if (model.KanbanItem.TodoItem.FamilyId > 0)
            {
                model.KanbanItem.TodoItem.Family = await familiesHttpClient.GetFamily(model.KanbanItem.TodoItem.FamilyId);
            }
            
            UserInfo todoUserInfo = await userInfosHttpClient.GetUserInfoByUserId(model.KanbanItem.TodoItem.CreatedBy);
            model.KanbanItem.TodoItem.CreatedBy = todoUserInfo.FullName();
            
            model.SetStatusList(model.KanbanItem.TodoItem.Status);
            model.ProgenyList = await viewModelSetupService.GetProgenySelectList();
            model.SetProgenyList();
            model.FamilyList = await viewModelSetupService.GetFamilySelectList();
            model.SetFamilyList();
            
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
                    if (kanbanItem.TodoItem.ProgenyId > 0)
                    {
                        kanbanItem.TodoItem.Progeny = await progenyHttpClient.GetProgeny(kanbanItem.TodoItem.ProgenyId);
                        if (kanbanItem.TodoItem.Progeny != null)
                        {
                            kanbanItem.TodoItem.Progeny.PictureLink = kanbanItem.TodoItem.Progeny.GetProfilePictureUrl();
                        }
                    }

                    if (kanbanItem.TodoItem.FamilyId > 0)
                    {
                        kanbanItem.TodoItem.Family = await familiesHttpClient.GetFamily(kanbanItem.TodoItem.FamilyId);
                        if (kanbanItem.TodoItem.Family != null)
                        {
                            kanbanItem.TodoItem.Family.PictureLink = kanbanItem.TodoItem.Family.GetProfilePictureUrl();
                        }
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
            
            KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanItem.KanbanBoardId);
            if (kanbanBoard == null || kanbanBoard.KanbanBoardId == 0)
            {
                return NotFound();
            }

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
            if (addedKanbanItem.TodoItem.ProgenyId > 0)
            {
                addedKanbanItem.TodoItem.Progeny = await progenyHttpClient.GetProgeny(addedKanbanItem.TodoItem.ProgenyId);
                if (addedKanbanItem.TodoItem.Progeny != null)
                {
                    addedKanbanItem.TodoItem.Progeny.PictureLink = addedKanbanItem.TodoItem.Progeny.GetProfilePictureUrl();
                }
            }
            if (addedKanbanItem.TodoItem.FamilyId > 0)
            {
                addedKanbanItem.TodoItem.Family = await familiesHttpClient.GetFamily(addedKanbanItem.TodoItem.FamilyId);
                if (addedKanbanItem.TodoItem.Family != null)
                {
                    addedKanbanItem.TodoItem.Family.PictureLink = addedKanbanItem.TodoItem.Family.GetProfilePictureUrl();
                }
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

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), kanbanItem.TodoItem.ProgenyId, kanbanItem.TodoItem.FamilyId, false);
            KanbanItemViewModel model = new(baseModel)
            {
                KanbanItem = kanbanItem
            };
            
            model.KanbanItem.TodoItem = await todoItemsHttpClient.GetTodoItem(model.KanbanItem.TodoItemId);
            if(model.KanbanItem.TodoItem == null || model.KanbanItem.TodoItem.TodoItemId == 0)
            {
                return NotFound();
            }

            if (model.KanbanItem.TodoItem.ProgenyId > 0)
            {
                model.KanbanItem.TodoItem.Progeny = await progenyHttpClient.GetProgeny(model.KanbanItem.TodoItem.ProgenyId);
                if (model.KanbanItem.TodoItem.Progeny != null)
                {
                    model.KanbanItem.TodoItem.Progeny.PictureLink = model.KanbanItem.TodoItem.Progeny.GetProfilePictureUrl();
                }
            }
            if (model.KanbanItem.TodoItem.FamilyId > 0)
            {
                model.KanbanItem.TodoItem.Family = await familiesHttpClient.GetFamily(model.KanbanItem.TodoItem.FamilyId);
                if (model.KanbanItem.TodoItem.Family != null)
                {
                    model.KanbanItem.TodoItem.Family.PictureLink = model.KanbanItem.TodoItem.Family.GetProfilePictureUrl();
                }
            }

            model.SetStatusList(model.KanbanItem.TodoItem.Status);

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList();
            model.SetProgenyList();
            model.FamilyList = await viewModelSetupService.GetFamilySelectList();
            model.SetFamilyList();

            // Get all KanbanBoards for all progeny and families the user can add for.
            List<int> progenyIds = [];
            List<int> familyIds = [];
            progenyIds.AddRange(model.ProgenyList.ConvertAll(p => int.Parse(p.Value)));
            familyIds.AddRange(model.FamilyList.ConvertAll(f => int.Parse(f.Value)));
            KanbanBoardsRequest kanbanBoardsRequest = new()
            {
                ProgenyIds = progenyIds,
                FamilyIds = familyIds,
                IncludeDeleted = false,
                Skip = 0,
                NumberOfItems = 0 // Get all.
            };
            KanbanBoardsResponse kanbanBoardsResponse = await kanbanBoardsHttpClient.GetKanbanBoardsList(kanbanBoardsRequest);
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

                if (kanbanBoard.ProgenyId > 0)
                {
                    kanbanBoard.Progeny = await progenyHttpClient.GetProgeny(kanbanBoard.ProgenyId);
                    SelectListItem item = new()
                    {
                        Text = kanbanBoard.Title + " (" + kanbanBoard.Progeny.NickName + ")",
                        Value = kanbanBoard.KanbanBoardId.ToString()
                    };
                    model.KanbanBoardsList.Add(item);
                }

                if (kanbanBoard.FamilyId > 0)
                {
                    kanbanBoard.Family = await familiesHttpClient.GetFamily(kanbanBoard.FamilyId);
                    SelectListItem item = new()
                    {
                        Text = kanbanBoard.Title + " (" + kanbanBoard.Family.Name + ")",
                        Value = kanbanBoard.KanbanBoardId.ToString()
                    };
                    model.KanbanBoardsList.Add(item);
                }
            }

            return PartialView("_CopyKanbanItemToKanbanBoardPartial", model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> CopyKanbanItemToKanbanBoard([FromForm] KanbanItemViewModel model)
        {
            int kanbanBoardId = model.KanbanItem.KanbanBoardId;
            KanbanItem kanbanItem = await kanbanItemsHttpClient.GetKanbanItem(model.KanbanItem.KanbanItemId);
            KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanBoardId);
            if (kanbanItem == null || kanbanItem.KanbanItemId == 0 || kanbanBoard == null)
            {
                return NotFound();
            }

            TodoItem existingTodoItem = await todoItemsHttpClient.GetTodoItem(kanbanItem.TodoItemId);
            if (existingTodoItem == null || existingTodoItem.TodoItemId == 0)
            {
                return NotFound();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), existingTodoItem.ProgenyId, existingTodoItem.FamilyId, false);
            model.SetBaseProperties(baseModel);

            // Copy by reference, or create a new TodoItem based on the existing one.
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
                    FamilyId = existingTodoItem.FamilyId,
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
            
            kanbanBoard.SetColumnsListFromColumns();
            // If the there is a column with a set status matching the TodoItem status, set the ColumnId to that column.
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

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), kanbanItem.TodoItem.ProgenyId, kanbanItem.TodoItem.FamilyId, false);
            KanbanItemViewModel model = new(baseModel)
            {
                KanbanItem = kanbanItem
            };
            model.KanbanItem.TodoItem = await todoItemsHttpClient.GetTodoItem(model.KanbanItem.TodoItemId);
            if (model.KanbanItem.TodoItem == null || model.KanbanItem.TodoItem.TodoItemId == 0)
            {
                return NotFound();
            }

            model.SetStatusList(model.KanbanItem.TodoItem.Status);

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList();
            model.SetProgenyList();
            model.FamilyList = await viewModelSetupService.GetFamilySelectList();
            model.SetFamilyList();

            List<int> progenyIds = [];
            List<int> familyIds = [];
            progenyIds.AddRange(model.ProgenyList.ConvertAll(p => int.Parse(p.Value)));
            familyIds.AddRange(model.FamilyList.ConvertAll(f => int.Parse(f.Value)));
            KanbanBoardsRequest kanbanBoardsRequest = new()
            {
                ProgenyIds = progenyIds,
                FamilyIds = familyIds,
                IncludeDeleted = false,
                Skip = 0,
                NumberOfItems = 0 // Get all.
            };
            KanbanBoardsResponse kanbanBoardsResponse = await kanbanBoardsHttpClient.GetKanbanBoardsList(kanbanBoardsRequest);
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

                if (kanbanBoard.ProgenyId > 0)
                {
                    kanbanBoard.Progeny = await progenyHttpClient.GetProgeny(kanbanBoard.ProgenyId);
                    SelectListItem item = new()
                    {
                        Text = kanbanBoard.Title + " (" + kanbanBoard.Progeny.NickName + ")",
                        Value = kanbanBoard.KanbanBoardId.ToString()
                    };
                    model.KanbanBoardsList.Add(item);
                }

                if (kanbanBoard.FamilyId > 0)
                {
                    kanbanBoard.Family = await familiesHttpClient.GetFamily(kanbanBoard.FamilyId);
                    SelectListItem item = new()
                    {
                        Text = kanbanBoard.Title + " (" + kanbanBoard.Family.Name + ")",
                        Value = kanbanBoard.KanbanBoardId.ToString()
                    };
                    model.KanbanBoardsList.Add(item);
                }
            }

            return PartialView("_MoveKanbanItemToKanbanBoardPartial", model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> MoveKanbanItemToKanbanBoard([FromForm] KanbanItemViewModel model)
        {
            int kanbanBoardId = model.KanbanItem.KanbanBoardId;
            KanbanItem kanbanItem = await kanbanItemsHttpClient.GetKanbanItem(model.KanbanItem.KanbanItemId);
            KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanBoardId);
            if (kanbanItem == null || kanbanItem.KanbanItemId == 0 || kanbanBoard == null)
            {
                return NotFound();
            }

            TodoItem existingTodoItem = await todoItemsHttpClient.GetTodoItem(kanbanItem.TodoItemId);
            if (existingTodoItem == null || existingTodoItem.TodoItemId == 0)
            {
                return NotFound();
            }

            if (existingTodoItem.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return Unauthorized();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), existingTodoItem.ProgenyId, existingTodoItem.FamilyId, false);
            model.SetBaseProperties(baseModel);
            
            
            kanbanBoard.SetColumnsListFromColumns();

            // If the there is a column with a set status matching the TodoItem status, set the ColumnId to that column.
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, 0, false);
            KanbanItemViewModel model = new(baseModel);
            model.KanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanBoardId);
            if (model.KanbanBoard == null || model.KanbanBoard.KanbanBoardId == 0 || model.KanbanBoard.ItemPerMission.PermissionLevel < PermissionLevel.Add)
            {
                return NotFound();
            }

            model.KanbanItem.TodoItem = new TodoItem();
            model.KanbanItem.KanbanBoardId = kanbanBoardId;
            model.KanbanItem.ColumnId = columnId;
            model.KanbanItem.RowIndex = rowIndex;
            
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

            model.SetStatusList(model.KanbanItem.TodoItem.Status);

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList();
            model.SetProgenyList();
            model.FamilyList = await viewModelSetupService.GetFamilySelectList();
            model.SetFamilyList();

           
            return PartialView("_AddKanbanItemPartial", model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> AddKanbanItem([FromForm] KanbanItemViewModel model)
        {
            KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(model.KanbanItem.KanbanBoardId);
            if (kanbanBoard == null || kanbanBoard.KanbanBoardId == 0 || kanbanBoard.ItemPerMission.PermissionLevel < PermissionLevel.Add)
            {
                return NotFound();
            }

            KanbanItem kanbanItem = new();
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.KanbanBoard.ProgenyId, model.KanbanBoard.FamilyId, false);
            model.SetBaseProperties(baseModel);

            TodoItem todoItemToAdd = model.CreateTodoItem();
            TodoItem todoItem = await todoItemsHttpClient.AddTodoItem(todoItemToAdd);
            if (todoItem == null || todoItem.TodoItemId == 0) return Json(kanbanItem);

            model.KanbanItem.TodoItemId = todoItem.TodoItemId;
            model.KanbanItem.TodoItem = todoItem;

            // If the there is a column with a set status matching the TodoItem status, set the ColumnId to that column.
            
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

            if (addedKanbanItem.TodoItem.ProgenyId > 0)
            {
                addedKanbanItem.TodoItem.Progeny = await progenyHttpClient.GetProgeny(addedKanbanItem.TodoItem.ProgenyId);
                if (addedKanbanItem.TodoItem.Progeny != null)
                {
                    addedKanbanItem.TodoItem.Progeny.PictureLink = addedKanbanItem.TodoItem.Progeny.GetProfilePictureUrl();
                }
            }
            if (addedKanbanItem.TodoItem.FamilyId > 0)
            {
                addedKanbanItem.TodoItem.Family = await familiesHttpClient.GetFamily(addedKanbanItem.TodoItem.FamilyId);
                if (addedKanbanItem.TodoItem.Family != null)
                {
                    addedKanbanItem.TodoItem.Family.PictureLink = addedKanbanItem.TodoItem.Family.GetProfilePictureUrl();
                }
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

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), existingKanbanItem.TodoItem.ProgenyId, existingKanbanItem.TodoItem.FamilyId);
            KanbanItemViewModel model = new(baseModel);
            
            TodoItem todoItem = await todoItemsHttpClient.GetTodoItem(existingKanbanItem.TodoItemId);
            if (todoItem == null || todoItem.ItemPerMission.PermissionLevel < PermissionLevel.Edit) return Json(new KanbanItem());

            existingKanbanItem.TodoItem = todoItem;
            if (existingKanbanItem.ColumnId != kanbanItem.ColumnId)
            {
                // If the column has a set status and the TodoItem status is different, update the TodoItem status to that.
                KanbanBoard kanbanBoard = await kanbanBoardsHttpClient.GetKanbanBoard(kanbanItem.KanbanBoardId);
                kanbanBoard.SetColumnsListFromColumns();
                KanbanBoardColumn column = kanbanBoard.ColumnsList.SingleOrDefault(k => k.Id == kanbanItem.ColumnId);
                if (column != null && column.SetStatus > -1)
                {
                    
                    if (todoItem.Status != column.SetStatus)
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
            if (updatedKanbanItem.TodoItem.ProgenyId > 0)
            {
                updatedKanbanItem.TodoItem.Progeny = await progenyHttpClient.GetProgeny(updatedKanbanItem.TodoItem.ProgenyId);
                if (updatedKanbanItem.TodoItem.Progeny != null)
                {
                    updatedKanbanItem.TodoItem.Progeny.PictureLink = updatedKanbanItem.TodoItem.Progeny.GetProfilePictureUrl();
                }
            }
            if (updatedKanbanItem.TodoItem.FamilyId > 0)
            {
                updatedKanbanItem.TodoItem.Family = await familiesHttpClient.GetFamily(updatedKanbanItem.TodoItem.FamilyId);
                if (updatedKanbanItem.TodoItem.Family != null)
                {
                    updatedKanbanItem.TodoItem.Family.PictureLink = updatedKanbanItem.TodoItem.Family.GetProfilePictureUrl();
                }
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
            if (kanbanItem.TodoItem.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return Unauthorized();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), kanbanItem.TodoItem.ProgenyId, kanbanItem.TodoItem.FamilyId, false);
            KanbanItemViewModel model = new(baseModel)
            {
                KanbanItem = kanbanItem
            };

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(kanbanItem.TodoItem.ProgenyId);
            model.SetProgenyList();
            model.FamilyList = await viewModelSetupService.GetFamilySelectList(kanbanItem.TodoItem.FamilyId);
            model.SetFamilyList();

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
            if (kanbanItem.TodoItem.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return Unauthorized();
            }
            
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.KanbanItem.TodoItem.ProgenyId, model.KanbanItem.TodoItem.FamilyId, false);
            model.SetBaseProperties(baseModel);
            
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
                if (updatedTodoItem.ProgenyId > 0)
                {
                    updatedTodoItem.Progeny = await progenyHttpClient.GetProgeny(updatedTodoItem.ProgenyId);
                    updatedTodoItem.Progeny.PictureLink = updatedTodoItem.Progeny.GetProfilePictureUrl();
                }
                if (updatedTodoItem.FamilyId > 0)
                {
                    updatedTodoItem.Family = await familiesHttpClient.GetFamily(updatedTodoItem.FamilyId);
                    updatedTodoItem.Family.PictureLink = updatedTodoItem.Family.GetProfilePictureUrl();
                }
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

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), kanbanItem.TodoItem.ProgenyId, kanbanItem.TodoItem.FamilyId, false);
            KanbanItemViewModel model = new(baseModel)
            {
                KanbanItem = kanbanItem
            };

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList();
            model.SetProgenyList();
            model.FamilyList = await viewModelSetupService.GetFamilySelectList();
            model.SetFamilyList();
            model.KanbanItem.TodoItem = await todoItemsHttpClient.GetTodoItem(model.KanbanItem.TodoItemId);
            if (model.KanbanItem.TodoItem == null || model.KanbanItem.TodoItem.TodoItemId == 0)
            {
                return NotFound();
            }

            if (model.KanbanItem.TodoItem.ItemPerMission.PermissionLevel < PermissionLevel.Admin)
            {
                return Unauthorized();
            }

            if (model.KanbanItem.TodoItem.ProgenyId > 0)
            {
                model.KanbanItem.TodoItem.Progeny = await progenyHttpClient.GetProgeny(model.KanbanItem.TodoItem.ProgenyId);
                model.KanbanItem.TodoItem.Progeny.PictureLink = model.KanbanItem.TodoItem.Progeny.GetProfilePictureUrl();

            }
            if (model.KanbanItem.TodoItem.FamilyId > 0)
            {
                model.KanbanItem.TodoItem.Family = await familiesHttpClient.GetFamily(model.KanbanItem.TodoItem.FamilyId);
                model.KanbanItem.TodoItem.Family.PictureLink = model.KanbanItem.TodoItem.Family.GetProfilePictureUrl();
            }
            
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
            if (existingTodoItem == null || existingTodoItem.TodoItemId == 0)
            {
                return NotFound();
            }
            if (existingTodoItem.ItemPerMission.PermissionLevel < PermissionLevel.Admin)
            {
                return Unauthorized();
            }
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), existingTodoItem.ProgenyId, existingTodoItem.FamilyId, false);
            model.SetBaseProperties(baseModel);

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
