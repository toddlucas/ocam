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
        string _cshtml;

        public ArchiveGenerator(ArchiveType type)
        {
            _type = type;
        }

        public void Generate(ISiteContext context, PageModel model)
        {
            int itemsPerPage = context.Config.ItemsPerPage;
            if (itemsPerPage <= 0)
            {
                throw new Exception("ItemsPerPage must be a positive number.");
            }

            string template;
            string segment;
            Dictionary<string, List<PageInfo>> list;
            if (_type == ArchiveType.Categories)
            {
                list = context.Categories;
                template = context.Config.CategoryTemplate;
                segment = context.Config.CategoryDir;
            }
            else // if (_type == ArchiveType.Tags)
            {
                list = context.Tags;
                template = context.Config.TagTemplate;
                segment = context.Config.TagDir;
            }

            string templatePath = Path.Combine(context.TemplateDir, template);
            if (!File.Exists(templatePath))
            {
                Console.WriteLine("Warning: Template '{0}' not found.", template);
                return;
            }

            using (var reader = new StreamReader(templatePath))
            {
                _cshtml = reader.ReadToEnd();
            }

            if (!context.PageTemplateService.HasTemplate(templatePath))
                context.PageTemplateService.Compile(_cshtml, typeof(PageModel), templatePath);

            string dir = Path.Combine(context.DestinationDir, segment);

            Directory.CreateDirectory(dir);

            foreach (var item in list)
            {
                GenerateContent(context, model, templatePath, segment, item.Key, item.Value, itemsPerPage);
            }
        }

        void GenerateContent(ISiteContext context, PageModel model, string path, string segment, string name, List<PageInfo> list, int itemsPerPage)
        {
            int pages = (list.Count + itemsPerPage - 1) / itemsPerPage;
            for (int page = 0; page < pages; page++)
            {
                GeneratePage(context, model, path, segment, name, list, page, page * itemsPerPage, itemsPerPage);
            }
        }

        void GeneratePage(ISiteContext context, PageModel model, string path, string segment, string name, List<PageInfo> list, int page, int skip, int take)
        {
            string format = FileUtility.GetArchiveUrl(context, segment, name, "{0}");
            string first = FileUtility.GetArchiveUrl(context, segment, name);
            string file = FileUtility.GetArchivePath(context, segment, name, page);
            ParseState.PageDepth = FileUtility.GetDepthFromPath(context.DestinationDir, file);

            // REVIEW: Allow to be configured?
            PageInfo[] pages = list
                .OrderByDescending(p => p.Date)
                .Skip(skip)
                .Take(take)
                .ToArray();

            var instance = context.PageTemplateService.GetTemplate(_cshtml, model, path);

            var executeContext = new RazorEngine.Templating.ExecuteContext();

            if (_type == ArchiveType.Categories)
                executeContext.ViewBag.Category = name;
            else // if (_type == ArchiveType.Tags)
                executeContext.ViewBag.Tag = name;

            model.Paginator = new PaginatorInfo(pages, page, list.Count, skip, take, first, format);

            string result = instance.Run(executeContext);

            string dest = Path.GetDirectoryName(file);
            Directory.CreateDirectory(dest);

            File.WriteAllText(file, result);
        }
    }
}
