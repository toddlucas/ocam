using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Ocam
{
    class MarkdownProcessor : RazorProcessor
    {
        MarkdownSharp.Markdown _markdown = new MarkdownSharp.Markdown();

        public MarkdownProcessor(ISiteContext context)
            : base(context)
        {
        }

        public void StartScan()
        {
            _markdown.Highlighter = new PassthroughHighlighter();
        }

        public void StartBuild()
        {
            if (_context.Config.UsePygments)
                _markdown.Highlighter = new PygmentsHighlighter();
            else
                _markdown.Highlighter = new PassthroughHighlighter();
        }

        public override PageTemplate<PageModel> ProcessFile(string src, string dst, string name, PageModel model, StartTemplate<StartModel> startTemplate, Action<string, string> writer)
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
                return ProcessRazorTemplate(cshtml, src, dst, name, model, startTemplate, writer);
            }
            catch (Exception ex)
            {
                throw new PageProcessingException("Error processing Markdown file.", src, cshtml, ex);
            }
        }

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
    }
}
