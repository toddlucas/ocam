using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ocam
{
    public class PageInfo
    {
        public bool Published { get; set; }
        public bool Rebase { get; set; }
        public DateTime Date { get; set; }
        public string Permalink { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Excerpt { get; set; }
        public string[] Categories { get; set; }
        public string[] Tags { get; set; }
        public string Url { get; set; }
    }
}
