using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Ocam
{
    class StartProcessor : IDisposable
    {
        bool _disposed = false;
        ISiteContext _context;
        Stack<string> _pageStartStack = new Stack<string>();
        RazorEngine.Templating.TemplateService _startTemplateService;

        public StartProcessor(ISiteContext context, RazorEngine.Configuration.TemplateServiceConfiguration config)
        {
            _context = context;
            _startTemplateService = new RazorEngine.Templating.TemplateService(config);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Managed resources
                    _startTemplateService.Dispose();
                }

                // Unmanaged resources
                _disposed = true;
            }
        }

        public StartTemplate<StartModel> GetStartTemplate(string src)
        {
            string pageStart = Path.Combine(src, _context.Config.PageStart);
            if (File.Exists(pageStart))
            {
                _pageStartStack.Push(pageStart);
            }
            else if (_pageStartStack.Count > 0)
            {
                // Copy it from the parent directory.
                _pageStartStack.Push(_pageStartStack.Peek());
            }
            else
            {
                _pageStartStack.Push(String.Empty);
            }

            StartTemplate<StartModel> startTemplate = null;
            if (_pageStartStack.Count > 0 && !String.IsNullOrWhiteSpace(_pageStartStack.Peek()))
            {
                startTemplate = ProcessStartFile(_pageStartStack.Peek());
            }

            return startTemplate;
        }

        StartTemplate<StartModel> ProcessStartFile(string path)
        {
            string cshtml;
            using (var reader = new StreamReader(path))
            {
                cshtml = reader.ReadToEnd();
            }

            try
            {
                return ProcessStartFileTemplate(cshtml, path);
            }
            catch (Exception ex)
            {
                throw new PageProcessingException("Error processing start file.", path, cshtml, ex);
            }
        }

        StartTemplate<StartModel> ProcessStartFileTemplate(string cshtml, string path)
        {
            // ParseState.PageDepth = depth; // Not used by start pages.
            if (!_startTemplateService.HasTemplate(path))
                _startTemplateService.Compile(cshtml, typeof(StartModel), path);
            var instance = _startTemplateService.GetTemplate(cshtml, new StartModel(), path);
            _startTemplateService.Run(instance);
            var startTemplate = instance as StartTemplate<StartModel>;
            if (startTemplate != null)
            {
                return startTemplate;
            }
            return null;
        }
    }
}
