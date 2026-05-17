// ייבוא ספריות הליבה של אנדרואיד לעבודה עם רכיבים גרפיים, דיאלוגים, רשימות וניהול מצבי תצוגה
using Android.App;
using Android.Content;
using Android.Gms.Extensions;
using Android.OS;
using Android.Views;
using Android.Widget;
// ייבוא מחלקות הבסיס של AndroidX לעבודה עם רכיבי Fragment מתקדמים ו-RecyclerView
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
// ייבוא שכבות האדפטרים והשירותים (כמו FireBaseHelper) של האפליקציה שלך
using Big17DataFirebase2.Adapters;
using Big17DataFirebase2.Service;
// ייבוא רכיבי ה-SDK הרשמיים של Firebase לאימות משתמשים ומסד הנתונים Firestore
using Firebase.Auth;
using Firebase.Firestore;
using System;
using System.Collections.Generic;

namespace Big17DataFirebase2
{
    // הגדרת המחלקה כיורשת של DialogFragment, המאפשרת לה להיות מוצגת כחלון מודאלי (Popup) מעל מסכים אחרים
    public class AccountFragment : AndroidX.Fragment.App.DialogFragment
    {
        // רכיבי קלט גרפיים לעריכת פרטי המשתמש
        EditText etFirstName, etLastName, etEmail, etMobile;
        Button btnUpdate;            // כפתור לעדכון הנתונים
        ImageButton ibAccountDelete; // כפתור מחיקת המשתמש (זמין לאדמין או למשתמש עצמו)
        string userId;               // מזהה המשתמש (UID) שבו אנו צופים כרגע
        bool isAdmin = false;        // דגל בוליאני המשמש לבדיקה: האם הצופה הנוכחי במסך הוא אדמין במצב ניהול?

        // רכיבי ממשק משתמש (UI) ואדפטרים להצגת הרשימות של המשתמש (רלוונטי רק במצב צפיית אדמין)
        TextView tvAdminListsTitle;  // כותרת טקסט עבור רשימות המשתמש (מוסתרת אם מדובר במשתמש רגיל)
        RecyclerView rvUserLists;     // רשימה דינמית להצגת הרשימות שהמשתמש חבר בהן
        AdminListsAdapter listsAdapter; // האדפטר הייעודי לציור שורות הרשימה של האדמין
        List<KeyValuePair<string, string>> userListsData; // אוסף מקומי בזיכרון של זוגות מפתח-ערך (ID של הרשימה + שם הרשימה)

        // פונקציית מחזור החיים הראשונית לקביעת סגנון ועיצוב חלון הדיאלוג
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // הגדרת החלון ללא כותרת מובנית ושימוש בעיצוב Material Light עם רוחב מינימלי מוגדר
            SetStyle(StyleNoTitle, Android.Resource.Style.ThemeMaterialLightDialogMinWidth);
        }

        // פונקציית מחזור החיים האחראית על ניפוח (Inflate) ה-Layout וקישור רכיבי ממשק המשתמש
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // טעינת קובץ ה-XML הויזואלי (account_layout1) והפיכתו לאובייקט View בקוד
            View view = inflater.Inflate(Resource.Layout.account_layout1, container, false);

            // מיפוי וקישור כל שדות הקלט והכפתורים מתוך קובץ ה-XML
            etFirstName = view.FindViewById<EditText>(Resource.Id.et_account_first_name);
            etLastName = view.FindViewById<EditText>(Resource.Id.et_account_last_name);
            etEmail = view.FindViewById<EditText>(Resource.Id.et_account_email);
            etMobile = view.FindViewById<EditText>(Resource.Id.et_account_mobile);
            btnUpdate = view.FindViewById<Button>(Resource.Id.btn_account_update);
            ibAccountDelete = view.FindViewById<ImageButton>(Resource.Id.ibAccountDelete);

            // מציאת כותרת הרשימות של האדמין מתוך ה-XML
            tvAdminListsTitle = view.FindViewById<TextView>(Resource.Id.tvAdminListsTitle);

            // אתחול ה-RecyclerView וקביעת מנהל פריסה אנכי (LinearLayoutManager)
            rvUserLists = view.FindViewById<RecyclerView>(Resource.Id.rvAdminUserLists);
            rvUserLists.SetLayoutManager(new LinearLayoutManager(Context));

