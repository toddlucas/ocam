using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ocam
{
    public class FileUtility
    {
        public static string GetRelativePath(string basePath, string fullPath)
        {
            var path = fullPath.Substring(basePath.Length);
            if (path.First() == Path.DirectorySeparatorChar)
                path = path.Substring(1);
            return path;
        }

        public static string GetArchivePath(ISiteContext context, string segment, string name, int page)
        {
            string pageNumber = page == 0 ? null : page.ToString();
            return GetArchivePath(context, segment, name, pageNumber);
        }

        public static string GetArchivePath(ISiteContext context, string segment, string name, string page = null)
        {
            string file;

            name = name
                .Replace(' ', '-')  // TODO: Replace unsafe file system and URL chars.
                .ToLower();         // TODO: Make this conditional

            string dest = Path.Combine(context.DestinationDir, segment);

            if (context.Config.Rebase)
            {
                string dir = Path.Combine(dest, name);
                if (!String.IsNullOrWhiteSpace(page))
                    dir = Path.Combine(dir, page);
                file = Path.Combine(dir, context.Config.IndexName);
            }
            else
            {
                if (!String.IsNullOrWhiteSpace(page))
                    name = name + page;
                file = Path.Combine(dest, name + context.Config.Extension);
            }

            return file;
        }

        public static int GetPathSegmentCount(string path)
        {
            int count = path.Where(c => c == Path.DirectorySeparatorChar).Count();
            if (path.First() == Path.DirectorySeparatorChar)
            {
                count--;
            }
            return count;
        }

        public static int GetDepthFromPath(string basePath, string fullPath)
        {
            return GetPathSegmentCount(GetRelativePath(basePath, fullPath));
        }

        public static string GetContentPath(int depth, string href)
        {
            if (depth < 0)
                throw new Exception("Depth must be non-negative.");

            if (href == null)
                return null;

            // Convert the URL to a relative path. References will then work
            // whether loaded from the file system or a web server.
            if (href.Length >= 2 && href.Substring(0, 2) == "~/")
            {
                string prefix = String.Join("../", new string[depth + 1]);
                href = prefix + href.Substring(2);
                if (href.Length == 0)
                    return ".";
                return href;
            }

            if (href.Length == 1 && href.Substring(0, 1) == "~")
            {
                string prefix = String.Join("../", new string[depth + 1]);
                href = prefix + href.Substring(1);
                if (href.Length == 0)
                    return ".";
                return href;
            }

            return href;
        }
    }
}
