using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ocam
{
    public interface IGenerator
    {
        void Generate(ISiteContext context, RazorEngine.Templating.TemplateService pageTemplateService, PageModel model);
    }
}
