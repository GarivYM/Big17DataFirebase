using Android.OS;
using Android.Views;
using Android.Widget;
using Big17DataFirebase2.BusinessLogic; // Adjust if your FireBaseHelper is elsewhere
using Big17DataFirebase2.Service;
using Firebase.Auth;
using Google.Android.Material.BottomSheet;
using System;

namespace Big17DataFirebase2
{
    public class AddJoinFragment : BottomSheetDialogFragment
    {
        private EditText etListName, etJoinCode;
        private Spinner spinnerType;
        private Button btnCreate, btnJoin;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use the layout name you saved (ensure it matches exactly)
            var view = inflater.Inflate(Resource.Layout.add_join_layout, container, false);

            // 1. Initialize Join Section
            etJoinCode = view.FindViewById<EditText>(Resource.Id.etJoinCode);
            btnJoin = view.FindViewById<Button>(Resource.Id.btnJoin);

            // 2. Initialize Create Section
            etListName = view.FindViewById<EditText>(Resource.Id.etListName);
            spinnerType = view.FindViewById<Spinner>(Resource.Id.spinnerType);
            btnCreate = view.FindViewById<Button>(Resource.Id.btnCreate);

            // 3. Setup Spinner
            // Note: If 'Resource.Array.list_types' gives an error, use the manual array below:
            var types = new string[] { "Standard", "Shopping", "Work", "Home" };
            var adapter = new ArrayAdapter<string>(Context, Android.Resource.Layout.SimpleSpinnerItem, types);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            spinnerType.Adapter = adapter;

            // 4. Handle Join Click
            btnJoin.Click += async (s, e) =>
            {
                string code = etJoinCode.Text.Trim().ToUpper();
                if (!string.IsNullOrEmpty(code))
                {
                    // Call Join method in HomeActivity
                    var home = Activity as HomeActivity;
                    if (home != null)
                    {
                        await home.JoinListByCode(code);
                        Dismiss(); // Close the slide-up menu
                    }
                }
                else
                {
                    Toast.MakeText(Context, "Please enter a code", ToastLength.Short).Show();
                }
            };

            // 5. Handle Create Click
            btnCreate.Click += async (s, e) =>
            {
                string name = etListName.Text.Trim();
                string type = spinnerType.SelectedItem.ToString();

                if (!string.IsNullOrEmpty(name))
                {
                    string uid = FirebaseAuth.Instance.CurrentUser.Uid;

                    // Call the Firebase Helper to create the list
                    bool success = await FireBaseHelper.CreateList(name, uid, type);

                    if (success)
                    {
                        Toast.MakeText(Context, "List Created!", ToastLength.Short).Show();

                        // Tell HomeActivity to refresh the list
                        var home = Activity as HomeActivity;
                        if (home != null)
                        {
                            await home.LoadUserLists();
                        }
                        Dismiss();
                    }
                }
                else
                {
                    Toast.MakeText(Context, "Please enter a list name", ToastLength.Short).Show();
                }
            };

            return view;
        }
    }
}