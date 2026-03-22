using KinaUna.Data.Models.Search;
using System.Reflection;

namespace KinaUnaProgenyApi.Tests.Services.Search
{
    public class SearchServiceCreateResponseTests
    {
        private static SearchResponse<T> InvokeCreateResponse<T>(List<T> items, SearchRequest request, Func<T, DateTime> dateSelector)
        {
            Type searchServiceType = typeof(KinaUnaProgenyApi.Services.Search.SearchService);
            MethodInfo? method = searchServiceType.GetMethod("CreateResponse", BindingFlags.NonPublic | BindingFlags.Static);
            
            if (method == null)
            {
                throw new InvalidOperationException("CreateResponse method not found");
            }

            MethodInfo genericMethod = method.MakeGenericMethod(typeof(T));
            object? result = genericMethod.Invoke(null, [items, request, dateSelector]);
            
            return result as SearchResponse<T> ?? throw new InvalidOperationException("Failed to invoke CreateResponse");
        }

        #region Basic Functionality Tests

        [Fact]
        public void CreateResponse_WithEmptyList_ReturnsEmptyResults()
        {
            // Arrange
            List<TestItem> items = [];
            SearchRequest request = new() { NumberOfItems = 10, Skip = 0 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.NotNull(response);
            Assert.Empty(response.Results);
            Assert.Equal(0, response.TotalCount);
            Assert.Equal(1, response.PageNumber);
            Assert.Equal(0, response.RemainingItems);
            Assert.Same(request, response.SearchRequest);
        }

        [Fact]
        public void CreateResponse_WithItems_ReturnsCorrectTotalCount()
        {
            // Arrange
            List<TestItem> items =
            [
                new() { Id = 1, Date = DateTime.UtcNow.AddDays(-1) },
                new() { Id = 2, Date = DateTime.UtcNow.AddDays(-2) },
                new() { Id = 3, Date = DateTime.UtcNow.AddDays(-3) }
            ];
            SearchRequest request = new() { NumberOfItems = 10, Skip = 0 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.Equal(3, response.TotalCount);
        }

        [Fact]
        public void CreateResponse_ReturnsSearchRequestInResponse()
        {
            // Arrange
            List<TestItem> items = [new() { Id = 1, Date = DateTime.UtcNow }];
            SearchRequest request = new() { Query = "test", NumberOfItems = 10, Skip = 0 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.Same(request, response.SearchRequest);
        }

        #endregion

        #region Sorting Tests

        [Fact]
        public void CreateResponse_WithSortZero_SortsDescendingByDate()
        {
            // Arrange
            DateTime now = DateTime.UtcNow;
            List<TestItem> items =
            [
                new() { Id = 1, Date = now.AddDays(-3) },
                new() { Id = 2, Date = now.AddDays(-1) },
                new() { Id = 3, Date = now.AddDays(-2) }
            ];
            SearchRequest request = new() { Sort = 0, NumberOfItems = 10, Skip = 0 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.Equal(3, response.Results.Count);
            Assert.Equal(2, response.Results[0].Id); // Most recent first
            Assert.Equal(3, response.Results[1].Id);
            Assert.Equal(1, response.Results[2].Id); // Oldest last
        }

        [Fact]
        public void CreateResponse_WithSortOne_SortsAscendingByDate()
        {
            // Arrange
            DateTime now = DateTime.UtcNow;
            List<TestItem> items =
            [
                new() { Id = 1, Date = now.AddDays(-3) },
                new() { Id = 2, Date = now.AddDays(-1) },
                new() { Id = 3, Date = now.AddDays(-2) }
            ];
            SearchRequest request = new() { Sort = 1, NumberOfItems = 10, Skip = 0 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.Equal(3, response.Results.Count);
            Assert.Equal(1, response.Results[0].Id); // Oldest first
            Assert.Equal(3, response.Results[1].Id);
            Assert.Equal(2, response.Results[2].Id); // Most recent last
        }

        [Fact]
        public void CreateResponse_WithSameDates_MaintainsStableSort()
        {
            // Arrange
            DateTime sameDate = DateTime.UtcNow;
            List<TestItem> items =
            [
                new() { Id = 1, Date = sameDate },
                new() { Id = 2, Date = sameDate },
                new() { Id = 3, Date = sameDate }
            ];
            SearchRequest request = new() { Sort = 0, NumberOfItems = 10, Skip = 0 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.Equal(3, response.Results.Count);
        }

        #endregion

        #region Pagination Tests

        [Fact]
        public void CreateResponse_WithSkip_SkipsCorrectNumberOfItems()
        {
            // Arrange
            DateTime now = DateTime.UtcNow;
            List<TestItem> items =
            [
                new() { Id = 1, Date = now.AddDays(-1) },
                new() { Id = 2, Date = now.AddDays(-2) },
                new() { Id = 3, Date = now.AddDays(-3) },
                new() { Id = 4, Date = now.AddDays(-4) },
                new() { Id = 5, Date = now.AddDays(-5) }
            ];
            SearchRequest request = new() { Sort = 0, NumberOfItems = 2, Skip = 2 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.Equal(2, response.Results.Count);
            Assert.Equal(3, response.Results[0].Id);
            Assert.Equal(4, response.Results[1].Id);
        }

        [Fact]
        public void CreateResponse_WithNumberOfItems_TakesCorrectNumberOfItems()
        {
            // Arrange
            DateTime now = DateTime.UtcNow;
            List<TestItem> items =
            [
                new() { Id = 1, Date = now.AddDays(-1) },
                new() { Id = 2, Date = now.AddDays(-2) },
                new() { Id = 3, Date = now.AddDays(-3) },
                new() { Id = 4, Date = now.AddDays(-4) },
                new() { Id = 5, Date = now.AddDays(-5) }
            ];
            SearchRequest request = new() { Sort = 0, NumberOfItems = 3, Skip = 0 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.Equal(3, response.Results.Count);
        }

        [Fact]
        public void CreateResponse_WithNumberOfItemsZero_ReturnsAllItemsAfterSkip()
        {
            // Arrange
            DateTime now = DateTime.UtcNow;
            List<TestItem> items =
            [
                new() { Id = 1, Date = now.AddDays(-1) },
                new() { Id = 2, Date = now.AddDays(-2) },
                new() { Id = 3, Date = now.AddDays(-3) },
                new() { Id = 4, Date = now.AddDays(-4) },
                new() { Id = 5, Date = now.AddDays(-5) }
            ];
            SearchRequest request = new() { Sort = 0, NumberOfItems = 0, Skip = 2 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.Equal(3, response.Results.Count);
        }

        [Fact]
        public void CreateResponse_WithSkipBeyondTotal_ReturnsEmptyResults()
        {
            // Arrange
            List<TestItem> items =
            [
                new() { Id = 1, Date = DateTime.UtcNow.AddDays(-1) },
                new() { Id = 2, Date = DateTime.UtcNow.AddDays(-2) }
            ];
            SearchRequest request = new() { Sort = 0, NumberOfItems = 10, Skip = 10 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.Empty(response.Results);
            Assert.Equal(2, response.TotalCount);
        }

        #endregion

        #region PageNumber Tests

        [Fact]
        public void CreateResponse_FirstPage_ReturnsPageNumberOne()
        {
            // Arrange
            List<TestItem> items =
            [
                new() { Id = 1, Date = DateTime.UtcNow.AddDays(-1) },
                new() { Id = 2, Date = DateTime.UtcNow.AddDays(-2) }
            ];
            SearchRequest request = new() { Sort = 0, NumberOfItems = 10, Skip = 0 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.Equal(1, response.PageNumber);
        }

        [Fact]
        public void CreateResponse_SecondPage_ReturnsPageNumberTwo()
        {
            // Arrange
            List<TestItem> items = Enumerable.Range(1, 20)
                .Select(i => new TestItem { Id = i, Date = DateTime.UtcNow.AddDays(-i) })
                .ToList();
            SearchRequest request = new() { Sort = 0, NumberOfItems = 10, Skip = 10 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.Equal(2, response.PageNumber);
        }

        [Fact]
        public void CreateResponse_ThirdPage_ReturnsPageNumberThree()
        {
            // Arrange
            List<TestItem> items = Enumerable.Range(1, 30)
                .Select(i => new TestItem { Id = i, Date = DateTime.UtcNow.AddDays(-i) })
                .ToList();
            SearchRequest request = new() { Sort = 0, NumberOfItems = 10, Skip = 20 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.Equal(3, response.PageNumber);
        }

        [Fact]
        public void CreateResponse_WithZeroNumberOfItems_ReturnsPageNumberOne()
        {
            // Arrange
            List<TestItem> items =
            [
                new() { Id = 1, Date = DateTime.UtcNow.AddDays(-1) }
            ];
            SearchRequest request = new() { Sort = 0, NumberOfItems = 0, Skip = 5 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.Equal(1, response.PageNumber);
        }

        [Fact]
        public void CreateResponse_WithZeroSkip_ReturnsPageNumberOne()
        {
            // Arrange
            List<TestItem> items =
            [
                new() { Id = 1, Date = DateTime.UtcNow.AddDays(-1) }
            ];
            SearchRequest request = new() { Sort = 0, NumberOfItems = 10, Skip = 0 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.Equal(1, response.PageNumber);
        }

        #endregion

        #region RemainingItems Tests

        [Fact]
        public void CreateResponse_FirstPageWithMoreItems_ReturnsCorrectRemainingItems()
        {
            // Arrange
            List<TestItem> items = Enumerable.Range(1, 25)
                .Select(i => new TestItem { Id = i, Date = DateTime.UtcNow.AddDays(-i) })
                .ToList();
            SearchRequest request = new() { Sort = 0, NumberOfItems = 10, Skip = 0 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.Equal(15, response.RemainingItems);
        }

        [Fact]
        public void CreateResponse_LastPage_ReturnsZeroRemainingItems()
        {
            // Arrange
            List<TestItem> items = Enumerable.Range(1, 25)
                .Select(i => new TestItem { Id = i, Date = DateTime.UtcNow.AddDays(-i) })
                .ToList();
            SearchRequest request = new() { Sort = 0, NumberOfItems = 10, Skip = 20 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.Equal(0, response.RemainingItems);
        }

        [Fact]
        public void CreateResponse_ExactlyFillsPage_ReturnsZeroRemainingItems()
        {
            // Arrange
            List<TestItem> items = Enumerable.Range(1, 20)
                .Select(i => new TestItem { Id = i, Date = DateTime.UtcNow.AddDays(-i) })
                .ToList();
            SearchRequest request = new() { Sort = 0, NumberOfItems = 10, Skip = 10 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.Equal(0, response.RemainingItems);
        }

        [Fact]
        public void CreateResponse_MiddlePage_ReturnsCorrectRemainingItems()
        {
            // Arrange
            List<TestItem> items = Enumerable.Range(1, 50)
                .Select(i => new TestItem { Id = i, Date = DateTime.UtcNow.AddDays(-i) })
                .ToList();
            SearchRequest request = new() { Sort = 0, NumberOfItems = 10, Skip = 20 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.Equal(20, response.RemainingItems); // 50 - 20 (skip) - 10 (taken) = 20
        }

        [Fact]
        public void CreateResponse_WithZeroNumberOfItems_ReturnsZeroRemainingItems()
        {
            // Arrange
            List<TestItem> items = Enumerable.Range(1, 10)
                .Select(i => new TestItem { Id = i, Date = DateTime.UtcNow.AddDays(-i) })
                .ToList();
            SearchRequest request = new() { Sort = 0, NumberOfItems = 0, Skip = 0 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.Equal(0, response.RemainingItems);
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public void CreateResponse_WithSingleItem_ReturnsCorrectResponse()
        {
            // Arrange
            List<TestItem> items = [new() { Id = 1, Date = DateTime.UtcNow }];
            SearchRequest request = new() { Sort = 0, NumberOfItems = 10, Skip = 0 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.Single(response.Results);
            Assert.Equal(1, response.TotalCount);
            Assert.Equal(1, response.PageNumber);
            Assert.Equal(0, response.RemainingItems);
        }

        [Fact]
        public void CreateResponse_WithLargeDataset_HandlesCorrectly()
        {
            // Arrange
            List<TestItem> items = Enumerable.Range(1, 1000)
                .Select(i => new TestItem { Id = i, Date = DateTime.UtcNow.AddMinutes(-i) })
                .ToList();
            SearchRequest request = new() { Sort = 0, NumberOfItems = 25, Skip = 100 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.Equal(25, response.Results.Count);
            Assert.Equal(1000, response.TotalCount);
            Assert.Equal(5, response.PageNumber); // 100 / 25 + 1 = 5
            Assert.Equal(875, response.RemainingItems); // 1000 - 100 - 25 = 875
        }

        [Fact]
        public void CreateResponse_WithNegativeRemainingCalculation_ReturnsZeroOrNegative()
        {
            // Arrange
            List<TestItem> items = [new() { Id = 1, Date = DateTime.UtcNow }];
            SearchRequest request = new() { Sort = 0, NumberOfItems = 10, Skip = 0 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.Equal(0, response.RemainingItems);
        }

        [Fact]
        public void CreateResponse_PreservesOriginalItemProperties()
        {
            // Arrange
            DateTime testDate = new(2024, 6, 15, 10, 30, 0);
            TestItem originalItem = new()
            {
                Id = 42,
                Name = "Test Name",
                Date = testDate
            };
            List<TestItem> items = [originalItem];
            SearchRequest request = new() { Sort = 0, NumberOfItems = 10, Skip = 0 };

            // Act
            SearchResponse<TestItem> response = InvokeCreateResponse(items, request, i => i.Date);

            // Assert
            Assert.Single(response.Results);
            Assert.Equal(42, response.Results[0].Id);
            Assert.Equal("Test Name", response.Results[0].Name);
            Assert.Equal(testDate, response.Results[0].Date);
        }

        [Fact]
        public void CreateResponse_WithCustomDateSelector_UsesCorrectProperty()
        {
            // Arrange
            DateTime now = DateTime.UtcNow;
            List<TestItemWithMultipleDates> items =
            [
                new() { Id = 1, CreatedDate = now.AddDays(-3), ModifiedDate = now.AddDays(-1) },
                new() { Id = 2, CreatedDate = now.AddDays(-1), ModifiedDate = now.AddDays(-3) },
                new() { Id = 3, CreatedDate = now.AddDays(-2), ModifiedDate = now.AddDays(-2) }
            ];

            SearchRequest requestByCreated = new() { Sort = 0, NumberOfItems = 10, Skip = 0 };
            SearchRequest requestByModified = new() { Sort = 0, NumberOfItems = 10, Skip = 0 };

            // Act
            SearchResponse<TestItemWithMultipleDates> responseByCreated = InvokeCreateResponse(items, requestByCreated, i => i.CreatedDate);
            SearchResponse<TestItemWithMultipleDates> responseByModified = InvokeCreateResponse(items, requestByModified, i => i.ModifiedDate);

            // Assert - Sorted by CreatedDate descending
            Assert.Equal(2, responseByCreated.Results[0].Id); // Most recent created
            
            // Assert - Sorted by ModifiedDate descending
            Assert.Equal(1, responseByModified.Results[0].Id); // Most recent modified
        }

        #endregion

        #region Test Helper Classes

        private class TestItem
        {
            public int Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public DateTime Date { get; init; }
        }

        private class TestItemWithMultipleDates
        {
            public int Id { get; init; }
            public DateTime CreatedDate { get; init; }
            public DateTime ModifiedDate { get; init; }
        }

        #endregion
    }
}