using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ocam
{
    public class PageModel
    {
        public PageInfo[] Pages { get; set; }
        public PostInfo[] Posts { get; set; }
        public string[] Categories { get; set; }
        public string[] Tags { get; set; }
        public Dictionary<string, PageInfo> PageMap { get; set; }
        public Dictionary<string, List<PageInfo>> CategoryPages { get; set; }
        public Dictionary<string, List<PageInfo>> TagPages { get; set; }
        public Dictionary<string, string> CategoryPaths { get; set; }
        public Dictionary<string, string> TagPaths { get; set; }

        public string Source { get; set; }

        public PageModel()
        {
            // Provide non-null defaults for first pass.
            PageMap = new Dictionary<string, PageInfo>();
            Pages = new PageInfo[0];
            Posts = new PostInfo[0];
            Categories = new string[0];
            Tags = new string[0];
            CategoryPaths = new Dictionary<string, string>();
            TagPaths = new Dictionary<string, string>();
            CategoryPages = new Dictionary<string, List<PageInfo>>();
            TagPages = new Dictionary<string, List<PageInfo>>();
        }

        public PageModel(ISiteContext context)
        {
            var map = new Dictionary<string, PageInfo>();
            var pages = new List<PageInfo>();
            var posts = new List<PostInfo>();

            // Transform the page map keys to relative paths and build out
            // separate page and post lists.
            foreach (var item in context.PageMap)
            {
                var path = item.Key;
                var page = item.Value;

                var file = GetRelativePath(context.SourceDir, path);

                map.Add(file, page);
                if (page.Post == null)
                {
                    pages.Add(page);
                }
                else
                {
                    page.Post.Page = page; // A circular reference
                    posts.Add(page.Post);
                }
            }

            var categoryList = new List<string>();
            var categoryPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in context.Categories)
            {
                categoryList.Add(item.Key);

                string path = GetArchivePath(context, context.Config.CategoryDir, item.Key);
                path = GetRelativePath(context.DestinationDir, path);
                path = path.Replace(Path.DirectorySeparatorChar, '/');

                categoryPaths.Add(item.Key, path);
            }

            var tagList = new List<string>();
            var tagPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in context.Tags)
            {
                tagList.Add(item.Key);

                string path = GetArchivePath(context, context.Config.TagDir, item.Key);
                path = GetRelativePath(context.DestinationDir, path);
                path = path.Replace(Path.DirectorySeparatorChar, '/');

                tagPaths.Add(item.Key, path);
            }

            Categories = categoryList.ToArray();
            Tags = tagList.ToArray();
            CategoryPaths = categoryPaths;
            TagPaths = tagPaths;
            CategoryPages = context.Categories;
            TagPages = context.Tags;
            PageMap = map;
            Pages = pages.OrderByDescending(p => p.Date).ToArray();
            Posts = posts.OrderByDescending(p => p.Page.Date).ToArray();
        }

        public static string GetRelativePath(string basePath, string fullPath)
        {
            var path = fullPath.Substring(basePath.Length);
            if (path.First() == Path.DirectorySeparatorChar)
                path = path.Substring(1);
            return path;
        }

        // TODO: Move this to a file related class.
        public static string GetArchivePath(ISiteContext context, string segment, string name)
        {
            string file;

            name = name
                .Replace(' ', '-')  // TODO: Replace unsafe file system and URL chars.
                .ToLower();         // TODO: Make this conditional

            string dest = Path.Combine(context.DestinationDir, segment);

            if (context.Config.Rebase)
            {
                string dir = Path.Combine(dest, name);
                file = Path.Combine(dir, context.Config.IndexName);
            }
            else
            {
                file = Path.Combine(dest, name + context.Config.Extension);
            }

            return file;
        }

        public PageInfo GetPageInfo()
        {
            if (!PageMap.ContainsKey(Source))
                return null;
            return PageMap[Source];
        }
    }
}
