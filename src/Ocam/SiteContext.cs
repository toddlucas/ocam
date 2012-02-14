using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RazorEngine.Configuration;
using RazorEngine.Templating;

namespace Ocam
{
    public class SiteContext : ISiteContext
    {
        public SiteConfiguration Config { get; set; }

        public Dictionary<string, PageInfo> PageMap { get; private set; }
        public Dictionary<string, List<PageInfo>> Categories { get; private set; }
        public Dictionary<string, List<PageInfo>> Tags { get; private set; }

        public string SourceDir { get; set; }
        public string DestinationDir { get; set; }
        public string TemplateDir { get; set; }

        public TemplateService PageTemplateService { get; private set; }

        public SiteContext(TemplateServiceConfiguration pageConfiguration)
        {
            PageMap = new Dictionary<string, PageInfo>(StringComparer.OrdinalIgnoreCase);
            Categories = new Dictionary<string, List<PageInfo>>(StringComparer.OrdinalIgnoreCase);
            Tags = new Dictionary<string, List<PageInfo>>(StringComparer.OrdinalIgnoreCase);

            PageTemplateService = new RazorEngine.Templating.TemplateService(pageConfiguration);
        }
    }
}
