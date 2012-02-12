using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RazorEngine.Templating;

namespace Ocam
{
    class TemplateResolver : ITemplateResolver
    {
        public string Resolve(string name)
        {
            // NOTE: We don't know whether this is being invoked by a call
            // to Include or a reference to _Layout. Thus, names must be
            // distinct to avoid a collision.
            string[] candidates = 
            {
                name,
                Path.Combine("Layouts", name),
                Path.Combine("Layouts", name + ".cshtml"),
                Path.Combine("Includes", name),
                Path.Combine("Includes", name + ".cshtml")
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                    return File.ReadAllText(path);
            }

            Console.WriteLine("Warning: Template '{0}' not found.", name);
            return null;
        }
    }
}
