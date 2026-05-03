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
using Firebase.Firestore;
using System;
using System.Collections.Generic;

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
    }
}