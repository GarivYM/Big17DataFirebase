using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Big17DataFirebase2.Service
{
    // This class tells the Android system how to handle the Firebase callback in C#
    public class MyEventListener : Java.Lang.Object, IEventListener
    {
        private readonly Action<Java.Lang.Object, FirebaseFirestoreException> _onEvent;

        public MyEventListener(Action<Java.Lang.Object, FirebaseFirestoreException> onEvent)
        {
            _onEvent = onEvent;
        }

        public void OnEvent(Java.Lang.Object value, FirebaseFirestoreException error)
        {
            _onEvent?.Invoke(value, error);
        }
    }
}