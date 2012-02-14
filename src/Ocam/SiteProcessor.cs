using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Ocam
{
    class ParseState
    {
        public static int PageDepth { get; set; }
    }

    class SiteProcessor : IDisposable
    {
        SiteConfiguration _config;
        SiteContext _context;

        string _root;
        bool _disposed = false;
        Stack<string> _pageStartStack = new Stack<string>();
        Regex _pathSeparatorRegex = new Regex(Path.DirectorySeparatorChar.ToString() + Path.DirectorySeparatorChar.ToString() + "+");
        MarkdownSharp.Markdown _markdown = new MarkdownSharp.Markdown();
        RazorEngine.Templating.TemplateService _startTemplateService;
        PageModel _pageModel;
        List<IGenerator> _generators;

        public delegate PageTemplate<PageModel> FileProcessor(string src, string dst, int depth, string name, StartTemplate<StartModel> startTemplate, Action<string, string> writer);

        string _siteDirName = "Site";
        string _htmlDirName = "Html";
        string _templateDirName = "Templates";

        string _siteRoot;
        string _htmlRoot;

        public SiteProcessor()
        {
            _config = new SiteConfiguration()
            {
                IndexName = "index.html",
                PageStart = "_PageStart.cshtml",
                Extension = ".html",
                CategoryDir = "category",
                CategoryTemplate = "Category.cshtml",
                TagDir = "tag",
                TagTemplate = "Tag.cshtml",
#if true
                Permalink = "{year}/{month}/{day}/{title}",
                Rebase = false,
#else
                Permalink = "{year}/{month}/{day}/{title}/",
                Rebase = true,
#endif
                ItemsPerPage = 1,
                Local = true
            };

            _generators = new List<IGenerator>()
            {
                new ArchiveGenerator(ArchiveType.Categories),
                new ArchiveGenerator(ArchiveType.Tags)
            };
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
                    _context.PageTemplateService.Dispose();
                    _startTemplateService.Dispose();
                }

                // Unmanaged resources
                _disposed = true;
            }
        }

        public void Process(string root)
        {
            try
            {
                _root = root;

                _siteRoot = Path.Combine(_root, _siteDirName);
                _htmlRoot = Path.Combine(_root, _htmlDirName);

                var resolver = new TemplateResolver();
                var activator = new TemplateActivator(_config);

                var pageConfiguration = new RazorEngine.Configuration.TemplateServiceConfiguration()
                {
                    BaseTemplateType = typeof(PageTemplate<>),
                    Resolver = resolver,
                    Activator = activator
                };

                var startConfiguration = new RazorEngine.Configuration.TemplateServiceConfiguration()
                {
                    BaseTemplateType = typeof(StartTemplate<>),
                    Resolver = resolver,
                    Activator = activator
                };

                // _pageTemplateService = new RazorEngine.Templating.TemplateService(pageConfiguration);
                _startTemplateService = new RazorEngine.Templating.TemplateService(startConfiguration);

                _context = new SiteContext(pageConfiguration)
                {
                    Config = _config,
                    SourceDir = _siteRoot,
                    DestinationDir = _htmlRoot,
                    TemplateDir = Path.Combine(_root, _templateDirName)
                };

                ProcessPages();
                RunGenerators();
            }
            catch (PageProcessingException ex)
            {
                // NOTE: Line number information is inaccurate due to file changes.
                if (ex.InnerException is RazorEngine.Templating.TemplateCompilationException)
                {
                    var inner = (RazorEngine.Templating.TemplateCompilationException)ex.InnerException;
                    
                    Console.Error.WriteLine(ex.Message);

                    foreach (var err in inner.Errors)
                    {
                        string[] lines = ex.Text.Split('\n');
                        Console.Error.WriteLine("{0}({1}): {2}", ex.FileName, err.Line, err.ErrorText);

                        // FIXME: The underlying file that caused the error 
                        // is gone at this point.
                        // Console.Error.WriteLine("{0}", GetErrorLines(err));
                    }
                }
                else if (ex.InnerException is RazorEngine.Templating.TemplateParsingException)
                {
                    var inner = (RazorEngine.Templating.TemplateParsingException)ex.InnerException;

                    Console.Error.WriteLine(ex.Message);
                    Console.Error.WriteLine("{0}: {1}", ex.FileName, inner.Message);
                }
                else
                {
                    Console.Error.WriteLine("{0}: {1}", ex.FileName, ex.Message);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }

        #region Processor

        void ProcessPages()
        {
            _pageModel = new PageModel();

            _markdown.Highlighter = new PassthroughHighlighter();

            Console.WriteLine("Scanning");
            Walk(_root, _siteDirName, _root, _htmlDirName, false, 0);

            _pageModel = new PageModel(_context);
            _markdown.Highlighter = new PygmentsHighlighter();

            Console.WriteLine("Building");
            Walk(_root, _siteDirName, _root, _htmlDirName, true, 0);
        }

        void Walk(string srcDir, string srcPart, string dstDir, string dstPart, bool write, int depth)
        {
            string src = Path.Combine(srcDir, srcPart);
            string dst = Path.Combine(dstDir, dstPart);
            var dir = new DirectoryInfo(src);

            StartTemplate<StartModel> startTemplate = GetStartTemplate(src, depth);

            foreach (var file in dir.GetFiles())
            {
                string ext = Path.GetExtension(file.Name);

                if (file.Name.Equals(_config.PageStart, StringComparison.OrdinalIgnoreCase))
                {
                    // Page start is handled via the page start stack.
                }
                else if (ext == ".md" || ext == ".markdown")
                {
                    ProcessFile(src, dst, file.Name, write, depth, startTemplate, ProcessMarkdownFile);
                }
                else if (ext == ".cshtml")
                {
                    ProcessFile(src, dst, file.Name, write, depth, startTemplate, ProcessRazorFile);
                }
                else
                {
                    string dstpath = Path.Combine(dst, file.Name);
                    if (write)
                    {
                        Directory.CreateDirectory(dst);
                        File.Copy(file.FullName, dstpath, true);
                    }
                }
            }

            foreach (var subdir in dir.GetDirectories())
            {
                Walk(src, subdir.Name, dst, subdir.Name, write, depth + 1);
            }
        }

        void ProcessFile(string src, string dst, string file, bool write, int depth, StartTemplate<StartModel> startTemplate, FileProcessor process)
        {
#if DEBUG
//            Console.Write(".");
#endif
            string srcfile = Path.Combine(src, file);
            string dstfile = null;
            Action<string, string> writer = null;
            if (write)
            {
                PageInfo pageInfo = _context.PageMap[srcfile];

                dstfile = RewriteDestinationPath(pageInfo, src, ref dst, ref file, ref depth);

                Directory.CreateDirectory(dst);

                writer = (d, r) => {
                    // Write the output file if a destination is specified.
                    if (!String.IsNullOrWhiteSpace(d))
                    {
                        File.WriteAllText(d, r);
                    }
                };
            }

            PageTemplate<PageModel> pageTemplate = process(srcfile, dstfile, depth, srcfile, startTemplate, writer);

            if (!write)
            {
                var contentStart = new StartTemplate<StartModel>()
                {
                    ForceLayout = true
                };

                string content = null;
                PageTemplate<PageModel> excerptTemplate = process(srcfile, null, depth, srcfile + "*", contentStart, (d, r) =>
                {
                    content = r;
                });

                if (pageTemplate.Excerpt == null)
                {
                    pageTemplate.Excerpt = ExtractExcerpt(content);
                }

                // If this file is named like a post, get the info.
                PathInfo pathInfo = PathInfo.GetpathInfo(src, file);

                DateTime date = pageTemplate.Date.HasValue 
                    ? pageTemplate.Date.Value
                    : (pathInfo == null 
                        ? DateTime.MinValue
                        : pathInfo.Date);

                if (date == DateTime.MinValue)
                {
                    // Note: It's probably OK for pages not to have dates,
                    // since they won't often be listed by date.
                    // Console.WriteLine("Warning: No date specified for {0}.", srcfile);
                }

                PageInfo pageInfo;
                if (pathInfo != null)
                {
                    pageInfo = new PostInfo(pathInfo);
                }
                else
                {
                    pageInfo = new PageInfo();
                }

                pageInfo.Permalink = pageTemplate.Permalink;
                pageInfo.Rebase = pageTemplate.Rebase;
                pageInfo.Title = pageTemplate.Title;
                pageInfo.Content = content;
                pageInfo.Excerpt = pageTemplate.Excerpt;
                pageInfo.Categories = pageTemplate.Categories;   // TODO: Copy
                pageInfo.Tags = pageTemplate.Tags;               // TODO: Copy
                pageInfo.Date = date;

                RewriteDestinationPath(pageInfo, src, ref dst, ref file, ref depth);

                // Build a URL fragment for internal linking.
                pageInfo.Url = GetInternalUrl(dst, file);

                AddCategories(pageInfo, pageTemplate.Categories);
                AddTags(pageInfo, pageTemplate.Tags);

                _context.PageMap.Add(srcfile, pageInfo);
            }
        }

        void RunGenerators()
        {
            foreach (var generator in _generators)
            {
                generator.Generate(_context, _pageModel);
            }
        }

        #endregion Processor

        #region General

        void AddCategories(PageInfo page, string[] categories)
        {
            if (categories == null)
                return;

            foreach (var category in categories)
            {
                if (!_context.Categories.ContainsKey(category))
                {
                    _context.Categories.Add(category, new List<PageInfo>());
                }

                _context.Categories[category].Add(page);
            }
        }

        void AddTags(PageInfo page, string[] tags)
        {
            if (tags == null)
                return;

            foreach (var tag in tags)
            {
                if (!_context.Tags.ContainsKey(tag))
                {
                    _context.Tags.Add(tag, new List<PageInfo>());
                }

                _context.Tags[tag].Add(page);
            }
        }

        StartTemplate<StartModel> GetStartTemplate(string src, int depth)
        {
            string pageStart = Path.Combine(src, _config.PageStart);
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
                startTemplate = ProcessStartFile(_pageStartStack.Peek(), depth);
            }

            return startTemplate;
        }

        string ExtractExcerpt(string content)
        {
            // TODO: Parse HTML and take first 100 words or so in a block preserving way.
            return content;
        }

        string GetErrorLines(System.CodeDom.Compiler.CompilerError error)
        {
            if (File.Exists(error.FileName))
            {
                var lines = File.ReadAllLines(error.FileName);
                return lines[error.Line];
            }
            return String.Empty;
        }

        #endregion General

        #region Permalinks

        string RewriteDestinationPath(PageInfo pageInfo, string src, ref string dst, ref string file, ref int depth)
        {
            if (!ApplyPermalink(pageInfo, src, ref dst, ref file, ref depth))
            {
                ApplyDefaultRewrite(pageInfo, src, ref dst, ref file, ref depth);
            }

            return Path.Combine(dst, file);
        }

        void ApplyDefaultRewrite(PageInfo pageInfo, string src, ref string dst, ref string file, ref int depth)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            string index = Path.GetFileNameWithoutExtension(_config.IndexName);
            if (name == index || !pageInfo.Rebase)
            {
                file = name + _config.Extension;
            }
            else
            {
                // Create a new directory: dst/page[/index.html]
                dst = Path.Combine(dst, name);
                file = _config.IndexName;
                depth++;
            }
        }

        bool ApplyPermalink(PageInfo pageInfo, string src, ref string dst, ref string file, ref int depth)
        {
            if (!(pageInfo is PostInfo))
            {
                return false;
            }

            var post = pageInfo as PostInfo;

            if (String.IsNullOrWhiteSpace(pageInfo.Permalink) &&
                String.IsNullOrWhiteSpace(_config.Permalink))
            {
                return false;
            }

            string pattern = String.IsNullOrWhiteSpace(pageInfo.Permalink)
                ? _config.Permalink
                : pageInfo.Permalink;

            var permalink = RewritePermalink(post, pattern, src, dst, post.Year, post.Month, post.Day);

            depth = GetPermalinkSegments(permalink);

            // If the rewrite isn't terminated by a path separator,
            // we must truncate the path to its directory component
            // and build a new filename from the last segment.
            if (permalink.Last() != Path.DirectorySeparatorChar)
            {
                int index = permalink.LastIndexOf(Path.DirectorySeparatorChar);
                file = permalink.Substring(index + 1) + _config.Extension;
                permalink = permalink.Substring(0, index);
                //depth--;
            }
            else
            {
                // Remove final separator, just for uniformity.
                permalink = permalink.Substring(0, permalink.Length - 1);
                file = _config.IndexName;
            }

            // Apply the rewrite.
            dst = _htmlRoot + permalink;
            return true;
        }

        string RewritePermalink(PostInfo postInfo, string pattern, string src, string dst, int year, int month, int day)
        {
            // Normalize to the filesystem path separator.
            pattern = pattern.Replace('/', Path.DirectorySeparatorChar);

            string permalink = pattern
                .Replace("{year}", year == 0 ? String.Empty : year.ToString())
                .Replace("{month}", month == 0 ? String.Empty : month.ToString())
                .Replace("{day}", day == 0 ? String.Empty : day.ToString())
                .Replace("{category}", GetDefaultCategorySegment(postInfo))
                .Replace("{title}", postInfo.Slug);

            // Remove any empty path segments.
            permalink = _pathSeparatorRegex.Replace(permalink, Path.DirectorySeparatorChar.ToString());

            // Ensure it starts with a separator, so segment (depth) count is uniform.
            if (permalink.First() != Path.DirectorySeparatorChar)
            {
                permalink = Path.DirectorySeparatorChar.ToString() + permalink;
            }

            return permalink;
        }

        int GetPermalinkSegments(string permalink)
        {
            return permalink.Where(c => c == Path.DirectorySeparatorChar).Count() - 1;
        }

        string GetInternalUrl(string dst, string file)
        {
            // Build a URL fragment for internal linking.
            string dir = dst.Substring(_htmlRoot.Length);
            if (!file.Equals(_config.IndexName, StringComparison.OrdinalIgnoreCase) || _config.Local)
                dir = Path.Combine(dir, file);
            if (dir.First() == Path.DirectorySeparatorChar && dir.Length > 0)
                dir = dir.Substring(1);
            
            return dir.Replace(Path.DirectorySeparatorChar, '/');
        }

        string GetDefaultCategorySegment(PageInfo pageInfo)
        {
            // FIXME: Escape invalid path characters.
            return pageInfo.Categories == null ?
                String.Empty :
                pageInfo.Categories[0]; // Use the first category
        }

        #endregion Permalinks

        #region Templates

        void ReadMarkdown(StreamReader reader, out string front, out string markdown)
        {
            front = String.Empty;
            markdown = String.Empty;

            if (reader.EndOfStream)
            {
                return;
            }

            // Read front matter, if any.
            var sb = new StringBuilder();
            var line = reader.ReadLine();
            if (line.Length >= 3 && line.TrimEnd() == "---")
            {
                line = reader.ReadLine();
                while (!reader.EndOfStream)
                {
                    if (line.Length >= 3 && line.TrimEnd() == "---")
                    {
                        break;
                    }
                    sb.Append(line);
                    line = reader.ReadLine();
                }
                front = sb.ToString();
                line = String.Empty;
            }

            // The rest is Markdown.
            markdown = line + reader.ReadToEnd();
        }

        PageTemplate<PageModel> ProcessRazorFile(string src, string dst, int depth, string name, StartTemplate<StartModel> startTemplate, Action<string, string> writer)
        {
            string cshtml;
            using (var reader = new StreamReader(src))
            {
                cshtml = reader.ReadToEnd();
            }

            try
            {
                return ProcessRazorTemplate(cshtml, src, dst, depth, name, startTemplate, writer);
            }
            catch (Exception ex)
            {
                throw new PageProcessingException("Error processing Razor file.", src, cshtml, ex);
            }
        }

        PageTemplate<PageModel> ProcessMarkdownFile(string src, string dst, int depth, string name, StartTemplate<StartModel> startTemplate, Action<string, string> writer)
        {
            string front;
            string markdown;
            using (var reader = new StreamReader(src))
            {
                ReadMarkdown(reader, out front, out markdown);
            }

            string cshtml = "@{\r\n" + front + "}\r\n" + _markdown.Transform(markdown);

            try
            {
                return ProcessRazorTemplate(cshtml, src, dst, depth, name, startTemplate, writer);
            }
            catch (Exception ex)
            {
                throw new PageProcessingException("Error processing Markdown file.", src, cshtml, ex);
            }
        }

        PageTemplate<PageModel> ProcessRazorTemplate(string cshtml, string path, string dst, int depth, string name, StartTemplate<StartModel> startTemplate, Action<string, string> writer)
        {
            // Set global page depth. Lame but OK since we're single threaded.
            if (dst != null)
            {
                int depthTest = FileUtility.GetDepthFromPath(_htmlRoot, dst);
                System.Diagnostics.Debug.Assert(depth == depthTest);
            }
            ParseState.PageDepth = depth;

            // This model state changes from page to page.
            _pageModel.Source = FileUtility.GetRelativePath(_siteRoot, path);

            // Create an instance of the page template for this cshtml.
            if (!_context.PageTemplateService.HasTemplate(name))
                _context.PageTemplateService.Compile(cshtml, typeof(PageModel), name);
            var instance = _context.PageTemplateService.GetTemplate(cshtml, _pageModel, name);

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

        StartTemplate<StartModel> ProcessStartFile(string path, int depth)
        {
            string cshtml;
            using (var reader = new StreamReader(path))
            {
                cshtml = reader.ReadToEnd();
            }

            try
            {
                return ProcessStartFileTemplate(cshtml, path, depth);
            }
            catch (Exception ex)
            {
                throw new PageProcessingException("Error processing start file.", path, cshtml, ex);
            }
        }

        StartTemplate<StartModel> ProcessStartFileTemplate(string cshtml, string path, int depth)
        {
            ParseState.PageDepth = depth;

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

        #endregion Templates
    }
}
