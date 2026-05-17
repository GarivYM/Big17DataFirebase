// ייבוא ספריות הליבה של אנדרואיד לעבודה עם רכיבי מערכת, חלונות וממשק משתמש
using Android.App;
using Android.Content;
using Android.Gms.Extensions;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
// ייבוא רכיב ה-RecyclerView של AndroidX להצגת רשימות
using AndroidX.RecyclerView.Widget;
// ייבוא מחלקות ואדאפטרים מתוך הפרויקט שלך
using Big17DataFirebase2.Adapters;
using Big17DataFirebase2.Model;
using Big17DataFirebase2.Service;
// ייבוא רכיבי ה-SDK של Firebase עבור אימות (Auth) ומסד הנתונים (Firestore)
using Firebase.Auth;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Big17DataFirebase2
{
    // הגדרת האקטיביטי כחלק מהאפליקציה (לא מסך ראשי)
    [Activity(Label = "ListActivity", MainLauncher = false)]
    public class ListActivity : Activity
    {
        // רכיבי ממשק המשתמש (UI) המופיעים בראש הדף
        TextView tvDelete, tvAdd, tvTitle;
        RecyclerView recyclerView;

        // רכיבי הניהול והתיווך של ה-RecyclerView
        RecyclerView.LayoutManager layoutManager;
        ItemsRViewAdapter adapter;

        // רשימת הפריטים המקומית בזיכרון של המסך
        List<Item> items;
        // רכיב דיאלוג להצגת גלגל הטעינה (ProgressBar)
        Dialog mProgressDialog;
        // משתנה לשמירת ה-ID של הרשימה הנוכחית שקיבלנו מהמסך הקודם
        string currentListId;

        // חדש: משתנה לשמירת קוד ההצטרפות של הרשימה הנוכחית.
        // נצטרך אותו כדי למצוא ולמחוק את כל הקישורים של המשתמשים לרשימה הזו מקולקציית UserList.
        string currentJoinCode = "";

        // פונקציית מחזור החיים שרצה בעת יצירת המסך לראשונה
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // חיבור הקוד לקובץ ה-Layout של ה-XML
            SetContentView(Resource.Layout.listlayout);

            // חילוץ הנתונים שנשלחו באמצעות ה-Intent מהמסך הקודם (HomeActivity)
            currentListId = Intent.GetStringExtra("listId"); // מזהה הרשימה ב-Firestore
            string listName = Intent.GetStringExtra("listTitle"); // שם הרשימה להצגה

            // אתחול רכיבי ה-UI שעל המסך
            InitializeViews(listName);

            // חדש: הפעלת בדיקה אסינכרונית ברקע - האם המשתמש המחובר כרגע הוא מנהל הרשימה?
            CheckIfUserIsManager();
        }

        // פונקציה לאתחול הפקדים, קישור ה-XML וחיבור המאזינים
        private void InitializeViews(string title)
        {
            // קישור משתני ה-C# לרכיבי ה-XML לפי ה-ID שלהם
            tvDelete = FindViewById<TextView>(Resource.Id.tvDelete);
            tvAdd = FindViewById<TextView>(Resource.Id.tvAdd);
            tvTitle = FindViewById<TextView>(Resource.Id.tvTitle);
            recyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);

            // חדש ובטוח: כברירת מחדל, נעלים לחלוטין את כפתור המחיקה מהמסך (Gone תופס 0 מקום ב-Layout).
            // הוא ייחשף רק בהמשך במידה ונזהה שהמשתמש הוא אכן ה-Manager (הבעלים).
            tvDelete.Visibility = ViewStates.Gone;

            // מציאת כפתור המידע (אייקון ה-i) וחיבור אירוע לחיצה לפתיחת הפופ-אפ של המשתתפים
            ImageView btnInfo = FindViewById<ImageView>(Resource.Id.btnInfo);
            btnInfo.Click += async (s, e) => {
                await ShowListInfoPopup();
            };

            // השמת שם הרשימה בכותרת, או טקסט ברירת מחדל אם הוא ריק
            tvTitle.Text = title ?? "List Page";

            // חיבור אירועי לחיצה לפונקציות ההוספה והמחיקה
            tvDelete.Click += TvDelete_Click;
            tvAdd.Click += TvAdd_Click;

            // הגדרת פריסה אנכית רגילה עבור ה-RecyclerView
            layoutManager = new LinearLayoutManager(this);
            recyclerView.SetLayoutManager(layoutManager);

            // יצירת הרשימה המקומית וחיבור האדאפטר הייעודי לפריטים
            items = new List<Item>();
            adapter = new ItemsRViewAdapter(items);

            // האזנה לאירוע שינוי מצב ה-CheckBox באדאפטר ועדכונו בשרת בזמן אמת
            adapter.CheckChanged += async (sender, position) =>
            {
                if (position >= 0 && position < items.Count)
                {
                    var clickedItem = items[position];
                    try
                    {
                        // עדכון שדה ה-isChecked ישירות בתוך תת-הקולקציה של הרשימה הספציפית ב-Firestore
                        await FirebaseFirestore.Instance
                            .Collection("lists")
                            .Document(currentListId)
                            .Collection("items")
                            .Document(clickedItem.Id)
                            .Update("isChecked", clickedItem.IsChecked);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("ListActivity", "Error updating item checked state: " + ex.Message);
                    }
                }
            };

            // חיבור האדאפטר המוכן ל-RecyclerView
            recyclerView.SetAdapter(adapter);
        }

        // חדש: פונקציה הבודקת האם המשתמש הנוכחי מורשה לראות את כפתור המחיקה
        private async void CheckIfUserIsManager()
        {
            // שליפת ה-UID הייחודי של המשתמש שמחובר כרגע למכשיר
            var currentUserId = FirebaseAuth.Instance.CurrentUser?.Uid;
            if (currentUserId == null) return;

            try
            {
                // משיכת מסמך הרשימה הנוכחית מתוך קולקציית 'lists' ב-Firestore
                var docSnapshot = await FirebaseFirestore.Instance.Collection("lists").Document(currentListId).Get() as DocumentSnapshot;

                if (docSnapshot != null && docSnapshot.Exists())
                {
                    // שליפת ה-ownerId (המזהה של יוצר הרשימה) מתוך הנתונים בשרת
                    string ownerId = docSnapshot.Get("ownerId")?.ToString();

                    // שמירת ה-joinCode של הרשימה במשתנה הגלובלי לצורך שימוש עתידי במחיקה
                    currentJoinCode = docSnapshot.Get("joinCode")?.ToString() ?? "";

                    // השוואה: אם ה-UID של המשתמש במכשיר זהה לחלוטין ל-ownerId של הרשימה - הוא המנהל!
                    if (currentUserId == ownerId)
                    {
                        // הפיכת כפתור המחיקה לגלוי ונראה לעין על המסך
                        tvDelete.Visibility = ViewStates.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("ListActivity", "Error checking manager status: " + ex.Message);
            }
        }

        // פונקציית מחזור חיים שמופעלת כשהמסך חוזר לקדמת הבמה - מפעילה את האזנת הנתונים
        protected override void OnResume()
        {
            base.OnResume();
            ShowProgressBar(true); // הצגת גלגל טעינה
            FetchItemsFromDB(); // משיכת הפריטים מהדאטהבייס
        }

        // פונקציה המאזינה בזמן אמת לשינויים בתת-קולקציית הפריטים (items)
        private void FetchItemsFromDB()
        {
            var firestore = FirebaseFirestore.Instance;

            firestore.Collection("lists")
                     .Document(currentListId)
                     .Collection("items")
                     .AddSnapshotListener(new MyEventListener((value, error) =>
                     {
                         ShowProgressBar(false); // הסרת גלגל הטעינה ברגע שהמידע הגיע

                         if (error != null)
                         {
                             Log.Debug("ListActivity", error.Message);
                             return;
                         }

                         var snapshot = value as QuerySnapshot;

                         if (snapshot != null)
                         {
                             items.Clear(); // ניקוי הרשימה המקומית למניעת כפילויות

                             // מעבר על כל מסמכי הפריטים שחזרו מהשרת
                             foreach (DocumentSnapshot doc in snapshot.Documents)
                             {
                                 var checkedObj = doc.Get("isChecked") ?? doc.Get("IsChecked");
                                 bool isCheckedValue = false;

                                 if (checkedObj != null)
                                 {
                                     if (checkedObj.GetType() == typeof(Java.Lang.Boolean))
                                         isCheckedValue = ((Java.Lang.Boolean)checkedObj).BooleanValue();
                                     else
                                         bool.TryParse(checkedObj.ToString(), out isCheckedValue);
                                 }

                                 // הוספת הפריט שחולץ אל קולקציית הפריטים של ה-UI
                                 items.Add(new Item
                                 {
                                     Id = doc.Id,
                                     Name = doc.Get("name")?.ToString() ?? doc.Get("Name")?.ToString() ?? "Unnamed",
                                     IsChecked = isCheckedValue
                                 });
                             }
                             // הוראה לאדאפטר לצייר מחדש את הרשימה המעודכנת על המסך
                             adapter.NotifyDataSetChanged();
                         }
                     }));
        }

        // פונקציית עזר לניהול דיאלוג גלגל הטעינה במסך
        private void ShowProgressBar(bool show)
        {
            if (show)
            {
                if (mProgressDialog == null)
                {
                    mProgressDialog = new Dialog(this, Android.Resource.Style.ThemeNoTitleBar);
                    View view = LayoutInflater.From(this).Inflate(Resource.Layout.fb_progressbar, null);
                    mProgressDialog.Window.SetBackgroundDrawableResource(Android.Resource.Color.Transparent);
                    mProgressDialog.SetContentView(view);
                    mProgressDialog.SetCancelable(false);
                }
                if (!mProgressDialog.IsShowing) mProgressDialog.Show();
            }
            else
            {
                mProgressDialog?.Dismiss();
            }
        }

        // חדש: פונקציה שמופעלת כאשר מנהל הרשימה לוחץ על כפתור המחיקה (tvDelete)
        private void TvDelete_Click(object sender, EventArgs e)
        {
            // יצירת חלון אזהרה (AlertDialog) כדי לוודא שהמשתמש לא לחץ בטעות
            new AlertDialog.Builder(this)
                .SetTitle("Delete List")
                .SetMessage("Are you sure you want to delete this list and all its items permanently?")

                // במידה והוא אישר ולחץ על כפתור ה-Delete בדיאלוג:
                .SetPositiveButton("Delete", async (dialogSender, el) =>
                {
                    ShowProgressBar(true); // הפעלת גלגל הטעינה (כי מחיקה משורשרת לוקחת קצת זמן ברשת)

                    // קריאה לפונקציית המחיקה המשורשרת שכתבנו למטה
                    bool success = await DeleteListFromFirebaseCascade();

                    ShowProgressBar(false); // הסרת גלגל הטעינה בסיום התהליך

                    if (success)
                    {
                        // הצגת הודעת הצלחה וסגירת המסך הנוכחי כדי לחזור אוטומטית למסך הבית
                        Toast.MakeText(this, "List deleted successfully", ToastLength.Short).Show();
                        Finish();
                    }
                    else
                    {
                        // במקרה של תקלה ברשת או חוסר הרשאות ב-Firebase Rules
                        Toast.MakeText(this, "Failed to delete list", ToastLength.Short).Show();
                    }
                })
                .SetNegativeButton("Cancel", (dialogSender, el) => { }) // כפתור ביטול - סוגר את הדיאלוג ללא שינוי
                .Show();
        }

        // חדש: פונקציית ליבה אסינכרונית שמבצעת "מחיקה משורשרת" (Cascade Delete) מכל הטבלאות
        private async Task<bool> DeleteListFromFirebaseCascade()
        {
            var firestore = FirebaseFirestore.Instance;

            try
            {
                // שלב א': מחיקת כל המסמכים (הפריטים) שנמצאים בתוך תת-הקולקציה 'items' של הרשימה הזו.
                // הערה: ב-Firestore, מחיקת מסמך אב לא מוחקת אוטומטית את תתי-הקולקציות שלו, לכן חובה למחוק אותם ידנית אחד-אחד!
                var itemsSnapshot = await firestore.Collection("lists")
                                                   .Document(currentListId)
                                                   .Collection("items")
                                                   .Get() as QuerySnapshot;

                if (itemsSnapshot != null && itemsSnapshot.Documents.Count > 0)
                {
                    foreach (var doc in itemsSnapshot.Documents)
                    {
                        await firestore.Collection("lists")
                                       .Document(currentListId)
                                       .Collection("items")
                                       .Document(doc.Id)
                                       .Delete();
                    }
                }

                // שלב ב': מחיקת מסמך הרשימה הראשי מתוך קולקציית 'lists' בשרת
                await firestore.Collection("lists").Document(currentListId).Delete();

                // שלב ג': ניקוי טבלת הקישורים 'UserList'.
                // נבצע שאילתה שתחפש את כל המסמכים שבהם ה-joinCode שווה לקוד של הרשימה שנמחקה, ונמחק את כולם.
                // זה יגרום לכך שהרשימה תיעלם אוטומטית ממסכי הבית (HomeActivity) של כל שאר חברי הרשימה!
                if (!string.IsNullOrEmpty(currentJoinCode))
                {
                    var userListsSnapshot = await firestore.Collection("UserList")
                                                           .WhereEqualTo("joinCode", currentJoinCode)
                                                           .Get() as QuerySnapshot;

                    if (userListsSnapshot != null && userListsSnapshot.Documents.Count > 0)
                    {
                        foreach (var doc in userListsSnapshot.Documents)
                        {
                            await firestore.Collection("UserList").Document(doc.Id).Delete();
                        }
                    }
                }

                return true; // כל השלבים עברו בהצלחה, מחזירים אמת
            }
            catch (Exception ex)
            {
                // רישום שגיאה במידה ומשהו נכשל (למשל: אינטרנט מנותק)
                Log.Error("ListActivity_Delete", "Error during cascade delete: " + ex.Message);
                return false;
            }
        }

        // פונקציה המופעלת בלחיצה על כפתור הוספת פריט (tvAdd)
        private void TvAdd_Click(object sender, EventArgs e)
        {
            ShowAddItemDialog();
        }

        // מציגה דיאלוג עם שדה טקסט להוספת פריט חדש לרשימה ב-Firestore
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

        // פונקציה המציגה חלון מידע מותאם אישית (Popup) עם רשימת המשתתפים שחברים ברשימה
        private async Task ShowListInfoPopup()
        {
            var firestore = FirebaseFirestore.Instance;
            var currentUserId = FirebaseAuth.Instance.CurrentUser?.Uid;
            string listId = Intent.GetStringExtra("listId");

            if (currentUserId == null) return;

            try
            {
                var result = await firestore.Collection("lists").Document(listId).Get();
                var listDoc = result as DocumentSnapshot;

                if (listDoc == null || !listDoc.Exists()) return;

                string joinCode = listDoc.Get("joinCode")?.ToString() ?? "N/A";
                string ownerId = listDoc.Get("ownerId")?.ToString();

                View dialogView = LayoutInflater.From(this).Inflate(Resource.Layout.dialog_list_info, null);
                TextView tvJoinCode = dialogView.FindViewById<TextView>(Resource.Id.tvInfoJoinCode);
                LinearLayout container = dialogView.FindViewById<LinearLayout>(Resource.Id.participantsContainer);

                tvJoinCode.Text = joinCode;
                container.RemoveAllViews();

                var userListResult = await firestore.Collection("UserList")
                                                    .WhereEqualTo("joinCode", joinCode)
                                                    .Get();
                var userListQuery = userListResult as QuerySnapshot;

                if (userListQuery != null)
                {
                    foreach (var doc in userListQuery.Documents)
                    {
                        string participantUid = doc.Get("UserID")?.ToString();
                        if (string.IsNullOrEmpty(participantUid)) continue;

                        var userResult = await firestore.Collection("users").Document(participantUid).Get();
                        var userDoc = userResult as DocumentSnapshot;

                        string fullName = "Unknown User";
                        if (userDoc != null && userDoc.Exists())
                        {
                            string fName = userDoc.Get("FirstName")?.ToString() ?? userDoc.Get("firstName")?.ToString() ?? "";
                            string lName = userDoc.Get("LastName")?.ToString() ?? userDoc.Get("lastName")?.ToString() ?? "";

                            if (!string.IsNullOrEmpty(fName) || !string.IsNullOrEmpty(lName))
                            {
                                fullName = $"{fName} {lName}".Trim();
                            }
                        }

                        List<string> tags = new List<string>();
                        if (participantUid == ownerId) tags.Add("List Manager");
                        if (participantUid == currentUserId) tags.Add("You");

                        string tagString = tags.Count > 0 ? $" ({string.Join(", ", tags)})" : "";

                        RunOnUiThread(() => {
                            TextView tvPerson = new TextView(this);
                            tvPerson.Text = $"• {fullName}{tagString}";
                            tvPerson.TextSize = 16;
                            tvPerson.SetPadding(0, 12, 0, 12);
                            tvPerson.SetTextColor(Android.Graphics.Color.Black);

                            container.AddView(tvPerson);
                        });
                    }
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