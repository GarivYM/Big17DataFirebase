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
            string listId = Intent.GetStringExtra("listId");

            try
            {
                // 1. Fetch the result and CAST it to DocumentSnapshot
                var result = await firestore.Collection("lists").Document(listId).Get();
                var listDoc = result as DocumentSnapshot; // This is the fix for the 'Get' error

                if (listDoc == null || !listDoc.Exists()) return;

                // Use .Get() now that the compiler knows this is a DocumentSnapshot
                string joinCode = listDoc.Get("joinCode")?.ToString() ?? "N/A";
                string ownerId = listDoc.Get("ownerId")?.ToString();

                // Cast the ArrayList properly
                var sharedWith = listDoc.Get("sharedWith") as Java.Util.ArrayList;

                // 2. Inflate the Dialog View
                View dialogView = LayoutInflater.From(this).Inflate(Resource.Layout.dialog_list_info, null);
                TextView tvJoinCode = dialogView.FindViewById<TextView>(Resource.Id.tvInfoJoinCode);
                LinearLayout container = dialogView.FindViewById<LinearLayout>(Resource.Id.participantsContainer);

                tvJoinCode.Text = joinCode;

                // 3. Prepare UIDs
                List<string> uids = new List<string>();
                if (!string.IsNullOrEmpty(ownerId)) uids.Add(ownerId);
                if (sharedWith != null)
                {
                    var array = sharedWith.ToArray();
                    foreach (var id in array) uids.Add(id.ToString());
                }

                container.RemoveAllViews();

                // 4. Loop and fetch user details
                foreach (var uid in uids.Distinct())
                {
                    // CAST this result as well!
                    var userResult = await firestore.Collection("users").Document(uid).Get();
                    var userDoc = userResult as DocumentSnapshot;

                    string fullName = "Unknown User";
                    if (userDoc != null && userDoc.Exists())
                    {
                        fullName = $"{userDoc.Get("firstName")} {userDoc.Get("lastName")}";
                    }

                    List<string> tags = new List<string>();
                    if (uid == ownerId) tags.Add("List Manager");
                    if (uid == currentUserId) tags.Add("You");

                    string tagString = tags.Count > 0 ? $" ({string.Join(", ", tags)})" : "";

                    TextView tvPerson = new TextView(this);
                    tvPerson.Text = $"• {fullName}{tagString}";
                    tvPerson.TextSize = 16;
                    tvPerson.SetPadding(0, 10, 0, 10);
                    tvPerson.SetTextColor(Android.Graphics.Color.Black);

                    container.AddView(tvPerson);
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