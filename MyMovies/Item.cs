using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyMovies
{
    /// <summary>
    /// Specific type of pair that provides an Int and String
    /// </summary>
    public class Item : Pair<int, string>
    {
        public Item() {
        }

        public Item(int id)
            : this(id, string.Empty)
        {
        }

        public Item(int id, string name)
            : base(id, name)
        {
        }

        public static bool operator < (Item item, int compare)
        {
            return item.First < compare;
        }
        public static bool operator >(Item item, int compare)
        {
            return item.First > compare;
        }
    }

    public class CheckedItem : Pair<Item, bool>
    {
        public CheckedItem(Item item)
            : this(item, false)
        {
        }

        public CheckedItem(Item item, bool itemChecked)
            : base(item, itemChecked)
        {
        }
    }
}
