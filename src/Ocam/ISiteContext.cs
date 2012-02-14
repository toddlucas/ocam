using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ocam
{
    public interface ISiteContext
    {
        SiteConfiguration Config { get; }
        Dictionary<string, PageInfo> PageMap { get;  }
        Dictionary<string, List<PageInfo>> Categories { get; }
        Dictionary<string, List<PageInfo>> Tags { get; }
        string SourceDir { get; }
        string DestinationDir { get; }
        string TemplateDir { get; }
        RazorEngine.Templating.TemplateService PageTemplateService { get; }
    }
}
