using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Ocam
{
    class Program
    {
        static void Main(string[] args)
        {
            var processor = new SiteProcessor();

            processor.Process(Directory.GetCurrentDirectory());
        }
    }
}
