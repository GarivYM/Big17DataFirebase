// ייבוא ספריות הליבה של אנדרואיד לעבודה עם רכיבים גרפיים, דיאלוגים וניווט
using Android.App;
using Android.Content;
using Android.Gms.Extensions;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
// ייבוא מחלקת הבסיס AppCompatActivity לתמיכה בעיצובים ורכיבים מתקדמים
using AndroidX.AppCompat.App;
// ייבוא רכיב הרשימה הדינמית RecyclerView
using AndroidX.RecyclerView.Widget;
// ייבוא שכבות הלוגיקה העסקית, האדפטרים והמודלים של האפליקציה שלך
using Big17DataFirebase2.Adapters;
using Big17DataFirebase2.BusinessLogic;
// ייבוא רכיבי Firebase הרשמיים לאימות (Auth) ומסד הנתונים (Firestore)
using Firebase.Auth;
using Firebase.Firestore;
// ייבוא כפתור הפעולה הצף (Floating Action Button) של גוגל
using Google.Android.Material.FloatingActionButton;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Big17DataFirebase2
{
    // הגדרת מחלקת מסך הבית. MainLauncher = false מסמן שזהו לא מסך העלייה הראשוני של האפליקציה
    [Activity(Label = "Home Page", MainLauncher = false)]
    public class HomeActivity : AppCompatActivity
    {
        // רכיבי ה-RecyclerView להצגת רשימות הקניות/משימות של המשתמש
        RecyclerView recyclerView;
        RecyclerView.LayoutManager layoutManager;
        ListsRViewAdapter listAdapter; // האדפטר המתווך שמצייר את הרשימות

        // רכיבי ממשק המשתמש (UI)
        TextView tvUserFullName; // שדה להצגת השם המלא של המשתמש המחובר
        TextView tvLeave;        // טקסט לחיץ המשמש כפתור התנתקות מהמערכת
        ImageButton btnAccount;  // כפתור תמונה לפתיחת פרופיל המשתמש
        FloatingActionButton fabAdd; // כפתור ה-+ הצף להוספה או הצטרפות לרשימה

        // נתונים מקומיים בזיכרון המסך
        List<Big17DataFirebase2.Model.List> lists; // רשימת האובייקטים מסוג List שיוצגו במסך
        Dialog mProgressDialog; // חלון דיאלוג עבור גלגל הטעינה (ProgressBar)

        // פונקציית מחזור החיים המופעלת ברגע יצירת המסך בפעם הראשונה
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // קשירת הקוד לקובץ ה-XML הויזואלי של מסך הבית (homepagelayout)
            SetContentView(Resource.Layout.homepagelayout);
            // קריאה לפונקציית האתחול של הרכיבים
            InitializeViews();
        }

        // פונקציה פנימית לקישור רכיבי ה-XML, הגדרת פריסת הרשימה וחיבור המאזינים
        private void InitializeViews()
        {
            // מציאת שדה השם המלא מתוך ה-XML
            tvUserFullName = FindViewById<TextView>(Resource.Id.tvUserFullName);

            // אתחול הטקסט הלחיץ ליציאה (tvLeave) וחיבורו לפונקציית הטיפול בלחיצות
            tvLeave = FindViewById<TextView>(Resource.Id.tvLeave);
            tvLeave.Click += TvLeave_Click;

            // אתחול כפתור החשבון וחיבורו לאירוע הלחיצה שלו
            btnAccount = FindViewById<ImageButton>(Resource.Id.btnAccount);
            btnAccount.Click += BtnAccount_Click;

            // אתחול כפתור הפעולה הצף (fabAdd) וחיבורו לפונקציה שפותחת דיאלוג הוספה/הצטרפות
            fabAdd = FindViewById<FloatingActionButton>(Resource.Id.fabAdd);
            fabAdd.Click += TvAdd_Click;

            // קישור ה-RecyclerView והגדרת מנהל פריסה אנכי (LinearLayoutManager)
            recyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);
            layoutManager = new LinearLayoutManager(this);
            recyclerView.SetLayoutManager(layoutManager);

            // יצירת רשימה ריקה בזיכרון ואתחול האדפטר הייעודי עבור הרשימות
            lists = new List<Big17DataFirebase2.Model.List>();
            listAdapter = new ListsRViewAdapter(lists);
            // רישום לאירוע הלחיצה על אחת הרשימות: מעבר לדף הפריטים הפנימי
            listAdapter.ItemClick += OnItemClick;
            // השמת האדפטר בתוך ה-RecyclerView להצגת הנתונים בפועל
            recyclerView.SetAdapter(listAdapter);
        }

        // לוגיקת ההתנתקות והחזרה למסך ה-Sign In מתוך הטקסט הלחיץ
        private void TvLeave_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. התנתקות רשמית ומאובטחת משרתי Firebase Auth
                FirebaseAuth.Instance.SignOut();

                // 2. איפוס אובייקט המשתמש השמור מקומית בזיכרון הגלובלי של האפליקציה
                ProManager.CurrentUser = null;

                // 3. מעבר למסך ההתחברות (SignInActivity) תוך כדי שימוש ב-Flags (דגלים) 
                // שמנקים לחלוטין את היסטוריית המסכים (מניעת האפשרות לחזור אחורה במסך הבית)
                Intent intent = new Intent(this, typeof(SignInActivity));
                intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask | ActivityFlags.ClearTask);
                StartActivity(intent);

                // 4. סגירה והשמדה סופית של ה-Activity הנוכחית (HomeActivity)
                Finish();
            }
            catch (Exception ex)
            {
                // במקרה של שגיאה בלתי צפויה, נדפיס אותה ללוג ונציג הודעת שגיאה למשתמש
                Log.Debug("HomeActivity", "LogoutError: " + ex.Message);
                Toast.MakeText(this, "Logout failed. Try again.", ToastLength.Short).Show();
            }
        }

        // פונקציה המופעלת בעת לחיצה על שורה (רשימה) כלשהי ב-RecyclerView
        private void OnItemClick(object sender, int position)
        {
            // שליפת אובייקט הרשימה שנבחר על פי המיקום שלו
            var selectedList = lists[position];
            // יצירת בקשת מעבר למסך הפריטים הפנימי (ListActivity)
            Intent intent = new Intent(this, typeof(ListActivity));
            // העברת נתונים קריטיים של הרשימה כתוספות (PutExtra) למסך הבא
            intent.PutExtra("listId", selectedList.Id);       // מזהה הרשימה ב-Firestore
            intent.PutExtra("listTitle", selectedList.Title);   // כותרת הרשימה
            intent.PutExtra("ownerId", selectedList.OwnerId);   // ה-ID של מנהל הרשימה
            // הרצת המעבר למסך הפריטים
            StartActivity(intent);
        }

        // פונקציית מחזור חיים אסינכרונית שרצה בכל פעם שהמסך עולה או חוזר לקדמת הבמה
        protected override async void OnResume()
        {
            base.OnResume();

            // בדיקת אבטחה של סטטוס החיבור: מוודא שיש גם משתמש מחובר בשרת וגם בזיכרון המקומי
            if (FirebaseAuth.Instance.CurrentUser != null && ProManager.CurrentUser != null)
            {
                // הצגת השם המלא המעודכן של המשתמש בראש המסך
                tvUserFullName.Text = $"{ProManager.CurrentUser.FirstName} {ProManager.CurrentUser.LastName}";

                // קריאה לפונקציה אסינכרונית הטוענת את הרשימות המשויכות למשתמש זה
                await LoadUserLists();
            }
            else
            {
                // אם המשתמש לא מחובר כראוי, נשלח אותו חזרה למסך ההתחברות ונסגור את מסך הבית
                StartActivity(typeof(SignInActivity));
                Finish();
            }
        }

        // פונקציה לפתיחת פרופיל המשתמש כחלון קופץ צף (DialogFragment)
        private void BtnAccount_Click(object sender, EventArgs e)
        {
            // הגנה: אם המשתמש אינו מחובר, לא נבצע דבר
            if (ProManager.CurrentUser == null || FirebaseAuth.Instance.CurrentUser == null) return;

            // יצירת אובייקט Bundle להעברת נתונים בין ה-Activity ל-Fragment
            Bundle args = new Bundle();
            args.PutString("UserID", FirebaseAuth.Instance.CurrentUser.Uid);
            args.PutString("FirstName", ProManager.CurrentUser.FirstName);
            args.PutString("LastName", ProManager.CurrentUser.LastName);
            args.PutString("Email", ProManager.CurrentUser.UserEmail);
            args.PutString("Mobile", ProManager.CurrentUser.UserMobile);

            // יצירת מופע של ה-AccountFragment, השמת הארגומנטים והצגתו על המסך
            AccountFragment accountFragment = new AccountFragment();
            accountFragment.Arguments = args;
            accountFragment.Show(SupportFragmentManager, "AccountFragment");
        }

        // פונקציה המופעלת בלחיצה על כפתור ה-+ הצף (fabAdd)
        private void TvAdd_Click(object sender, EventArgs e)
        {
            // יצירת חלון קופץ מותאם אישית (AddJoinFragment) המאפשר יצירת רשימה חדשה או הצטרפות לפי קוד לקיימת
            var frag = new AddJoinFragment();
            frag.Show(SupportFragmentManager, "AddJoinTag");
        }

        // לוגיקה אסינכרונית מורכבת לשליפת כל הרשימות שהמשתמש חבר בהן (שיטת הגשר / Bridge)
        public async Task LoadUserLists()
        {
            var firestore = FirebaseFirestore.Instance;
            var currentUserId = FirebaseAuth.Instance.CurrentUser?.Uid;
            if (currentUserId == null) return;

            try
            {
                // הצגת גלגל טעינה על המסך בתחילת התהליך
                ShowProgressBar(true);

                // === שלב 1: שליפת כל מסמכי הקישור מקולקציית UserList שבהם ה-UserID שווה למשתמש הנוכחי ===
                var mappingResult = await firestore.Collection("UserList")
                                                   .WhereEqualTo("UserID", currentUserId)
                                                   .Get();

                var mappingQuery = mappingResult as QuerySnapshot;

                // הגנה: אם אין למשתמש הזה אף קישור לרשימה (השאילתה ריקה)
                if (mappingQuery == null || mappingQuery.IsEmpty)
                {
                    // ננקה את המסך ונחזור (חובה לעדכן רכיבי UI ב-Main Thread דרך RunOnUiThread)
                    RunOnUiThread(() =>
                    {
                        lists.Clear(); // ניקוי הרשימה המקומית
                        listAdapter.NotifyDataSetChanged(); // עדכון האדפטר שיציג מסך ריק
                        ShowProgressBar(false); // העלמת הטעינה
                    });
                    return;
                }

                // === שלב 2: חילוץ של כל קודי ההצטרפות (joinCode) מתוך מסמכי הקישור שנמצאו ===
                List<string> myCodes = mappingQuery.Documents
                    .Select(d => d.Get("joinCode")?.ToString())
                    .Where(c => !string.IsNullOrEmpty(c)) // סינון קודים ריקים או לא תקינים
                    .ToList();

                // === שלב 3: שליפת פרטי הרשימות המלאים מקולקציית lists על פי רשימת הקודים שחילצנו (באמצעות WhereIn) ===
                var listDataResult = await firestore.Collection("lists")
                    // המרה של קודי ה-C# לטיפוסי אובייקטים של Java עבור ה-SDK של אנדרואיד
                    .WhereIn(FieldPath.Of("joinCode"), myCodes.Select(x => (Java.Lang.Object)x).ToList())
                    .Get();

                var listsQuery = listDataResult as QuerySnapshot;

                // === שלב 4: עיבוד הנתונים שחזרו מהשרת והזרקתם לתוך ה-UI ===
                RunOnUiThread(() =>
                {
                    lists.Clear(); // ניקוי הנתונים הישנים מהרשימה
                    if (listsQuery != null)
                    {
                        // מעבר בלולאה על כל מסמכי הרשימות שחזרו מהשרת
                        foreach (var doc in listsQuery.Documents)
                        {
                            // בניית אובייקט List מקומי והוספתו לאוסף המסך
                            lists.Add(new Big17DataFirebase2.Model.List()
                            {
                                Id = doc.Id, // מזהה המסמך ב-Firebase
                                Title = doc.Get("Title")?.ToString() ?? "Untitled List", // שם הרשימה
                                OwnerId = doc.Get("ownerId")?.ToString() // מזהה היוצר
                            });
                        }
                    }
                    // הודעה לאדפטר שהנתונים השתנו ושיש לצייר מחדש את השורות ב-RecyclerView
                    listAdapter.NotifyDataSetChanged();
                    // העלמת גלגל הטעינה
                    ShowProgressBar(false);
                });
            }
            catch (Exception ex)
            {
                // טיפול בשגיאות טעינה, הדפסת לוג והסרת גלגל הטעינה מהמסך
                Log.Debug("HomeActivity", "LoadError: " + ex.Message);
                ShowProgressBar(false);
            }
        }

        // פונקציה אסינכרונית להצטרפות לרשימה קיימת באמצעות קוד הצטרפות (joinCode)
        public async Task JoinListByCode(string code)
        {
            var firestore = FirebaseFirestore.Instance;
            var currentUserId = FirebaseAuth.Instance.CurrentUser.Uid;

            try
            {
                // הצגת חלון טעינה
                ShowProgressBar(true);

                // 1. בדיקה האם קיימת בכלל רשימה כזו בשרת תחת קוד ההצטרפות שהוקלד
                var result = await firestore.Collection("lists").WhereEqualTo("joinCode", code).Get();
                var query = result as QuerySnapshot;

                // אם לא נמצאה אף רשימה תואמת לקוד
                if (query == null || query.IsEmpty)
                {
                    ShowProgressBar(false); // העלמת חלון טעינה
                    // הצגת הודעת שגיאה קופצת למשתמש על קוד שגוי ב-Main Thread
                    RunOnUiThread(() => Toast.MakeText(this, "Invalid Code!", ToastLength.Long).Show());
                    return;
                }

                // 2. במידה והקוד תקין, ניצור מסמך קישור חדש בתוך קולקציית UserList
                var mapping = new Java.Util.HashMap();
                mapping.Put("UserID", currentUserId); // מזהה המשתמש הנוכחי
                mapping.Put("joinCode", code);         // קוד הרשימה אליה הוא מצטרף
                mapping.Put("JoinAt", Java.Lang.JavaSystem.CurrentTimeMillis()); // חותמת זמן הנוכחית במילישניות

                // שמירת מסמך הקישור ב-Firestore
                await firestore.Collection("UserList").Add(mapping);

                // 3. רענון אוטומטי של רשימות המשתמש במסך הבית כדי להציג מיד את הרשימה החדשה
                await LoadUserLists();
            }
            catch (Exception ex)
            {
                // העלמת חלון הטעינה במקרה של שגיאה והדפסתה
                ShowProgressBar(false);
                Log.Debug("HomeActivity", "JoinError: " + ex.Message);
            }
        }

        // פונקציה לניהול הצגת והסרת גלגל הטעינה (ProgressBar) בצורה בטוחה
        private void ShowProgressBar(bool show)
        {
            if (show)
            {
                // יצירת חלון הדיאלוג וה-Layout שלו במידה ולא אותחל בעבר
                if (mProgressDialog == null)
                {
                    mProgressDialog = new Dialog(this, Android.Resource.Style.ThemeNoTitleBar);
                    View view = LayoutInflater.From(this).Inflate(Resource.Layout.fb_progressbar, null);
                    mProgressDialog.Window.SetBackgroundDrawableResource(Android.Resource.Color.Transparent); // רקע שקוף
                    mProgressDialog.SetContentView(view);
                    mProgressDialog.SetCancelable(false); // חסימת סגירה על ידי המשתמש
                }
                // הצגה בפועל אם הוא אינו מוצג כרגע
                if (!mProgressDialog.IsShowing) mProgressDialog.Show();
            }
            else
            {
                // סגירת חלון הדיאלוג במידה והוא פעיל
                mProgressDialog?.Dismiss();
            }
        }
    }
}