using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Big17DataFirebase2.BusinessLogic;
using Big17DataFirebase2.Model;
using Big17DataFirebase2.Service;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Big17DataFirebase2
{
    [Activity(Label = "MainPage")]
    public class MainPage : Activity
    {
        TextView tvusername, tvisadmin, tvuserslist;
        Button btnRemoveUser;
        Dialog mProgressDialog;
        List<User> users;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.mainpage_layout);

            InitializeViews();
        }

        private void InitializeViews()
        {
            tvusername = FindViewById<TextView>(Resource.Id.tvUsername);
            tvisadmin = FindViewById<TextView>(Resource.Id.tvIsAdmin);
            tvuserslist = FindViewById<TextView>(Resource.Id.tvUserslist);
            btnRemoveUser = FindViewById<Button>(Resource.Id.btnUserRemove);
        }

        protected override void OnResume()
        {
            base.OnResume();
            tvusername.Text = ProManager.CurrentUser.FirstName;

            if (ProManager.CurrentUser.IsAdmin)
                tvisadmin.Visibility = ViewStates.Visible;

            ShowProgressBar(true);
            //GetUsersFromDB();
            FetchUsersFromDB();
        }

        protected override void OnPause()
        {
            base.OnPause();
            FireBaseHelper.FirestoreEventListener = null;
        }

        private void FetchUsersFromDB()
        {
            FireBaseHelper.FetchUsersListener();
            FireBaseHelper.FirestoreEventListener.getEvent += (error, args) =>
            {
                ShowProgressBar(false);
                if (users != null)
                    users.Clear(); //Clear users list
                else
                    users = new List<User>();

                var snapshot = (QuerySnapshot)args.Result;
                if (!snapshot.IsEmpty)
                {
                    var documents = snapshot.Documents;
                    foreach (DocumentSnapshot item in documents)
                    {
                        User _user = new User()
                        {
                            Id = item.Id,
                            FirstName = item.Get("FirstName").ToString(),
                            LastName = item.Get("LastName").ToString(),
                            UserEmail = item.Get("UserEmail").ToString(),
                            UserMobile = item.Get("UserMobile").ToString(),
                            UserPass = item.Get("UserPassword").ToString(),
                            IsAdmin = bool.Parse(item.Get("IsAdmin").ToString())
                        };
                        users.Add(_user);
                    }
                    FillUsersList();
                }
            };
        }

        private async void GetUsersFromDB()
        {            
            users = await FireBaseHelper.GetUsersCollection();
            if(users.Count > 0)
            {
                FillUsersList();
            }
            else
            {
                tvuserslist.Text = "Users collection is empty...";
            }
            ShowProgressBar(false);
        }

        private void FillUsersList()
        {
            tvuserslist.Text = "Users List:";
            foreach (var user in users)
            {
                tvuserslist.Text += $"\n{user.FirstName} {user.UserEmail}";
            }
        }

        private void ShowProgressBar(bool show)
        {
            //android:background="@android:color/transparent"

            if (show)
            {
                mProgressDialog = new Dialog(this, Android.Resource.Style.ThemeNoTitleBar);
                View view = LayoutInflater.From(this).Inflate(Resource.Layout.fb_progressbar, null);
                //var mProgressMessage = (TextView)view.FindViewById(Resource.Id.;
                //mProgressMessage.Text = "Loading...";
                mProgressDialog.Window.SetBackgroundDrawableResource(Resource.Color.mtrl_btn_transparent_bg_color);
                mProgressDialog.SetContentView(view);
                mProgressDialog.SetCancelable(false);
                mProgressDialog.Show();
            }
            else
            {
                mProgressDialog.Dismiss();
            }
        }
    }
}