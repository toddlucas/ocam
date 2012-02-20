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
        void Configure(ISiteContext context);
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

        int _pageDepth;

        public PageTemplate()
        {
            Published = true;
        }

        public virtual void Configure(ISiteContext context)
        {
            Rebase = context.Config.Rebase; // Initialize to site default.
            _pageDepth = context.PageDepth;
        }

        /// <summary>
        /// Link to an internal file based on its destination name.
        /// Prefix with a tilde to resolve the relative URL.
        /// </summary>
        /// <param name="href"></param>
        /// <returns></returns>
        public string Href(string href)
        {
            return FileUtility.GetContentUrl(_pageDepth, href);
        }

        /// <summary>
        /// Link to an internal file based on its source name relative
        /// to the site directory.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public string Link(string source)
        {
            var model = Model as PageModel;
            if (model == null)
                return source;

            return FileUtility.GetDestinationUrl(model.PageMap, _pageDepth, source);
        }
    }
}
