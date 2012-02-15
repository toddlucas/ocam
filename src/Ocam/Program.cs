using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using NDesk.Options;

namespace Ocam
{
    public class Options
    {
        public bool Local { get; set; }
        public bool Verbose { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Options options = ProcessCommandLine(args);
            if (options == null)
            {
                ShowHelp();
                return;
            }

            var processor = new SiteProcessor();

            processor.Process(Directory.GetCurrentDirectory(), options);
        }

        static Options ProcessCommandLine(string[] args)
        {
            bool help = false;
            var options = new Options();

            var p = new OptionSet() {
                { "h|?|help",   v => help = true },
                { "l|local",    v => options.Local = v != null },
                { "v|verbose",  v => options.Verbose = v != null },
            };
            List<string> extra = p.Parse(args);

            if (help)
            {
                ShowHelp();
                return null;
            }

            return options;
        }

        static void ShowHelp()
        {
            Console.Error.WriteLine("Usage: DomainDaemon.Processor <options>");
            Console.Error.WriteLine();
            Console.Error.WriteLine("  --server     Start scheduling server");
        }
    }
}
