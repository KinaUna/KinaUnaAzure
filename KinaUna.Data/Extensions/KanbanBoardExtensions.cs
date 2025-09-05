using System.Collections.Generic;
using System.Text.Json;
using KinaUna.Data.Models;

namespace KinaUna.Data.Extensions
{
    public static class KanbanBoardExtensions
    {
        public static List<KanbanBoardColumn> GetColumnsListFromColumns(this KanbanBoard kanbanBoard)
        {
            if (!string.IsNullOrEmpty(kanbanBoard.Columns))
            {
                return JsonSerializer.Deserialize<List<KanbanBoardColumn>>(kanbanBoard.Columns, JsonSerializerOptions.Web);
            }

            return [];
        }

        public static void SetColumnsListFromColumns(this KanbanBoard kanbanBoard)
        {
            if (!string.IsNullOrEmpty(kanbanBoard.Columns))
            {
                kanbanBoard.ColumnsList = JsonSerializer.Deserialize<List<KanbanBoardColumn>>(kanbanBoard.Columns, JsonSerializerOptions.Web);
            }
        }

        public static string GetColumnsFromColumnsList(this KanbanBoard kanbanBoard)
        {
            return JsonSerializer.Serialize(kanbanBoard.ColumnsList, JsonSerializerOptions.Web);
        }

        public static void SetColumnsFromColumnsList(this KanbanBoard kanbanBoard)
        {
            kanbanBoard.Columns = JsonSerializer.Serialize(kanbanBoard.ColumnsList, JsonSerializerOptions.Web);
        }

        public static void EnsureColumnsAreValid(this KanbanBoard kanbanBoard)
        {
            if (kanbanBoard.ColumnsList == null || kanbanBoard.ColumnsList.Count == 0)
            {
                kanbanBoard.SetColumnsListFromColumns();
            }

            // If still null or empty, add default column
            if (kanbanBoard.ColumnsList == null || kanbanBoard.ColumnsList.Count == 0)
            {
                KanbanBoardColumn defaultColumn = new()
                {
                    Id = 1,
                    ColumnIndex = 0,
                    Title = "To Do",
                    WipLimit = 0
                };

                kanbanBoard.ColumnsList = [defaultColumn];
            }

            // Check that column Ids are unique
            kanbanBoard.EnsureCorrectColumnIds();

            // Check that column Indexes are unique
            kanbanBoard.EnsureCorrectColumnIndexes();
        }

        private static void EnsureCorrectColumnIndexes(this KanbanBoard kanbanBoard)
        {
            if (kanbanBoard.ColumnsList == null || kanbanBoard.ColumnsList.Count == 0)
            {
                kanbanBoard.SetColumnsListFromColumns();
            }
            if (kanbanBoard.ColumnsList == null || kanbanBoard.ColumnsList.Count == 0)
            {
                return;
            }
            // Sort by ColumnIndex and then reassign ColumnIndex values to be sequential starting from 0
            kanbanBoard.ColumnsList.Sort((x, y) => x.ColumnIndex.CompareTo(y.ColumnIndex));
            for (int i = 0; i < kanbanBoard.ColumnsList.Count; i++)
            {
                kanbanBoard.ColumnsList[i].ColumnIndex = i;
            }

            kanbanBoard.SetColumnsFromColumnsList();
        }

        private static void EnsureCorrectColumnIds(this KanbanBoard kanbanBoard)
        {
            if (kanbanBoard.ColumnsList == null || kanbanBoard.ColumnsList.Count == 0)
            {
                kanbanBoard.SetColumnsListFromColumns();
            }

            if (kanbanBoard.ColumnsList == null || kanbanBoard.ColumnsList.Count == 0)
            {
                return;
            }

            // Ensure that column Ids are unique and sequential starting from 1
            // If there are duplicates or invalid Ids (less than 1), reassign Ids
            HashSet<int> columnIds = [];
            int nextId = 1;
            foreach (KanbanBoardColumn column in kanbanBoard.ColumnsList)
            {
                if (column.Id < 1 || !columnIds.Add(column.Id))
                {
                    // Assign a new Id, note that this can lead to non-sequential Ids if there are gaps
                    while (!columnIds.Add(nextId))
                    {
                        nextId++;
                    }
                    column.Id = nextId;
                }
                nextId++;
            }

            kanbanBoard.SetColumnsFromColumnsList();
        }
    }
}
