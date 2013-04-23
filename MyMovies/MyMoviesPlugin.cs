using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

using MediaPortal.GUI.Library;
using MediaPortal.Util;
using MediaPortal.Player;
using MediaPortal.Dialogs;
using MediaPortal.GUI.View;
using System.Data;
using System.Data.SqlClient;
using MediaPortal.Configuration;
using System.Text;
using WakeOnLan;
using System.Net;
using System.IO;
using MediaPortal.Services;
using MediaPortal.Playlists;

namespace MyMovies
{
    public class MyMoviesPlugin : GUIWindow, ISetupForm
    {
        #region SkinControls

        [SkinControlAttribute(50)]
        protected GUIFacadeControl facadeView = null;
        [SkinControlAttribute(2)]
        protected GUIMenuButton btnLayout = null;
        [SkinControlAttribute(3)]
        protected GUISortButtonControl btnSortBy = null;
        [SkinControlAttribute(4)]
        protected GUIMenuButton btnSwitchViews = null;
        [SkinControlAttribute(5)]
        protected GUIMenuButton btnFilters = null;
        [SkinControlAttribute(6)]
        protected GUIMenuButton btnPlayDVD = null;
        [SkinControlAttribute(7)]
        protected GUICheckButton btnWatched = null;
        [SkinControlAttribute(8)]
        protected GUIMenuButton btnEnterPIN = null;
        [SkinControlAttribute(9)]
        protected GUIMenuButton btnActors = null;
        [SkinControlAttribute(10)]
        protected GUIMenuButton btnUsers = null;

        #endregion

        #region Enumerations

        /// <summary>
        /// Enumeration to define the titles that will be displayed.
        /// </summary>
        private enum Views
        {
            [StringEnum("All titles")]
            AllTitles,
        };

        private enum Filters
        { 
            [StringEnum("Genre")]
            Genre,
            [StringEnum("Years")]
            Years,
            [StringEnum("Certification")]
            Certification,
            [StringEnum("Flags")]
            Flags,
            [StringEnum("Clear all")]
            Clear,
        }

        /// <summary>
        /// Enumeration of the possible layout options.
        /// </summary>
        private enum LayoutOptions
        {
            [StringEnum("Layout: List")]
            List = 0,
            [StringEnum("Layout: Icons")]
            Icons,
            [StringEnum("Layout: Big Icons")]
            LargeIcons,
            [StringEnum("Layout: Filmstrip")]
            FilmStrip,
        }

        #endregion

        #region variables

        int                     _currentSelectionIndex = 0; // Default to the first selection.
        GUIListItem             _currentSelection   = null;
        GUIListItemMovie        _nowPlaying         = null;

        PlayListPlayer          _playlistPlayer     = new PlayListPlayer(); //PlayListPlayer.SingletonPlayer;
        MyMoviesDB              _myMoviesDB         = null;
        Views                   _currentView        = Views.AllTitles;
        Sorting.Options         _currentSorting     = MyMovies.Sorting.Options.Name;
        LayoutOptions           _currentLayout      = LayoutOptions.List;
        bool                    _sortAscending      = false;
        int                     _numLastAdded       = 50;
        int                     _numLastPlayed      = 50;
        int                     _selectedUser       = 0;
        Users                   _availableUsers;
        int                     _maxViewableRating  = 4;        // What the user will actually see.
        int                     _maxConfiguredRating;           // What has been configured as the "normal" setting
        int                     _maxRating;                     // The rating used when the PIN is successfully entered.
        DriveReplacements       _driveReplacements;

        RemoteServer            _remoteServer   = null;
        string                  _sqlConnection  = null;
        string                  _programDataPath;
        string                  _serverName;       
        string                  _dbInstance;       
        string                  _userName;         
        string                  _password;
        string                  _storedPIN;
        bool                    _chkRemoteWakeup;
        MacAddress              _macAddress;
        IPAddress               _ipAddress;
        int                     _wakeupRetries = 3;
        int                     _wakeupRetryTimeout = 3000;
        int                     _actor = 0;

        private Set _currentFlags = new Set();
        private Set _currentYears = new Set();
        private Set _currentGenres = new Set();
        private Set _currentCertifications = new Set();


        #endregion

        #region SQL Constants

        private const string _maxRatingSQL  = @"SELECT MAX(intCertification) FROM tblTitles";
        private const string _directorsSQL  = @"SELECT distinct p.nvcName, tp.intTitle FROM tblPersons p INNER JOIN tblTitlePerson tp ON p.intId = tp.intPerson AND tp.intType = 2";
        private const string _studiosSQL    = @"SELECT s.nvcName, ts.intTitle FROM tbltitlestudios ts INNER JOIN tblStudios s ON ts.intStudio = s.intId";

        #endregion

        #region constructors

        public MyMoviesPlugin()
        {
            // If MyMovies is being used, replace / use the MyMoviesDVDHandler
            ISelectDVDHandler myMoviesDVDHandler = new MyMoviesDVDHandler();
            if (GlobalServiceProvider.IsRegistered<ISelectDVDHandler>())
            {
                GlobalServiceProvider.Replace<ISelectDVDHandler>(myMoviesDVDHandler);
            }
            else
            {
                GlobalServiceProvider.Add<ISelectDVDHandler>(myMoviesDVDHandler);
            }

            //            GUIPropertyManager.OnPropertyChanged += new GUIPropertyManager.OnPropertyChangedHandler(GUIPropertyManager_OnPropertyChanged); 
        }

        //void GUIPropertyManager_OnPropertyChanged(string tag, string tagValue)
        //{
        //    // Hack work around.
        //    if (tag.Equals("#Play.Current.Plot"))
        //    {
        //        if (string.IsNullOrEmpty(tagValue))
        //        {
        //            GUIPropertyManager.SetProperty("#PlotOutline", NowPlaying.Plot);
        //        }
        //    }
        //}

        #endregion

        public int Actor
        {
            get { return _actor; }
            set 
            {
                _actor = value; 
            }
        }

        #region SQL Properties

        /// <summary>
        /// Base query for fetching movies.
        /// Only include tblResume if sorting by LastPlayed (avoids duplicates)
        /// </summary>
        private string SqlMovies
        {
            get 
            {
                SelectBuilder select = new SelectBuilder("tbltitles t");
                select.Fields.Add("nvcLocalTitle as title");
                select.Fields.Add("intCollectionNumber as collectionNum");
                select.Fields.Add("dt.nvcLocation as pathName");
                select.Fields.Add("t.intId	as id");
                select.Fields.Add("intRuntime as runTime");
                select.Fields.Add("intProductionYear as year");
                select.Fields.Add("bitWatched as watched");
                select.Fields.Add("t.intRating as stars");
                select.Fields.Add("ISNULL(intCertification, -1) as rating");
                select.Fields.Add("nvcCertificationCause as ratingCause");
                select.Fields.Add("intLocation as online");
                select.Fields.Add("nvcCover as frontCover");
                select.Fields.Add("nvcCoverBack  as backCover");
                select.Fields.Add("ntxDescription as plot");
                select.Fields.Add("nvcAspectRatio as aspect");
                select.Fields.Add("ISNULL(c.intId, 4) as country");

                select.Joins.Add("tblTitlePersonal tp ON t.intId = tp.intTitle");
                select.Joins.Add("tblDiscs dt ON t.intId = dt.intTitle");
                select.Joins.Add("( SELECT min(intId) as firstIntId, intTitle FROM tblDiscs GROUP BY intTitle ) firstDisk ON  t.intId = firstDisk.intTitle AND dt.intId = firstDisk.firstIntId");
                select.Joins.Add("tblCountries c ON t.intCountry = c.intid", SelectBuilderJoins.Types.LEFT);

                select.Where.Add(string.Concat("t.intCertification <= ", _maxViewableRating));
                select.Where.Add(SqlActor("t.intId"));
               
                if (_currentSorting == Sorting.Options.LastPlayed)
                {
                    select.Joins.Add("tblResume r ON t.intId = r.intId", SelectBuilderJoins.Types.LEFT);
                    select.Where.Add(string.Concat("r.intUserId = ", _selectedUser));
                }

                select.Order.Add(SqlSorting);
                select.Order.Ascending = _sortAscending;

                CurrentFilter = SqlFilters(select.Where);

                return select.ToString();
            }
        }


        /// <summary>
        /// Provide the available certifications based upon the current view.
        /// </summary>
        private string SqlRating
        {
            get
            {
                SelectBuilder select = new SelectBuilder("tblTitles");
                select.Distinct = true;
                select.Fields.Add("intCertification");
                select.Where.Add(SqlActor("intId"));
                select.Order.Add("intCertification");
                return select.ToString();
            }
        }

        /// <summary>
        /// Provide the available genres based upon the current view.
        /// </summary>
        private string SqlGenres
        {
            get
            {
                SelectBuilder select = new SelectBuilder("tblGenres G");
                select.Distinct = true;
                select.Fields.Add("G.intId");
                select.Fields.Add("G.nvcName");
                select.Joins.Add("tblTitleGenre t ON G.intId = t.intGenre");
                select.Where.Add(SqlActor("t.intTitle"));
                return select.ToString();
            }
        }

