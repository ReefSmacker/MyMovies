using System;
using System.Collections.Generic;
using System.Text;

using MediaPortal.GUI.Library;
using System.Globalization;

namespace MyMovies
{
    public class Sorting // : IComparer<GUIListItem>
    {
        /// <summary>
        /// Enumeration of the possible sort options.
        /// </summary>
        public enum Options
        {
            [StringEnum("Sort by: Name")]
            Name = 0,

            [StringEnum("Sort by: Last played")]
            LastPlayed,

            [StringEnum("Sort by: Date added")]
            DateAdded,

            [StringEnum("Sort by: Year")]
            Year,

            [StringEnum("Sort by: Rating")]
            Rating,

            [StringEnum("Sort by: DVD#")]
            DVD,

            [StringEnum("Sort by: Duration")]
            Duration,

            [StringEnum("Sort by: Label")]
            Label,
        }

        protected Options _currentSortOption;
        protected bool _sortAscending;

        public Sorting(Options currentSortOption, bool ascending)
        {
            _sortAscending      = ascending;
            _currentSortOption  = currentSortOption;
        }

        //public int Compare(GUIListItem item1, GUIListItem item2)
        //{
        //    if (item1 == item2) return 0;
        //    if (item1 == null) return -1;
        //    if (item2 == null) return -1;
        //    if (item1.IsFolder && item1.Label == "..") return -1;
        //    if (item2.IsFolder && item2.Label == "..") return -1;
        //    if (item1.IsFolder && !item2.IsFolder) return -1;
        //    else if (!item1.IsFolder && item2.IsFolder) return 1;


        //    switch (_currentSortOption)
        //    {
        //        case Options.Year:
        //            {
        //                if (_sortAscending)
        //                {
        //                    if (item1.Year > item2.Year) return 1;
        //                    if (item1.Year < item2.Year) return -1;
        //                }
        //                else
        //                {
        //                    if (item1.Year > item2.Year) return -1;
        //                    if (item1.Year < item2.Year) return 1;
        //                }
        //                return 0;
        //            }
        //        case Options.Rating:
        //            {
        //                if (_sortAscending)
        //                {
        //                    if (item1.Rating > item2.Rating) return 1;
        //                    if (item1.Rating < item2.Rating) return -1;
        //                }
        //                else
        //                {
        //                    if (item1.Rating > item2.Rating) return -1;
        //                    if (item1.Rating < item2.Rating) return 1;
        //                }
        //                return 0;
        //            }

        //        case Options.Name:

        //            if (_sortAscending)
        //            {
        //                return String.Compare(item1.Label, item2.Label, true);
        //            }
        //            else
        //            {
        //                return String.Compare(item2.Label, item1.Label, true);
        //            }

        //        case Options.Label:
        //            if (_sortAscending)
        //            {
        //                return String.Compare(item1.DVDLabel, item2.DVDLabel, true);
        //            }
        //            else
        //            {
        //                return String.Compare(item2.DVDLabel, item1.DVDLabel, true);
        //            }
        //        case Options.Duration:
        //            if (item1.FileInfo == null || item2.FileInfo == null)
        //            {
        //                if (_sortAscending)
        //                {
        //                    return (int)(item1.Duration - item2.Duration);
        //                }
        //                else
        //                {
        //                    return (int)(item2.Duration - item1.Duration);
        //                }
        //            }
        //            else
        //            {
        //                if (_sortAscending)
        //                {
        //                    return (int)(item1.FileInfo.Length - item2.FileInfo.Length);
        //                }
        //                else
        //                {
        //                    return (int)(item2.FileInfo.Length - item1.FileInfo.Length);
        //                }
        //            }



        //        //case Options.DateAdded:
        //        //    if (item1.FileInfo == null) return -1;
        //        //    if (item2.FileInfo == null) return -1;

        //        //    item1.Label2 = item1.FileInfo.ModificationTime.ToShortDateString() + " " + item1.FileInfo.CreationTime.ToString("t", CultureInfo.CurrentCulture.DateTimeFormat);
        //        //    item2.Label2 = item2.FileInfo.ModificationTime.ToShortDateString() + " " + item2.FileInfo.CreationTime.ToString("t", CultureInfo.CurrentCulture.DateTimeFormat);
        //        //    if (_sortAscending)
        //        //    {
        //        //        return DateTime.Compare(item1.FileInfo.ModificationTime, item2.FileInfo.ModificationTime);
        //        //    }
        //        //    else
        //        //    {
        //        //        return DateTime.Compare(item2.FileInfo.ModificationTime, item1.FileInfo.ModificationTime);
        //        //    }


        //    }
        //    return 0;
        //}
    }
}