            // בדיקה האם הועברו ארגומנטים (Bundle) בעת פתיחת ה-Fragment
            if (Arguments != null)
            {
                // שליפת ה-ID של המשתמש שבו אנו צופים
                userId = Arguments.GetString("UserID", "");
                // הדפסת בדיקה ל-Logcat לוודא שה-ID הגיע בהצלחה לפרגמנט
                Android.Util.Log.Debug("ADMIN_DEBUG", $"Fragment received UserID: '{userId}'");

                // מילוי שדות הקלט בנתונים הנוכחיים שהתקבלו מהמסך הקודם
                etFirstName.Text = Arguments.GetString("FirstName", "");
                etLastName.Text = Arguments.GetString("LastName", "");
                etEmail.Text = Arguments.GetString("Email", "");
                etMobile.Text = Arguments.GetString("Mobile", "");

                // === בדיקת אבטחה והרשאות דינמית ===
                // שליפת ה-UID של המשתמש שמחובר כרגע פיזית למכשיר מתוך Firebase Auth
                string currentLoggedInUid = FirebaseAuth.Instance.CurrentUser?.Uid;

                // אם ה-ID של הפרופיל המוצג שווה ל-ID של המשתמש המחובר, סימן שהוא צופה בחשבון של עצמו ולא אדמין
                if (!string.IsNullOrEmpty(userId) && userId == currentLoggedInUid)
                {
                    isAdmin = false; // מצב משתמש רגיל
                }
                else
                {
                    isAdmin = true; // ה-ID שונה, המשמעות היא שמנהל מערכת צופה במשתמש מתוך מסך הניהול
                }
            }
            else
            {
                // תיעוד לוג במידה וה-Bundle הגיע ריק (תקלה בהעברת הנתונים)
                Android.Util.Log.Debug("ADMIN_DEBUG", "Arguments bundle is NULL!");
            }

            // הגדרת כפתור המחיקה כגלוי על המסך כברירת מחדל
            ibAccountDelete.Visibility = ViewStates.Visible;

            // חיבור אירועי לחיצה לכפתור העדכון וכפתור המחיקה
            btnUpdate.Click += BtnUpdate_Click;
            ibAccountDelete.Click += IbAccountDelete_Click;

            // קריאה לפונקציית טעינת הרשימות - הלוגיקה הפנימית שלה תחליט אם להציג אותן (לאדמין) או להסתירן (למשתמש רגיל)
            LoadUserLists();

