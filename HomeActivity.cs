using Android.App;
using Android.Content;
using Android.Gms.Extensions;
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
using System.Threading.Tasks;

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
            SetContentView(Resource.Layout.homepagelayout);
            InitializeViews();
        }

        private void InitializeViews()
        {
            tvUserFullName = FindViewById<TextView>(Resource.Id.tvUserFullName);
            tvTitle = FindViewById<TextView>(Resource.Id.tvTitle);
            tvLists = FindViewById<TextView>(Resource.Id.tvLists);

            fabAdd = FindViewById<FloatingActionButton>(Resource.Id.fabAdd);
            fabAdd.Click += (s, e) => {
                TvAdd_Click(s, e);
            };

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

            if (ProManager.CurrentUser != null)
            {
                tvUserFullName.Text = $"{ProManager.CurrentUser.FirstName} {ProManager.CurrentUser.LastName}";
                tvTitle.Text = "Home Page";
            }
            else
            {
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
            View dialogView = LayoutInflater.From(this).Inflate(Resource.Layout.dialog_add_join, null);
            EditText etCreate = dialogView.FindViewById<EditText>(Resource.Id.etListName);
            EditText etJoin = dialogView.FindViewById<EditText>(Resource.Id.etJoinCode);

            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetTitle("Add or Join List");
            builder.SetView(dialogView);

            builder.SetPositiveButton("Confirm", async (s, args) =>
            {
                string createName = etCreate.Text.Trim();
                string joinCode = etJoin.Text.Trim().ToUpper();

                if (!string.IsNullOrEmpty(createName))
                {
                    ShowProgressBar(true);
                    bool success = await FireBaseHelper.CreateList(createName, FirebaseAuth.Instance.CurrentUser.Uid, "Standard");
                    ShowProgressBar(false);

                    if (success)
                    {
                        // Force use of Android.Widget to solve the conversion error
                        RunOnUiThread(() => Android.Widget.Toast.MakeText(this, "List Created!", Android.Widget.ToastLength.Short).Show());
                    }
                }
                else if (!string.IsNullOrEmpty(joinCode))
                {
                    ShowProgressBar(true);
                    await JoinListByCode(joinCode);
                    ShowProgressBar(false);
                }
                else
                {
                    // Force use of Android.Widget here too
                    Android.Widget.Toast.MakeText(this, "Please fill one of the options", Android.Widget.ToastLength.Short).Show();
                }
            });

            builder.SetNegativeButton("Cancel", (s, args) => { });
            builder.Show();
        }

        private async Task JoinListByCode(string code)
        {
            var firestore = FirebaseFirestore.Instance;
            var currentUserId = FirebaseAuth.Instance.CurrentUser.Uid;

            try
            {
                var result = await firestore.Collection("lists").WhereEqualTo("joinCode", code).Get();
                var query = result as QuerySnapshot;

                if (query == null || query.IsEmpty)
                {
                    RunOnUiThread(() => Toast.MakeText(this, "Invalid Code! No list found.", ToastLength.Long).Show());
                    return;
                }

                var doc = query.Documents[0];
                await firestore.Collection("lists").Document(doc.Id).Update("sharedWith", FieldValue.ArrayUnion(currentUserId));

                RunOnUiThread(() => Toast.MakeText(this, "Successfully joined the list!", ToastLength.Short).Show());
            }
            catch (Exception ex)
            {
                Log.Debug("HomeActivity", "Join Error: " + ex.Message);
                RunOnUiThread(() => Toast.MakeText(this, "Error joining list.", ToastLength.Short).Show());
            }
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
                        var sharedWith = item.Get("sharedWith") as Java.Util.ArrayList;
                        bool isSharedWithMe = sharedWith != null && sharedWith.Contains(currentUserId);

                        if (ownerId == currentUserId || isSharedWithMe)
                        {
                            lists.Add(new Big17DataFirebase2.Model.List()
                            {
                                Id = item.Id,
                                Title = item.Get("title")?.ToString() ?? "Untitled List",
                                OwnerId = ownerId
                            });
                        }
                    }
                    listAdapter.NotifyDataSetChanged();
                }
                catch (Exception ex)
                {
                    Log.Debug("HomeActivity", "Error fetching: " + ex.Message);
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