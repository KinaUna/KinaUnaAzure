using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Services.KanbanServices
{
    public class KanbanBoardsService(ProgenyDbContext progenyDbContext): IKanbanBoardsService
    {
        public async Task<KanbanBoard> GetKanbanBoardById(int kanbanBoardId)
        {
            KanbanBoard kanbanBoard = await progenyDbContext.KanbanBoardsDb.AsNoTracking().SingleOrDefaultAsync(k => k.KanbanBoardId == kanbanBoardId);
            
            return kanbanBoard;
        }

        public async Task<KanbanBoard> AddKanbanBoard(KanbanBoard kanbanBoard)
        {
            kanbanBoard.UId = System.Guid.NewGuid().ToString();
            await progenyDbContext.KanbanBoardsDb.AddAsync(kanbanBoard);
            await progenyDbContext.SaveChangesAsync();
            return kanbanBoard;
        }

        public async Task<KanbanBoard> UpdateKanbanBoard(KanbanBoard kanbanBoard)
        {
            KanbanBoard existingKanbanBoard = await progenyDbContext.KanbanBoardsDb.SingleOrDefaultAsync(k => k.KanbanBoardId == kanbanBoard.KanbanBoardId);
            if (existingKanbanBoard == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(existingKanbanBoard.UId))
            {
                existingKanbanBoard.UId = System.Guid.NewGuid().ToString();
            }

            existingKanbanBoard.Title = kanbanBoard.Title;
            existingKanbanBoard.Description = kanbanBoard.Description;
            existingKanbanBoard.Columns = kanbanBoard.Columns;
            existingKanbanBoard.ModifiedBy = kanbanBoard.ModifiedBy;
            existingKanbanBoard.ModifiedTime = kanbanBoard.ModifiedTime;
            existingKanbanBoard.AccessLevel = kanbanBoard.AccessLevel;
            
            progenyDbContext.KanbanBoardsDb.Update(existingKanbanBoard);
            await progenyDbContext.SaveChangesAsync();

            return existingKanbanBoard;
        }

        public async Task<KanbanBoard> DeleteKanbanBoard(KanbanBoard existingKanbanBoard)
        {
            KanbanBoard kanbanBoardToDelete = await progenyDbContext.KanbanBoardsDb.SingleOrDefaultAsync(k => k.KanbanBoardId == existingKanbanBoard.KanbanBoardId);
            if (kanbanBoardToDelete == null)
            {
                return null;
            }

            // Delete the associated KanbanItems too.
            List<KanbanItem> kanbanItemsToDelete = await progenyDbContext.KanbanItemsDb.AsNoTracking().Where(ki => ki.KanbanBoardId == kanbanBoardToDelete.KanbanBoardId).ToListAsync();
            if (kanbanItemsToDelete.Count != 0)
            {
                progenyDbContext.KanbanItemsDb.RemoveRange(kanbanItemsToDelete);
                await progenyDbContext.SaveChangesAsync();
            }

            progenyDbContext.KanbanBoardsDb.Remove(kanbanBoardToDelete);
            await progenyDbContext.SaveChangesAsync();

            return kanbanBoardToDelete;
        }
    }
}
