using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.CodeDom.Compiler;
using System.Globalization;

namespace Ocam
{
    class PluginCompiler
    {
        public static bool CompileLibrary(string sourceName, string outputName, string[] referencedAssemblies = null)
        {
            return Compile(sourceName, outputName, false, referencedAssemblies);
        }

        public static bool Compile(string sourceName, string outputName, bool executable, string[] referencedAssemblies)
        {
            FileInfo sourceFile = new FileInfo(sourceName);
            CodeDomProvider provider = null;

            // Select the code provider based on the input file extension.
            if (sourceFile.Extension.ToUpper(CultureInfo.InvariantCulture) == ".CS")
            {
                provider = CodeDomProvider.CreateProvider("CSharp");
            }
            else if (sourceFile.Extension.ToUpper(CultureInfo.InvariantCulture) == ".VB")
            {
                provider = CodeDomProvider.CreateProvider("VisualBasic");
            }
            else
            {
                Console.WriteLine("Source file must have a .cs or .vb extension");
                return false;
            }

            // Format the executable file name.
            // Build the output assembly path using the current directory
            // and <source>_cs.dll or <source>_vb.dll.

#if false
            String outputPath = String.Format(@"{0}\{1}.{2}",
                System.Environment.CurrentDirectory,
                sourceFile.Name.Replace(".", "_"),
                extension);
#endif

            CompilerParameters cp = new CompilerParameters();

            // Class library
            cp.GenerateExecutable = executable;

            // Specify the assembly file name to generate.
            cp.OutputAssembly = outputName;

            // Save the assembly as a physical file.
            cp.GenerateInMemory = false;

            // Set whether to treat all warnings as errors.
            cp.TreatWarningsAsErrors = false;

            if (referencedAssemblies != null)
            {
                cp.ReferencedAssemblies.AddRange(referencedAssemblies);
            }

            // Invoke compilation of the source file.
            CompilerResults cr = provider.CompileAssemblyFromFile(cp, sourceName);

            bool compileOk = cr.Errors.Count == 0;
            if (!compileOk)
            {
                // Display compilation errors.
                Console.Error.WriteLine("Errors building {0} into {1}",
                    sourceName, cr.PathToAssembly);
                foreach (CompilerError ce in cr.Errors)
                {
                    Console.Error.WriteLine("  {0}", ce.ToString());
                    Console.Error.WriteLine();
                }
            }

            return compileOk;
        }
    }
}
