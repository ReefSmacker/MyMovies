using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.GUI.Library;

namespace MyMovies
{
    public class Filter
    {
        DialogCheckedList _checkedList = (DialogCheckedList)GUIWindowManager.GetWindow((int)MyMovies.GUIWindowID.CheckedDialog);
        Set _currentSet;

        /// <summary>
        /// Construct a filter dialog with Checked options.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="currentSet"></param>
        /// <param name="availableItems"></param>
        public Filter(int label, Set currentSet, List<Item> availableItems)
            : this(GUILocalizeStrings.Get(label), currentSet, availableItems)
        {
        }
        public Filter(string label, Set currentSet, List<Item> availableItems)
        {
            _currentSet = currentSet;

            if (_checkedList != null)
            {
                _checkedList.Reset();
                _checkedList.SetHeading(label);

                foreach (Item item in availableItems)
                {
                    // if the item is currently selected, add it as selected.
                    _checkedList.Add(new CheckedItem(item, currentSet.Contains(item.First)));
                }
            }        
        }

        public DialogCheckedList Dialog
        {
            get { return _checkedList; }
        }

        public void DoModal()
        {
            DoModal(false);
        }
        public void DoModal(bool addDescription)
        {
            // show dialog and wait for result
            _checkedList.DoModal(GUIWindowManager.ActiveWindow);
            if (_checkedList.DialogResult)
            {
                foreach (CheckedItem checkedItem in _checkedList.Items)
                {
                    if (checkedItem.Second)
                    {
                        _currentSet.Add(checkedItem.First.First, addDescription ? checkedItem.First.Second : string.Empty);
                    }
                    else
                    {
                        _currentSet.Remove(checkedItem.First.First);
                    }
                }
            }
        }
    }
}
