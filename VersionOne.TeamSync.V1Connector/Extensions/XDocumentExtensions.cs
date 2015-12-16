using System.Xml.Linq;

namespace VersionOne.TeamSync.V1Connector.Extensions
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
            var total = root.Attribute("total");
            return total != null && int.Parse(total.Value) > 0;
        }

        public static string GetAssetID(this XElement xElement)
        {
            if (xElement == null)
                return "";

            return xElement.Attribute("id").Value.Split(':')[1];
        }


        public static string GetToken(this XElement xElement)
        {
            if (xElement == null)
                return "";

           return xElement.Attribute("id").Value;
        }

        public static string GetAssetHref(this XElement xElement)
        {
            if (xElement == null)
                return "";

            return xElement.Attribute("href").Value;
        }
    }

}
