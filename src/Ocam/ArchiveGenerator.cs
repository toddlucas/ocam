using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ocam
{
    public enum ArchiveType
    {
        Category,
        Tag
    }

    public class ArchiveGenerator : IGenerator
    {
        ArchiveType _type;
        TemplateProcessor<PageModel> _template;

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
            if (_type == ArchiveType.Category)
            {
                list = context.Categories;
                template = context.Config.CategoryTemplate;
                segment = context.Config.CategoryDir;
            }
            else // if (_type == ArchiveType.Tag)
            {
                list = context.Tags;
                template = context.Config.TagTemplate;
                segment = context.Config.TagDir;
            }

            string templatePath = Path.Combine(context.TemplatesDir, template);
            if (!File.Exists(templatePath))
            {
                Console.WriteLine("Warning: Template '{0}' not found.", template);
                return;
            }

            _template = new TemplateProcessor<PageModel>(context, templatePath);
            _template.Load();

            string dir = Path.Combine(context.DestinationDir, segment);

            Directory.CreateDirectory(dir);

            foreach (var item in list)
            {
                GenerateContent(context, model, templatePath, segment, item.Key, item.Value, itemsPerPage);
            }
        }

        void GenerateContent(ISiteContext context, PageModel model, string path, string segment, string name, List<PageInfo> list, int itemsPerPage)
        {
            int pages = PaginatorInfo.PageCount(itemsPerPage, list.Count);
            for (int page = 0; page < pages; page++)
            {
                GeneratePage(context, model, path, segment, name, list, page, page * itemsPerPage, itemsPerPage);
            }
        }

        void GeneratePage(ISiteContext context, PageModel model, string path, string segment, string name, List<PageInfo> list, int page, int skip, int take)
        {
            string format = FileUtility.GetArchiveUrl(context, segment, name, "{0}");
            string first = FileUtility.GetArchiveUrl(context, segment, name);
            string file = FileUtility.GetArchivePath(context, segment, name, false, page);

            // Provide a default list to the template. The template 
            // may present their own ordering using skip/take.
            PageInfo[] pages = list
                .OrderByDescending(p => p.Date)
                .Skip(skip)
                .Take(take)
                .ToArray();

            model.Paginator = new PaginatorInfo(pages, page, list.Count, skip, take, first, format);

            string result = _template.Build(model, file, true, (ctx) =>
                {
                    ctx.ViewBag.ArchiveName = name;
                    ctx.ViewBag.ArchiveType = _type.ToString();
                });
        }
    }
}
