using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ocam
{
    public class PageModel
    {
        public Dictionary<string, List<PageInfo>> Categories { get; set; }
        public Dictionary<string, List<PageInfo>> Tags { get; set; }
        public Dictionary<string, PageInfo> PageMap { get; set; }
        public PageInfo[] Pages { get; set; }
        public PostInfo[] Posts { get; set; }

        public string Source { get; set; }

        public PageModel()
        {
            // Provide non-null defaults for first pass.
            Categories = new Dictionary<string, List<PageInfo>>();
            Tags = new Dictionary<string, List<PageInfo>>();
            PageMap = new Dictionary<string, PageInfo>();
            Pages = new PageInfo[0];
            Posts = new PostInfo[0];
        }

        public PageModel(string siteRoot, Dictionary<string, PageInfo> pageMap, Dictionary<string, List<PageInfo>> categories, Dictionary<string, List<PageInfo>> tags)
        {
            var map = new Dictionary<string, PageInfo>();
            var pages = new List<PageInfo>();
            var posts = new List<PostInfo>();

            // Transform the page map keys to relative paths and build out
            // separate page and post lists.
            foreach (var item in pageMap)
            {
                var path = item.Key;
                var page = item.Value;

                var file = GetRelativePath(siteRoot, path);

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

            Categories = categories;
            Tags = tags;
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

        public PageInfo GetPageInfo()
        {
            if (!PageMap.ContainsKey(Source))
                return null;
            return PageMap[Source];
        }
    }
}
