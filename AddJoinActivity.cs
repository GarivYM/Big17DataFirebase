using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Big17DataFirebase2.Service;
using Firebase.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Big17DataFirebase2
{
    [Activity(Label = "Add or Join List")]
    public class AddJoinActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.add_join_layout);

            var btnCreate = FindViewById<Button>(Resource.Id.btnCreate);
            var etListName = FindViewById<EditText>(Resource.Id.etListName);
            var spinnerType = FindViewById<Spinner>(Resource.Id.spinnerType);

            btnCreate.Click += async (s, e) => {
                string name = etListName.Text;
                string type = spinnerType.SelectedItem.ToString();
                string userId = FirebaseAuth.Instance.CurrentUser.Uid;

                if (!string.IsNullOrEmpty(name))
                {
                    await FireBaseHelper.CreateList(name, userId, type);
                    Finish(); // This takes you back to the Home Page automatically!
                }
            };

            // Add your Join logic here using FireBaseHelper.JoinListByCode...
        }
    }
}