        /// <summary>
        /// Provide the available years based upon the current view.
        /// </summary>
        private string SqlYears
        {
            get
            {
                SelectBuilder select = new SelectBuilder("tblTitles");
                select.Distinct = true;
                select.Fields.Add("intProductionYear");
                select.Where.Add(SqlActor("intId"));
                select.Order.Add("intProductionYear");
                select.Order.Ascending = false;
                return select.ToString();
            }
        }

        /// <summary>
        /// Provide the 'in' filter for the specified Actor
        /// </summary>
        /// <param name="field">The field name to filter against</param>
        private string SqlActor(string movieIdFieldName)
        {
            string actorSuffix = string.Empty;
            if (Actor > 0)
            {
                actorSuffix = string.Format("{0} in (select intTitle from tblTitlePerson where intPerson = {1})", movieIdFieldName, Actor); 
            }
            return actorSuffix;
        }

        /// <summary>
        /// Provide where filter suffixes based on current settings
        /// </summary>
        private string SqlFilters(SelectBuilderWhere whereClaue)
        {
            StringBuilder currentFilter = new StringBuilder();

            if (_currentCertifications.Count > 0)
            {
                whereClaue.Add(string.Format("t.intCertification in ({0})", _currentCertifications.ToCSV()));

                _currentCertifications.OnIterate += new Set.OnIterateHandler(Certifications_OnIterate);
                currentFilter.AppendFormat("{0}Cert{1}({2})", (currentFilter.Length > 0) ? " : " : "", _currentCertifications.Count > 1 ? "s" : "", _currentCertifications.ToCSV());
                _currentCertifications.OnIterate -= new Set.OnIterateHandler(Certifications_OnIterate);
            }
            if (_currentYears.Count > 0)
            {
                whereClaue.Add(string.Format("t.intProductionYear in ({0})", _currentYears.ToCSV()));

                currentFilter.AppendFormat("{0}Year{1}({2})", (currentFilter.Length > 0) ? " : " : "", _currentYears.Count > 1 ? "s" : "", _currentYears.ToCSV());
            }
            if (_currentGenres.Count > 0)
            {
                whereClaue.Add(string.Format("t.intId in ( SELECT intTitle FROM tblTitleGenre WHERE intGenre in ({0}) )", _currentGenres.ToCSV()));

                _currentGenres.OnIterate += new Set.OnIterateHandler(Genres_OnIterate);
                currentFilter.AppendFormat("{0}Genre{1}({2})", (currentFilter.Length > 0) ? " : " : "", _currentGenres.Count > 1 ? "s" : "", _currentGenres.ToCSV());
                _currentGenres.OnIterate -= new Set.OnIterateHandler(Genres_OnIterate);
            }
            if (_currentFlags.Contains((int)Flags.IsWatched))
            {
                whereClaue.Add("bitWatched = 1");

                currentFilter.AppendFormat("{0}Is watched", (currentFilter.Length > 0) ? " : " : "");
            }
            if (_currentFlags.Contains((int)Flags.NotWatched))
            {
                whereClaue.Add("bitWatched = 0");

                currentFilter.AppendFormat("{0}Not watched", (currentFilter.Length > 0) ? " : " : "");
            }
            if (_currentFlags.Contains((int)Flags.OnlineTitles))
            {
                whereClaue.Add("intLocation = 1");

                currentFilter.AppendFormat("{0}Online", (currentFilter.Length > 0) ? " : " : "");
            }
            if (_currentFlags.Contains((int)Flags.OfflineTitles))
            {
                whereClaue.Add("intLocation = 0");
                currentFilter.AppendFormat("{0}Offline", (currentFilter.Length > 0) ? " : " : "");
            }

            if (currentFilter.Length > 0)
            {
                CurrentModule = string.Concat("Filtered ", (Actor > 0) ? "actor" : "titles");
            }
            else
            {
                CurrentModule = (Actor > 0) ? "Actor" : "All titles";
            }
                
            return currentFilter.ToString();
        }

        /// <summary>
        /// Return the genre data to be used for the CSV output
        /// </summary>
        /// <param name="data">genre data</param>
        /// <param name="description">genre description</param>
        /// <returns>String description</returns>
        string Genres_OnIterate(int data, string description)
        {
            return description;
        }

        /// <summary>
        /// Return the certification data to be used for the CSV output
        /// </summary>
        /// <param name="data">certification data</param>
        /// <param name="description">certification description</param>
        /// <returns>String description</returns>
        private string Certifications_OnIterate(int data, string description)
        {
            return Certification.Rating(data);
        }

        /// <summary>
        /// Using the currently selected sort critieria determine the approriate SQL data column.
        /// </summary>
        /// <returns>SQL data column for title search</returns>
        public string SqlSorting
        {
            get
            {
                string sqlDataColumn; // datAdded DESC
                switch (_currentSorting)
                {
                    case Sorting.Options.LastPlayed:
                        sqlDataColumn = "r.datLastPlayed";
                        break;
                    case Sorting.Options.DateAdded:
                        sqlDataColumn = "tp.datAdded";
                        break;
                    case Sorting.Options.Duration:
                        sqlDataColumn = "t.intRuntime";
                        break;
                    case Sorting.Options.DVD:
                        sqlDataColumn = "t.intCollectionNumber";
                        break;
                    case Sorting.Options.Label:
                        sqlDataColumn = "t.nvcSortTitle";
                        break;
                    case Sorting.Options.Name:
                        sqlDataColumn = "t.nvcLocalTitle";
                        break;
                    case Sorting.Options.Rating:
                        sqlDataColumn = "t.intCertification";
                        break;
                    case Sorting.Options.Year:
                        sqlDataColumn = "t.intProductionYear";
                        break;
                    default:
                        sqlDataColumn = "t.nvcLocalTitle";
                        break;
                }
                return sqlDataColumn;
            }
        }

        #endregion

        #region BaseWindow Members

        #region GPlayer Events

        private void OnPlayBackChanged(MediaPortal.Player.g_Player.MediaType type, int timeMovieStopped, string filename)
        {
            if (type != g_Player.MediaType.Video) return;

            Log.Debug("MyMovies::OnPlayBackChanged: {0} time stopped {1}", filename, timeMovieStopped);
        }

