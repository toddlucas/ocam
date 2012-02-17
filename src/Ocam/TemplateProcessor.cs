using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Ocam
{
    class TemplateProcessor<T>
    {
        ISiteContext _context;
        string _path;
        string _cshtml;

        public TemplateProcessor(ISiteContext context, string path)
        {
            _context = context;
            _path = path;
        }

        public void Load()
        {
            using (var reader = new StreamReader(_path))
            {
                _cshtml = reader.ReadToEnd();
            }

            if (!_context.PageTemplateService.HasTemplate(_path))
                _context.PageTemplateService.Compile(_cshtml, typeof(T), _path);
        }

        public string Build(T model, string file, bool mkdir, Action<RazorEngine.Templating.ExecuteContext> prepare = null)
        {
            ParseState.PageDepth = FileUtility.GetDepthFromPath(_context.DestinationDir, file);
            string result = Build(model, prepare);
            if (mkdir)
            {
                string dest = Path.GetDirectoryName(file);
                Directory.CreateDirectory(dest);
            }
            File.WriteAllText(file, result);
            return result;
        }

        public string Build(T model, Action<RazorEngine.Templating.ExecuteContext> prepare = null)
        {
            var instance = _context.PageTemplateService.GetTemplate(_cshtml, model, _path);
            var executeContext = new RazorEngine.Templating.ExecuteContext();
            if (prepare != null)
                prepare(executeContext);
            return instance.Run(executeContext);
        }
    }
}
