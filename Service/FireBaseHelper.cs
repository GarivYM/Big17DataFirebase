using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Gms.Extensions;
using Android.Gms.Tasks;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Big17DataFirebase2.BusinessLogic;
using Big17DataFirebase2.Model;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Firestore.Auth;
using Java.Lang;
using Java.Util;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Xamarin.Grpc.NameResolver;

namespace Big17DataFirebase2.Service
{
	public class FireBaseHelper
	{
		public static FirestoreEventListener FirestoreEventListener;
        protected static FireBaseHelper me;
		private FirebaseApp app;
		
		static FireBaseHelper() { me = new FireBaseHelper(); }

		protected FireBaseHelper() { InitializeFirebase(); }

		//Initialize Firebase app
		private void InitializeFirebase()
		{
			try
			{
				//1.
				//Parse Firebase json file:
				//Install Newtonsoft.Json NuGet latest version
				//Rename json file google-services.json to googleservices.json 
				//Place json file google-services.json into Root/Assets
				//Set its Build Action in Property to "AndroidAsset"	

				string json;
				string projectId = "";
				string apiKey = "";
				string storageBucket = "";
				AssetManager assets = Application.Context.Assets;
				using (Stream stream = assets.Open("googleservices.json")) //Correct way to access raw resource
				{
					// Reading from app data directory
					using (StreamReader r = new StreamReader(stream))
					{
						json = r.ReadToEnd();

						//using Newtonsoft.Json.Linq;
						//JObject.Parse(json) parses the JSON string into a JObject, making it easy to navigate the JSON structure.
						//JToken is used to access the individual elements within the JSON.
						JObject jsonObj = JObject.Parse(json);
						JToken projectInfo = jsonObj["project_info"];

						if (projectInfo != null)
						{
							projectId = (string)projectInfo["project_id"];
							storageBucket = (string)projectInfo["storage_bucket"];
						}
						else
						{
							Log.Error(ProManager.TAG, "project_info is null");
							return; //Exit, as we cannot continue without project_info
						}

						JToken client = jsonObj["client"][0]; // Access the client array
						apiKey = (string)client["api_key"][0]["current_key"];
					}
				}

				//2. Initilize Firebase App
				app = FirebaseApp.InitializeApp(Application.Context); //using Firebase;
				if (app == null)
				{
					var options = new FirebaseOptions.Builder()
					.SetProjectId(projectId)
					.SetApplicationId(projectId)
					.SetApiKey(apiKey)
					.SetDatabaseUrl(projectId + ".firebaseapp.com")
					.SetStorageBucket(storageBucket)
					.Build();

					app = FirebaseApp.InitializeApp(Application.Context, options);
				}
			}
			catch (FileNotFoundException ex)
			{
				Android.Util.Log.Error(ProManager.TAG, $"File not found: {ex.Message}");
			}
			catch (System.Exception ex)
			{
				Android.Util.Log.Error(ProManager.TAG, $"Error parsing JSON: {ex.Message}");
			}
		}

