using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace Ocam
{
    public class Add
    {
        [XmlAttribute]
        public string key { get; set; }

        [XmlAttribute]
        public string value { get; set; }
    }

    public class SiteConfiguration
    {
        public List<Add> Settings { get; set; }

        public string IndexName { get; set; }
        public string PageStart { get; set; }
        public string Extension { get; set; }
        public string Permalink { get; set; }
        public string CategoryDir { get; set; }
        public string CategoryTemplate { get; set; }
        public string TagDir { get; set; }
        public string TagTemplate { get; set; }
        public int ItemsPerPage { get; set; }

        /// <summary>
        /// When running locally from a file-system (and not a web server),
        /// index.html files will be referenced explicitly.
        /// </summary>
        public bool Local { get; set; }
        public bool Rebase { get; set; }
        public bool UsePygments { get; set; }

        public SiteConfiguration()
        {
            // Set defaults
            IndexName = "index.html";
            PageStart = "_PageStart.cshtml";
            Extension = ".html";
            CategoryDir = "category";
            CategoryTemplate = "Category.cshtml";
            TagDir = "tag";
            TagTemplate = "Tag.cshtml";
#if true
            Permalink = "{year}/{month}/{day}/{title}";
            Rebase = false;
#else
            Permalink = "{year}/{month}/{day}/{title}/";
            Rebase = true;
#endif
            ItemsPerPage = 20;
            Local = true;
            UsePygments = true;
        }

        public static SiteConfiguration Load(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(SiteConfiguration));
            using (var reader = new StreamReader(path))
                return (SiteConfiguration)serializer.Deserialize(reader);
        }
    }
}
