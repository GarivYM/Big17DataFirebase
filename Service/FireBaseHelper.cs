// ייבוא מרחבי השמות הבסיסיים של אנדרואיד לעבודה עם משאבים, לוגים ומערכת ההפעלה
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Gms.Extensions;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
// ייבוא מודלים ושכבות הלוגיקה הפנימיות של האפליקציה שלך
using Big17DataFirebase2.BusinessLogic;
using Big17DataFirebase2.Model;
// ייבוא רכיבי הליבה הרשמיים של Firebase: אימות (Auth) ומסד נתונים בזמן אמת (Firestore)
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Firestore.Auth;
using Firebase.Firestore.Model;
// ייבוא מבני נתונים ג'אוואיים (כמו HashMap) הנחוצים לעבודה מול ה-SDK של אנדרואיד
using Java.Util;
// ייבוא ספריית Newtonsoft לניתוח (Parsing) קובץ ה-JSON של הגדרות השרת
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
        // רכיבים סטטיים לניהול מאזינים (Listeners) בזמן אמת עבור שינויים בבסיס הנתונים
        public static IListenerRegistration Registration;
        public static FirestoreEventListener listener;
        protected static FireBaseHelper me; // מימוש תבנית עיצוב מסוג Singleton (מופע יחיד)
        private FirebaseApp app;

        // בנאי סטטי (Static Constructor) - רץ פעם אחת בלבד בעת טעינת המחלקה לזיכרון ומאתחל את המופע
        static FireBaseHelper() { me = new FireBaseHelper(); }

        // בנאי מוגן (Protected) המונע יצירת מופעים חיצוניים ומפעיל את אתחול החיבור לשרת
        protected FireBaseHelper() { InitializeFirebase(); }

        // פונקציה האחראית על טעינת ופרוק הגדרות ה-Firebase מקובץ ה-Assets של האפליקציה
        private void InitializeFirebase()
        {
            try
            {
                // שליפת מנהל הנכסים (AssetManager) של אנדרואיד לצורך גישה לקבצים מצורפים
                AssetManager assets = Application.Context.Assets;
                string json;
                string projectId = "";
                string apiKey = "";
                string storageBucket = "";

                // פתיחה בטוחה של קובץ ההגדרות 'googleservices.json' הנמצא בתיקיית Assets
                using (Stream stream = assets.Open("googleservices.json"))
                {
                    using (StreamReader r = new StreamReader(stream))
                    {
                        json = r.ReadToEnd(); // קריאת כל תוכן הקובץ כמחרוזת טקסט אחת

                        // ניתוח ה-JSON בעזרת אובייקטים של Newtonsoft לשליפת פרטי השרת
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
                            return; // יציאה מהפונקציה במידה והמידע האלמנטרי חסר
                        }

                        // חילוץ מפתח ה-API מתוך מערך הלקוחות (Client Array) שבקובץ
                        JToken client = jsonObj["client"][0];
                        apiKey = (string)client["api_key"][0]["current_key"];
                    }
                }

                // ניסיון ראשוני לאתחל את אפליקציית ה-Firebase עם קונטקסט ברירת המחדל
                app = FirebaseApp.InitializeApp(Application.Context);
                if (app == null)
                {
                    // אם האתחול נכשל, נבנה ידנית אובייקט אפשרויות (FirebaseOptions) עם המפתחות שחילצנו מה-JSON
                    var options = new FirebaseOptions.Builder()
                    .SetProjectId(projectId)
                    .SetApplicationId(projectId)
                    .SetApiKey(apiKey)
                    .SetDatabaseUrl(projectId + ".firebaseapp.com")
                    .SetStorageBucket(storageBucket)
                    .Build();

                    // אתחול האפליקציה מחדש עם ההגדרות המותאמות אישית
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

        #region Users (ניהול משתמשים)

        // פונקציה אסינכרונית לחיבור משתמש קיים באמצעות אימייל וסיסמה
        public static async Task<string> SignInUserAsync(string uemail, string upass)
        {
            try
            {
                FirebaseAuth mAuth = FirebaseAuth.Instance;
                // ביצוע תהליך האימות מול שרתי Firebase Auth (שימוש בהרחבת .AsAsync() של אנדרואיד)
                await mAuth.SignInWithEmailAndPassword(uemail, upass);
                Log.Debug(ProManager.TAG, $"MyApp: User Auth {uemail} SignIn success");
                return mAuth.CurrentUser.Uid; // החזרת מזהה המשתמש הייחודי שנוצר בשרת
            }
            catch (FirebaseAuthException ex)
            {
                Log.Error(ProManager.TAG, $"SignInUserAsync: User Auth SignIn failed: {ex.Message}");
                return null; // החזרת ערך ריק שמסמן כשל בחיבור
            }
            catch (System.Exception ex)
            {
                Log.Error(ProManager.TAG, $"SignInUserAsync: General error: {ex.Message}");
                return null;
            }
        }

        // פונקציה ראשית לרישום משתמש חדש - מייצרת אותו במערכת האימות ולאחר מכן שומרת את פרטיו ב-Database
        public static async Task<string> InsertAsync(Model.User user)
        {
            try
            {
                // שלב 1: יצירת המשתמש ברכיב ה-Authentication וקבלת ה-UID שלו
                user.Id = await RegisterUserForAuth(user);
                // שלב 2: יצירת מסמך תואם עם פרטי המשתמש הנוספים בתוך מסד הנתונים Firestore
                await AddUserToFirestore(user);
                return user.Id;
            }
            catch (Exception ex)
            {
                Log.Error(ProManager.TAG, $"Insert user failed: {ex.Message}");
                throw new Exception("Insert user failed");
            }
        }

        // פונקציה פנימית לרישום משתמש חדש ברמת רכיב ה-Authentication של השרת
        public static async Task<string> RegisterUserForAuth(Model.User user)
        {
            try
            {
                FirebaseAuth mAuth = FirebaseAuth.Instance;
                // יצירת החשבון בשרת באמצעות אימייל וסיסמה
                await mAuth.CreateUserWithEmailAndPasswordAsync(user.UserEmail, user.UserPass);
                Log.Debug(ProManager.TAG, $"RegisterUserForAuth: User Auth {user.UserEmail} SignIn success");

                return mAuth?.CurrentUser.Uid; // החזרת ה-UID החדש שהוקצה למשתמש
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

        // פונקציה השומרת את המידע המורחב של המשתמש בטבלת/קולקציית "users" ב-Firestore
        public static async Task AddUserToFirestore(Model.User user)
        {
            try
            {
                // שימוש ב-HashMap של ג'אווה על מנת להכין את מבנה הנתונים (מפתח-ערך) עבור ה-SDK
                HashMap userMap = new HashMap();
                userMap.Put("FirstName", user.FirstName);
                // קביעת הרשאת מנהל מערכת (אדמין) אוטומטית אם המשתמש נרשם עם הכתובת הספציפית הזו
                userMap.Put("IsAdmin", user.UserEmail == "admin@gmail.com");
                userMap.Put("LastName", user.LastName);
                userMap.Put("UserEmail", user.UserEmail);
                userMap.Put("UserMobile", user.UserMobile);
                userMap.Put("UserPassword", user.UserPass);

                // יצירת הפניה (Reference) למסמך ספציפי ששמו הוא ה-UID של המשתמש
                DocumentReference userReference = FirebaseFirestore.Instance
                                                                .Collection("users")
                                                                .Document(user.Id);
                // שמירת המפה (Map) בתוך השרת בשיטת Set (דורס או יוצר מחדש)
                await userReference.Set(userMap);
                Log.Debug(ProManager.TAG, $"Add User to Firestore completed");
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

        // פונקציה אסינכרונית לשליפת נתוני משתמש בודד על פי מזהה ה-ID שלו
        public static async Task<Model.User> GetUserById(string userId)
        {
            Model.User newuser = null;
            try
            {
                DocumentReference userRef = FirebaseFirestore.Instance
                .Collection("users")
                .Document(userId);

                // ביצוע השילוב מול השרת וקבלת המסמך הגולמי
                var userObject = await userRef.Get();

                // המרת המסמך הגולמי (DocumentSnapshot) לאובייקט מסוג User של האפליקציה שלנו
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
                return null;
            }
            catch (System.Exception ex)
            {
                Log.Debug(ProManager.TAG, $"GetUserByID general error: {ex.Message}");
                return null;
            }
        }

        // פונקציה השולפת את כל רשימת המשתמשים הקיימים במערכת (משמש בעיקר את מסכי הניהול של האדמין)
        public static async Task<List<Model.User>> GetUsersCollection()
        {
            List<Model.User> users = new List<Model.User>();

            try
            {
                // שליפת כל המסמכים הקיימים תחת קולקציית users
                var documents = await FirebaseFirestore.Instance.Collection("users").Get();
                var FirestoreUsersCollection = (QuerySnapshot)documents;

                if (!FirestoreUsersCollection.IsEmpty)
                {
                    var usersCollection = FirestoreUsersCollection.Documents;
                    // ריצה בלולאה על כל מסמך שהתקבל והמרתו לאובייקט מקומי בשפה שלנו
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
                        users.Add(user); // הוספת המשתמש המומר לרשימה הכללית
                    }
                    Log.Debug(ProManager.TAG, $"GetUsersCollection: loaded successfully! Count: {users.Count}");
                }
                return users;
            }
            catch (FirebaseFirestoreException ex)
            {
                Log.Debug(ProManager.TAG, $"GetUsersCollection failed: {ex.Message}");
                return users;
            }
            catch (System.Exception ex)
            {
                Log.Debug(ProManager.TAG, $"GetUsersCollection general error: {ex.Message}");
                return users;
            }
        }

        // עדכון שדות ספציפיים (שם פרטי, משפחה וטלפון) של משתמש קיים בבסיס הנתונים
        public static async Task UpdateUser(Model.User user)
        {
            try
            {
                DocumentReference userRef = FirebaseFirestore.Instance
                                            .Collection("users").Document(user.Id);

                // ביצוע פקודות עדכון נקודתיות (Update) ללא דריסת שאר השדות במסמך
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

        // לוגיקה מורכבת ביותר למחיקת משתמש: טיפול בהעברת בעלות על רשימותיו או מחיקתן הפיזית מהשרת
        public static async System.Threading.Tasks.Task<bool> DeleteUser(string userId)
        {
            try
            {
                // === שלב 1: טיפול ברשימות שבבעלות המשתמש המיועד למחיקה ===
                var listsSnapshot = await FirebaseFirestore.Instance.Collection("lists")
                    .WhereEqualTo("OwnerId", userId).Get();
                var listsQuery = listsSnapshot as QuerySnapshot;

                if (listsQuery != null && listsQuery.Documents != null)
                {
                    foreach (var listDoc in listsQuery.Documents)
                    {
                        // בדיקה מי עוד חבר ברשימה הספציפית הזו באמצעות קולקציית הגשר 'UserList'
                        var membersSnapshot = await FirebaseFirestore.Instance.Collection("UserList")
                            .WhereEqualTo("ListId", listDoc.Id).Get();
                        var membersQuery = membersSnapshot as QuerySnapshot;

                        // שימוש בטכנולוגיית LINQ של C# על מנת לסנן את המשתמש הנמחק ולמיין את השותפים
                        // לפי הזמן שבו הצטרפו (JoinAt) מהישן ביותר לחדיש ביותר.
                        // היתרון הגדול: מונע את הצורך ביצירת אינדקסים מורכבים (Composite Index) בתוך הגדרות ה-Firebase.
                        var otherMembers = membersQuery?.Documents
                                    .Where(d => d.GetString("UserId") != userId)
                                    .OrderBy(d => d.Contains("JoinAt") && d.GetLong("JoinAt") != null
                                    ? d.GetLong("JoinAt").LongValue() // חילוץ הערך והמרה לטיפוס long של סישארפ
                                    : long.MaxValue).ToList();

                        if (otherMembers != null && otherMembers.Count > 0)
                        {
                            // אם נמצא שותף אחד לפחות, נעביר אליו את הבעלות על הרשימה (עדכון השדה OwnerId)
                            string newOwnerId = otherMembers[0].GetString("UserId");
                            await listDoc.Reference.Update("OwnerId", newOwnerId);
                        }
                        else
                        {
                            // במידה ואין אף שותף אחר לרשימה, נמחק אותה לחלוטין.
                            // ב-Firestore, מחיקת מסמך אב אינה מוחקת אוטומטית את תתי-הקולקציות שלו (Sub-collections),
                            // לכן עלינו קודם כל למחוק ידנית את כל המסמכים הנמצאים בתוך תת-הקולקציה "items".
                            var itemsSnapshot = await listDoc.Reference.Collection("items").Get();
                            var itemsQuery = itemsSnapshot as QuerySnapshot;
                            if (itemsQuery != null && itemsQuery.Documents != null)
                            {
                                foreach (var itemDoc in itemsQuery.Documents)
                                {
                                    await itemDoc.Reference.Delete(); // מחיקת פריט בתוך הרשימה
                                }
                            }
                            await listDoc.Reference.Delete(); // מחיקת מסמך הרשימה עצמו
                        }
                    }
                }

                // === שלב 2: ניקוי כל רשומות הקישור והגשר של המשתמש מקולקציית UserList ===
                var userListSnapshot = await FirebaseFirestore.Instance.Collection("UserList")
                    .WhereEqualTo("UserId", userId).Get();
                var userListQuery = userListSnapshot as QuerySnapshot;
                if (userListQuery != null && userListQuery.Documents != null)
                {
                    foreach (var doc in userListQuery.Documents)
                    {
                        await doc.Reference.Delete(); // הסרת הקישור מהטבלה המקשרת
                    }
                }

                // === שלב 3: מחיקת מסמך המשתמש הראשי מקולקציית users ===
                await FirebaseFirestore.Instance.Collection("users").Document(userId).Delete();
                return true; // סימון שהמחיקה המורכבת בוצעה בהצלחה מלאה
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("FireBaseHelper", $"Error deleting user: {ex.Message}");
                return false; // סימון כשל בתהליך
            }
        }

        // הפעלת מאזין קבוע (Realtime Listener) שמקשיב לכל שינוי שמתבצע בקולקציית הרשימות (lists)
        public static void FetchListsListener()
        {
            listener = new FirestoreEventListener();
            // הרשמה לאירוע הצילום (Snapshot) של Firestore - יפעיל את האובייקט שלנו בכל עדכון בשרת
            Registration = FirebaseFirestore.Instance
                .Collection("lists")
                .AddSnapshotListener(listener);
        }

        // פונקציה לניתוק והפסקת פעילות המאזין של הרשימות (חשוב מאוד למניעת זליגות זיכרון וצריכת נתונים מיותרת)
        public static void StopListsListener()
        {
            Registration?.Remove(); // הסרת הרישום בשרת
            Registration = null;
            listener = null;
        }

        // הפעלת מאזין קבוע בזמן אמת לשינויים בטבלת המשתמשים
        public static void FetchUsersListener()
        {
            listener = new FirestoreEventListener();
            Registration = FirebaseFirestore.Instance
                .Collection("users")
                .AddSnapshotListener(listener);
        }

        // עצירת המאזין של קולקציית המשתמשים
        public static void StopUsersListener()
        {
            Registration?.Remove();
            Registration = null;
            listener = null;
        }
        #endregion

        #region App Data
        #endregion

        #region Lists (ניהול רשימות פריטים)

        // פונקציה אסינכרונית ליצירת רשימה חדשה ויצירת מסמך קישור מתאים בטבלת הגשר
        public static async Task<bool> CreateList(string title, string ownerId, string type)
        {
            try
            {
                var firestore = FirebaseFirestore.Instance;
                // יצירת מזהה ייחודי (GUID), חיתוך 6 התווים הראשונים שלו והפיכתם לקוד הצטרפות קצר וקריא באותיות גדולות
                string joinCode = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();

                // 1. הגדרת נתוני הרשימה החדשה בתוך מילון ייעודי של אנדרואיד (JavaDictionary)
                var listData = new Android.Runtime.JavaDictionary<string, object>
                {
                    { "Title", title },
                    { "ownerId", ownerId },
                    { "type", type },
                    { "joinCode", joinCode }
                };
                // הוספת חותמת זמן רשמית של השרת (ServerTimestamp) כדי להבטיח אחידות בין מכשירים שונים
                listData.Add("createdAt", FieldValue.ServerTimestamp());

                // הוספת המסמך לקולקציית ה-lists (השרת ייצר מזהה מסמך רנדומלי באופן אוטומטי)
                await firestore.Collection("lists").Add(listData);

                // 2. יצירת מסמך מקשר בקולקציית הגשר 'UserList'
                // חיוני ביותר: ללא יצירת הקישור הזה, פונקציות טעינת רשימות המשתמש לא ידעו שהבעלים שייך לרשימה זו!
                var mappingData = new Android.Runtime.JavaDictionary<string, object>
                {
                    { "UserID", ownerId },
                    { "joinCode", joinCode }
                };

                await firestore.Collection("UserList").Add(mappingData);
                return true;
            }
            catch (Exception ex)
            {
                Log.Debug("FirebaseError", "Crash in CreateList: " + ex.Message);
                return false;
            }
        }

        // פונקציה השולפת את כמות כל הרשימות הקיימות במסד הנתונים הגלובלי
        public static async System.Threading.Tasks.Task<int> GetGlobalListsCount()
        {
            try
            {
                // 1. ביצוע שליפה גלובלית וביצוע המרה מפורשת (Casting) ל-QuerySnapshot כדי שהקומפיילר יזהה את התכונות
                var result = await FirebaseFirestore.Instance.Collection("lists").Get();
                var querySnapshot = result as QuerySnapshot;

                // 2. החזרת כמות המסמכים שנמצאו באמצעות שימוש בנתיב ה-C#-אי התקני (.Documents.Count)
                if (querySnapshot != null && querySnapshot.Documents != null)
                {
                    return querySnapshot.Documents.Count;
                }
                return 0;
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("FireBaseHelper", $"Error counting lists: {ex.Message}");
                return 0;
            }
        }

        // עדכון מצב הסימון (נרכש/בוצע או לא) של פריט בודד השוכן בתוך תת-קולקציית המוצרים הפנימית
        public static async Task ToggleItemStatus(string listId, string itemId, bool isChecked)
        {
            await FirebaseFirestore.Instance
                .Collection("lists").Document(listId)
                .Collection("items").Document(itemId)
                .Update("isChecked", isChecked); // עדכון שדה ה-Boolean ישירות בשרת
        }

        // הסרת משתמש מסוים מרשימה משותפת על ידי מחיקת רשומת הגשר שלו בקולקציית UserList
        public static async Task RemoveUserFromList(string joinCode, string userIdToRemove)
        {
            var firestore = FirebaseFirestore.Instance;

            // חיפוש מסמך הקישור הספציפי שבו גם ה-UserID וגם ה-joinCode מתאימים לפרמטרים שקיבלנו
            var snapshot = await firestore.Collection("UserList")
                .WhereEqualTo("UserID", userIdToRemove)
                .WhereEqualTo("joinCode", joinCode)
                .Get();

            var query = snapshot as QuerySnapshot;
            // ריצה על המסמכים שנמצאו (בדרך כלל מסמך יחיד) ומחיקת הקישור מהשרת
            foreach (var doc in query.Documents)
            {
                await doc.Reference.Delete();
            }
        }

        // הוספת מוצר/פריט חדש לתוך תת-הקולקציה הפנימית "items" של רשימה קיימת
        public static async Task AddItemToList(string listId, string text)
        {
            HashMap itemMap = new HashMap();
            itemMap.Put("text", text);
            itemMap.Put("isChecked", false); // אתחול כברירת מחדל כ"לא מסומן" על מנת למנוע קריסות בעת עדכון עתידי

            await FirebaseFirestore.Instance
                .Collection("lists")
                .Document(listId)
                .Collection("items")
                .Add(itemMap); // הוספה ישירה לתת-הקולקציה של מסמך האב
        }

        // שליפת כל מחרוזות הטקסט של הפריטים השייכים לרשימה מסוימת
        public static async Task<List<string>> GetItems(string listId)
        {
            List<string> items = new List<string>();

            try
            {
                // שליפת כל המסמכים הנמצאים בתוך תת-הקולקציה הפנימית "items" של הרשימה המבוקשת
                var snapshot = await FirebaseFirestore.Instance
                    .Collection("lists")
                    .Document(listId)
                    .Collection("items")
                    .Get();

                var docs = (QuerySnapshot)snapshot;

                // מעבר על כל מסמך פריט, חילוץ שדה הטקסט שלו והוספה לרשימה המקומית
                foreach (DocumentSnapshot doc in docs.Documents)
                {
                    items.Add(doc.Get("text").ToString());
                }

                return items;
            }
            catch (Exception ex)
            {
                Log.Error(ProManager.TAG, "GetItems failed: " + ex.Message);
                return items; // החזרת הרשימה (אפילו אם היא ריקה או מלאה בחלקרה) במקרה של שגיאה
            }
        }
        #endregion
    }

    // מחלקת התשתית הג'אוואית המשמשת כמאזין לאירועי ה-Snapshot של Firestore וממירה אותם לאירועים (Events) של סישארפ
    public class FirestoreEventListener : Java.Lang.Object, Firebase.Firestore.IEventListener
    {
        // הגדרת אירוע (Event) שרכיבים חיצוניים באפליקציה יכולים להירשם אליו כדי לקבל את המידע העדכני
        public event EventHandler<TaskListenerEventArgs> getEvent;

        // מחלקת עזר פנימית לעטיפת הנתונים הגולמיים המגיעים משרתי גוגל
        public class TaskListenerEventArgs : EventArgs
        {
            public Java.Lang.Object Result { get; set; }
        }

        // פונקציית היעד של ה-SDK הרשמי של אנדרואיד - מופעלת אוטומטית בכל שינוי בבסיס הנתונים
        public void OnEvent(Java.Lang.Object obj, FirebaseFirestoreException error)
        {
            // הפעלת ה-Event של סישארפ והזרקת אובייקט התוצאה (obj) לתוכו לשימוש המסכים השונים
            getEvent?.Invoke(this, new TaskListenerEventArgs { Result = obj });
        }
    }
}