            // החזרת ה-View המוכן לאנדרואיד להצגה על המסך
            return view;
        }

        // פונקציה אסינכרונית לטעינת רשימות המשויכות למשתמש (מוצג רק לאדמינים)
        private async void LoadUserLists()
        {
            // חסימת אבטחה ותצוגה: אם הצופה אינו אדמין, נעלים לחלוטין את ה-RecyclerView ואת הכותרת שלו מהמסך
            if (!isAdmin)
            {
                if (rvUserLists != null)
                {
                    rvUserLists.Visibility = ViewStates.Gone; // העלמת רכיב הרשימה מהמסך ואי ניצול מקום (Gone)
                }
                if (tvAdminListsTitle != null)
                {
                    tvAdminListsTitle.Visibility = ViewStates.Gone; // העלמת טקסט הכותרת מהמסך
                }
                Android.Util.Log.Debug("ADMIN_DEBUG", "LoadUserLists aborted: User is viewing their own profile. UI hidden.");
                return; // יציאה מהפונקציה - משתמש רגיל לא צריך לראות או לטעון מידע זה
            }

            // אם הגענו לכאן - הצופה הוא אדמין. נאתחל את אוסף הנתונים לרשימות
            userListsData = new List<KeyValuePair<string, string>>();
            try
            {
                // הגנה: ודואים שיש לנו מזהה משתמש תקין לעבודה
                if (string.IsNullOrEmpty(userId))
                {
                    Android.Util.Log.Error("ADMIN_DEBUG", "LoadUserLists aborted: userId is EMPTY or NULL!");
                    return;
                }

                Android.Util.Log.Debug("ADMIN_DEBUG", $"Searching 'UserList' collection where UserID == '{userId}'...");

                // 1. שליפת כל קשרי הגשר מקולקציית UserList השייכים למשתמש הנבדק
                var userListSnap = await FirebaseFirestore.Instance.Collection("UserList")
                    .WhereEqualTo("UserID", userId).Get();

                var querySnap = userListSnap as QuerySnapshot;

                if (querySnap != null)
                {
                    Android.Util.Log.Debug("ADMIN_DEBUG", $"Found {querySnap.Documents?.Count ?? 0} documents in UserList for this user.");

                    if (querySnap.Documents != null)
                    {
                        // 2. לולאה על כל מסמכי הקישור שנמצאו לצורך חילוץ קוד ההצטרפות (joinCode)
                        foreach (var doc in querySnap.Documents)
                        {
                            string joinCode = doc.Get("joinCode")?.ToString();
                            Android.Util.Log.Debug("ADMIN_DEBUG", $"Extracted joinCode: '{joinCode}' from doc ID: {doc.Id}");

                            if (string.IsNullOrEmpty(joinCode)) continue;

                            // 3. שליפת מסמך הרשימה המלא מקולקציית lists על פי קוד ההצטרפות (joinCode)
                            var listDocSnap = await FirebaseFirestore.Instance.Collection("lists")
                                .WhereEqualTo("joinCode", joinCode).Get();

                            var listQuerySnap = listDocSnap as QuerySnapshot;

                            // במידה ונמצאה רשימה תואמת בשרת
                            if (listQuerySnap != null && !listQuerySnap.IsEmpty)
                            {
                                var listDoc = listQuerySnap.Documents[0]; // לוקחים את המסמך הראשון שנמצא
                                string listId = listDoc.Id;
                                string listName = listDoc.Get("Title")?.ToString() ?? "Unnamed List";

                                Android.Util.Log.Debug("ADMIN_DEBUG", $"Successfully matched joinCode to List! ID: {listId}, Name: {listName}");
                                // הוספת מזהה הרשימה ושמה כצמד KeyValuePair לתוך האוסף המקומי במסך
                                userListsData.Add(new KeyValuePair<string, string>(listId, listName));
                            }
                            else
                            {
                                Android.Util.Log.Debug("ADMIN_DEBUG", $"No list found in 'lists' collection with joinCode: '{joinCode}'");
                            }
                        }
                    }
                }

                // 4. אתחול האדפטר הייעודי של האדמין, חיבור לאירוע המחיקה והשמתו ב-RecyclerView
                listsAdapter = new AdminListsAdapter(userListsData);
                listsAdapter.OnDeleteListClick += ListsAdapter_OnDeleteListClick;
                rvUserLists.SetAdapter(listsAdapter);
            }
            catch (Exception ex)
            {
                // תיעוד שגיאות במקרה של כשל בשליפת הנתונים מה-Firestore
                Android.Util.Log.Error("ADMIN_DEBUG", $"Error loading lists: {ex.Message}");
            }
        }

        // פונקציה המופעלת כאשר האדמין לוחץ על כפתור מחיקת רשימה בודדת מתוך ה-RecyclerView
        private void ListsAdapter_OnDeleteListClick(object sender, string listId)
        {
            // יצירת חלון התרעה (AlertDialog) לאישור מחיקה סופית של הרשימה ותכניה
            Android.App.AlertDialog.Builder builder = new Android.App.AlertDialog.Builder(Context);
            builder.SetTitle("Delete List Entirely");
            builder.SetMessage("Are you sure you want to delete this list and all its content from the database?");

            // טיפול בלחיצה על כפתור המחיקה (החיובי)
            builder.SetPositiveButton("Delete", async (s, args) =>
            {
                try
                {
                    // שלב א': מחיקת כל מסמכי המוצרים/פריטים הנמצאים בתוך תת-הקולקציה הפנימית "items" של אותה רשימה
                    var itemsSnap = await FirebaseFirestore.Instance.Collection("lists").Document(listId).Collection("items").Get();
                    var itemsQuery = itemsSnap as QuerySnapshot;
                    if (itemsQuery != null && itemsQuery.Documents != null)
                    {
                        foreach (var itemDoc in itemsQuery.Documents)
                        {
                            // מחיקה פיזית של כל פריט ברשימה מהשרת
                            await itemDoc.Reference.Delete();
                        }
                    }

                    // שלב ב': מחיקת מסמך האב הראשי של הרשימה מתוך קולקציית "lists"
                    await FirebaseFirestore.Instance.Collection("lists").Document(listId).Delete();

                    // שלב ג': ניקוי והסרת כל מסמכי השיוך של משתמשים אחרים לרשימה זו מתוך קולקציית הגשר "UserList"
                    var userListsSnap = await FirebaseFirestore.Instance.Collection("UserList").WhereEqualTo("ListId", listId).Get();
                    var userListsQuery = userListsSnap as QuerySnapshot;
                    if (userListsQuery != null && userListsQuery.Documents != null)
                    {
                        foreach (var doc in userListsQuery.Documents)
                        {
                            // מחיקת מסמך הקישור
                            await doc.Reference.Delete();
                        }
                    }

                    // הצגת הודעת הצלחה למנהל המערכת
                    Toast.MakeText(Context, "List deleted successfully", ToastLength.Short).Show();

                    // רענון אסינכרוני מחדש של תצוגת הרשימות בפרגמנט כדי להעלים את הרשימה שנמחקה
                    LoadUserLists();
                }
                catch (Exception ex)
                {
                    // טיפול והצגת שגיאה במידה ותהליך המחיקה המורכב נכשל באחד השלבים
                    Toast.MakeText(Context, $"Error deleting list: {ex.Message}", ToastLength.Short).Show();
                }
            });

            // במקרה של ביטול, החלון ייסגר ללא ביצוע פעולה
            builder.SetNegativeButton("Cancel", (s, args) => { });
            builder.Show(); // הצגת חלון האישור על גבי המסך
        }

        // פונקציה המופעלת בעת לחיצה על כפתור העדכון (btnUpdate) לשמירת פרטי המשתמש החדשים
        private async void BtnUpdate_Click(object sender, EventArgs e)
        {
            // 1. בדיקת תקינות קלט (ולידציה): וידוא ששדות השם הפרטי ושם המשפחה אינם ריקים
            if (string.IsNullOrEmpty(etFirstName.Text) || string.IsNullOrEmpty(etLastName.Text))
            {
                Toast.MakeText(Context, "Fields cannot be empty", ToastLength.Short).Show();
                return; // עצירת הפונקציה
            }

            try
            {
                // 2. בניית אובייקט User חדש המכיל את הנתונים המעודכנים שהוקלדו על המסך
                Model.User updatedUser = new Model.User()
                {
                    Id = userId, // שמירה על ה-ID המקורי של המשתמש
                    FirstName = etFirstName.Text,
                    LastName = etLastName.Text,
                    UserMobile = etMobile.Text,
                    UserEmail = etEmail.Text
                };

                // 3. קריאה למתודה האסינכרונית במחלקת העזר (FireBaseHelper) לצורך ביצוע פקודת העדכון (Set/Update) ב-Firestore
                await FireBaseHelper.UpdateUser(updatedUser);

                // 4. הצגת הודעת הצלחה וסגירת חלון ה-Fragment (חזרה למסך אבא)
                Toast.MakeText(Context, "User updated successfully!", ToastLength.Short).Show();
                Dismiss();
            }
            catch (System.Exception ex)
            {
                // תפיסת שגיאות עדכון והצגתן למשתמש
                Toast.MakeText(Context, $"Update failed: {ex.Message}", ToastLength.Short).Show();
            }
        }

        // פונקציה המופעלת בעת לחיצה על כפתור מחיקת המשתמש לחלוטין מהמערכת (ibAccountDelete)
        private void IbAccountDelete_Click(object sender, EventArgs e)
        {
            // יצירת חלון התרעה לאישור מחיקה קבועה של המשתמש מהמערכת
            Android.App.AlertDialog.Builder builder = new Android.App.AlertDialog.Builder(Context);
            builder.SetTitle("Delete User Permanently");
            // הצגת הודעה דינמית עם שם המשתמש והסבר על העברת בעלות על רשימותיו למשתמש הוותיק ביותר
            builder.SetMessage($"Are you sure you want to delete {etFirstName.Text}? (Owned lists will be transferred to oldest joined user)");

            // במידה והמשתמש/אדמין אישר סופית את המחיקה
            builder.SetPositiveButton("Yes, Delete", async (s, args) =>
            {
                // קריאה לפונקציה אסינכרונית ב-FireBaseHelper המבצעת את לוגיקת המחיקה ומחזירה ערך בוליאני (האם הצליח)
                bool isDeleted = await FireBaseHelper.DeleteUser(userId);
                if (isDeleted)
                {
                    // הצגת הודעת אישור וסגירת חלון הדיאלוג
                    Toast.MakeText(Context, "User removed from system", ToastLength.Short).Show();
                    Dismiss();
                }
            });

            // כפתור ביטול לסגירת הדיאלוג ללא שינוי
            builder.SetNegativeButton("Cancel", (s, args) => { });
            builder.Show(); // הצגת הדיאלוג למשתמש
        }
    }
}