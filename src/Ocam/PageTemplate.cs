using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RazorEngine.Templating;

namespace Ocam
{
    // Used by the template activator to pass configuration to the page template.
    public interface IConfigurable
    {
        void Configure(SiteConfiguration config);
    }

    public class PageTemplate<T> : TemplateBase<T>, IConfigurable
    {
        public string[] Tags { get; set; }
        public string[] Categories { get; set; }

        public bool Published { get; set; }
        public DateTime? Date { get; set; }

        public string Permalink { get; set; }
        public bool Rebase { get; set; }

        public string Layout // Make the Layout property the same as MVC.
        {
            get { return _Layout; }
            set { _Layout = value; }
        }

        /// <summary>
        /// Set the title in the page header. It will be reflected in
        /// PageInfo and available to the layouts and includes from ViewBag.
        /// </summary>
        public string Title
        {
            get { return ViewBag.Title; }
            set { ViewBag.Title = value; }
        }

        public string Excerpt { get; set; }

        public PageTemplate()
        {
            Published = true;
        }

        public virtual void Configure(SiteConfiguration config)
        {
            Rebase = config.Rebase; // Initialize to site default.
        }

        public string Href(string href)
        {
            // Convert the URL to a relative path. References will then work
            // whether loaded from the file system or a web server.
            if (href != null && href.Length >= 2 && href.Substring(0, 2) == "~/")
            {
                string prefix = String.Join("../", new string[ParseState.PageDepth + 1]);
                return prefix + href.Substring(2);
            }
            return href;
        }

        public string Link(string source)
        {
            if (String.IsNullOrWhiteSpace(source))
                throw new ArgumentException("source");

            var model = Model as PageModel;
            if (model == null)
                return source;

#if true
            string path = source.Replace('/', Path.DirectorySeparatorChar);
            if (path.First() == '~')
                path = path.Substring(1);
            if (path.First() == Path.DirectorySeparatorChar)
                path = path.Substring(1);
            if (!model.PageMap.ContainsKey(path))
                return source;
            var pageInfo = model.PageMap[path];
#else
            if (ParseState.PageMap == null)
                return source;

            string path = source.Replace('/', Path.DirectorySeparatorChar);
            if (path.First() == '~')
                path = path.Substring(1);
            if (path.First() == Path.DirectorySeparatorChar)
                path = path.Substring(1);
            path = ParseState.PageBase + Path.DirectorySeparatorChar + path;
            if (!ParseState.PageMap.ContainsKey(path))
                return source;
            var pageInfo = ParseState.PageMap[path];
#endif
            return Href("~/" + pageInfo.Url);
        }

        //public RazorEngine.Text.IEncodedString Raw(string text)
        //{
        //    return new RazorEngine.Text.RawString(text);
        //}
    }
}
