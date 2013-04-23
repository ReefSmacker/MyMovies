using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Services;
using System.Collections;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using MediaPortal.Player;
using MediaPortal.Dialogs;
using System.IO;

namespace MyMovies
{
    /// <summary>
    /// Service class to display DVD selection dialog
    /// </summary>
    public class MyMoviesDVDHandler : ISelectDVDHandler
    {
        #region ISelectDVDHandler Members

        private MyMoviesPlugin _myMovies = null;


        /// <summary>
        /// Access the main MyMovie plugin class
        /// </summary>
        private MyMoviesPlugin MoviesDetails
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


        public bool OnPlayDVD(string drive, int parentId)
        {
            Log.Info("MyMoviesDVDHandler: OnPlayDVD() playing DVD {0}", drive);
            return MoviesDetails.OnPlayDvd(drive);
        }

        public string ShowSelectDriveDialog(int parentId, bool DVDonly)
        {
            Log.Info("MyMoviesDVDHandler: ShowSelectDriveDialog()");
            return MoviesDetails.OnSelectDvd();
        }

        public string ShowSelectDVDDialog(int parentId)
        {
            Log.Info("MyMoviesDVDHandler: ShowSelectDVDDialog()");
            return MoviesDetails.OnSelectDvd();
        }

        public string GetFolderVideoFile(string path)
        {
            // IFind first movie file in folder
            string strExtension = System.IO.Path.GetExtension(path).ToLower();
            if (VirtualDirectory.IsImageFile(strExtension))
            {
                return path;
            }
            else
            {
                if (VirtualDirectories.Instance.Movies.IsRemote(path))
                {
                    return string.Empty;
                }
                if (!path.EndsWith(@"\"))
                {
                    path = path + @"\";
                }
                string[] strDirs = null;
                try
                {
                    strDirs = System.IO.Directory.GetDirectories(path, "video_ts");
                }
                catch (Exception)
                {
                }
                if (strDirs != null)
                {
                    if (strDirs.Length == 1)
                    {
                        Log.Debug("MyMoviesDVDHandler: DVD folder detected - {0}", strDirs[0]);
                        return String.Format(@"{0}\VIDEO_TS.IFO", strDirs[0]);
                    }
                }
                string[] strFiles = null;
                try
                {
                    strFiles = System.IO.Directory.GetFiles(path);
                }
                catch (Exception)
                {
                }
                if (strFiles != null)
                {
                    for (int i = 0; i < strFiles.Length; ++i)
                    {
                        string extensionension = System.IO.Path.GetExtension(strFiles[i]);
                        if (VirtualDirectory.IsImageFile(extensionension))
                        {
                            if (DaemonTools.IsEnabled)
                            {
                                return strFiles[i];
                            }
                            continue;
                        }
                        if (VirtualDirectory.IsValidExtension(strFiles[i], MediaPortal.Util.Utils.VideoExtensions, false))
                        {
                            // Skip hidden files
                            if ((System.IO.File.GetAttributes(strFiles[i]) & System.IO.FileAttributes.Hidden) == System.IO.FileAttributes.Hidden)
                            {
                                continue;
                            }
                            return strFiles[i];
                        }
                    }
                }
            }
            return string.Empty;
        }

        public bool IsDvdDirectory(string path)
        {
            return false;       // ToDo - Check what is required here.
        }

        public void SetIMDBThumbs(IList items, bool markWatchedFilesm)
        {
        }

        #endregion
    }
}
