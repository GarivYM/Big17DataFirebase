using Android.App;
using Android.Content;
using Android.Gms.Extensions;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Big17DataFirebase2
{
    [Activity(Label = "ListActivity", MainLauncher = false)]
    public class ListActivity : Activity
    {
        // UI
        TextView tvDelete, tvBar, tvTitle;
        RecyclerView recyclerView;

        // RecyclerView
        RecyclerView.LayoutManager layoutManager;
        ItemsRViewAdapter adapter;

        // Data
        List<Item> items;
        Dialog mProgressDialog;
        string currentListId; // To store which list we are looking at

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.listlayout);

            // Get data passed from HomeActivity
            currentListId = Intent.GetStringExtra("listId");
            string listName = Intent.GetStringExtra("listTitle");

            InitializeViews(listName);
        }

        private void InitializeViews(string title)
        {
            tvDelete = FindViewById<TextView>(Resource.Id.tvDelete);
            tvBar = FindViewById<TextView>(Resource.Id.tvBar);
            tvTitle = FindViewById<TextView>(Resource.Id.tvTitle);
            recyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);
            // Link the new Info Icon
            ImageView btnInfo = FindViewById<ImageView>(Resource.Id.btnInfo);

            // Trigger the popup when clicked
            btnInfo.Click += async (s, e) => {
                await ShowListInfoPopup();
            };
            tvTitle.Text = title ?? "List Page";

            tvDelete.Click += TvDelete_Click;
            tvBar.Click += TvBar_Click;

            layoutManager = new LinearLayoutManager(this);
            recyclerView.SetLayoutManager(layoutManager);

            items = new List<Item>();
            adapter = new ItemsRViewAdapter(items);
            recyclerView.SetAdapter(adapter);
        }

        protected override void OnResume()
        {
            base.OnResume();
            ShowProgressBar(true);
            FetchItemsFromDB();
        }

        private void FetchItemsFromDB()
        {
            var firestore = FirebaseFirestore.Instance;

            // We pass 'this' (the Activity) as the first argument.
            // This helps with lifecycle management and type conversion.
            firestore.Collection("lists")
         .Document(currentListId)
         .Collection("items")
         .AddSnapshotListener(new MyEventListener((value, error) =>
         {
             ShowProgressBar(false);

             if (error != null)
             {
                 Log.Debug("ListActivity", error.Message);
                 return;
             }

             // --- THE FIX IS HERE ---
             // Cast the Java.Lang.Object to a QuerySnapshot
             var snapshot = value as QuerySnapshot;

             if (snapshot != null)
             {
                 items.Clear();
                 foreach (DocumentSnapshot doc in snapshot.Documents)
                 {
                     items.Add(new Item
                     {
                         Id = doc.Id,
                         Name = doc.Get("name")?.ToString() ?? "Unnamed",
                         IsChecked = doc.Get("isChecked") != null && (bool)doc.Get("isChecked")
                     });
                 }
                 adapter.NotifyDataSetChanged();
             }
         }));
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

        private void TvDelete_Click(object sender, EventArgs e)
        {
            // Add logic here later to delete the whole list from Firestore
            Toast.MakeText(this, "Delete List clicked", ToastLength.Short).Show();
        }

        private void TvBar_Click(object sender, EventArgs e)
        {
            // Add logic here to show a dialog to add a NEW item to this list
            ShowAddItemDialog();
        }

        private void ShowAddItemDialog()
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetTitle("Add New Item");
            EditText input = new EditText(this);
            builder.SetView(input);

            builder.SetPositiveButton("Add", async (s, args) =>
            {
                string itemName = input.Text;
                if (!string.IsNullOrEmpty(itemName))
                {
                    var itemData = new Android.Runtime.JavaDictionary<string, object>
                    {
                        { "name", itemName },
                        { "isChecked", false }
                    };

                    await FirebaseFirestore.Instance
                        .Collection("lists")
                        .Document(currentListId)
                        .Collection("items")
                        .Add(itemData);
                }
            });
            builder.Show();
        }
        private async Task ShowListInfoPopup()
        {
            var firestore = FirebaseFirestore.Instance;
            var currentUserId = FirebaseAuth.Instance.CurrentUser.Uid;

            // We need the ID to find the list details, and the Code to find the participants
            string listId = Intent.GetStringExtra("listId");

            try
            {
                // 1. Fetch the List details (for the Title and JoinCode)
                var result = await firestore.Collection("lists").Document(listId).Get();
                var listDoc = result as DocumentSnapshot;

                if (listDoc == null || !listDoc.Exists()) return;

                string joinCode = listDoc.Get("joinCode")?.ToString() ?? "N/A";
                string ownerId = listDoc.Get("ownerId")?.ToString();

                // 2. Inflate the Dialog View
                View dialogView = LayoutInflater.From(this).Inflate(Resource.Layout.dialog_list_info, null);
                TextView tvJoinCode = dialogView.FindViewById<TextView>(Resource.Id.tvInfoJoinCode);
                LinearLayout container = dialogView.FindViewById<LinearLayout>(Resource.Id.participantsContainer);

                tvJoinCode.Text = joinCode;
                container.RemoveAllViews();

                // 3. NEW DATABASE LOGIC: Find all participants in the 'UserList' collection
                // We look for every document where the joinCode matches this list
                var userListResult = await firestore.Collection("UserList")
                                                    .WhereEqualTo("joinCode", joinCode)
                                                    .Get();
                var userListQuery = userListResult as QuerySnapshot;

                if (userListQuery != null)
                {
                    foreach (var doc in userListQuery.Documents)
                    {
                        // Get the UserID from the UserList mapping
                        string participantUid = doc.Get("UserID")?.ToString();
                        if (string.IsNullOrEmpty(participantUid)) continue;

                        // 4. Fetch the actual user's name from the 'users' collection
                        var userResult = await firestore.Collection("users").Document(participantUid).Get();
                        var userDoc = userResult as DocumentSnapshot;

                        string fullName = "Unknown User";
                        if (userDoc != null && userDoc.Exists())
                        {
                            fullName = $"{userDoc.Get("firstName")} {userDoc.Get("lastName")}";
                        }

                        // 5. Build the "You" and "Manager" tags
                        List<string> tags = new List<string>();
                        if (participantUid == ownerId) tags.Add("List Manager");
                        if (participantUid == currentUserId) tags.Add("You");

                        string tagString = tags.Count > 0 ? $" ({string.Join(", ", tags)})" : "";

                        // Create the TextView for this participant
                        TextView tvPerson = new TextView(this);
                        tvPerson.Text = $"• {fullName}{tagString}";
                        tvPerson.TextSize = 16;
                        tvPerson.SetPadding(0, 10, 0, 10);
                        tvPerson.SetTextColor(Android.Graphics.Color.Black);

                        container.AddView(tvPerson);
                    }
                }

                RunOnUiThread(() => {
                    new AlertDialog.Builder(this)
                        .SetView(dialogView)
                        .SetPositiveButton("OK", (s, e) => { })
                        .Show();
                });
            }
            catch (Exception ex)
            {
                Log.Debug("InfoPopup", "Error: " + ex.Message);
            }
        }
    }
}