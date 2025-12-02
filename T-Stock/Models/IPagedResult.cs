using System.Collections.Generic;
namespace T_Stock.Models
{
    public interface IPagedResult<T>
    {
        IReadOnlyList<T> Items { get; }
        int Page { get; }
        int PageSize { get; }
        long TotalItems { get; }
        int TotalPages { get; }
    }

    public class PagedResult<T> : IPagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    }
}
