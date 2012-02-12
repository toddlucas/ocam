using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RazorEngine.Templating;

namespace Ocam
{
    public class StartTemplate<T> : TemplateBase<T>
    {
        // This Layout value is not used to set the _Layout for this template.
        // Instead, it's used to set the default layout for the page templates.
        // If _Layout is renamed, we'll need to change this property name.
        public string Layout { get; set; }

        // Force the pages to use this layout.
        public bool ForceLayout { get; set; }
    }
}
