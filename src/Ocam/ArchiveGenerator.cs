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

        public void Generate(ISiteContext context, PageModel model, int itemsPerPage)
        {
            if (itemsPerPage <= 0)
            {
                throw new Exception("ItemsPerPage must be a positive number.");
            }

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

            if (!context.PageTemplateService.HasTemplate(path))
                context.PageTemplateService.Compile(_cshtml, typeof(PageModel), path);

            string dir = Path.Combine(context.DestinationDir, _segment);

            Directory.CreateDirectory(dir);

            foreach (var item in list)
            {
                GenerateContent(context, model, path, item.Key, item.Value, itemsPerPage);
            }
        }

        void GenerateContent(ISiteContext context, PageModel model, string path, string name, List<PageInfo> list, int itemsPerPage)
        {
            int pages = (list.Count + itemsPerPage - 1) / itemsPerPage;
            for (int page = 0; page < pages; page++)
            {
                GeneratePage(context, model, path, name, list, page, page * itemsPerPage, itemsPerPage);
            }
        }

        void GeneratePage(ISiteContext context, PageModel model, string path, string name, List<PageInfo> list, int page, int skip, int take)
        {
            string file = FileUtility.GetArchivePath(context, _segment, name, page);
            ParseState.PageDepth = FileUtility.GetDepthFromPath(context.DestinationDir, file);

            var instance = context.PageTemplateService.GetTemplate(_cshtml, model, path);

            var executeContext = new RazorEngine.Templating.ExecuteContext();

            if (_type == ArchiveType.Categories)
                executeContext.ViewBag.Category = name;
            else // if (_type == ArchiveType.Tags)
                executeContext.ViewBag.Tag = name;

            executeContext.ViewBag.Skip = skip;
            executeContext.ViewBag.Take = take;

            string result = instance.Run(executeContext);

            string dest = Path.GetDirectoryName(file);
            Directory.CreateDirectory(dest);

            File.WriteAllText(file, result);
        }
    }
}
