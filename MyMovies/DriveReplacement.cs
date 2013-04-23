using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using MediaPortal.GUI.Library;
using System.Xml.XPath;

namespace MyMovies
{
    public class DriveReplacements
    {
        XmlDocument _doc = new XmlDocument();

        public DriveReplacements(string xml)
        {
            try
            {
                _doc.LoadXml(xml);
            }
            catch (Exception ex)
            {
                Log.Error("MyMovies::DriveReplacements - cannot parse '{0}' as xml", xml);
                Log.Error(ex);
            }
        }

        public string Translate(string originlPathName)
        {
            XmlNodeList replacementPathNodes = Select("//Path");

            foreach (XmlNode replacementPathNode in replacementPathNodes)
            {
                string searchPrefix = replacementPathNode.Attributes["name"].Value;
                if (!string.IsNullOrEmpty(searchPrefix))
                {
                    if (originlPathName.StartsWith(searchPrefix))
                    {
                        return originlPathName.Replace(searchPrefix, replacementPathNode.InnerText);
                    }
                }
            }
            return originlPathName;
        }

        public XmlNodeList Select(string xpath)
        {
            return _doc.DocumentElement.SelectNodes(xpath);
        }
    }
}
