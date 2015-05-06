using System.Xml.Linq;

namespace PlexScrobble.Utilities
{
    public static class XDocumentExtensions
    {
        public static string ValueOrEmpty(this XElement xelement)
        {
            return xelement != null ? xelement.Value : string.Empty;
        }

        public static XElement ElementOrEmpty(this XDocument xDocument, string name)
        {
            return xDocument.Element(name) ?? new XElement(name);
        }

        public static XElement ElementOrEmpty(this XDocument xDocument, XNamespace xname, string name)
        {
            string elementString = "{" + xname + "}" + name;
            return ElementOrEmpty(xDocument, elementString);
        }

        public static XElement ElementOrEmpty(this XElement xelement, string name)
        {
            return xelement.Element(name) ?? new XElement(name);
        }

        public static XElement ElementOrEmpty(this XElement xelement, XNamespace xname, string name)
        {
            string elementString = "{" + xname + "}" + name;
            return ElementOrEmpty(xelement, elementString);
        }

        public static XContainer ContainerOrEmpty(this XContainer xelement, string name)
        {
            return xelement.Element(name) ?? new XElement(name);
        }

        public static XAttribute AttributeOrEmpty(this XElement xelement, string name)
        {
            return xelement.Attribute(name) ?? new XAttribute(name, "");
        }
    }
}
