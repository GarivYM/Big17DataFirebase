using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Big17DataFirebase2.BusinessLogic;
using System;

namespace Big17DataFirebase2
{
	[Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = false)]
    public class MainActivity : AppCompatActivity, Android.Views.View.IOnClickListener
	{
		TextView tvSignIn, tvSignUp;
		private readonly string TAG = "YAIRAPP";

		protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

			InitialViews();
			Log.Debug(TAG, $"MainActivity: OnCreate()");

			//Debug Mode
			if (ProManager.DebugMode)
				StartActivity(typeof(SignUpActivity));
				//StartActivity(typeof(SignInActivity));
		}

		private void InitialViews()
		{
			tvSignIn = FindViewById<TextView>(Resource.Id.tvSignIn);
			tvSignUp = FindViewById<TextView>(Resource.Id.tvSignUp);
			tvSignIn.SetOnClickListener(this);
			tvSignUp.SetOnClickListener(this);
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

		public void OnClick(View v)
		{
			if (v == tvSignIn)
			{
				StartActivity(new Intent(this,typeof(SignInActivity)));
			}
			else if (v == tvSignUp)
			{
				StartActivity(typeof(SignUpActivity));
			}
		}
	}
}