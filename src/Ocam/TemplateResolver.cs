using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RazorEngine.Templating;

namespace Ocam
{
    class TemplateResolver : ITemplateResolver
    {
        ISiteContext _context;

        public TemplateResolver(ISiteContext context)
        {
            _context = context;
        }

        public string Resolve(string name)
        {
            // NOTE: We don't know whether this is being invoked by a call
            // to Include or a reference to _Layout. Thus, names must be
            // distinct to avoid a collision.
            string[] candidates = 
            {
                name,
                Path.Combine(_context.LayoutsDir, name),
                Path.Combine(_context.LayoutsDir, name + ".cshtml"),
                Path.Combine(_context.IncludesDir, name),
                Path.Combine(_context.IncludesDir, name + ".cshtml")
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
