using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace OpenHeroSelectGUI.Settings
{
    /// <summary>
    /// Handling MarvelMods specific XML actions
    /// </summary>
    internal class MarvelModsXML
    {
        /// <summary>
        /// Get the root XML elemet by providing the path to an XML file. File must not have an XML identifier.
        /// </summary>
        /// <returns>The root XML element containing the complete XML structure</returns>
        public static XmlElement? GetXmlElement(string Path)
        {
            XmlElement? XmlElement = null;
            if (File.Exists(Path))
            {
                XmlDocument XmlDocument = new();
                using XmlReader reader = XmlReader.Create(Path, new XmlReaderSettings() { IgnoreComments = true });
                XmlDocument.Load(reader);
                if (XmlDocument.FirstChild is XmlElement Root) XmlElement = Root;
            }
            return XmlElement;
        }
    }
}
