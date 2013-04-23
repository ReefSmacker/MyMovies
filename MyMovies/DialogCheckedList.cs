using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;

namespace MyMovies
{
    public class DialogCheckedList : GUIDialogWindow
    {
        [SkinControl(1)]    protected GUILabelControl   lblHeading = null;
        [SkinControl(2)]    protected GUIButtonControl  btnOK = null;               // Closes the dialog
        [SkinControl(3)]    protected GUIButtonControl  btnSelectAll = null;
        [SkinControl(4)]    protected GUIButtonControl  btnSelectNone = null;
        [SkinControl(5)]    protected GUIButtonControl  btnCancel = null;           // Closes the dialog
        [SkinControl(10)]   protected GUIListControl    chkList = null;

        bool _result = false;
        List<CheckedItem> _items = new List<CheckedItem>();

        public delegate void OnCheckedHandler(CheckedItem item, DialogCheckedList thisDialog);
        public event OnCheckedHandler OnChecked;

        public DialogCheckedList()
        {
            GetID = (int)GUIWindowID.CheckedDialog;
        }

        public void SetHeading(int iString)    
        {      
            SetHeading(GUILocalizeStrings.Get(iString));   
        }

        public bool DialogResult
        {
            get { return _result; }
        }

        /// <summary>
        /// Causes the skin to be loaded early.
        /// </summary>
        public override bool SupportsDelayedLoad
        {
            get { return false; }
        }

        public void SetHeading(string line)    
        {      
            //LoadSkin();      
            AllocResources();      
            InitControls();
            SetControlLabel(GetID, (int)lblHeading.GetID, line);
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.GetThemedSkinFile(@"\DialogCheckedList.xml"));
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();
            Update();
        }

        public void Add(CheckedItem checkedItem)
        {
            _items.Add(checkedItem);
        }

        /// <summary>
        /// Get the subset of checked items
        /// </summary>
        public System.Collections.ObjectModel.ReadOnlyCollection<CheckedItem> Items
        {
            get
            {
                return _items.AsReadOnly();
            }
        }

        private void Update()
        {
            chkList.Clear();

            foreach (CheckedItem item in _items)
            {
                GUIListItem listItem = new GUIListItem();
                listItem.Label = item.First.Second;
                listItem.IsFolder = false;
                listItem.IsRemote = false;
                listItem.Selected = false;
                listItem.TVTag = item;                  // Allows updating of the item
                listItem.PinImage = (item.Second) ? "checkmark_checked.png" : "checkmark_unchecked.png";
                chkList.Add(listItem);
            }
        }

        public override void Reset()
        {
            base.Reset();
            chkList.Clear();
            _items.Clear();
            _result = false;
        }

        protected override void OnClicked(int controlId, GUIControl control, MediaPortal.GUI.Library.Action.ActionType actionType)
        {
            if (control == chkList)
            {
                OnClickedItem(chkList.SelectedListItem);
            }
            else if (control == btnOK)
            {
                _result = true;
                PageDestroy();
            }
            else if (control == btnCancel)
            {
                PageDestroy();
            }
            else if (control == btnSelectAll)
            {
                foreach (CheckedItem item in _items)
                {
                    item.Second = true;
                }
                Update();
            }
            else if (control == btnSelectNone)
            {
                foreach (CheckedItem item in _items)
                {
                    item.Second = false;
                }
                Update();
            }
            base.OnClicked(controlId, control, actionType);
        }

        private void OnClickedItem(GUIListItem gUIListItem)
        {
            CheckedItem checkedItem = gUIListItem.TVTag as CheckedItem;
            checkedItem.Second = !checkedItem.Second;
            gUIListItem.PinImage = (checkedItem.Second) ? "checkmark_checked.png" : "checkmark_unchecked.png";
            if (OnChecked != null)
            {
                OnChecked(checkedItem, this);
            }
        }

        public override bool OnMessage(GUIMessage message)
        {
            switch (message.Message)
            {
                case GUIMessage.MessageType.GUI_MSG_WINDOW_DEINIT:
                    {
                        base.OnMessage(message);
                        DeInitControls();
                        Dispose();
                        return true;
                    }

                case GUIMessage.MessageType.GUI_MSG_WINDOW_INIT:
                    {
                        base.OnMessage(message);
                        GUIControl.FocusControl(GetID, btnOK.GetID);
                    }
                    return true;
            }

            return base.OnMessage(message);
        }

        internal void SetChecked(int itemID, bool check)
        {
            for(int i=0; i < chkList.Count; i++)
            {
                CheckedItem checkedItem = chkList[i].TVTag as CheckedItem;
                if (checkedItem != null)
                {
                    if (checkedItem.First.First == itemID)
                    {
                        if (checkedItem.Second != check)
                        {
                            OnClickedItem(chkList[i]);    // this will toggle to the specified item.
                        }
                        break;
                    }
                }
            }
        }
    }
}
