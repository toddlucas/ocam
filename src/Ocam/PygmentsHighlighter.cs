using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ocam
{
    class PygmentsHighlighter : MarkdownSharp.IMarkdownHighlighter
    {
        public bool Highlight(string text, string syntax, out string highlighted)
        {
            try
            {
                int exitCode;
                int wait = 5 * 1000;
                string stdOut;
                string stdErr;
                string args = "-f html";
                if (String.IsNullOrWhiteSpace(syntax))
                    args += " -g"; // guess
                else
                    args += " -l " + syntax; // lexer

                if (ConsoleProcess.Start("pygmentize", args, text, wait, out exitCode, out stdOut, out stdErr))
                {
                    // Escape the Razor prefix token.
                    highlighted = stdOut.Replace("@", "@@");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }

            highlighted = null;
            return false;
        }
    }

    class PassthroughHighlighter : MarkdownSharp.IMarkdownHighlighter
    {
        public bool Highlight(string text, string syntax, out string highlighted)
        {
            // Escape HTML entities.
            text = text.Replace("&", "&amp;");
            text = text.Replace("<", "&lt;");
            text = text.Replace(">", "&gt;");

            // Escape the Razor prefix token.
            text = text.Replace("@", "@@");

            highlighted = String.Concat("<div class=\"highlight\"><pre>", text, "\n</pre></div>");
            return true;
        }
    }
}
