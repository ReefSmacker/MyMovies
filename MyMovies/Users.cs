using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using MediaPortal.GUI.Library;

namespace MyMovies
{
    public class Users
    {
        List<string> _users = new List<string>();

        public Users(string xml)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                XmlNodeList usersNodes = doc.DocumentElement.SelectNodes("//User");
                foreach (XmlNode user in usersNodes)
                {
                    _users.Add(user.Attributes["name"].Value);
                }
            }
            catch (Exception ex)
            {
                Log.Error("MyMovies::Users - cannot parse '{0}' as xml", xml);
                Log.Error(ex);
            }
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<string> Collection
        {
            get
            {
                return _users.AsReadOnly();
            }
        }

    }
}