        private void OnPlayBackStopped(MediaPortal.Player.g_Player.MediaType type, int timeMovieStopped, string filename)
        {
            try
            {
                if (type != g_Player.MediaType.Video) return;

                Log.Debug("MyMovies::OnPlayBackStopped: {0} time stopped {1}", filename, timeMovieStopped);

                // Test if a DVD has just been stopped and more there are more items in the playlist after the DVD.
                // If so, then prompt to check if we should play the next item.
                if (ContinueWithPlaylist(filename))
                {
                    OnPlayBackEnded(g_Player.MediaType.Video, filename);
                }
                else
                {
                    if (timeMovieStopped > 0)
                    {
                        byte[] resumeData = null;
                        g_Player.Player.GetResumeState(out resumeData);

                        PlayListItem currentItem = _playlistPlayer.GetCurrentItem();
                        if (currentItem != null)
                        {
                            MoviesDB.SetMovieStopTimeAndResumeData(NowPlaying.ItemId, _selectedUser, timeMovieStopped, resumeData, currentItem.Description);
                            //MoviesDB.SetMovieStopTimeAndResumeData(Movies.NowPlaying.ItemId, timeMovieStopped, resumeData, (string.IsNullOrEmpty(Movies.NowPlaying.MountedFileName)) ? filename : Movies.NowPlaying.MountedFileName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        private void OnPlayBackEnded(MediaPortal.Player.g_Player.MediaType type, string filename)
        {
            try
            {
                if (type != g_Player.MediaType.Video) return;

                // Check the next file is not a image file that needs to be mounted.
                int nextItem = _playlistPlayer.CurrentSong + 1;
                if (!PlayMovieFromPlayList(_playlistPlayer.GetPlaylist(PlayListType.PLAYLIST_VIDEO_TEMP), nextItem))
                {
                    // Set resumedata to zero
                    MoviesDB.SetMovieStopTimeAndResumeData(NowPlaying.ItemId, _selectedUser, 0, new byte[0], string.Empty);

                    // Set movie as watched.
                    MoviesDB.SetMovieWatched(NowPlaying.ItemId, true);
                    GUIPropertyManager.SetProperty("#iswatched", "true");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        #endregion

        /// <summary>
        /// Load stored settings.
        /// </summary>
        protected override void OnPageLoad()
        {
            // handle sorting direction changes.
            btnSortBy.SortChanged += new SortEventHandler(SortChanged);

            UpdateButtonStates();

            // the "normal" setting is what is configured.
            _maxViewableRating = _maxConfiguredRating;
            if (_maxViewableRating == _maxRating)
            {
                btnEnterPIN.Disabled = true;    // No point having the option if it can't do anything.
            }

            // Initially load the movies from SQL based upon last settings.
            LoadCurrentSettings();
            
            base.OnPageLoad();
        }

        /// <summary>
        /// Save current settings
        /// </summary>
        /// <param name="new_windowId"></param>
        protected override void OnPageDestroy(int new_windowId)
        {
            using (MediaPortal.Profile.Settings xmlwriter = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
            {
                xmlwriter.SetValue("MyMovies", "layout", (int)_currentLayout);
                xmlwriter.SetValue("MyMovies", "view", (int)_currentView);
                xmlwriter.SetValue("MyMovies", "sorting", (int)_currentSorting);
                xmlwriter.SetValueAsBool("MyMovies", "sortDirection", _sortAscending);
                xmlwriter.SetValue("MyMovies", "currentUser", _selectedUser);
            }
            btnSortBy.SortChanged -= new SortEventHandler(SortChanged);


            base.OnPageDestroy(new_windowId);
        }

        public override void DeInit()
        {
            g_Player.PlayBackStopped -= new g_Player.StoppedHandler(OnPlayBackStopped);
            g_Player.PlayBackChanged -= new g_Player.ChangedHandler(OnPlayBackChanged);
            g_Player.PlayBackEnded -= new g_Player.EndedHandler(OnPlayBackEnded);

            base.DeInit();
        }

        public override bool Init()
        {
            bool result = Load(GUIGraphicsContext.Skin + @"\MyMovies.xml");

            try
            {
                using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
                {
                    _currentLayout      = (LayoutOptions)xmlreader.GetValueAsInt("MyMovies", "layout", (int)LayoutOptions.List);
                    _currentView        = (Views)xmlreader.GetValueAsInt("MyMovies", "view", (int)_currentView);
                    _currentSorting     = (Sorting.Options)xmlreader.GetValueAsInt("MyMovies", "sorting", (int)_currentSorting);
                    _selectedUser       = xmlreader.GetValueAsInt("MyMovies", "currentUser", _selectedUser);
                    _sortAscending      = xmlreader.GetValueAsBool("MyMovies", "sortDirection", _sortAscending);
                    _programDataPath    = xmlreader.GetValueAsString("MyMovies", "txtProgramDataPath", @"C:\ProgramData\My Movies\FileStorage");
                    _serverName         = xmlreader.GetValueAsString("MyMovies", "txtServerName", "localhost");
                    _dbInstance         = xmlreader.GetValueAsString("MyMovies", "txtDBInstance", "MYMOVIES");
                    _userName           = xmlreader.GetValueAsString("MyMovies", "txtUserName", string.Empty);
                    _password           = xmlreader.GetValueAsString("MyMovies", "txtPassword", string.Empty);
                    _storedPIN          = xmlreader.GetValueAsString("MyMovies", "txtPINCode", "4321");
                    _chkRemoteWakeup    = xmlreader.GetValueAsBool("MyMovies", "chkRemoteWakeup", false);
                    _macAddress         = MacAddress.Parse(xmlreader.GetValueAsString("MyMovies", "MACAddress", "00-00-00-00-00-00"));
                    _ipAddress          = IPAddress.Parse(xmlreader.GetValueAsString("MyMovies", "IPAddress", "0.0.0.0"));
                    _numLastAdded       = xmlreader.GetValueAsInt("MyMovies", "numLastAdded", 50);
                    _numLastPlayed      = xmlreader.GetValueAsInt("MyMovies", "numLastPlayed", 50);
                    _wakeupRetries      = xmlreader.GetValueAsInt("MyMovies", "wakeupRetries", 3);
                    _wakeupRetryTimeout = xmlreader.GetValueAsInt("MyMovies", "wakeupRetryTimeout", 3000);
                    _maxConfiguredRating= xmlreader.GetValueAsInt("MyMovies", "maximumViewableRating", 4);

                    string xml = xmlreader.GetValueAsString("MyMovies", "xmlPathReplacement", string.Empty);
                    _driveReplacements  = new DriveReplacements(xml);

                    string xmlUsers = xmlreader.GetValueAsString("MyMovies", "xmlUsers", string.Empty);
                    _availableUsers = new Users(xmlUsers);
                }

                Log.Info(string.Format("MyMovies::Init - RemoteWakeup {0}, MAC {1}, IP {2}", _chkRemoteWakeup, _macAddress, _ipAddress));
            }
            catch (Exception ex)
            {
                Log.Error("MyMovies::Init - Cannot load settings");
                Log.Error(ex);
            }

            // Determine the maximum assigned rating within the DB.
            _maxRating = GetMaximumRating();

            g_Player.PlayBackStopped += new g_Player.StoppedHandler(OnPlayBackStopped);
            g_Player.PlayBackChanged += new g_Player.ChangedHandler(OnPlayBackChanged);
            g_Player.PlayBackEnded += new g_Player.EndedHandler(OnPlayBackEnded);

            return result;
        }

        public override bool OnMessage(GUIMessage message)
        {
            switch (message.Message)
            {
                case GUIMessage.MessageType.GUI_MSG_CD_REMOVED:
                    if (g_Player.Playing && g_Player.IsDVD &&
                        message.Label.Equals(g_Player.CurrentFile.Substring(0, 2), StringComparison.InvariantCultureIgnoreCase))   // test if it is our drive
                    {
                        Log.Info("MyMovies::OnMessage Stop dvd since DVD is ejected");
                        g_Player.Stop();
                    }

                    break;

            }
            return base.OnMessage(message);
        }

        protected override void OnClicked(int controlId, GUIControl control, MediaPortal.GUI.Library.Action.ActionType actionType)
        {
            // If the Layout buton is selected, cycle the way the list is shown.
            if (control == btnLayout)
            {
                OnSwitchLayout(controlId);
            }
            else if (control == facadeView)
            {
                // show the details window.
                if ((actionType == MediaPortal.GUI.Library.Action.ActionType.ACTION_SELECT_ITEM)    ||
                    (actionType == MediaPortal.GUI.Library.Action.ActionType.ACTION_PLAY)           ||
                    (actionType == MediaPortal.GUI.Library.Action.ActionType.ACTION_MUSIC_PLAY))
                {
                    OnPlayDvd();
                }
            }
            else if (control == btnSortBy)
            {
                OnShowSortOptions();
            }
            else if (control == btnSwitchViews)
            {
                OnSwitchViews();
            }
            else if (control == btnFilters)
            {
                OnFilter();
            }
            else if (control == btnPlayDVD)
            {
                OnPlayDvd();
            }
            else if (control == btnEnterPIN)
            {
                OnEnterPIN();
            }
            else if (control == btnWatched)
            {
                SelectedMovie.IsPlayed = btnWatched.Selected;
                MoviesDB.SetMovieWatched(SelectedMovie.ItemId, SelectedMovie.IsPlayed);
                GUIPropertyManager.SetProperty("#iswatched", (btnWatched.Selected) ? "true" : "false");
            }
            else if (control == btnActors)
            {
                // Note the Selected Movie is already set.
                GUIWindowManager.ActivateWindow((int)MyMovies.GUIWindowID.Actors, false);
            }
            else if (control == btnUsers)
            {
                OnUsers();
            }

            base.OnClicked(controlId, control, actionType);
        }

        /// <summary>
        /// Monitor for next and previous commands. so we can skip to the next/prev file in playlist.
        /// </summary>
        /// <param name="action">Action type</param>
        public override void OnAction(MediaPortal.GUI.Library.Action action)
        {
            PlayList list = _playlistPlayer.GetPlaylist(PlayListType.PLAYLIST_VIDEO_TEMP);
            if (list.Count > 1)
            {
                if ((action.wID == MediaPortal.GUI.Library.Action.ActionType.ACTION_NEXT_ITEM) ||
                    (action.wID == MediaPortal.GUI.Library.Action.ActionType.ACTION_NEXT_CHAPTER))
                {
                    _playlistPlayer.CurrentSong++;
                    _playlistPlayer.g_Player.Play(list[_playlistPlayer.CurrentSong].FileName);
                }

                if ((action.wID == MediaPortal.GUI.Library.Action.ActionType.ACTION_PREV_ITEM) ||
                    (action.wID == MediaPortal.GUI.Library.Action.ActionType.ACTION_PREV_CHAPTER))
                {
                    _playlistPlayer.CurrentSong--;
                    _playlistPlayer.g_Player.Play(list[_playlistPlayer.CurrentSong].FileName);
                }
            }
            base.OnAction(action);
        }

        /// <summary>
        /// Select the currently selected item after a layout change.
        /// </summary>
        protected void SelectCurrentItem()
        {
            int iItem = facadeView.SelectedListItemIndex;
            if (iItem > -1)
            {
                GUIControl.SelectItemControl(GetID, facadeView.GetID, iItem);
            }
            UpdateButtonStates();
        }

        protected virtual void UpdateButtonStates()
        {
            GUIControl.HideControl(GetID, facadeView.GetID);

            int iControl = facadeView.GetID;
            GUIControl.ShowControl(GetID, iControl);
            GUIControl.FocusControl(GetID, iControl);

            GUIControl.SetControlLabel(GetID, btnLayout.GetID, StringEnum.Get(_currentLayout));

            btnSortBy.Label = StringEnum.Get(_currentSorting);
            btnSortBy.IsAscending = _sortAscending;
        }

        /// <summary>
        /// Change the selected layout.
        /// </summary>
        /// <param name="controlId">Control ID passed during onClick.</param>
        private void OnSwitchLayout(int controlId)
        {
            GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg != null)
            {
                dlg.Reset();
                dlg.SetHeading(792);    // Views menu

                // Add each enum value
                foreach (LayoutOptions value in Enum.GetValues(typeof(LayoutOptions)))
                {
                    dlg.Add(StringEnum.Get(value));
                }

                // set the focus to currently used view
                dlg.SelectedLabel = (int)_currentLayout;

                // show dialog and wait for result
                dlg.DoModal(GetID);
                if (dlg.SelectedId != -1)
                {
                    _currentLayout = (LayoutOptions)dlg.SelectedLabel;

                    SetLayout(_currentLayout);
                    SelectCurrentItem();
                    GUIControl.FocusControl(GetID, controlId);
                }
            }


        }

        private void SetLayout(LayoutOptions layout)
        {
            switch (layout)
            {
                case LayoutOptions.Icons:
                    facadeView.CurrentLayout = GUIFacadeControl.Layout.SmallIcons;
                    break;

                case LayoutOptions.LargeIcons:
                    facadeView.CurrentLayout = GUIFacadeControl.Layout.LargeIcons;
                    break;

                case LayoutOptions.FilmStrip:
                    facadeView.CurrentLayout = GUIFacadeControl.Layout.Filmstrip;
                    break;

                case LayoutOptions.List:
                    facadeView.CurrentLayout = GUIFacadeControl.Layout.List;
                    break;
            }
        }

        /// <summary>
        /// Sort By dialog creation and processing
        /// </summary>
        private void OnShowSortOptions()
        {
            GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg != null)
            {
                dlg.Reset();
                dlg.SetHeading(495); // Sort options

                // Add each enum value
                foreach (Sorting.Options value in Enum.GetValues(typeof(Sorting.Options)))
                {
                    dlg.Add(StringEnum.Get(value));
                }

                // set the focus to currently used view
                dlg.SelectedLabel = (int)_currentSorting;

                // show dialog and wait for result
                dlg.DoModal(GetID);
                if (dlg.SelectedId != -1)
                {
                    _currentSorting = (Sorting.Options)dlg.SelectedLabel;
                    GUIControl.SetControlLabel(GetID, btnSortBy.GetID, StringEnum.Get(_currentSorting));
                    LoadCurrentSettings();
                }
            }
        }

        /// <summary>
        /// Sort button event handler for a direction change
        /// </summary>
        /// <param name="sender">GUIControl sort button</param>
        /// <param name="e">SortEventArgs</param>
        void SortChanged(object sender, SortEventArgs e)
        {
            _sortAscending = e.Order != System.Windows.Forms.SortOrder.Descending;

            // Avoid the GUI control doing the sorting and allow SQL to do it for us.
            // Probably a bit slower, but at least it is consistent.
            LoadCurrentSettings();

            //// Change direction of the sorting.
            //facadeView.Sort(new Sorting(_currentSorting, _sortAscending));

            // Do I need to SetLabels????

            GUIControl.FocusControl(GetID, ((GUIControl)sender).GetID);

            // May need to re-jig the _currentSelectionIndex as our list is reversed
            // Do we want the same item selected when we change the order???
            // _currentSelectionIndex = (facadeView.Count - 1) - _currentSelectionIndex;
            ///facadeView.SelectedListItemIndex = _currentSelectionIndex;
        }

        #region Dialogs

        /// <summary>
        /// Display the list of GUIListItems and return the index of the chosen item.
        /// </summary>
        /// <param name="selected">index of the item to be initially selected</param>
        /// <param name="items">List<GUIListItem></param>
        /// <param name="heading">text to be displayed.</param>
        /// <returns>selected index.</returns>
        private int DialogSelect(int selected, List<GUIListItem> items, string heading)
        {
            int selectedItem = selected;

            if (items.Count.Equals(0))
            {
                selectedItem = -1;
            }
            else
            {
                // Display a dialog with all drives to select from
                GUIDialogSelect2 dlgSel = (GUIDialogSelect2)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_SELECT2);
                if (dlgSel == null)
                {
                    Log.Info("MyMoviesDetails::DialogSelect Could not open dialog, defaulting to selected item");
                }
                else
                {
                    dlgSel.Reset();
                    dlgSel.SetHeading(heading);
                    foreach (GUIListItem item in items)
                    {
                        dlgSel.Add(item);
                    }
                    dlgSel.DoModal(GetID);

                    selectedItem = dlgSel.SelectedLabel;
                }
            }
            return selectedItem;
        }

        private bool DialogYesNo(bool defaultYes, string heading, string line1, string line2)
        {
            GUIDialogYesNo dlgYesNo = (GUIDialogYesNo)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_YES_NO);
            if (null != dlgYesNo)
            {
                dlgYesNo.SetHeading(heading);
                dlgYesNo.SetLine(1, line1);
                if (!string.IsNullOrEmpty(line2)) dlgYesNo.SetLine(2, line2);
                dlgYesNo.SetDefaultToYes(defaultYes);
                dlgYesNo.DoModal(GUIWindowManager.ActiveWindow);

                return dlgYesNo.IsConfirmed;
            }
            Log.Error("MyMoviesDetails::DialogYesNo - Dialog creation error");
            return false;
        }


        private void DialogNoDisk()
        {
            //no disc in drive...
            GUIDialogOK dlgOk = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
            dlgOk.SetHeading(3);//my videos
            dlgOk.SetLine(1, 219);//no disc
            dlgOk.DoModal(GetID);
            Log.Info("MyMoviesDVDHandler: did not find a movie");
        }

        /// <summary>
        /// Notify the user that something has ocurred using a modal dialog.
        /// </summary>
        /// <param name="userMessage">Message to display.</param>
        private void DialogNotify(string headingText, string userMessage)
        {
            GUIDialogNotify notifyDlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
            if (notifyDlg != null)
            {
                notifyDlg.Reset();
                notifyDlg.SetHeading(headingText);
                notifyDlg.SetText(userMessage);
                notifyDlg.DoModal(GetID);
            }
        }

        /// <summary>
        /// View type dialog creation and processing
        /// </summary>
        private void OnSwitchViews()
        {
            GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg != null)
            {
                dlg.Reset();
                dlg.SetHeading(499);    // Views menu

                // Add each enum value
                foreach (Views value in Enum.GetValues(typeof(Views)))
                {
                    dlg.Add(StringEnum.Get(value));
                }

                // set the focus to currently used view
                dlg.SelectedLabel = (int)_currentView;

                // show dialog and wait for result
                dlg.DoModal(GetID);
                if (dlg.SelectedId != -1)
                {
                    _currentView = (Views)dlg.SelectedLabel;

                    Actor = 0;                      // Don't include actor filtering
                    _currentSelectionIndex = 0;     // Always start at the top.
                    LoadCurrentSettings();
                }
            }
        }


        /// <summary>
        /// Display a dialog of the available filter options.
        /// If selected, display the requested filter option details.
        /// </summary>
        private void OnFilter()
        {
            GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg != null)
            {
                dlg.Reset();
                dlg.SetHeading("Apply filters");

                // Add each enum value
                foreach (Filters value in Enum.GetValues(typeof(Filters)))
                {
                    dlg.Add(StringEnum.Get(value));
                }

                // set the focus to the first filter option
                dlg.SelectedLabel = 0;

                // show dialog and wait for result
                dlg.DoModal(GetID);
                if (dlg.SelectedId != -1)
                {
                    switch ((Filters)dlg.SelectedLabel)
                    {
                        case Filters.Genre:
                            {
                                OnGenre();
                                break;
                            }

                        case Filters.Certification:
                            {
                                OnCertification();
                                break;
                            }

                        case Filters.Flags:
                            {
                                OnFlags();
                                break;
                            }

                        case Filters.Years:
                            {
                                OnYears();
                                break;
                            }

                        case Filters.Clear:
                            {
                                OnClear();
                                break;
                            }
                    }
                    LoadCurrentSettings();
                }
            }
        }

        private void OnClear()
        {
            _currentYears.Clear();
            _currentCertifications.Clear();
            _currentGenres.Clear();
            _currentFlags.Clear();

            Actor = 0;
        }

        private void OnCertification()
        {
            List<Item> availableCertifications = new List<Item>();       // Based upon the current view (All, Last played, Last Added)
            QueryReader<List<Item>> sql = new QueryReader<List<Item>>(SqlRating);
            sql.OnRow += new QueryReader<List<Item>>.Row(OnRow_Rating);
            sql.OnConnection += new Sql.ConnectionEvent(sql_OnConnection);
            sql.Execute(ConnectionString, availableCertifications);

            Filter dialog = new Filter("Certifications", _currentCertifications, availableCertifications);
            dialog.DoModal();
        }

        private enum Flags
        { 
            [StringEnum("Is watched")]
            IsWatched = 0,
            [StringEnum("Not watched")]
            NotWatched,
            [StringEnum("Online titles")]
            OnlineTitles,
            [StringEnum("Offline titles")]
            OfflineTitles, 
        }


        private void OnFlags()
        {
            List<Item> availableFlags = new List<Item>();       // Based upon the current view (All, Last played, Last Added)

            foreach (Flags value in Enum.GetValues(typeof(Flags)))
            {
                availableFlags.Add(new Item((int)value, StringEnum.Get(value)));
            }

            Filter dialog = new Filter("Flags", _currentFlags, availableFlags);
            dialog.Dialog.OnChecked += new DialogCheckedList.OnCheckedHandler(Flags_OnChecked);
            dialog.DoModal();
            dialog.Dialog.OnChecked -= new DialogCheckedList.OnCheckedHandler(Flags_OnChecked);
        }

        private void Flags_OnChecked(CheckedItem item, DialogCheckedList dialog)
        {
            // If checked has been enabled
            if (item.Second)
            {
                switch ((Flags)item.First.First)
                {
                    case Flags.IsWatched:
                        dialog.SetChecked((int)Flags.NotWatched, false);
                        break;
                    case Flags.NotWatched:
                        dialog.SetChecked((int)Flags.IsWatched, false);
                        break;
                    case Flags.OfflineTitles:
                        dialog.SetChecked((int)Flags.OnlineTitles, false);
                        break;
                    case Flags.OnlineTitles:
                        dialog.SetChecked((int)Flags.OfflineTitles, false);
                        break;
                }
            }
        }


        private void OnYears()
        {
            List<Item> availableYears = new List<Item>();       // Based upon the current view (All, Last played, Last Added)
            QueryReader<List<Item>> sql = new QueryReader<List<Item>>(SqlYears);
            sql.OnRow += new QueryReader<List<Item>>.Row(OnRow_Year);
            sql.OnConnection += new Sql.ConnectionEvent(sql_OnConnection);
            sql.Execute(ConnectionString, availableYears);

            Filter dialog = new Filter("Production Years", _currentYears, availableYears);
            dialog.DoModal();
        }

        private void OnGenre()
        {
            List<Item> availableGenre = new List<Item>();       // Based upon the current view (All, Last played, Last Added)
            QueryReader<List<Item>> sql = new QueryReader<List<Item>>(SqlGenres);
            sql.OnRow += new QueryReader<List<Item>>.Row(OnRow_Genre);
            sql.OnConnection += new Sql.ConnectionEvent(sql_OnConnection);
            sql.Execute(ConnectionString, availableGenre);

            Filter dialog = new Filter(135, _currentGenres, availableGenre);
            dialog.DoModal(true);
        }

        private void OnUsers()
        {
            if (_availableUsers.Collection.Count == 0)
            {
                DialogNotify("Select User", "There are no users defined");
            }
            else
            {
                GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
                if (dlg != null)
                {
                    dlg.Reset();
                    dlg.SetHeading("Select User");    // User selection menu

                    // Add each user
                    foreach (string user in _availableUsers.Collection)
                    {
                        dlg.Add(user);
                    }

                    // set the focus to currently selected user
                    dlg.SelectedLabel = _selectedUser;

                    // show dialog and wait for result
                    dlg.DoModal(GetID);
                    if (dlg.SelectedId != -1)
                    {
                        if (dlg.SelectedLabel != _selectedUser)
                        {
                            _selectedUser = dlg.SelectedLabel;

                            // Only reload if the changed user selection changes the available options.
                            if ((Actor == 0) && (_currentSorting == Sorting.Options.LastPlayed))
                            {
                                _currentSelectionIndex = 0;     // Always start at the top.
                                LoadCurrentSettings();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The user has selected they want to enter a PIN.
        /// </summary>
        private void OnEnterPIN()
        {
            // if The PIN is already entered, no point doing it again.
            if (_maxViewableRating < _maxRating)
            {
                VirtualKeyboard keyboard = (VirtualKeyboard)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_VIRTUAL_KEYBOARD);
                if (null != keyboard)
                {
                    keyboard.Reset();
                    keyboard.Password = true;
                    keyboard.Text = string.Empty;
                    keyboard.DoModal(GetID);
                    if (keyboard.IsConfirmed && (_storedPIN == keyboard.Text))
                    {
                        _maxViewableRating = _maxRating;
                        btnEnterPIN.Disabled = true;    // No point having the option if it can't do anything.
                        LoadCurrentSettings();
                    }
                }
            }
        }

        
        #endregion

        /// <summary>
        /// Load the facadeView with the items in the list
        /// </summary>
        /// <param name="itemList"></param>
        private void LoadItems(ArrayList itemList, int selectionIndex)
        {
            try
            {
                GUIControl.ClearControl(GetID, facadeView.GetID);
                foreach (GUIListItem item in itemList)
                {
                    facadeView.Add(item);
                }

                SetLayout(_currentLayout);
                facadeView.SelectedListItemIndex = selectionIndex;

                //set object count label
                GUIPropertyManager.SetProperty("#itemcount", MediaPortal.Util.Utils.GetObjectCountLabel(itemList.Count));
            }
            catch (Exception ex)
            {
                Log.Error("MyMovies::LoadItems - Error loading movies");
                Log.Error(ex);
            }
        }

        /// <summary>
        /// Read the maximum configured rating within the Database.
        /// </summary>
        /// <returns>The maximum configured rating</returns>
        private int GetMaximumRating()
        {
            int maxRating = 8;  // Add a default.
            try
            {
                QueryValue getMaxRating = new QueryValue(_maxRatingSQL);
                maxRating = Convert.ToInt32(getMaxRating.Execute(ConnectionString));
            }
            catch (Exception ex)
            {
                Log.Error("MyMovies::GetMaximumRating - Cannot read setting. Defaulting");
                Log.Error(ex);
            }
            return maxRating;
        }

        /// <summary>
        /// Read the data from SQL using the current settings.
        /// </summary>
        private void LoadCurrentSettings()
        {
            GUIWaitCursor.Show();

            try
            {
                ArrayList itemlist = new ArrayList();
                QueryReader<ArrayList> sql = new QueryReader<ArrayList>(SqlMovies);
                sql.OnRow += new QueryReader<ArrayList>.Row(OnRow_Movie);
                sql.OnConnection += new Sql.ConnectionEvent(sql_OnConnection);
                sql.Execute(ConnectionString, itemlist);

                LoadItems(itemlist, _currentSelectionIndex);
                GUIWaitCursor.Hide();
            }
            catch (Exception ex)
            {
                Log.Error("MyMovies::LoadCurrentSettings - Error loading movies");
                Log.Error(ex);

                GUIWaitCursor.Hide();
                GUIDialogOK dlgOk = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
                dlgOk.SetHeading(342);//Movies
                dlgOk.SetLine(1, 315);//Database error
                dlgOk.DoModal(GetID);
            }
        }

        public bool sql_OnConnection()
        {
            bool wakeupOK = false;

            if (_chkRemoteWakeup)
            {
                int retry = 0;
                GUIDialogProgress progress = null;

                try
                {
                    RemoteServer sqlDB = new RemoteServer("My Movies DB", _ipAddress, _macAddress);
                    while (!sqlDB.IsAwake() && retry < _wakeupRetries)
                    {
                        progress = (GUIDialogProgress)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_PROGRESS);
                        if (progress != null)
                        {
                            progress.SetHeading(185);
                            progress.SetLine(1, "Waking up server");
                            progress.SetPercentage(0);
                            progress.Progress();
                            progress.ShowProgressBar(false);

                            GUIWindowManager.Process();
                        }
                        retry++;
                        sqlDB.WakeUp();
                        Thread.Sleep(_wakeupRetryTimeout);
                    }
                    wakeupOK = true;
                }
                catch (Exception ex)
                {
                    Log.Error("MyMovies::SQL_OnConnection - Error waking server " + _ipAddress.ToString());
                    Log.Error(ex);
                }
            }
            else
            {
                wakeupOK = true;
            }
            return wakeupOK;
        }

        void newItem_OnRetrieveArt(GUIListItem item)
        {
            GUIListItemMovie movie = item as GUIListItemMovie;
            if (movie != null)
            {
                item.ThumbnailImage = movie.FrontCover;
            }
        }

        void OnItemSelected(GUIListItem item, GUIControl parent)
        {
            if (item != _currentSelection)
            {
                _currentSelection = item;
                _currentSelectionIndex = facadeView.SelectedListItemIndex;
                GUIListItemMovie movie = item as GUIListItemMovie;
                if (movie != null)
                {
                    GUIPropertyManager.SetProperty("#director", GetMovieDirectors(movie.ItemId));
                    GUIPropertyManager.SetProperty("#studios", GetStudios(movie.ItemId));
                    GUIPropertyManager.SetProperty("#genre", GetMovieGenres(movie.ItemId));
                    GUIPropertyManager.SetProperty("#imdbnumber", movie.ItemId.ToString());
                    GUIPropertyManager.SetProperty("#file", movie.TranslatedPath);
                    GUIPropertyManager.SetProperty("#plot", movie.Plot);
                    GUIPropertyManager.SetProperty("#rating", movie.Stars.ToString());
                    GUIPropertyManager.SetProperty("#title", movie.Label);
                    GUIPropertyManager.SetProperty("#year", movie.Year.ToString());
                    GUIPropertyManager.SetProperty("#runtime", movie.Duration.ToString());
                    GUIPropertyManager.SetProperty("#certificationCause", movie.CertificationCause);    // no longer being used.
                    GUIPropertyManager.SetProperty("#iswatched", (movie.IsPlayed) ? "true" : "false");
                    GUIPropertyManager.SetProperty("#selectedthumb", movie.FrontCover);
                    GUIPropertyManager.SetProperty("#frontCover", movie.FrontCover);
                    GUIPropertyManager.SetProperty("#online", movie.Online.ToString());
                    GUIPropertyManager.SetProperty("#collection", movie.CollectionNum.ToString());
                    GUIPropertyManager.SetProperty("#selecteditem", movie.Label);
                    GUIPropertyManager.SetProperty("#hideinfo", "false");       // Required to show info in skin
                    GUIPropertyManager.SetProperty("#watchedcount", "0");       // ToDo
                    GUIPropertyManager.SetProperty("#watchedpercent", WatchedPercentage(movie.ItemId, movie.Duration).ToString());
                    GUIPropertyManager.SetProperty("#mpaarating", movie.Certification);   
                    GUIPropertyManager.SetProperty("#AspectRatio", AspectToIcon(movie.Aspect));    // Options refer to C:\ProgramData\Team MediaPortal\MediaPortal\skin\Titan\Media\Logos\aspectratio

                    // These propertiesare related to the actual file content
                    //GUIPropertyManager.SetProperty("#VideoResolution", "SD");   // ToDo - options 240, 480, 540, 576, 720p, 1080i, 1080p, SD
                    //GUIPropertyManager.SetProperty("#VideoCodec", "x264");      // ToDo - options refer to C:\ProgramData\Team MediaPortal\MediaPortal\skin\Titan\Media\Logos\video
                    //GUIPropertyManager.SetProperty("#AudioCodec", "dts");       // ToDo - options refer to C:\ProgramData\Team MediaPortal\MediaPortal\skin\Titan\Media\Logos\audio
                    //GUIPropertyManager.SetProperty("#AudioChannels", "5.1");    // ToDo - options refer to C:\ProgramData\Team MediaPortal\MediaPortal\skin\Titan\Media\Logos\audio

                    btnWatched.Selected = movie.IsPlayed; 
                    
                    facadeView.FilmstripLayout.InfoImageFileName = movie.IconImageBig;
                    return;
                }
            }
        }

        /// <summary>
        /// Convert the MyMovies aspect ratio to a provided MP icon name
        /// </summary>
        /// <param name="aspectRatio"></param>
        /// <returns></returns>
        private string AspectToIcon(string aspectRatio)
        {
            string[] fields = aspectRatio.Split(new char[] { ':', '-' });
            double aspect;
            if (!double.TryParse(fields[0], out aspect))
            {
                aspect = 1.33;      // SD 
            }
            if (aspect < 1.5)   return "1.33";
            if (aspect < 1.71)  return "1.66";
            if (aspect < 1.81)  return "1.78";
            if (aspect < 2.0)   return "1.85";
            return "widescreen";
        }

        #region SQL OnRow_handlers

        /// <summary>
        /// Retrieve the available Genres from the MyMovies database.
        /// </summary>
        /// <param name="row">SqlDataReader for the row being processed</param>
        /// <param name="list">ArrayList for storage of Genres</param>
        void OnRow_Genre(SqlDataReader row, List<Item> list)
        {
            list.Add(new Item(Convert.ToInt32(row["intId"]), row["nvcName"].ToString()));
        }

        /// <summary>
        /// Retrieve the available Ratings from the MyMovies database.
        /// </summary>
        /// <param name="row">SqlDataReader for the row being processed</param>
        /// <param name="list">ArrayList for storage of Years</param>
        void OnRow_Rating(SqlDataReader row, List<Item> list)
        {
            list.Add(new Item(Convert.ToInt32(row[0]), Certification.Rating(Convert.ToInt32(row[0]))));
        }

        /// <summary>
        /// Retrieve the available Years from the MyMovies database.
        /// </summary>
        /// <param name="row">SqlDataReader for the row being processed</param>
        /// <param name="list">ArrayList for storage of Years</param>
        void OnRow_Year(SqlDataReader row, List<Item> list)
        {
            list.Add(new Item(Convert.ToInt32(row[0]), row[0].ToString()));
        }


        /// <summary>
        /// Retrieve the movie details from this record.
        /// </summary>
        /// <param name="row">SqlDataReader for the row being processed</param>
        /// <param name="itemlist">ArrayList of GUIListItem for storage of last added movies</param>
        public void OnRow_Movie(SqlDataReader row, ArrayList itemlist)
        {
            GUIListItemMovie newItem = new GUIListItemMovie();

            newItem.IsFolder = true;     // MyMovies uses 1 movie per folder. Files are played in alphabetical order.
            newItem.Label = row["title"].ToString();
            newItem.Path = row["pathName"].ToString();
            newItem.Plot = row["plot"].ToString();
            newItem.Aspect = row["aspect"].ToString();
            newItem.Certification = Certification.Rating(Convert.ToInt32(row["rating"]), Convert.ToInt32(row["country"]));
            newItem.CertificationCause = row["ratingCause"].ToString();
            newItem.CollectionNum = Convert.ToInt32(row["collectionNum"]);
            newItem.ItemId = Convert.ToInt32(row["id"]);
            newItem.Duration = Convert.ToInt32(row["runTime"]);
            newItem.Year = Convert.ToInt32(row["year"]);
            newItem.Stars = Convert.ToInt32(row["stars"]);
            newItem.IsPlayed = Convert.ToBoolean(row["watched"]);
            newItem.Online = Convert.ToInt32(row["online"]).Equals(1);

            newItem.FrontCover = string.Format(@"{0}\Covers\{1}.jpg", DataPath, row["frontCover"].ToString());
            newItem.BackCover = string.Format(@"{0}\Covers\{1}.jpg", DataPath, row["backCover"].ToString());

            newItem.OnItemSelected += new GUIListItem.ItemSelectedHandler(OnItemSelected);
            newItem.OnRetrieveArt += new GUIListItem.RetrieveCoverArtHandler(newItem_OnRetrieveArt);

            itemlist.Add(newItem);
        }

        /// <summary>
        /// String builder handler for the movie genres.
        /// </summary>
        /// <param name="row">SqlDataReader</param>
        /// <param name="csvGenres">StringBuilder for data output.</param>
        void OnRow_CSV(SqlDataReader row, StringBuilder csv)
        {
            if (csv.Length.Equals(0))
            {
                csv.Append(row[0].ToString());
            }
            else
            {
                csv.AppendFormat(", {0}", row[0]);
            }
        }

        #endregion

        #region SQL Accessors

        /// <summary>
        /// Create a CSV of the genres associated with the specified movie.
        /// </summary>
        /// <param name="movieId">int movie ID</param>
        /// <returns>csv of Genres</returns>
        private string GetMovieGenres(int movieId)
        {
            StringBuilder genres = new StringBuilder();
            QueryReader<StringBuilder> sql = new QueryReader<StringBuilder>(string.Format("SELECT g.nvcName FROM tblTitleGenre tg INNER JOIN tblGenres g ON tg.intGenre = g.intId WHERE intTitle = {0}", movieId));
            sql.OnRow += new QueryReader<StringBuilder>.Row(OnRow_CSV);
            sql.Execute(ConnectionString, genres);

            return genres.ToString();
        }

        /// <summary>
        /// Create a CSV of the directors associated with the specified movie.
        /// </summary>
        /// <param name="movieId">int movie ID</param>
        /// <returns>csv of directors names</returns>
        private string GetMovieDirectors(int movieId)
        {
            StringBuilder directors = new StringBuilder();
            QueryReader<StringBuilder> sql = new QueryReader<StringBuilder>(string.Format("{0} WHERE intTitle = {1}", _directorsSQL, movieId));
            sql.OnRow += new QueryReader<StringBuilder>.Row(OnRow_CSV);
            sql.Execute(ConnectionString, directors);

            return directors.ToString();
        }

        /// <summary>
        /// Create a CSV of the studios associated with the specified movie.
        /// </summary>
        /// <param name="movieId">int movie ID</param>
        /// <returns>csv of studio names</returns>
        private string GetStudios(int movieId)
        {
            StringBuilder studios = new StringBuilder();
            QueryReader<StringBuilder> sql = new QueryReader<StringBuilder>(string.Format("{0} WHERE intTitle = {1}", _studiosSQL, movieId));
            sql.OnRow += new QueryReader<StringBuilder>.Row(OnRow_CSV);
            sql.Execute(ConnectionString, studios);

            return studios.ToString();
        }

        #endregion

        /// <summary>
        /// If a server MAC address is defined, ensure the machine can respond to a PING comand. 
        /// If not, then send a MAGIC packet wakeup request.
        /// </summary>
        private bool RemoteServerWakeup()
        {
            if (_chkRemoteWakeup)
            {
                try
                {
                    if (!RemoteServer.IsAwake())
                    {
                        RemoteServer.WakeUp();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("MyMovies::RemoteServerWakeup - Error retrieving waking remmote server");
                    Log.Error(ex);
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region ISetupForm Members

        public string Author()
        {
            return "Matty";
        }

        public bool CanEnable()
        {
            return true;
        }

        public bool DefaultEnabled()
        {
            return true;
        }

        public string Description()
        {
            return "This is a client plugin for Brian Binnerup's 'MyMovies 2' database.";
        }

        /// <summary>
        /// If the plugin should have it's own button on the main menu of MediaPortal then it
        /// should return true to this method, otherwise if it should not be on home
        /// it should return false
        /// </summary>
        /// <param name="strButtonText">text the button should have</param>
        /// <param name="strButtonImage">image for the button, or empty for default</param>
        /// <param name="strButtonImageFocus">image for the button, or empty for default</param>
        /// <param name="strPictureImage">subpicture for the button or empty for none</param>
        /// <returns>true : plugin needs it's own button on home
        /// false : plugin does not need it's own button on home</returns>
        public bool GetHome(out string strButtonText, out string strButtonImage, out string strButtonImageFocus, out string strPictureImage)
        {
            strButtonText = PluginName();
            strButtonImage = String.Empty;
            strButtonImageFocus = String.Empty;
            strPictureImage = "hover_my movies.png";
            return true;
        }

        public int GetWindowId()
        {
            return (int)MyMovies.GUIWindowID.Main;
        }

        private int WatchedPercentage(int itemId, int runtime)
        {
            try
            {
                string fileDescription;
                byte[] resumeData = null;
                int watchedTime = MoviesDB.GetMovieStopTimeAndResumeData(itemId, _selectedUser, out fileDescription, out resumeData);
                if (watchedTime > runtime)
                {
                    watchedTime = runtime;
                }
                else if (watchedTime < 0)
                {
                    watchedTime = 0;
                }
                return watchedTime * 100 / runtime;
            }
            catch (Exception ex)
            {
                Log.Error("MyMovies::WatchedPercentage - Error retrieving current percentage");
                Log.Error(ex);
                return 0;
            }
        }

        public bool HasSetup()
        {
            return true;
        }

        public string PluginName()
        {
            return "MyMovies";
        }

        public void ShowPlugin()
        {
            System.Windows.Forms.Form setup = new Configuration();
            setup.ShowDialog();
        }

        #endregion

        #region Properties


        /// <summary>
        /// Get the configured WakeOnLan remote server.
        /// </summary>
        private RemoteServer RemoteServer
        {
            get 
            {
                if (_remoteServer == null)
                {
                    // Check for dotted decminal notation.
                    _remoteServer = new RemoteServer("MyMovies server", _ipAddress, _macAddress);
                }
                return _remoteServer;
            }
        }

        /// <summary>
        /// Format the SQL connection string based upon configuration defined options.
        /// </summary>
        public string ConnectionString
        {
            get 
            {
                if (_sqlConnection == null)
                {
                    _sqlConnection = QueryReader<int>.Connection(_serverName, _dbInstance, _userName, _password);
                }
                Log.Debug("MyMovies::ConnectionString - '{0}'", _sqlConnection);

                return _sqlConnection.ToString();
            }
        }

        public override int GetID
        {
            get
            {
                return GetWindowId();
            }
            set { } // Do nothing.
        }

        public GUIListItemMovie NowPlaying
        {
            get
            {
                return _nowPlaying;
            }
            set
            {
                _nowPlaying = value;
                GUIPropertyManager.SetProperty("#Play.Current.Title",       NowPlaying.Label);
//                GUIPropertyManager.SetProperty("#Play.Current.Genre",       NowPlaying.Gen);
                GUIPropertyManager.SetProperty("#Play.Current.Year",        NowPlaying.Year.ToString());
                GUIPropertyManager.SetProperty("#Play.Current.Director",    NowPlaying.Director);
                GUIPropertyManager.SetProperty("#Play.Current.PlotOutline", NowPlaying.Plot);
                GUIPropertyManager.SetProperty("#Play.Current.Plot",        NowPlaying.Plot);
            }
        }

        /// <summary>
        /// Access to MyMoviesDB instance
        /// </summary>
        public MyMoviesDB MoviesDB
        {
            get
            {
                if (_myMoviesDB == null)
                {
                    _myMoviesDB = new MyMoviesDB(ConnectionString);
                }
                return _myMoviesDB;
            }
        }

        public GUIListItemMovie SelectedMovie
        {
            get
            {
                return _currentSelection as GUIListItemMovie;
            }
        }

        public DriveReplacements DriveReplacements
        {
            get
            {
                return _driveReplacements;
            }
        }

        public string DataPath
        {
            get
            {
                return _programDataPath;
            }
        }

        /// <summary>
        /// Set the current module
        /// </summary>
        private string CurrentModule
        {
            set
            {
                GUIPropertyManager.SetProperty("#currentmodule", string.Format("MyMovies: {0}", value));
            }
        }

        /// <summary>
        /// Set the current filter in use
        /// </summary>
        private string CurrentFilter
        {
            set
            {
                GUIPropertyManager.SetProperty("#currentfilter", value);
            }
        }

        #endregion

        #region Mounting

        public bool MountImageFile(int windowID, string file, out string virtualDrive)
        {
            bool driveMounted = false;

            Log.Debug("MyMovies: MountImageFile");
            if (!DaemonTools.IsMounted(file))
            {
                driveMounted = DaemonTools.Mount(file, out virtualDrive);
            }
            else
            {
                virtualDrive = DaemonTools.GetVirtualDrive();
                driveMounted = (virtualDrive.Length > 0);
            }
            return driveMounted;
        }

        /// <summary>
        /// Mount an image file and determine the file to play.
        /// </summary>
        /// <param name="windowID"></param>
        /// <param name="videoFile">[in] Video image file. [out] Video file to play.</param>
        /// <returns>true if the image file is mounted correctly</returns>
        private bool MountImageFile(int windowID, ref string videoFile)
        {
            bool status;

            Log.Info("MyMovies: MountImageFile - {0}", videoFile);

            MediaPortal.Ripper.AutoPlay.StopListening();

            string strDir;

            if ((status = MountImageFile(windowID, videoFile, out strDir)))
            {
                // Check if the mounted image is actually a DVD. If so, bypass
                // autoplay to play the DVD without user intervention
                if (System.IO.File.Exists(strDir + Constants.FileNames.DVD))
                {
                    videoFile = strDir + Constants.FileNames.DVD;  // DVD              
                }
                else if (System.IO.File.Exists(strDir + Constants.FileNames.SVCD))
                {
                    videoFile = strDir + Constants.FileNames.SVCD;      // SVCD
                }
                else if (System.IO.File.Exists(strDir + Constants.FileNames.VCD))
                {
                    videoFile = strDir + Constants.FileNames.VCD;     // VCD
                }
                else
                {
                    Log.Error("MyMovies::PlayMountedImageFile - Unknown image type mounted on drive {0}", strDir);
                    status = false;
                }
            }
            MediaPortal.Ripper.AutoPlay.StartListening();

            return status;
        }

        #endregion

        #region Playlist Control

        private void AddPlayListItem(bool initialise, string fileToPlay, string fileDescription)
        {
            PlayList playlist = _playlistPlayer.GetPlaylist(PlayListType.PLAYLIST_VIDEO_TEMP);

            if (initialise)
            {
                _playlistPlayer.Reset();
                _playlistPlayer.CurrentPlaylistType = PlayListType.PLAYLIST_VIDEO_TEMP;
                playlist.Clear();
            }
            PlayListItem newitem = new PlayListItem();
            newitem.FileName = fileToPlay;
            newitem.Description = fileDescription;
            newitem.Type = MediaPortal.Playlists.PlayListItem.PlayListItemType.Video;
            playlist.Add(newitem);

            Log.Debug("MyMovies::AddPlayListItem - Adding {0}", fileToPlay);
        }

        public void PlayMovieFromPlayList(bool askForResumeMovie)
        {
            string fileDescription;
            byte[] resumeData = null;
            int playListItem = -1;
            PlayList playList = _playlistPlayer.GetPlaylist(PlayListType.PLAYLIST_VIDEO_TEMP);
            int timeMovieStopped = MoviesDB.GetMovieStopTimeAndResumeData(NowPlaying.ItemId, _selectedUser, out fileDescription, out resumeData);

            // There is nothing valid to play.
            if (playList.Count.Equals(0))
            {
                DialogNotify("Invalid movie selection", "There are no valid movie files to play !!");
            }
            else if (playList.Count > 1)    // Check if the user wants to play a specific file.
            {
                playListItem = ShowPlayListItems(playList, fileDescription);
            }
            else
            {
                playListItem = 0;
            }

            if (playListItem >= 0)
            {
                // If required, and the previously stopped file is the one selected, then prompt for resume.
                if (askForResumeMovie && (timeMovieStopped > 0))
                {
                    if (playList[playListItem].Description.Equals(fileDescription))
                    {
                        string title = NowPlaying.Label;

                        GUIResumeDialog.Result result = GUIResumeDialog.ShowResumeDialog(title, timeMovieStopped, GUIResumeDialog.MediaType.Video);
                        if (result == GUIResumeDialog.Result.Abort)
                        {
                            return;
                        }
                        
                        //                        if (!DialogYesNo(true, GUILocalizeStrings.Get(900), title, string.Format("{0} {1}", GUILocalizeStrings.Get(936), MediaPortal.Util.Utils.SecondsToHMSString(timeMovieStopped))))
                        if (result == GUIResumeDialog.Result.PlayFromBeginning)
                        {
                            timeMovieStopped = 0;
                        }

                    }
                    else
                    {
                        timeMovieStopped = 0;   // Start at the beginning.
                    }
                }

                if (PlayMovieFromPlayList(playList, playListItem))
                {
                    if (g_Player.Playing && timeMovieStopped > 0)
                    {
                        if (g_Player.IsDVD)
                        {
                            g_Player.Player.SetResumeState(resumeData);
                        }
                        else
                        {
                            GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_SEEK_POSITION, 0, 0, 0, 0, 0, null);
                            msg.Param1 = (int)timeMovieStopped;
                            GUIGraphicsContext.SendMessage(msg);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Play the item identified by hte index. 
        /// If the item is an image, then mount it and play it
        /// </summary>
        /// <param name="playlist">PlayList</param>
        /// <param name="index">int</param>
        /// <returns>true if the item is playing, otherwise false</returns>
        private bool PlayMovieFromPlayList(PlayList playlist, int index)
        {

            // Validate the input.
            if (index < 0)
            {
                index = 0;
            }
            else if (index >= playlist.Count)
            {
                // End of PlayList encountered.
                return false;
            }
            string videoFile = playlist[index].FileName;

            // If the playlist item is a DVD image, then mount and 
            // replace the image name with the mounted filename
            if (VirtualDirectory.IsImageFile(Path.GetExtension(videoFile)))
            {
                if (MountImageFile(GetID, ref videoFile))
                {
                    playlist[index].FileName = videoFile;    // The "real" play list item.
                }
                else
                {
                    // Abort playing the image.
                    DialogNotify("Invalid video file", string.Format("The image file [{0}] cannot be played", videoFile));
                    return false;
                }
            }

            // Play the play list item
            bool res = _playlistPlayer.Play(index);

            g_Player.currentDescription = NowPlaying.Plot;

            return res;
        }

        private int ShowPlayListItems(PlayList playList, string fileName)
        {
            int playListItemSelected = 0;
            GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlg != null)
            {
                dlg.Reset();
                dlg.ShowQuickNumbers = false;
                dlg.SetHeading("Select the file to play");    // Views menu

                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

                // Add each enum value
                for (int i = 0; i < playList.Count; i++)
                {
                    string dlgText = Path.GetFileNameWithoutExtension(playList[i].FileName);
                    dlg.Add(dlgText);
                    if (dlgText.Equals(fileNameWithoutExt))
                    {
                        playListItemSelected = i;
                    }
                }

                // set the focus to the first item
                dlg.SelectedLabel = playListItemSelected;

                // show dialog and wait for result
                dlg.DoModal(GetID);

                playListItemSelected = dlg.SelectedId - 1;
            }
            return playListItemSelected;
        }


        /// <summary>
        /// Test if a DVD has just been stopped and more there are more items in the playlist after the DVD.
        /// If so, then prompt to check if we should play the next item.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>false unless the user return true to continue after a DVD is stopped.</returns>
        private bool ContinueWithPlaylist(string filename)
        {
            bool continuePlaylist = false;

            if (filename.Contains(Constants.FileNames.DVD) ||
                filename.Contains(Constants.FileNames.SVCD) ||
                filename.Contains(Constants.FileNames.VCD))
            {
                PlayList playList = _playlistPlayer.GetPlaylist(PlayListType.PLAYLIST_VIDEO_TEMP);
                if ((playList.Count > 1) &&
                    (playList.Count > (_playlistPlayer.CurrentSong + 1)))
                {
                    continuePlaylist = DialogYesNo(true, GUILocalizeStrings.Get(136), "Continue with next item?", null);
                }
            }
            return continuePlaylist;
        }


        #endregion

        #region DVD Control

        public void OnPlayDvd()
        {
            string moviePath = SelectedMovie.TranslatedPath;

            if (!SelectedMovie.Online || String.IsNullOrEmpty(moviePath))
            {
                // Show the Collection Number to the user.
                DialogNotify("Movie collection number", string.Format("Please load movie #{0}", GUIPropertyManager.GetProperty("#collection")));
            }
            else
            {
                GUIWaitCursor.Show();
                try
                {
                    bool initialisedPlaylist = false;

                    // Save the current selection as Now Plying.
                    NowPlaying = SelectedMovie;

                    // We have the path, get all files.
                    string[] movieFiles = Directory.GetFiles(moviePath);

                    foreach (string file in movieFiles)
                    {
                        if (VirtualDirectory.IsImageFile(Path.GetExtension(file)) ||
                            (VirtualDirectory.IsValidExtension(file, MediaPortal.Util.Utils.VideoExtensions, false)))
                        {
                            AddPlayListItem(!initialisedPlaylist, file, Path.GetFileName(file));
                            initialisedPlaylist = true;
                        }
                    }
                    GUIWaitCursor.Hide();
                    PlayMovieFromPlayList(true);
                }
                catch (Exception ex)
                {
                    Log.Error("MyMovies::OnPlayDvd - Error playing movie");
                    Log.Error(ex);

                    GUIWaitCursor.Hide();
                    GUIDialogOK dlgOk = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
                    dlgOk.SetHeading(342);//Movies
                    dlgOk.SetLine(1, 477);//unable to load playlist
                    dlgOk.DoModal(GetID);
                }
            }
        }

        /// <summary>
        /// Setup the required attributes for the Playing DVD and playlist.
        /// </summary>
        /// <param name="drive">The drive letter being played.</param>
        /// <returns>true if the drive contains a valid disk and is playing</returns>
        public bool OnPlayDvd(string drive)
        {
            try
            {
                if (MediaPortal.Util.Utils.getDriveType(drive) == 5) //cd or dvd drive
                {
                    string videoFile = null;
                    if (VideoDisk(drive, ref videoFile))
                    {
                        GUIListItemMovie diskMovie = new GUIListItemMovie();
                        diskMovie.Path = videoFile;
                        diskMovie.Label = MediaPortal.Util.Utils.GetDriveName(drive);
                        diskMovie.ItemId = -1;                          // Only remember one item for actual disks.
                        diskMovie.MountedFileName = diskMovie.Label;    // Stored for resume.

                        NowPlaying = diskMovie;

                        AddPlayListItem(true, videoFile, diskMovie.Label);               // Add the single item

                        PlayMovieFromPlayList(true);

                        return true;
                    }

                    DialogNoDisk();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            return false;
        }

        /// <summary>
        /// Check the provided drive for one of the following video disks
        /// DVD, SVCD, or VCD
        /// </summary>
        /// <param name="drive">Drive letter</param>
        /// <param name="videoFile">output video file if compatible</param>
        /// <returns>true if compatible vidoe disk is found</returns>
        private bool VideoDisk(string drive, ref string videoFile)
        {
            bool videoDiskFound = true;

            string driveLetter = drive.Substring(0, 1);

            string dvdfileName = String.Format("{0}:{1}", driveLetter, Constants.FileNames.DVD);
            string vcdfileName = String.Format("{0}:{1}", driveLetter, Constants.FileNames.VCD);
            string svcdfileName = String.Format("{0}:{1}", driveLetter, Constants.FileNames.SVCD);

            if (File.Exists(dvdfileName))
            {
                videoFile = dvdfileName;
            }
            else if (File.Exists(vcdfileName))
            {
                videoFile = vcdfileName;
            }
            else if (File.Exists(svcdfileName))
            {
                videoFile = svcdfileName;
            }
            else
            {
                videoDiskFound = false;
            }
            return videoDiskFound;
        }


        /// <summary>
        /// Check all CD / DVD drives for a valid disk to play.
        /// <remarks>This is required for the MyMovies DVD Handler</remarks>
        /// </summary>
        /// <returns></returns>
        public string OnSelectDvd()
        {
            try
            {
                //check if dvd is inserted
                List<GUIListItem> disks = new List<GUIListItem>();

                // Add all drive entries that have a DVD, VCD or SVCD disk inserted.
                string[] drives = Environment.GetLogicalDrives();
                foreach (string drive in drives)
                {
                    if (MediaPortal.Util.Utils.getDriveType(drive) == 5) //cd or dvd drive
                    {
                        string videoFileName = null;

                        if (VideoDisk(drive, ref videoFileName))
                        {
                            string driveName = MediaPortal.Util.Utils.GetDriveName(drive);
                            if (driveName == "")
                            {
                                driveName = GUILocalizeStrings.Get(1061);
                            }

                            GUIListItem item = new GUIListItem();
                            item.Path = drive.Substring(0, 1).ToUpper() + ":"; ;
                            item.Label = String.Format("({0}) {1}", item.Path, driveName);
                            item.IsFolder = true;

                            Utils.SetDefaultIcons(item);

                            disks.Add(item);
                        }
                    }
                }

                if (disks.Count.Equals(0))
                {
                    DialogNoDisk();
                }
                else if (disks.Count == 1)
                {
                    return disks[0].Path; // Only one DVD available, play it!
                }
                else
                {
                    int selectedIndex = DialogSelect(0, disks, GUILocalizeStrings.Get(196));
                    if (selectedIndex >= 0)
                    {
                        return disks[selectedIndex].Path;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            return null;
        }

        #endregion
    }
}
