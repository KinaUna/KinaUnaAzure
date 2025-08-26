using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services.TodosServices;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services.KanbanServices
{
    public class KanbanItemsService(ProgenyDbContext progenyDbContext, ITodosService todosService): IKanbanItemsService
    {
        
        public async Task<KanbanItem> GetKanbanItemById(int kanbanItemId)
        {
            KanbanItem kanbanItem = await progenyDbContext.KanbanItemsDb.AsNoTracking().SingleOrDefaultAsync(k => k.KanbanItemId == kanbanItemId);
            kanbanItem.TodoItem = await todosService.GetTodoItem(kanbanItem.TodoItemId);

            return kanbanItem;
        }

        public async Task<KanbanItem> AddKanbanItem(KanbanItem kanbanItem)
        {
            await progenyDbContext.KanbanItemsDb.AddAsync(kanbanItem);
            await progenyDbContext.SaveChangesAsync();
            return kanbanItem;
        }

        public async Task<KanbanItem> UpdateKanbanItem(KanbanItem kanbanItem)
        {
            KanbanItem existingKanbanItem = await progenyDbContext.KanbanItemsDb.SingleOrDefaultAsync(k => k.KanbanItemId == kanbanItem.KanbanItemId);
            if (existingKanbanItem == null)
            {
                return null;
            }

            existingKanbanItem.ColumnIndex = kanbanItem.ColumnIndex;
            existingKanbanItem.RowIndex = kanbanItem.RowIndex;
            existingKanbanItem.ModifiedBy = kanbanItem.ModifiedBy;
            existingKanbanItem.ModifiedTime = kanbanItem.ModifiedTime;
            
            progenyDbContext.Update(existingKanbanItem);
            await progenyDbContext.SaveChangesAsync();

            return existingKanbanItem;
        }

        public async Task<KanbanItem> DeleteKanbanItem(KanbanItem kanbanItem)
        {
            KanbanItem existingKanbanItem = await progenyDbContext.KanbanItemsDb.SingleOrDefaultAsync(k => k.KanbanItemId == kanbanItem.KanbanItemId);
            if (existingKanbanItem == null)
            {
                return null;
            }

            progenyDbContext.KanbanItemsDb.Remove(existingKanbanItem);
            await progenyDbContext.SaveChangesAsync();

            return existingKanbanItem;
        }
    }
}
