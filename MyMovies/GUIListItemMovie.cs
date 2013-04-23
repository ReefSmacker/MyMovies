using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.GUI.Library;

namespace MyMovies
{
    public class GUIListItemMovie : GUIListItem
    {
        string _director;
        string _plot;
        string _frontCover;
        string _backCover;
        string _certification;
        string _certificationCause;
        string _aspect;
        string _mountedFileName;
        int _collectionNum;
        int _stars;
        bool _online;

        MyMoviesPlugin _myMovies = null;

        public string Aspect
        {
            get { return string.Concat(_aspect.Replace(':', '-'), ".png"); }
            set { _aspect = value; }
        }

        public string MountedFileName
        {
            get { return _mountedFileName; }
            set { _mountedFileName = value; }
        }

        public bool Online
        {
            get { return _online; }
            set { _online = value; }
        }

        public int CollectionNum
        {
            get { return _collectionNum; }
            set { _collectionNum = value; }
        }

        public int Stars
        {
            get { return _stars; }
            set { _stars = value; }
        }

        public string Certification
        {
            get { return _certification; }
            set { _certification = value; }
        }

        public string CertificationCause
        {
            get { return _certificationCause; }
            set { _certificationCause = value; }
        }

        // Extra properties for a movie.
        public string Director
        {
            get { return _director; }
            set { _director = value; }
        }
        public string Plot
        {
            get { return _plot; }
            set { _plot = value; }
        }

        public string FrontCover
        {
            get { return _frontCover; }
            set { _frontCover = value; }
        }
        public string BackCover
        {
            get { return _backCover; }
            set { _backCover = value; }
        }

        private const int _driveLength = 2;

        public string TranslatedPath
        {
            get
            {
                return Movies.DriveReplacements.Translate(this.Path);
            }
        }

        /// <summary>
        /// Callback access the main MyMovie plugin class
        /// </summary>
        private MyMoviesPlugin Movies
        {
            get
            {
                if (_myMovies == null)
                {
                    _myMovies = GUIWindowManager.GetWindow((int)MyMovies.GUIWindowID.Main) as MyMoviesPlugin;
                }
                return _myMovies;
            }
        }
    }
}