		#region Users
		public static async Task<string> SignInUserAsync(string uemail, string upass)
		{
			try
			{
				FirebaseAuth mAuth = FirebaseAuth.Instance;
				//using Android.Gms.Extensions;
				await mAuth.SignInWithEmailAndPassword(uemail, upass);
				Log.Debug(ProManager.TAG, $"MyApp: User Auth {uemail} SignIn success");
				return mAuth.CurrentUser.Uid; // Indicate success
			}
			catch (FirebaseAuthException ex)
			{
				Log.Error(ProManager.TAG, $"SignInUserAsync: User Auth SignIn failed: {ex.Message}");
				return null; // Indicate failure
			}
			catch (System.Exception ex)
			{
				Log.Error(ProManager.TAG, $"SignInUserAsync: User Auth SignIn failed, general error: {ex.Message}");
				return null; // Indicate failure
			}
		}
		public static async Task<string> RegisterUserForAuth(Model.User user)
		{
            try
            {
                FirebaseAuth mAuth = FirebaseAuth.Instance;
                //using Android.Gms.Extensions;
                await mAuth.CreateUserWithEmailAndPasswordAsync(user.UserEmail, user.UserPass);
                Log.Debug(ProManager.TAG, $"RegisterUserForAuth: User Auth {user.UserEmail} SignIn success");
             
				return mAuth?.CurrentUser.Uid;
            }
            catch (FirebaseAuthException ex)
            {
                Log.Error(ProManager.TAG, $"SignInUserAsync: User Auth SignIn failed: {ex.Message}");
                return null; // Indicate failure
            }
            catch (System.Exception ex)
            {
                Log.Error(ProManager.TAG, $"SignInUserAsync: User Auth SignIn failed, general error: {ex.Message}");
                return null; // Indicate failure
            }           
        }
        public static async Task<bool> InsertAsync(Model.User user)
        {
            try
            {
                //Insert user to FireStore database
                HashMap userMap = new HashMap(); //using Java.Util;
                userMap.Put("FirstName", user.FirstName);
                userMap.Put("IsAdmin", user.IsAdmin);
                userMap.Put("LastName", user.LastName);
                userMap.Put("UserEmail", user.UserEmail);
                userMap.Put("UserMobile", user.UserMobile);
                userMap.Put("UserPassword", user.UserPass);


                DocumentReference userReference = FirebaseFirestore.Instance
                                                                        .Collection("users")
                                                                        .Document(user.Id);
                await userReference.Set(userMap);
                Log.Debug(ProManager.TAG, $"InsertAsync: Insert User to Firestore complited");
                return true; // Indicate success
            }
            catch (FirebaseFirestoreException ex)
            {
                Log.Error(ProManager.TAG, $"InsertAsync: Insert User to Firestore failed: {ex.Message}");
                return false; // Indicate failure
            }
            catch (System.Exception ex)
            {
                Log.Error(ProManager.TAG, $"MyApp: Insert User to Firestore failed: {ex.Message}");
                return false; // Indicate failure
            }
        }
		public static async Task<Model.User> GetUserById(string userId)
		{
			Model.User newuser = null;
			try
			{
                DocumentReference userRef = FirebaseFirestore.Instance
                .Collection("users")
                .Document(userId);

                var userObject = await userRef.Get();
				
				newuser = new Model.User()
				{
					Id = userId,
					FirstName = ((DocumentSnapshot)userObject).Get("FirstName").ToString(),
					LastName = ((DocumentSnapshot)userObject).Get("LastName").ToString(),
					UserEmail = ((DocumentSnapshot)userObject).Get("UserEmail").ToString(),
					UserMobile = ((DocumentSnapshot)userObject).Get("UserMobile").ToString(),
					UserPass = ((DocumentSnapshot)userObject).Get("UserPassword").ToString(),
					IsAdmin = bool.Parse(((DocumentSnapshot)userObject).Get("IsAdmin").ToString())
				};
                Log.Debug(ProManager.TAG, $"GetUserById: Get User from Firestore DB success");
                return newuser;
            }
			catch (FirebaseFirestoreException ex)
			{
                Log.Debug(ProManager.TAG, $"GetUserByID: Get User from Firestore failed: {ex.Message}");
                return null; // Indicate failure
            }
			catch (System.Exception ex)
			{
                Log.Debug(ProManager.TAG, $"GetUserByID general error: {ex.Message}");
                return null;
			}
        }
        public static async Task<List<Model.User>> GetUsersCollection()
        {
			List <Model.User> users = new List <Model.User>();

			try
			{
                var documents = await FirebaseFirestore.Instance.Collection("users").Get();
				var FirestoreUsersCollection = (QuerySnapshot)documents;

                if (!FirestoreUsersCollection.IsEmpty)
                {
                    var usersCollection = FirestoreUsersCollection.Documents;
                    foreach (DocumentSnapshot item in usersCollection)
                    {
                        Model.User user = new Model.User()
                        {
                            Id = item.Id,
                            FirstName = item.Get("FirstName").ToString(),
                            LastName = item.Get("LastName").ToString(),
                            UserEmail = item.Get("UserEmail").ToString(),
                            UserMobile = item.Get("UserMobile").ToString(),
                            UserPass = item.Get("UserPassword").ToString(),
                            IsAdmin = bool.Parse(item.Get("IsAdmin").ToString())
                        }; 
						users.Add(user);
                    }
					Log.Debug(ProManager.TAG, $"GetUsersCollection: loaded successfully! " +
											  $"Count: {users.Count}");                   
                }
                return users;
            }
            catch (FirebaseFirestoreException ex)
            {
                Log.Debug(ProManager.TAG, $"GetUsersCollection failed: {ex.Message}");
                return users; // Indicate failure
            }
            catch (System.Exception ex)
            {
                Log.Debug(ProManager.TAG, $"GetUsersCollection general error: {ex.Message}");
                return users;
            }
        }
        public static void FetchUsersListener()
        {
            FirestoreEventListener = new FirestoreEventListener();
            FirebaseFirestore.Instance
                .Collection("users")
                .AddSnapshotListener(FirestoreEventListener);
        }     
        #endregion
    }
    public class FirestoreEventListener : Java.Lang.Object, Firebase.Firestore.IEventListener
    {
        public event EventHandler<TaskListenerEventArgs> getEvent;
        public class TaskListenerEventArgs : EventArgs
        {
            public Java.Lang.Object Result { get; set; }
        }
        public void OnEvent(Java.Lang.Object obj, FirebaseFirestoreException error)
        {
            getEvent?.Invoke(this, new TaskListenerEventArgs { Result = obj });
        }
    }
}