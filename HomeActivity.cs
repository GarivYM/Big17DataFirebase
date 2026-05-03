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
using Firebase.Auth;
using Firebase.Firestore;
using Google.Android.Material.FloatingActionButton;
using System;
using System.Collections.Generic;

namespace Big17DataFirebase2
{
    [Activity(Label = "HomeActivity", MainLauncher = false)]
    public class HomeActivity : Activity
    {
        // RecyclerView Components
        RecyclerView recyclerView;
        RecyclerView.LayoutManager layoutManager;
        ListsRViewAdapter listAdapter;

        // UI Elements
        TextView tvUserFullName, tvTitle, tvLists;
        FloatingActionButton fabAdd;

        // Data
        List<Big17DataFirebase2.Model.List> lists;
        Dialog mProgressDialog;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set the layout - Ensure this matches your filename exactly
            SetContentView(Resource.Layout.homepagelayout);

            InitializeViews();
        }

        private void InitializeViews()
        {
            // 1. Link Top Bar UI
            tvUserFullName = FindViewById<TextView>(Resource.Id.tvUserFullName);
            tvTitle = FindViewById<TextView>(Resource.Id.tvTitle);
            tvLists = FindViewById<TextView>(Resource.Id.tvLists);

            // 2. Setup Floating Action Button
            fabAdd = FindViewById<FloatingActionButton>(Resource.Id.fabAdd);
            fabAdd.Click += (s, e) => {
                // Instead of jumping to another activity, show your dialog!
                TvAdd_Click(s, e);
            };

            // 3. Setup RecyclerView
            recyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);
            layoutManager = new LinearLayoutManager(this);
            recyclerView.SetLayoutManager(layoutManager);

            lists = new List<Big17DataFirebase2.Model.List>();
            listAdapter = new ListsRViewAdapter(lists);
            listAdapter.ItemClick += OnItemClick;
            recyclerView.SetAdapter(listAdapter);
        }

        private void OnItemClick(object sender, int position)
        {
            // Transfer to the specific list details
            var selectedList = lists[position];
            Intent intent = new Intent(this, typeof(ListActivity));
            intent.PutExtra("listId", selectedList.Id);
            intent.PutExtra("listTitle", selectedList.Title);
            intent.PutExtra("ownerId", selectedList.OwnerId);
            StartActivity(intent);
        }

        protected override void OnResume()
        {
            base.OnResume();

            // Safety Check & User Name Display
            if (ProManager.CurrentUser != null)
            {
                tvUserFullName.Text = $"{ProManager.CurrentUser.FirstName} {ProManager.CurrentUser.LastName}";
                tvTitle.Text = "Home Page";
            }
            else
            {
                // If session is lost, bounce back to login
                StartActivity(typeof(SignInActivity));
                Finish();
                return;
            }

            ShowProgressBar(true);
            FetchListsFromDB();
        }

        protected override void OnPause()
        {
            base.OnPause();
            FireBaseHelper.StopListsListener();
        }
        private void TvAdd_Click(object sender, EventArgs e)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetTitle("Create New List");

            EditText input = new EditText(this);
            input.Hint = "Enter list name (e.g., Groceries)";
            builder.SetView(input);

            builder.SetPositiveButton("Create", async (s, args) =>
            {
                string listName = input.Text.Trim();

                if (!string.IsNullOrEmpty(listName))
                {
                    ShowProgressBar(true);

                    string currentUserId = FirebaseAuth.Instance.CurrentUser.Uid;

                    // Call the helper
                    bool success = await FireBaseHelper.CreateList(listName, currentUserId, "Standard");

                    ShowProgressBar(false);

                    if (success)
                    {
                        Toast.MakeText(this, $"List '{listName}' created!", ToastLength.Short).Show();
                        // No need to manually refresh; your FetchListsListener will 
                        // automatically see the new data and update the RecyclerView!
                    }
                    else
                    {
                        Toast.MakeText(this, "Failed to create list. Try again.", ToastLength.Long).Show();
                    }
                }
            });

            builder.SetNegativeButton("Cancel", (s, args) => { });
            builder.Show();
        }
        private void FetchListsFromDB()
        {
            FireBaseHelper.FetchListsListener();

            FireBaseHelper.listener.getEvent += (error, args) =>
            {
                ShowProgressBar(false);

                if (lists == null) lists = new List<Big17DataFirebase2.Model.List>();
                lists.Clear();

                try
                {
                    var snapshot = (QuerySnapshot)args.Result;
                    string currentUserId = FirebaseAuth.Instance.CurrentUser.Uid;

                    foreach (DocumentSnapshot item in snapshot.Documents)
                    {
                        string ownerId = item.Get("ownerId")?.ToString();

                        // Handle potential null sharedWith arrays
                        var sharedWith = item.Get("sharedWith") as Java.Util.ArrayList;
                        bool isSharedWithMe = sharedWith != null && sharedWith.Contains(currentUserId);

                        // Logic: Only show lists relevant to the user
                        if (ownerId == currentUserId || isSharedWithMe)
                        {
                            var listObj = new Big17DataFirebase2.Model.List()
                            {
                                Id = item.Id,
                                Title = item.Get("title")?.ToString() ?? "Untitled List",
                                OwnerId = ownerId
                            };
                            lists.Add(listObj);
                        }
                    }

                    listAdapter.NotifyDataSetChanged();
                }
                catch (Exception ex)
                {
                    Log.Debug("HomeActivity", "Error fetching lists: " + ex.Message);
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
    }
}