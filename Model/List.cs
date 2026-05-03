using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Big17DataFirebase2.Model
{
    public class List
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string OwnerId { get; set; }

        // Add these to match your flowchart logic:
        public string ListCode { get; set; } // The 6-digit code for "Join List"
        public List<string> SharedWith { get; set; } = new List<string>(); // IDs of users who joined
        public string Type { get; set; } // e.g., "Standard" or "Checklist"
    }
}