using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Ocam
{
    public class PostInfo : PageInfo
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public DateTime PathDate { get; set; }
//        public int Depth { get; set; }
        public string Slug { get; set; }

        public PostInfo(PathInfo pathInfo)
        {
            Year = pathInfo.Year;
            Month = pathInfo.Month;
            Day = pathInfo.Day;
            PathDate = pathInfo.Date;
            Slug = pathInfo.Title;
        }
    }
}
