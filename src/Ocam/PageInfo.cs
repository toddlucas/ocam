using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

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

        public virtual string GetDestinationPath(ISiteContext context, string src, string dst, string file)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            string index = Path.GetFileNameWithoutExtension(context.Config.IndexName);
            if (name == index || !this.Rebase)
            {
                file = name + context.Config.Extension;
            }
            else
            {
                // Create a new directory: dst/page[/index.html]
                dst = Path.Combine(dst, name);
                file = context.Config.IndexName;
            }
            return Path.Combine(dst, file);
        }
    }
}
