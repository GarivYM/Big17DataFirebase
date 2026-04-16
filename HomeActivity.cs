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
using Big17DataFirebase2.Model;
using Big17DataFirebase2.Service;
using Firebase.Auth;
using Firebase.Firestore;

namespace Big17DataFirebase2
{
    [Activity(Label = "HomeActivity" , MainLauncher = true)]
    public class HomeActivity : Activity
    {
        // RecyclerView
        RecyclerView recyclerView;
        RecyclerView.LayoutManager layoutManager;
        ListsRViewAdapter listAdapter;

        // UI
        TextView tvJoin, tvAdd, tvTitle, tvUserslist;

        // Data
        List<List> lists;
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
            tvJoin = FindViewById<TextView>(Resource.Id.tvJoin);
            tvAdd = FindViewById<TextView>(Resource.Id.tvAdd);
            tvTitle = FindViewById<TextView>(Resource.Id.tvTitle);
            tvUserslist = FindViewById<TextView>(Resource.Id.tvUserslist);
            recyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);

            tvTitle.Text = "My Lists";

            tvJoin.Click += TvJoin_Click;
            tvAdd.Click += TvAdd_Click;

            layoutManager = new LinearLayoutManager(this);
            recyclerView.SetLayoutManager(layoutManager);

            lists = new List<List>();
            listAdapter = new ListsRViewAdapter(lists);

            listAdapter.ItemClick += OnItemClick;

            recyclerView.SetAdapter(listAdapter);
        }

        private void OnItemClick(object sender, int position)
        {
            Intent intent = new Intent(this, typeof(ListActivity));

            intent.PutExtra("listId", lists[position].Id);

            StartActivity(intent);
        }

        protected override void OnResume()
        {
            base.OnResume();

            ShowProgressBar(true);
            FetchListsFromDB();
        }

        protected override void OnPause()
        {
            base.OnPause();
            FireBaseHelper.StopListsListener();
        }

        private void FetchListsFromDB()
        {
            FireBaseHelper.FetchListsListener();

            FireBaseHelper.listener.getEvent += (error, args) =>
            {
                ShowProgressBar(false);

                if (lists == null)
                    lists = new List<List>();

                lists.Clear();

                try
                {
                    var snapshot = (QuerySnapshot)args.Result;

                    string currentUserId =
                        FirebaseAuth.Instance.CurrentUser.Uid;

                    foreach (DocumentSnapshot item in snapshot.Documents)
                    {
                        string ownerId = item.Get("ownerId").ToString();

                        if (ownerId == currentUserId)
                        {
                            List list = new List()
                            {
                                Id = item.Id,
                                Title = item.Get("title").ToString(),
                                OwnerId = ownerId
                            };

                            lists.Add(list);
                        }
                    }

                    listAdapter.NotifyDataSetChanged();
                }
                catch (Exception ex)
                {
                    Log.Debug("HomeActivity", ex.Message);
                }
            };
        }

        private void ShowProgressBar(bool show)
        {
            if (show)
            {
                mProgressDialog = new Dialog(this, Android.Resource.Style.ThemeNoTitleBar);
                View view = LayoutInflater.From(this).Inflate(Resource.Layout.fb_progressbar, null);

                mProgressDialog.Window.SetBackgroundDrawableResource(Android.Resource.Color.Transparent);
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