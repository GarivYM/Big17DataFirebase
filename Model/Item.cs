using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Big17DataFirebase2.Model
{
    public class Item
    {
        public string Id { get; set; }        // The Firestore Document ID
        public string Name { get; set; }      // The name of the item (e.g. "Milk")
        public bool IsChecked { get; set; }   // Whether the item is checked off

        // Constructor is optional but helpful for quick creation
        public Item() { }

        public Item(string id, string name, bool isChecked)
        {
            Id = id;
            Name = name;
            IsChecked = isChecked;
        }
    }
}