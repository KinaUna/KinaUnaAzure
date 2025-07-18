namespace KinaUna.OpenIddict.Extensions
{
    public static class AsyncEnumerableExtensions
    {
        public static Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
        {
            return source == null ? throw new ArgumentNullException(nameof(source)) : ExecuteAsync();

            async Task<List<T>> ExecuteAsync()
            {
                List<T> list = [];

                await foreach (T element in source)
                {
                    list.Add(element);
                }

                return list;
            }
        }
    }
}
