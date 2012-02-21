using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Ocam
{
    interface IPageProcessor
    {
        PageTemplate<PageModel> ProcessFile(string src, string dst, string name, PageModel model, StartTemplate<StartModel> startTemplate, Action<string, string> writer);
    }

    class RazorProcessor : IPageProcessor
    {
        protected ISiteContext _context;

        public RazorProcessor(ISiteContext context)
        {
            _context = context;
        }

        public virtual PageTemplate<PageModel> ProcessFile(string src, string dst, string name, PageModel model, StartTemplate<StartModel> startTemplate, Action<string, string> writer)
        {
            string cshtml;
            using (var reader = new StreamReader(src))
            {
                cshtml = reader.ReadToEnd();
            }

            try
            {
                return ProcessRazorTemplate(cshtml, src, dst, name, model, startTemplate, writer);
            }
            catch (Exception ex)
            {
                throw new PageProcessingException("Error processing Razor file.", src, cshtml, ex);
            }
        }

        protected PageTemplate<PageModel> ProcessRazorTemplate(string cshtml, string path, string dst, string name, PageModel model, StartTemplate<StartModel> startTemplate, Action<string, string> writer)
        {
            // NOTE: On the first pass (scan), we don't have a destination.
            if (!String.IsNullOrWhiteSpace(dst))
                // Set global page depth. Lame but OK since we're single threaded.
                _context.PageDepth = FileUtility.GetDepthFromPath(_context.DestinationDir, dst);
            else
                _context.PageDepth = 0;

            // This model state changes from page to page.
            model.Source = FileUtility.GetRelativePath(_context.SourceDir, path);

            // The page may reference it's own info.
            if (_context.PageMap.ContainsKey(model.Source))
                model.PageInfo = _context.PageMap[model.Source];
            else
                // Provide a default on the scan pass to obviate null checks.
                model.PageInfo = new PageInfo();

            // Create an instance of the page template for this cshtml.
            if (!_context.PageTemplateService.HasTemplate(name))
                _context.PageTemplateService.Compile(cshtml, typeof(PageModel), name);
            var instance = _context.PageTemplateService.GetTemplate(cshtml, model, name);

            // Apply any _PageStart defaults.
            var pageTemplate = instance as PageTemplate<PageModel>;
            if (pageTemplate != null && startTemplate != null)
            {
                if (startTemplate.ForceLayout ||
                    (String.IsNullOrWhiteSpace(pageTemplate.Layout) &&
                    !String.IsNullOrWhiteSpace(startTemplate.Layout)))
                {
                    pageTemplate.Layout = startTemplate.Layout;
                }
            }

            string result = _context.PageTemplateService.Run(instance);

            if (writer != null)
                writer(dst, result);

            return pageTemplate;
        }
    }
}
