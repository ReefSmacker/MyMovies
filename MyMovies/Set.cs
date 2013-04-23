using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyMovies
{
    public class Set
    {
        public delegate string OnIterateHandler(int data, string description);
        public event OnIterateHandler OnIterate;

        private Dictionary<int, string> _setData = new Dictionary<int, string>();

        public int Count
        {
            get { return _setData.Count; }
        }

        public void Add(int data)
        {
            Add(data, string.Empty);
        }
        public void Add(int data, string description)
        {
            if (!_setData.Contains(new KeyValuePair<int, string>(data, description)))
            {
                _setData.Add(data, description);
            }
        }

        public void Remove(int data)
        {
            _setData.Remove(data);
        }

        public bool Contains(int data, string description)
        {
            return _setData.Contains(new KeyValuePair<int, string>(data, description));
        }
        public bool Contains(string description)
        {
            return _setData.ContainsValue(description);
        }
        public bool Contains(int data)
        {
            return _setData.ContainsKey(data);
        }

        public void Clear()
        {
            _setData.Clear();
        }

        public string ToCSV()
        {
            bool first = true;
            StringBuilder csv = new StringBuilder();
            foreach (KeyValuePair<int, string> keyValuePair in _setData)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    csv.Append(", ");
                }
                csv.Append((OnIterate == null) ? keyValuePair.Key.ToString() : OnIterate(keyValuePair.Key, keyValuePair.Value));
            }
            return csv.ToString();
        }
    }
}
