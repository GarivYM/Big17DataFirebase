using System;
using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Big17DataFirebase2.Adapters;
using Big17DataFirebase2.Model;
using Big17DataFirebase2.Service;
using Firebase.Firestore;

namespace Big17DataFirebase2
{
    [Activity(Label = "ListActivity" , MainLauncher = false)]
    public class ListActivity : Activity
    {
        // UI
        TextView tvDelete, tvBar, tvTitle;
        RecyclerView recyclerView;

        // RecyclerView
        RecyclerView.LayoutManager layoutManager;
        UsersRViewAdapter adapter;

        // Data
        List<User> users;
        Dialog mProgressDialog;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.listlayout);

            InitializeViews();
        }

        private void InitializeViews()
        {
            tvDelete = FindViewById<TextView>(Resource.Id.tvDelete);
            tvBar = FindViewById<TextView>(Resource.Id.tvBar);
            tvTitle = FindViewById<TextView>(Resource.Id.tvTitle);
            recyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);

            tvTitle.Text = "List Page";

            tvDelete.Click += TvDelete_Click;
            tvBar.Click += TvBar_Click;

            // RecyclerView setup
            layoutManager = new LinearLayoutManager(this);
            recyclerView.SetLayoutManager(layoutManager);

            users = new List<User>();
            adapter = new UsersRViewAdapter(this, users);

            // ❌ NO ItemClick here

            recyclerView.SetAdapter(adapter);
        }

        protected override void OnResume()
        {
            base.OnResume();

            ShowProgressBar(true);
            FetchUsersFromDB();
        }

        protected override void OnPause()
        {
            base.OnPause();
            FireBaseHelper.StopUsersListener();
        }

        private void FetchUsersFromDB()
        {
            FireBaseHelper.FetchUsersListener();

            FireBaseHelper.FirestoreEventListener.getEvent += (error, args) =>
            {
                ShowProgressBar(false);

                if (users != null)
                    users.Clear();
                else
                    users = new List<User>();

                try
                {
                    var snapshot = (QuerySnapshot)args.Result;

                    if (!snapshot.IsEmpty)
                    {
                        foreach (DocumentSnapshot item in snapshot.Documents)
                        {
                            User _user = new User()
                            {
                                Id = item.Id,
                                FirstName = item.Get("FirstName").ToString(),
                                LastName = item.Get("LastName").ToString(),
                                UserEmail = item.Get("UserEmail").ToString(),
                                UserMobile = item.Get("UserMobile").ToString(),
                                UserPass = item.Get("UserPassword").ToString(),
                                IsAdmin = bool.Parse(item.Get("IsAdmin").ToString()),
                                ImageId = Resource.Drawable.maleicon
                            };

                            users.Add(_user);
                        }

                        adapter.NotifyDataSetChanged();
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug("ListActivity", ex.Message);
                }
            };
        }

        private void ShowProgressBar(bool show)
        {
            if (show)
            {
                mProgressDialog = new Dialog(this, Android.Resource.Style.ThemeNoTitleBar);
                View view = LayoutInflater.From(this).Inflate(Resource.Layout.fb_progressbar, null);

                mProgressDialog.Window.SetBackgroundDrawableResource(Resource.Color.mtrl_btn_transparent_bg_color);
                mProgressDialog.SetContentView(view);
                mProgressDialog.SetCancelable(false);
                mProgressDialog.Show();
            }
            else
            {
                mProgressDialog?.Dismiss();
            }
        }

        private void TvDelete_Click(object sender, EventArgs e)
        {
            Toast.MakeText(this, "Delete clicked", ToastLength.Short).Show();

            // Example: clear UI list (NOT Firebase)
            users.Clear();
            adapter.NotifyDataSetChanged();
        }

        private void TvBar_Click(object sender, EventArgs e)
        {
            Toast.MakeText(this, "BAR clicked", ToastLength.Short).Show();
        }
    }
}