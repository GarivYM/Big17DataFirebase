using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Firebase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Big17DataFirebase2.Model
{
    public class UserListMapping
    {
        public string UserID { get; set; }     // The Person
        public string JoinCode { get; set; }   // The List they belong to
        
    }

}