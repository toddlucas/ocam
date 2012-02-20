using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ocam
{
    public class FileUtility
    {
        // File system reserved: < > : " / \ | ? *
        static string _fsReserved = "<>:\"/\\|?*";

        // ! # $ & ' ( ) * + , / : ; = ? @ [ ]
        static string _uriReserved = "!#$&'()*+,/:;=?@[]";

        public static string GetRelativePath(string basePath, string fullPath)
        {
            var path = fullPath.Substring(basePath.Length);
            if (path.Length > 0 && path.First() == Path.DirectorySeparatorChar)
                path = path.Substring(1);
            return path;
        }

        public static string GetArchivePath(ISiteContext context, string segment, string name, bool uri, int page)
        {
            string pageNumber = page == 0 ? null : page.ToString();
            return GetArchivePath(context, segment, name, uri, pageNumber);
        }

        public static string GetArchivePath(ISiteContext context, string segment, string name, bool uri, string page = null)
        {
            name = EncodePathSegment(name, uri)
                .ToLower();         // TODO: Make this conditional

            string dest = Path.Combine(context.DestinationDir, segment);

            string file;
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
                    name = name + "-" + page;
                file = Path.Combine(dest, name + context.Config.Extension);
            }

            return file;
        }

        public static string GetArchiveUrl(ISiteContext context, string segment, string name, string page = null)
        {
            string path = FileUtility.GetArchivePath(context, segment, name, true, page);
            path = FileUtility.GetRelativePath(context.DestinationDir, path);
            return path.Replace(Path.DirectorySeparatorChar, '/');
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

        /// <summary>
        /// Builds a relative URL, replacing ~/ with relative path
        /// segments (../) based on the depth of the referencing page.
        /// </summary>
        public static string GetContentUrl(int depth, string href)
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

        /// <summary>
        /// Converts a source path to a destination URL that's relative to the
        /// current page's depth.
        /// </summary>
        public static string GetDestinationUrl(Dictionary<string, PageInfo> pageMap, int depth, string source)
        {
            if (String.IsNullOrWhiteSpace(source))
                throw new ArgumentException("source");

            string path = source.Replace('/', Path.DirectorySeparatorChar);
            if (path.First() == '~')
                path = path.Substring(1);
            if (path.First() == Path.DirectorySeparatorChar)
                path = path.Substring(1);
            if (!pageMap.ContainsKey(path))
                return source;
            var pageInfo = pageMap[path];

            return GetContentUrl(depth, "~/" + pageInfo.Url);
        }

        /// <summary>
        /// Given the full path to a destination file, returns a relative
        /// URL. Does not include index files if config Local is false.
        /// </summary>
        public static string GetInternalUrl(ISiteContext context, string path)
        {
            string dst = Path.GetDirectoryName(path);
            string file = Path.GetFileName(path);

            // Build a URL fragment for internal linking.
            string dir = FileUtility.GetRelativePath(context.DestinationDir, dst);
            if (!file.Equals(context.Config.IndexName, StringComparison.OrdinalIgnoreCase) || context.Config.Local)
                dir = Path.Combine(dir, file);

            return dir.Replace(Path.DirectorySeparatorChar, '/');
        }

        public static string EncodePathSegment(string segment, bool uri)
        {
            var sb = new StringBuilder();
            foreach (var c in segment)
            {
                if (Char.IsWhiteSpace(c))
                {
                    sb.Append('-');
                }
                else if (_fsReserved.IndexOf(c) >= 0)
                {
                    sb.Append("_");
                }
                else if (_uriReserved.IndexOf(c) >= 0)
                {
                    if (uri)
                        sb.Append("%" + ((int)c).ToString("X2"));
                    else
                        sb.Append(c);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
