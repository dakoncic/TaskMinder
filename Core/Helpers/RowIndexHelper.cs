using Shared.Interfaces;

namespace Core.Helpers
{
    public static class RowIndexHelper
    {
        public static void ManaulReorderRowIndexes<T>(IEnumerable<T> items, int newIndex, int currentIndex) where T : IHasRowIndex
        {
            if (newIndex < currentIndex)
            {
                foreach (var item in items.Where(item => item.RowIndex >= newIndex && item.RowIndex < currentIndex))
                {
                    item.RowIndex++;
                }
            }
            else if (newIndex > currentIndex)
            {
                foreach (var item in items.Where(item => item.RowIndex > currentIndex && item.RowIndex <= newIndex))
                {
                    item.RowIndex--;
                }
            }
        }
    }
}
