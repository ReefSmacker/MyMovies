using System;
using System.Text;
using MediaPortal.GUI.Library;

namespace MyMovies
{
    public class GUIListItemActor : GUIListItem
    {
        string _name;
        string _character;
        string _picture;
        string _biography;

        public string Actor
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Character
        {
            get { return _character; }
            set
            {
                _character = string.IsNullOrEmpty(value) ? "** Anonymous **" : value;

            }
        }

        public string Picture
        {
            get { return _picture; }
            set { _picture = value; }
        }

        public string Biography
        {
            get { return _biography; }
            set
            {
                _biography = string.IsNullOrEmpty(value) ? "** No information availble **" : value;
                _biography = value;
            }
        }
    }
}
