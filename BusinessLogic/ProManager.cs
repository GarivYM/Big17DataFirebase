using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Big17DataFirebase2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Big17DataFirebase2.BusinessLogic
{
	public class ProManager
	{
		public static bool DebugMode = false;
		public static readonly string TAG = "KOSTYAPP";

		public static User CurrentUser { get; set; }
    }
}