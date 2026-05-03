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
using static Android.Content.ClipData;

namespace Big17DataFirebase2 // Add this wrapper!
{
    [Activity(Label = "AdminActivity")]
    public class AdminActivity : Activity
    {
        RecyclerView userRecyclerView;
        UsersRViewAdapter userAdapter;
        List<Big17DataFirebase2.Model.User> allUsers;
        TextView tvAdminTitle;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.admin_layout);

            tvAdminTitle = FindViewById<TextView>(Resource.Id.tvAdminTitle);
            userRecyclerView = FindViewById<RecyclerView>(Resource.Id.userRecyclerView);

            // Standard AndroidX LinearLayoutManager
            userRecyclerView.SetLayoutManager(new LinearLayoutManager(this));

            await LoadUsers();
        }

        private async System.Threading.Tasks.Task LoadUsers()
        {
            // 1. Get users from Firebase
            allUsers = await FireBaseHelper.GetUsersCollection();

            // 2. Initialize adapter with 'this' context and the list
            userAdapter = new UsersRViewAdapter(this, allUsers);


            userRecyclerView.SetAdapter(userAdapter);
        }

        // This fulfills the "Confirm Dialog" step in your flowchart
        private System.Threading.Tasks.Task<bool> ShowConfirmDialog(Model.User user)
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();

            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetTitle("Remove User");
            builder.SetMessage($"Are you sure you want to delete {user.FirstName} {user.LastName}?");

            builder.SetPositiveButton("Delete", (s, args) => tcs.SetResult(true));
            builder.SetNegativeButton("Cancel", (s, args) => tcs.SetResult(false));

            builder.SetCancelable(false);
            builder.Show();

            return tcs.Task;
        }
    }
}