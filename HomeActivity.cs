using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Big17DataFirebase2.Adapters;
using Big17DataFirebase2.BusinessLogic;
using Big17DataFirebase2.Model;
using Big17DataFirebase2.Service;
using Firebase.Firestore;

namespace Big17DataFirebase2
{
    [Activity(Label = "HomeActivity")]

    public class HomeActivity : Activity
    {
        // RecyclerView
        RecyclerView recyclerView;
        RecyclerView.LayoutManager layoutManager;
        UsersRViewAdapter userAdapter;

        // UI
        TextView tvJoin, tvAdd, tvTitle, tvUserslist;

        // Data
        List<User> users;
        Dialog mProgressDialog;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.homepagelayout);

            InitializeViews();
        }

        private void InitializeViews()
        {
            // Bind views
            tvJoin = FindViewById<TextView>(Resource.Id.tvJoin);
            tvAdd = FindViewById<TextView>(Resource.Id.tvAdd);
            tvTitle = FindViewById<TextView>(Resource.Id.tvTitle);
            tvUserslist = FindViewById<TextView>(Resource.Id.tvUserslist);
            recyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);

            // Title
            tvTitle.Text = "Home Page";

            // Clicks
            tvJoin.Click += TvJoin_Click;
            tvAdd.Click += TvAdd_Click;

            // RecyclerView setup (SAME as MainPage)
            layoutManager = new LinearLayoutManager(this);
            recyclerView.SetLayoutManager(layoutManager);

            users = new List<User>();
            userAdapter = new UsersRViewAdapter(this, users);

            userAdapter.ItemClick += OnItemClick;

            recyclerView.SetAdapter(userAdapter);
        }

        private void OnItemClick(object sender, int position)
        {
            Intent intent = new Intent(this, typeof(ListActivity));

            // Pass data if needed
            intent.PutExtra("userID", users[position].Id);

            StartActivity(intent);
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

                        userAdapter.NotifyDataSetChanged();
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug(ProManager.TAG, ex.Message);
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

        private void TvJoin_Click(object sender, EventArgs e)
        {
            Toast.MakeText(this, "Join clicked", ToastLength.Short).Show();
        }

        private void TvAdd_Click(object sender, EventArgs e)
        {
            Toast.MakeText(this, "Add clicked", ToastLength.Short).Show();
        }
    }
}