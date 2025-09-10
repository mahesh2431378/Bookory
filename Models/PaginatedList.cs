using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookStoreMVC.Models
{
    /// <summary>
    /// A helper class to manage paginated data, allowing for efficient server-side paging.
    /// This class takes a query and fetches only one page of data at a time.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the list.</typeparam>
    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            this.AddRange(items);
        }

        // A property to check if there is a previous page.
        public bool HasPreviousPage => PageIndex > 1;

        // A property to check if there is a next page.
        public bool HasNextPage => PageIndex < TotalPages;

        /// <summary>
        /// Creates a paginated list from a data source by executing an efficient database query.
        /// </summary>
        /// <param name="source">The IQueryable data source (the query plan).</param>
        /// <param name="pageIndex">The current page number to retrieve.</param>
        /// <param name="pageSize">The number of items to include on each page.</param>
        /// <returns>A PaginatedList containing only the items for the requested page.</returns>
        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync();
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}