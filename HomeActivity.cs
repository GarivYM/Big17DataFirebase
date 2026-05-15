using Android.App;
using Android.Content;
using Android.Gms.Extensions;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App; // Necessary for AppCompatActivity
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
using System.Linq;
using System.Threading.Tasks;

namespace Big17DataFirebase2
{
    [Activity(Label = "Home Page", MainLauncher = false)]
    // Changed inheritance to AppCompatActivity to support Fragments
    public class HomeActivity : AppCompatActivity
    {
        // RecyclerView Components
        RecyclerView recyclerView;
        RecyclerView.LayoutManager layoutManager;
        ListsRViewAdapter listAdapter;

        // UI Elements
        TextView tvUserFullName, tvTitle;
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

            fabAdd = FindViewById<FloatingActionButton>(Resource.Id.fabAdd);
            fabAdd.Click += TvAdd_Click;

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

        protected override async void OnResume()
        {
            base.OnResume();

            // Check Login Status
            if (FirebaseAuth.Instance.CurrentUser != null && ProManager.CurrentUser != null)
            {
                tvUserFullName.Text = $"{ProManager.CurrentUser.FirstName} {ProManager.CurrentUser.LastName}";
                tvTitle.Text = "My Lists";

                // Fetch data using the new Bridge logic
                await LoadUserLists();
            }
            else
            {
                StartActivity(typeof(SignInActivity));
                Finish();
            }
        }

        // Logic to open your new Fragment
        private void TvAdd_Click(object sender, EventArgs e)
        {
            var frag = new AddJoinFragment();
            frag.Show(SupportFragmentManager, "AddJoinTag");
        }

        // The "Bridge" Logic: UserID -> UserList -> joinCode -> lists
        public async Task LoadUserLists()
        {
            var firestore = FirebaseFirestore.Instance;
            var currentUserId = FirebaseAuth.Instance.CurrentUser?.Uid;
            if (currentUserId == null) return;

            try
            {
                ShowProgressBar(true);

                // 1. Find all list codes I have access to
                var mappingResult = await firestore.Collection("UserList")
                                                   .WhereEqualTo("UserID", currentUserId)
                                                   .Get();

                var mappingQuery = mappingResult as QuerySnapshot;

                if (mappingQuery == null || mappingQuery.IsEmpty)
                {
                    RunOnUiThread(() => {
                        lists.Clear();
                        listAdapter.NotifyDataSetChanged();
                        ShowProgressBar(false);
                    });
                    return;
                }

                // 2. Extract codes into a list
                List<string> myCodes = mappingQuery.Documents
                    .Select(d => d.Get("joinCode")?.ToString())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToList();

                // 3. Fetch the actual list details from 'lists' collection
                // Note: WhereIn is limited to 10 items per query
                var listDataResult = await firestore.Collection("lists")
                    .WhereIn(FieldPath.Of("joinCode"), myCodes.Select(x => (Java.Lang.Object)x).ToList())
                    .Get();

                var listsQuery = listDataResult as QuerySnapshot;

                // 4. Update the UI List
                RunOnUiThread(() => {
                    lists.Clear();
                    if (listsQuery != null)
                    {
                        foreach (var doc in listsQuery.Documents)
                        {
                            lists.Add(new Big17DataFirebase2.Model.List()
                            {
                                Id = doc.Id,
                                Title = doc.Get("Title")?.ToString() ?? "Untitled List",
                                OwnerId = doc.Get("ownerId")?.ToString()
                            });
                        }
                    }
                    listAdapter.NotifyDataSetChanged();
                    ShowProgressBar(false);
                });
            }
            catch (Exception ex)
            {
                Log.Debug("HomeActivity", "LoadError: " + ex.Message);
                ShowProgressBar(false);
            }
        }

        public async Task JoinListByCode(string code)
        {
            var firestore = FirebaseFirestore.Instance;
            var currentUserId = FirebaseAuth.Instance.CurrentUser.Uid;

            try
            {
                ShowProgressBar(true);

                // Check if list exists
                var result = await firestore.Collection("lists").WhereEqualTo("joinCode", code).Get();
                var query = result as QuerySnapshot;

                if (query == null || query.IsEmpty)
                {
                    ShowProgressBar(false);
                    RunOnUiThread(() => Toast.MakeText(this, "Invalid Code!", ToastLength.Long).Show());
                    return;
                }

                // Create the bridge document
                var mapping = new Java.Util.HashMap();
                mapping.Put("UserID", currentUserId);
                mapping.Put("joinCode", code);

                await firestore.Collection("UserList").Add(mapping);

                // Refresh data
                await LoadUserLists();
            }
            catch (Exception ex)
            {
                ShowProgressBar(false);
                Log.Debug("HomeActivity", "JoinError: " + ex.Message);
            }
        }

        private void ShowProgressBar(bool show)
        {
            if (show)
            {
                if (mProgressDialog == null)
                {
                    mProgressDialog = new Dialog(this, Android.Resource.Style.ThemeNoTitleBar);
                    View view = LayoutInflater.From(this).Inflate(Resource.Layout.fb_progressbar, null);
                    mProgressDialog.Window.SetBackgroundDrawableResource(Android.Resource.Color.Transparent);
                    mProgressDialog.SetContentView(view);
                    mProgressDialog.SetCancelable(false);
                }
                if (!mProgressDialog.IsShowing) mProgressDialog.Show();
            }
            else
            {
                mProgressDialog?.Dismiss();
            }
        }
    }
}