using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ocam
{
    public class PaginatorInfo
    {
        public PageInfo[] Pages { get; set; }

        public int Page { get; set; }
        public int Count { get; set; }

        public int Skip { get; set; }
        public int Take { get; set; }

        public string First { get; set; }
        public string Next { get; set; }
        public string Prev { get; set; }
        public string Format { get; set; }

        public PaginatorInfo(PageInfo[] pages, int page, int count, int skip, int take, string first, string format)
        {
            Pages = pages;
            Page = page;
            Count = count;
            First = first;
            Format = format;

            if (page > 1)
            {
                Prev = String.Format(format, page - 1);
            }
            else if (page > 0)
            {
                Prev = First;
            }

            if (page + 1 < count)
            {
                Next = String.Format(format, page + 1);
            }
        }
    }
}
