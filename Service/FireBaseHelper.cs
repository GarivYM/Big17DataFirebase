using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Gms.Extensions;
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
using Firebase.Firestore.Model;
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
        public static IListenerRegistration Registration;
        public static FirestoreEventListener listener;
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
        public static async Task<string> InsertAsync(Model.User user)
        {
            try
            {
                //Add user account to Firebase Auth Module
                user.Id = await RegisterUserForAuth(user);
                await AddUserToFirestore(user);
                return user.Id;
            }
            catch (Exception ex)
            {
                Log.Error(ProManager.TAG, $"Insert user failed: {ex.Message}");
                throw new Exception("Insert user failed");
            }
        }
        public static async Task<string> RegisterUserForAuth(Model.User user)
		{
            //Add user account to Firebase Auth Module
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
                Log.Error(ProManager.TAG, $"RegisterUserForAuth: {ex.Message}");
				throw new Exception("RegisterUserForAuth Failed!");
            }
            catch (System.Exception ex)
            {
                Log.Error(ProManager.TAG, $"RegisterUserForAuth general error: {ex.Message}");
                throw new Exception("RegisterUserForAuth Failed!");
            }           
        }		
        public static async Task AddUserToFirestore(Model.User user)
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
                Log.Debug(ProManager.TAG, $"Add User to Firestore complited");
            }
            catch (FirebaseFirestoreException ex)
            {
                Log.Error(ProManager.TAG, $"Add User to Firestore failed: {ex.Message}");
				throw new Exception("Add User to Firestore failed");
            }
            catch (System.Exception ex)
            {
                Log.Error(ProManager.TAG, $"Add User to Firestore failed: {ex.Message}");
                throw new Exception("Add User to Firestore failed");
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
        public static async Task UpdateUser(Model.User user)
        {
			try
			{
				DocumentReference userRef = FirebaseFirestore.Instance
											.Collection("users").Document(user.Id);

				await userRef.Update("FirstName", user.FirstName);
				await userRef.Update("LastName", user.LastName);
				await userRef.Update("UserMobile", user.UserMobile);

				Log.Debug(ProManager.TAG, $"FirebaseHelper: Update {user.UserEmail} success");
			}
			catch (System.Exception ex)
			{
				Log.Debug(ProManager.TAG, $"FirebaseHelper: Update {user.UserEmail} failed " + ex.Message);
				throw new Exception($"Update {user.UserEmail} failed");
			}
        }
		public static async Task Delete(Model.User userToDelete)
		{
			try
			{				
				// Create the credential for the account being deleted
				AuthCredential credential = EmailAuthProvider.GetCredential(userToDelete.UserEmail, userToDelete.UserPass);

				// Reauthenticate the ACTIVE user session specifically
				await FirebaseAuth.Instance.SignInWithCredential(credential);

				// Now delete the Firestore data
				await FirebaseFirestore.Instance.Collection("users").Document(userToDelete.Id).Delete();

				// Now delete the Auth record
				await FirebaseAuth.Instance.CurrentUser.DeleteAsync();

				// Reauthenticate the ACTIVE user session to Current User CredentiaLS
				credential = EmailAuthProvider.GetCredential(ProManager.CurrentUser.UserEmail, ProManager.CurrentUser.UserPass);
				await FirebaseAuth.Instance.SignInWithCredential(credential);
			}
			catch (Exception ex)
			{
				Log.Debug(ProManager.TAG, $"Delete user failed! " + ex.Message);
				throw new Exception("Delete user failed!");
			}
		}
        public static void FetchListsListener()
        {
            listener = new FirestoreEventListener();

            Registration = FirebaseFirestore.Instance
                .Collection("lists")
                .AddSnapshotListener(listener);
        }

        public static void StopListsListener()
        {
            Registration?.Remove();
            Registration = null;
            listener = null;
        }
        public static void FetchUsersListener()
        {
            listener = new FirestoreEventListener();
            Registration = FirebaseFirestore.Instance
				.Collection("users")
				.AddSnapshotListener(listener);
        }
        public static void StopUsersListener()
        {
            Registration?.Remove();
            Registration = null;
            listener = null;
        }
        #endregion

        #region App Data

        #endregion

        #region Lists
        public static async Task<bool> CreateList(string title, string ownerId, string type)
        {
            try
            {
                var firestore = FirebaseFirestore.Instance;
                string joinCode = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();

                // Use JavaDictionary to ensure compatibility with the Android SDK
                var listData = new Android.Runtime.JavaDictionary<string, object>
                {
                    { "title", title },
                    { "ownerId", ownerId },
                    { "type", type },
                    { "joinCode", joinCode },
                    { "sharedWith", new Java.Util.ArrayList() }
                };

                // Note: FieldValue.ServerTimestamp() works best when passed via a Java Map
                listData.Add("createdAt", FieldValue.ServerTimestamp());

                // Now .Add() will accept listData because JavaDictionary implements the necessary Java interfaces
                await firestore.Collection("lists").Add(listData);

                return true;
            }
            catch (Exception ex)
            {
                Log.Debug("FirebaseError", ex.Message);
                return false;
            }
        }
        private async Task JoinListByCode(string code)
        {
            var firestore = FirebaseFirestore.Instance;
            var currentUserId = FirebaseAuth.Instance.CurrentUser.Uid;

            try
            {
                var result = await firestore.Collection("lists")
                                            .WhereEqualTo("joinCode", code)
                                            .Get();

                var query = result as QuerySnapshot;

                if (query == null || query.IsEmpty)
                {
                    new Android.OS.Handler(Android.OS.Looper.MainLooper).Post(() => {
                        // Explicitly calling Android.Widget.Toast to avoid conversion errors
                        Android.Widget.Toast.MakeText(Android.App.Application.Context, "Invalid Code! No list found.", Android.Widget.ToastLength.Short).Show();
                    });
                    return;
                }

                var doc = query.Documents[0];
                string listDocId = doc.Id;

                await firestore.Collection("lists")
                               .Document(listDocId)
                               .Update("sharedWith", FieldValue.ArrayUnion(currentUserId));

                new Android.OS.Handler(Android.OS.Looper.MainLooper).Post(() => {
                    Android.Widget.Toast.MakeText(Android.App.Application.Context, "Successfully joined!", Android.Widget.ToastLength.Short).Show();
                });
            }
            catch (Exception ex)
            {
                Log.Debug("JoinError", ex.Message);

                new Android.OS.Handler(Android.OS.Looper.MainLooper).Post(() => {
                    Android.Widget.Toast.MakeText(Android.App.Application.Context, "Error: Could not join list.", Android.Widget.ToastLength.Short).Show();
                });
            }
        }
        public static async Task ToggleItemStatus(string listId, string itemId, bool isChecked)
        {
            await FirebaseFirestore.Instance
                .Collection("lists").Document(listId)
                .Collection("items").Document(itemId)
                .Update("isChecked", isChecked);
        }
        public static void FetchMyLists(string userId)
        {
            listener = new FirestoreEventListener();
            // This query finds lists where you are the owner OR a participant
            Registration = FirebaseFirestore.Instance.Collection("lists")
                .WhereEqualTo("ownerId", userId)
                // Note: You might need a separate query or a Composite Index for 'sharedWith'
                .AddSnapshotListener(listener);
        }
        public static async Task RemoveUserFromList(string listId, string userIdToRemove)
        {
            await FirebaseFirestore.Instance.Collection("lists").Document(listId)
                .Update("sharedWith", FieldValue.ArrayRemove(userIdToRemove));
        }
        public static async Task AddItemToList(string listId, string text)
        {
            try
            {
                HashMap itemMap = new HashMap();
                itemMap.Put("text", text);

                DocumentReference itemRef = FirebaseFirestore.Instance
                    .Collection("lists")
                    .Document(listId)
                    .Collection("items")
                    .Document(); // auto ID

                await itemRef.Set(itemMap);
            }
            catch (Exception ex)
            {
                Log.Error(ProManager.TAG, "AddItemToList failed: " + ex.Message);
                throw;
            }
        }
        public static async Task<List<string>> GetItems(string listId)
        {
            List<string> items = new List<string>();

            try
            {
                var snapshot = await FirebaseFirestore.Instance
                    .Collection("lists")
                    .Document(listId)
                    .Collection("items")
                    .Get();

                var docs = (QuerySnapshot)snapshot;

                foreach (DocumentSnapshot doc in docs.Documents)
                {
                    items.Add(doc.Get("text").ToString());
                }

                return items;
            }
            catch (Exception ex)
            {
                Log.Error(ProManager.TAG, "GetItems failed: " + ex.Message);
                return items;
            }
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