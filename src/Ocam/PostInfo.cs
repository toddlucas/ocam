using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

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

        static Regex _pathSeparatorRegex = new Regex(Path.DirectorySeparatorChar.ToString() + Path.DirectorySeparatorChar.ToString() + "+");

        public PostInfo(PathInfo pathInfo)
        {
            Year = pathInfo.Year;
            Month = pathInfo.Month;
            Day = pathInfo.Day;
            PathDate = pathInfo.Date;
            Slug = pathInfo.Title;
        }

        public override string GetDestinationPath(ISiteContext context, string src, string dst, string file)
        {
            if (String.IsNullOrWhiteSpace(this.Permalink) &&
                String.IsNullOrWhiteSpace(context.Config.Permalink))
            {
                return base.GetDestinationPath(context, src, dst, file);
            }

            string pattern = String.IsNullOrWhiteSpace(this.Permalink)
                ? context.Config.Permalink
                : this.Permalink;

            var permalink = RewritePermalink(pattern, src, dst, this.Year, this.Month, this.Day);

            // If the rewrite isn't terminated by a path separator,
            // we must truncate the path to its directory component
            // and build a new filename from the last segment.
            if (permalink.Last() != Path.DirectorySeparatorChar)
            {
                int index = permalink.LastIndexOf(Path.DirectorySeparatorChar);
                file = permalink.Substring(index + 1) + context.Config.Extension;
                permalink = permalink.Substring(0, index);
            }
            else
            {
                // Remove final separator, just for uniformity.
                permalink = permalink.Substring(0, permalink.Length - 1);
                file = context.Config.IndexName;
            }

            // Apply the rewrite.
            dst = context.DestinationDir + permalink;

            return Path.Combine(dst, file);
        }

        string RewritePermalink(string pattern, string src, string dst, int year, int month, int day)
        {
            // Normalize to the filesystem path separator.
            pattern = pattern.Replace('/', Path.DirectorySeparatorChar);

            string permalink = pattern
                .Replace("{year}", year == 0 ? String.Empty : year.ToString())
                .Replace("{month}", month == 0 ? String.Empty : month.ToString())
                .Replace("{day}", day == 0 ? String.Empty : day.ToString())
                .Replace("{category}", GetDefaultCategorySegment())
                .Replace("{title}", this.Slug);

            // Remove any empty path segments.
            permalink = _pathSeparatorRegex.Replace(permalink, Path.DirectorySeparatorChar.ToString());

            // Ensure it starts with a separator, so segment (depth) count is uniform.
            if (permalink.First() != Path.DirectorySeparatorChar)
            {
                permalink = Path.DirectorySeparatorChar.ToString() + permalink;
            }

            return permalink;
        }

        string GetDefaultCategorySegment()
        {
            // FIXME: Escape invalid path characters.
            return this.Categories == null ?
                String.Empty :
                this.Categories[0]; // Use the first category
        }
    }
}
