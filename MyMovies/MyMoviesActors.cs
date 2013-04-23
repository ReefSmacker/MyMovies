using System;
using System.Text;
using MediaPortal.GUI.Library;
using System.Collections;
using System.Data.SqlClient;

namespace MyMovies
{
    public class MyMoviesActors : GUIWindow
    {
        MyMoviesPlugin _myMovies = null;

        #region SkinControls

        [SkinControlAttribute(50)]
        protected GUIFacadeControl facadeView = null;

        #endregion

        #region Constants

        const string _movieActorsSQL = @"select P.intId as itemID, nvcCharacter, nvcName, nvcImage, ntxBiography from tblTitlePerson TP
INNER JOIN tblPersons P
	ON P.intId = TP.intPerson
WHERE intTitle = {0} ORDER BY intSort";

        #endregion

        #region Variables

        GUIListItem _currentSelection = null;
        int         _currentSelectionIndex = 0; // Default to the first selection.

        #endregion

        public MyMoviesActors()
        {
            GetID = (int)MyMovies.GUIWindowID.Actors;
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\MyMoviesActors.xml");
        }

        /// <summary>
        /// Determine the currently selected Movie and retrieve all actors.
        /// </summary>
        protected override void OnPageLoad()
        {
            LoadMovieActors(Movies.SelectedMovie.ItemId);

            base.OnPageLoad();
        }

        protected override void OnClicked(int controlId, GUIControl control, MediaPortal.GUI.Library.Action.ActionType actionType)
        {
            if (control == facadeView)
            {
                GUIListItemActor actorItem = _currentSelection as GUIListItemActor;
                if (actorItem != null)
                {
                    Movies.Actor = actorItem.ItemId;
                    GUIWindowManager.ActivateWindow((int)MyMovies.GUIWindowID.Main);
                }
            }

            base.OnClicked(controlId, control, actionType);
        }

        private void LoadMovieActors(int movieID)
        {
            GUIWaitCursor.Show();
            GUIControl.ClearControl(GetID, facadeView.GetID);
            ArrayList itemlist = new ArrayList();

            QueryReader<ArrayList> sql = new QueryReader<ArrayList>(string.Format(_movieActorsSQL, movieID));
            sql.OnRow += new QueryReader<ArrayList>.Row(OnRow_Actor);
            sql.OnConnection += new Sql.ConnectionEvent(Movies.sql_OnConnection);
            sql.Execute(Movies.ConnectionString, itemlist);

            foreach (GUIListItem item in itemlist)
            {
                facadeView.Add(item);
            }
            //set object count label
            GUIPropertyManager.SetProperty("#itemcount", MediaPortal.Util.Utils.GetObjectCountLabel(itemlist.Count));
            GUIPropertyManager.SetProperty("#currentmodule", "MyMovies Artists");
            facadeView.CurrentLayout = GUIFacadeControl.Layout.List;
            facadeView.SelectedListItemIndex = _currentSelectionIndex;
            GUIWaitCursor.Hide();
        }

        /// <summary>
        /// Retrieve the movie details from this record.
        /// </summary>
        /// <param name="row">SqlDataReader for the row being processed</param>
        /// <param name="itemlist">ArrayList of GUIListItem for storage of last added movies</param>
        void OnRow_Actor(SqlDataReader row, ArrayList itemlist)
        {
            GUIListItemActor newItem = new GUIListItemActor();

            newItem.ItemId      = System.Convert.ToInt32(row["itemID"]);
            newItem.Actor       = row["nvcName"].ToString();
            newItem.Character   = row["nvcCharacter"].ToString();
            newItem.Picture     = string.Format(@"{0}\Photos\{1}.jpg", Movies.DataPath, row["nvcImage"].ToString());
            newItem.Biography   = row["ntxBiography"].ToString();

            newItem.Label       = newItem.Character;

            newItem.OnItemSelected += new GUIListItem.ItemSelectedHandler(OnItemSelected);
            newItem.OnRetrieveArt += new GUIListItem.RetrieveCoverArtHandler(newItem_OnRetrieveArt);

            itemlist.Add(newItem);
        }

        void newItem_OnRetrieveArt(GUIListItem item)
        {
            GUIListItemActor movie = item as GUIListItemActor;
            if (movie != null)
            {
                item.ThumbnailImage = movie.Picture;
            }
        }

        void OnItemSelected(GUIListItem item, GUIControl parent)
        {
            if (item != _currentSelection)
            {
                _currentSelection = item;
                _currentSelectionIndex = facadeView.SelectedListItemIndex;
                GUIListItemActor actorItem = SelectedItem;
                if (actorItem != null)
                {
                    GUIPropertyManager.SetProperty("#actor", actorItem.Actor);
                    GUIPropertyManager.SetProperty("#selectedthumb", actorItem.Picture);
                    GUIPropertyManager.SetProperty("#biography", actorItem.Biography);
                    GUIPropertyManager.SetProperty("#selecteditem", actorItem.Character);

                    facadeView.FilmstripLayout.InfoImageFileName = actorItem.Picture;

                    return;
                }
            }
        }

        private GUIListItemActor SelectedItem
        {
            get
            {
                return _currentSelection as GUIListItemActor;
            }
        }

        /// <summary>
        /// Access the main MyMovie plugin class
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
