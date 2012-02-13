using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ocam
{
    public enum ArchiveType
    {
        Categories,
        Tags
    }

    public class ArchiveGenerator : IGenerator
    {
        ArchiveType _type;
        string _template;
        string _segment;
        string _cshtml;

        public ArchiveGenerator(ArchiveType type, string template, string segment)
        {
            _type = type;
            _template = template;
            _segment = segment;
        }

        public void Generate(ISiteContext context, RazorEngine.Templating.TemplateService pageTemplateService, PageModel model)
        {
            string path = Path.Combine(context.TemplateDir, _template);
            if (!File.Exists(path))
            {
                Console.WriteLine("Warning: Template '{0}' not found.", _template);
                return;
            }

            using (var reader = new StreamReader(path))
            {
                _cshtml = reader.ReadToEnd();
            }

            Dictionary<string, List<PageInfo>> list;
            if (_type == ArchiveType.Categories)
                list = context.Categories;
            else // if (_type == ArchiveType.Tags)
                list = context.Tags;

            if (!pageTemplateService.HasTemplate(path))
                pageTemplateService.Compile(_cshtml, typeof(PageModel), path);

            string dir = Path.Combine(context.DestinationDir, _segment);

            Directory.CreateDirectory(dir);

            foreach (var item in list)
            {
                GeneratePage(context, pageTemplateService, model, path, item.Key, item.Value);
            }
        }

        void GeneratePage(ISiteContext context, RazorEngine.Templating.TemplateService pageTemplateService, PageModel model, string path, string name, List<PageInfo> list)
        {
            var instance = pageTemplateService.GetTemplate(_cshtml, model, path);

            var executeContext = new RazorEngine.Templating.ExecuteContext();

            if (_type == ArchiveType.Categories)
                executeContext.ViewBag.Category = name;
            else // if (_type == ArchiveType.Tags)
                executeContext.ViewBag.Tag = name;

            string result = instance.Run(executeContext);

            string file = PageModel.GetArchivePath(context, _segment, name);
            string dest = Path.GetDirectoryName(file);
            Directory.CreateDirectory(dest);

            File.WriteAllText(file, result);
        }
    }
}
