using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Ocam
{
    public class PathInfo
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public DateTime Date { get; set; }
        public string Title { get; set; }

        static Regex _pathRegex = new Regex(@"(?:(?<ymd>\\(\d+)\\(\d+)\\(\d+))|(?<ym>\\(\d+)\\(\d+))|(?<y>\\(\d+)))");
        static Regex _fileRegex = new Regex(@"(?:(?<ymd>^(\d+)-(\d+)-(\d+)-)|(?<ym>^(\d+)-(\d+)-)|(?<y>^(\d+)-))(.*)\.");

        public static PathInfo GetpathInfo(string path, string file)
        {
            //int fy = 0, fm = 0, fd = 0;
            //int py = 0, pm = 0, pd = 0;
            int y = 0, m = 0, d = 0;
            int fa, fb, fc;
            int pa, pb, pc;
            string title = null, dummy;

            if (ParseDate(_fileRegex, file, out fa, out fb, out fc, out title))
            {
                y = fa;
                m = fb;
                d = fc;
            }

            if (ParseDate(_pathRegex, path, out pa, out pb, out pc, out dummy))
            {
                // yy\mm\dd\title.ext
                if (pa != 0 && pb != 0 && pc != 0)
                {
                    if (fa != 0 || fb != 0 || fc != 0)
                        Console.WriteLine("Warning: Day present in both path and file name.");

                    y = pa;
                    m = pb;
                    d = pc;
                }

                // yy\mm\[dd-]title.ext
                if (pa != 0 && pb != 0)
                {
                    if (fb != 0 || fc != 0)
                        Console.WriteLine("Warning: Month present in both path and file name.");

                    y = pa;
                    m = pb;
                    d = fa;
                }

                // yy\[mm-[dd-]]title.ext
                if (pa != 0)
                {
                    if (fc != 0)
                        Console.WriteLine("Warning: Year present in both path and file name.");

                    y = pa;
                    m = fa;
                    d = fb;
                }
            }

            if (!String.IsNullOrWhiteSpace(title))
            {
                return new PathInfo()
                {
                    Year = y,
                    Month = m,
                    Day = d,
                    Date = new DateTime(y, m == 0 ? 1 : m, d == 0 ? 1 : d),
                    Title = title
                };
            }

            return null;
        }

        public static bool ParseDate(Regex re, string text, out int yy, out int mm, out int dd, out string title)
        {
            yy = 0;
            mm = 0;
            dd = 0;
            title = null;

            Match m = re.Match(text);
            if (m.Success)
            {
                if (m.Groups["ymd"].Captures.Count > 0)
                {
                    yy = int.Parse(m.Groups[1].Captures[0].Value);
                    mm = int.Parse(m.Groups[2].Captures[0].Value);
                    dd = int.Parse(m.Groups[3].Captures[0].Value);
                }
                else if (m.Groups["ym"].Captures.Count > 0)
                {
                    yy = int.Parse(m.Groups[4].Captures[0].Value);
                    mm = int.Parse(m.Groups[5].Captures[0].Value);
                }
                else if (m.Groups["y"].Captures.Count > 0)
                {
                    //var g = m.Groups[5];
                    //var c = g.Captures[0];
                    yy = int.Parse(m.Groups[6].Captures[0].Value);
                }

                if (m.Groups[7].Captures.Count > 0) // File only
                    title = m.Groups[7].Captures[0].Value;
                return true;
            }

            return false;
        }
    }
}
