﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Ocam
{
    class SiteProcessor : IDisposable
    {
        SiteConfiguration _config;
        SiteContext _context;

        bool _disposed = false;
        Regex _pathSeparatorRegex = new Regex(Path.DirectorySeparatorChar.ToString() + Path.DirectorySeparatorChar.ToString() + "+");
        PageModel _pageModel;
        PluginManager _pluginManager;
        List<IGenerator> _generators;
        StartProcessor _startProcessor;
        RazorProcessor _razorProcessor;
        MarkdownProcessor _markdownProcessor;

        string _siteDirName = "Site";
        string _htmlDirName = "Html";
        string _codeDirName = "Code";
        string _layoutsDirName = "Layouts";
        string _includesDirName = "Includes";
        string _templatesDirName = "Templates";

        public SiteProcessor()
        {
            _generators = new List<IGenerator>()
            {
                new ArchiveGenerator(ArchiveType.Category),
                new ArchiveGenerator(ArchiveType.Tag)
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
                    _startProcessor.Dispose();
                }

                // Unmanaged resources
                _disposed = true;
            }
        }

        public void Process(string root, Options options)
        {
            try
            {
                var configPath = Path.Combine(root, "Site.config");
                if (File.Exists(configPath))
                {
                    _config = SiteConfiguration.Load(configPath);
                }
                else
                {
                    _config = new SiteConfiguration();
                }

                if (options.Local)
                    _config.Local = true;
                if (options.Verbose)
                    _config.Verbose = true;

                _context = new SiteContext()
                {
                    Config = _config,
                    Options = options,
                    ProjectDir = root,
                    SourceDir = Path.Combine(root, _siteDirName),
                    DestinationDir = Path.Combine(root, _htmlDirName),
                    CodeDir = Path.Combine(root, _codeDirName),
                    LayoutsDir = Path.Combine(root, _layoutsDirName),
                    IncludesDir = Path.Combine(root, _includesDirName),
                    TemplatesDir = Path.Combine(root, _templatesDirName),
                };

                var resolver = new TemplateResolver(_context);
                var activator = new TemplateActivator(_context);

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

                _pluginManager = new PluginManager(_context);
                _pluginManager.LoadPlugins();

                _startProcessor = new StartProcessor(_context, startConfiguration);
                _context.InitializeService(pageConfiguration);

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

                        // FIXME: The underlying generated file that caused 
                        // the error is gone at this point.
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
            _razorProcessor = new RazorProcessor(_context);
            _markdownProcessor = new MarkdownProcessor(_context);
            _markdownProcessor.StartScan();

            Console.WriteLine("Scanning");
            string root = _context.ProjectDir;
            Walk(root, _siteDirName, root, _htmlDirName, false, 0);

            _pageModel = new PageModel(_context);
            _markdownProcessor.StartBuild();

            _pluginManager.PreBuild(_pageModel);

            Console.WriteLine("Building");
            Walk(root, _siteDirName, root, _htmlDirName, true, 0);

            _pluginManager.PostBuild(_pageModel);
        }

        void Walk(string srcDir, string srcPart, string dstDir, string dstPart, bool write, int depth)
        {
            string src = Path.Combine(srcDir, srcPart);
            string dst = Path.Combine(dstDir, dstPart);
            var dir = new DirectoryInfo(src);

            StartTemplate<StartModel> startTemplate = _startProcessor.GetStartTemplate(src);

            foreach (var file in dir.GetFiles())
            {
                string ext = Path.GetExtension(file.Name);

                if (file.Name.Equals(_config.PageStart, StringComparison.OrdinalIgnoreCase))
                {
                    // Page start is handled via the page start stack.
                }
                else if (ext == ".md" || ext == ".markdown")
                {
                    ProcessFile(src, dst, file.Name, write, startTemplate, _markdownProcessor);
                }
                else if (ext == ".cshtml")
                {
                    ProcessFile(src, dst, file.Name, write, startTemplate, _razorProcessor);
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

        void ProcessFile(string src, string dst, string file, bool write, StartTemplate<StartModel> startTemplate, IPageProcessor processor)
        {
#if DEBUG
//            Console.Write(".");
#endif
            string srcfile = Path.Combine(src, file);
            string dstfile = null;
            Action<string, string> writer = null;
            if (write)
            {
                if (!_context.PageMap.ContainsKey(srcfile))
                {
                    // This file is unpublished.
                    return;
                }

                PageInfo pageInfo = _context.PageMap[srcfile];

                dstfile = pageInfo.GetDestinationPath(_context, src, dst, file);

                string dstdir = Path.GetDirectoryName(dstfile);
                Directory.CreateDirectory(dstdir);

                if (_context.Options.Verbose)
                    Console.WriteLine(dstfile);

                writer = (d, r) => {
                    // Write the output file if a destination is specified.
                    if (!String.IsNullOrWhiteSpace(d))
                    {
                        File.WriteAllText(d, r);
                    }
                };
            }

            PageTemplate<PageModel> pageTemplate = processor.ProcessFile(srcfile, dstfile, srcfile, _pageModel, startTemplate, writer);

            if (!write && pageTemplate.Published)
            {
                // Force the use of an empty layout to get the just the content.
                var contentStart = new StartTemplate<StartModel>()
                {
                    ForceLayout = true
                };

                string content = null;
                PageTemplate<PageModel> excerptTemplate = processor.ProcessFile(srcfile, null, srcfile + "*", _pageModel, contentStart, (d, r) =>
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

                dstfile = pageInfo.GetDestinationPath(_context, src, dst, file);

                // Build a URL fragment for internal linking.
                pageInfo.Url = FileUtility.GetInternalUrl(_context, dstfile);

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
    }
}
