using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Ocam
{
    class PluginManager
    {
        SiteContext _context;
        Dictionary<string, System.Reflection.Assembly> _assemblyMap = new Dictionary<string, System.Reflection.Assembly>();

        public PluginManager(SiteContext context)
        {
            _context = context;
        }

        public void LoadPlugins()
        {
            if (!Directory.Exists(_context.CodeDir))
                return;

            var domain = AppDomain.CurrentDomain;
            string[] assemblies = domain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .Select(a => a.Location)
                .ToArray();

            var dir = new DirectoryInfo(_context.CodeDir);

            AppDomain.CurrentDomain.AssemblyResolve += (o, e) =>
                {
                    if (_assemblyMap.ContainsKey(e.Name))
                        return _assemblyMap[e.Name];
                    return null;
                };

            foreach (var file in dir.GetFiles("*.cs"))
            {
                string input = Path.Combine(_context.CodeDir, file.Name);
                string output = Path.GetFileNameWithoutExtension(file.Name) + ".dll";
                output = Path.Combine(_context.CodeDir, output);

                PluginCompiler.CompileLibrary(input, output, assemblies);

                var asm = System.Reflection.Assembly.LoadFrom(output);
                _assemblyMap.Add(asm.FullName, asm);

                // Load the new assembly into the current app domain so it's
                // visible to the templates.
                AppDomain.CurrentDomain.Load(asm.FullName);
            }
        }

        public void PreBuild(PageModel model)
        {
        }

        public void PostBuild(PageModel model)
        {
        }
    }
}
