using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace VersionOne.Api.Extensions
{
    public static class XDocumentExtensions
    {
        public static bool HasAssets(this XDocument doc)
        {
            if (doc.Root == null)
                return false;

            if (!doc.Root.HasAttributes)
                return false;

            var root = doc.Root;
            var total = int.Parse(root.Attribute("total").Value);
            return total > 0;
        }

        public static string GetAssetID(this XElement xElement)
        {
            if (xElement == null)
                return "";

            return xElement.Attribute("id").Value.Split(':')[1];
        }

        public static string GetAssetHref(this XElement xElement)
        {
            if (xElement == null)
                return "";

            return xElement.Attribute("href").Value;
        }
    }

}
