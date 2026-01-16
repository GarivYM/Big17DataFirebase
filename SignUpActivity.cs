using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Big17DataFirebase2.BusinessLogic;
using Big17DataFirebase2.Model;
using Big17DataFirebase2.Service;
using Firebase.Firestore.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Big17DataFirebase2
{
	[Activity(Label = "SignUpActivity")]
	public class SignUpActivity : Activity
	{
		EditText _firstName, _lastName, _userEmail, _userPassword, _userMobile;
		Button _btnSignUp;
        Dialog mProgressDialog;
        Model.User _user;


        protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			SetContentView(Resource.Layout.signup_layout);

			InitializeViews();
		}
        private void InitializeViews()
        {
			_firstName = FindViewById<EditText>(Resource.Id.et_first_name);
			_lastName = FindViewById<EditText>(Resource.Id.et_last_name);
			_userEmail = FindViewById<EditText>(Resource.Id.et_email);
			_userPassword = FindViewById<EditText>(Resource.Id.et_password);
            _userMobile = FindViewById<EditText>(Resource.Id.et_mobile);
			_btnSignUp = FindViewById<Button>(Resource.Id.btn_register);

            _btnSignUp.Click += BtnSignUp_Click;
        }
        private void BtnSignUp_Click(object sender, EventArgs e)
        {
			_user = new Model.User()
			{
				FirstName = _firstName.Text,
				LastName = _lastName.Text,
				UserEmail = _userEmail.Text,
				UserPass = _userPassword.Text,
				UserMobile = _userMobile.Text
			};

			RegisterNewUser();
        }
        private async void RegisterNewUser()
        {
            ShowProgressBar(true);

            //1. Create new user in Firebase Auth
            string userAuthID = await FireBaseHelper.RegisterUserForAuth(_user);
            if (userAuthID != null) //Success
            {
                Log.Debug(ProManager.TAG, $"Firebase: Add new user to Auth success!");
                _user.Id = userAuthID;
                RegisterUserInFireStore();
                ShowProgressBar(false);
            }
            else //Fail
            {
                ShowProgressBar(false);
                Log.Debug(ProManager.TAG, $"Firebase: Add new user to Auth failed!");
                Toast.MakeText(this, $"Firebase: Add new user to Auth failed!", ToastLength.Short).Show();
            }
        }
        private async void RegisterUserInFireStore()
        {
            //1. Create new user in Firestore
            bool result = await FireBaseHelper.InsertAsync(_user);
            if (result) //Success
            {
                Log.Debug(ProManager.TAG, $"Firebase: Add new user to FireStore success!");                
                ShowProgressBar(false);
                //Toast.MakeText(this, $"Register user success!", ToastLength.Short).Show();

                //Set Current User
                ProManager.CurrentUser = _user;
                StartActivity(typeof(MainPage));
            }
            else //Fail
            {
                ShowProgressBar(false);
                Log.Debug(ProManager.TAG, $"Firebase: Add new user to FireStore failed!!");
                Toast.MakeText(this, $"Firebase: Add new user to FireStore failed!", ToastLength.Short).Show();
            }
        }
        private void ShowProgressBar(bool show)
        {
            //android:background="@android:color/transparent"

            if (show)
            {
                mProgressDialog = new Dialog(this, Android.Resource.Style.ThemeNoTitleBar);
                View view = LayoutInflater.From(this).Inflate(Resource.Layout.fb_progressbar, null);
                //var mProgressMessage = (TextView)view.FindViewById(Resource.Id.;
                //mProgressMessage.Text = "Loading...";
                mProgressDialog.Window.SetBackgroundDrawableResource(Resource.Color.mtrl_btn_transparent_bg_color);
                mProgressDialog.SetContentView(view);
                mProgressDialog.SetCancelable(false);
                mProgressDialog.Show();
            }
            else
            {
                mProgressDialog.Dismiss();
            }
        }
    }
}