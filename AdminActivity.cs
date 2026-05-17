// ייבוא ספריות הליבה של אנדרואיד לעבודה עם רכיבים גרפיים, דיאלוגים וניווט
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
// ייבוא מחלקת הבסיס AppCompatActivity לתמיכה ברכיבי עיצוב מתקדמים
using AndroidX.AppCompat.App;
// ייבוא רכיב ה-RecyclerView להצגת רשימת המשתמשים בצורה דינמית
using AndroidX.RecyclerView.Widget;
// ייבוא שכבות האדפטרים, המודלים והשירותים של הפרויקט שלך
using Big17DataFirebase2.Adapters;
using Big17DataFirebase2.Model;
using Big17DataFirebase2.Service;
using Big17DataFirebase2.BusinessLogic; // תמיכה במחלקת הניהול ProManager במידת הצורך
// ייבוא הספריות הרשמיות של Firebase לאימות (Auth) ומסד הנתונים (Firestore)
using Firebase.Auth;
using Firebase.Firestore;
using System;
using System.Collections.Generic;

namespace Big17DataFirebase2
{
    // הגדרת מחלקת מסך מנהל המערכת (Admin)
    [Activity(Label = "AdminActivity")]
    public class AdminActivity : AppCompatActivity
    {
        // רכיבי ה-RecyclerView להצגת כלל המשתמשים הרשומים באפליקציה
        RecyclerView userRecyclerView;
        UsersRViewAdapter userAdapter; // האדפטר המתווך שמצייר את המשתמשים ברשימה
        List<User> allUsers;           // רשימה מקומית בזיכרון שמחזיקה את אובייקטי המשתמשים

        // רכיבי טקסט (UI) להצגת כותרות ונתונים סטטיסטיים בדשבורד
        TextView tvAdminTitle, tvTotalUsers, tvTotalLists;
        TextView tvAdminLeave; // טקסט לחיץ המשמש כפתור התנתקות מאובטחת עבור האדמין
        Dialog mProgressDialog; // חלון דיאלוג עבור גלגל הטעינה (ProgressBar)

        // פונקציית מחזור החיים המופעלת ברגע יצירת המסך בפעם הראשונה
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // קשירת הקוד לקובץ ה-XML הויזואלי של מסך האדמין (admin_layout)
            SetContentView(Resource.Layout.admin_layout);

            // קישור רכיבי הטקסט והרשימה מה-XML למשתנים המתאימים בקוד
            tvAdminTitle = FindViewById<TextView>(Resource.Id.tvAdminTitle);
            tvTotalUsers = FindViewById<TextView>(Resource.Id.tvTotalUsers);
            tvTotalLists = FindViewById<TextView>(Resource.Id.tvTotalLists);
            userRecyclerView = FindViewById<RecyclerView>(Resource.Id.userRecyclerView);

            // אתחול וקישור אירוע לחיצה לטקסט הלחיץ ליציאה (tvAdminLeave)
            tvAdminLeave = FindViewById<TextView>(Resource.Id.tvAdminLeave);
            tvAdminLeave.Click += TvAdminLeave_Click;

            // הגדרת מנהל פריסה אנכי (LinearLayoutManager) ל-RecyclerView של המשתמשים
            userRecyclerView.SetLayoutManager(new LinearLayoutManager(this));

            // יצירת רשימה ריקה בזיכרון ואתחול האדפטר המציג את המשתמשים
            allUsers = new List<User>();
            userAdapter = new UsersRViewAdapter(this, allUsers);
            // רישום לאירוע לחיצה על משתמש ברשימה: פתיחת חלון ה-Popup עם פרטיו המלאים
            userAdapter.ItemClick += UserAdapter_ItemClick;
            // חיבור האדפטר ל-RecyclerView כדי שיוכל לצייר את הנתונים
            userRecyclerView.SetAdapter(userAdapter);
        }

        // לוגיקת התנתקות אדמין, סגירת מאזינים בטוחה ומעבר למסך ההתחברות
        private void TvAdminLeave_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. עצירת המאזין של ה-Firebase באופן מיידי כדי למנוע זליגת זיכרון (Memory Leak) ברקע
                FireBaseHelper.StopUsersListener();

                // 2. התנתקות רשמית משרתי Firebase Auth
                FirebaseAuth.Instance.SignOut();

                // 3. איפוס אובייקט המשתמש הלוקאלי בזיכרון הגלובלי של המערכת
                ProManager.CurrentUser = null;

                // 4. מעבר למסך ההתחברות (SignInActivity) תוך ניקוי מוחלט של כל היסטוריית המסכים (Backstack)
                Intent intent = new Intent(this, typeof(SignInActivity));
                intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask | ActivityFlags.ClearTask);
                StartActivity(intent);

                // 5. סגירה מוחלטת והשמדה של ה-Activity הנוכחית (AdminActivity)
                Finish();
            }
            catch (Exception ex)
            {
                // תיעוד השגיאה במידה וההתנתקות נכשלה והצגת הודעה מתאימה
                Log.Error("AdminActivity", "LogoutError: " + ex.Message);
                Toast.MakeText(this, "Logout failed.", ToastLength.Short).Show();
            }
        }

        // פונקציית מחזור חיים אסינכרונית שרצה בכל פעם שהמסך עולה או חוזר לקדמת הבמה
        protected override async void OnResume()
        {
            base.OnResume();

            // 1. שליפה וטעינה אסינכרונית של הנתונים הסטטיסטיים של ה-Dashboard (כמות הרשימות הגלובלית באפליקציה)
            int totalListsCount = await FireBaseHelper.GetGlobalListsCount();
            tvTotalLists.Text = totalListsCount.ToString();

            // 2. הצגת גלגל טעינה והפעלת המאזין בזמן אמת (Real-time Listener) עבור רשימת המשתמשים
            ShowProgressBar(true);
            StartRealTimeUserListener();
        }

        // פונקציית מחזור חיים שרצה כשהמסך יוצא ממיקוד (למשל, מעבר למסך אחר או סגירת האפליקציה)
        protected override void OnPause()
        {
            base.OnPause();
            // עצירה של המאזין כדי שלא ימשיך לצרוך נתונים ומשאבים כשהאדמין לא צופה במסך
            FireBaseHelper.StopUsersListener();
        }

        // פונקציה שמחברת ומנהלת את קבלת הנתונים מהמאזין בזמן אמת של המשתמשים
        private void StartRealTimeUserListener()
        {
            // הפעלת פונקציית השירות שיוצרת את ה-SnapshotListener בשרת
            FireBaseHelper.FetchUsersListener();

            // רישום לאירוע קבלת הנתונים המותאם אישית (getEvent) שנמצא בתוך ה-FireBaseHelper
            FireBaseHelper.listener.getEvent += (error, args) =>
            {
                // העלמת גלגל הטעינה מיד עם הגעת העדכון הראשון מהשרת
                ShowProgressBar(false);

                // ניקוי או אתחול מחדש של הרשימה המקומית בזיכרון כדי למנוע כפל נתונים בכל עדכון בזמן אמת
                if (allUsers != null)
                    allUsers.Clear();
                else
                    allUsers = new List<User>();

                try
                {
                    // המרת התוצאה הגולמית שהתקבלה מהאירוע לאובייקט מסוג QuerySnapshot
                    var snapshot = (QuerySnapshot)args.Result;

                    // בדיקה שהמידע שהתקבל תקין ואינו ריק
                    if (snapshot != null && !snapshot.IsEmpty)
                    {
                        // לולאה שעוברת מסמך-מסמך (משתמש-משתמש) מתוך קולקציית users בשרת
                        foreach (DocumentSnapshot item in snapshot.Documents)
                        {
                            // בניית אובייקט User חדש וחילוץ שדותיו בצורה בטוחה מהמסמך ב-Firestore
                            User _user = new User()
                            {
                                Id = item.Id, // מזהה המסמך הייחודי של המשתמש (UID)
                                FirstName = item.Get("FirstName")?.ToString() ?? "",
                                LastName = item.Get("LastName")?.ToString() ?? "",
                                UserEmail = item.Get("UserEmail")?.ToString() ?? "",
                                UserMobile = item.Get("UserMobile")?.ToString() ?? "",
                                // בדיקה וחילוץ שדה בוליאני המציין האם המשתמש הוא אדמין (אם השדה חסר, ברירת המחדל היא false)
                                IsAdmin = item.Get("IsAdmin") != null ? bool.Parse(item.Get("IsAdmin").ToString()) : false,
                                // השמת תמונת ברירת מחדל (אייקון) לפרופיל המשתמש ברשימה
                                ImageId = Resource.Drawable.maleicon
                            };
                            // הוספת המשתמש המעובד לרשימה הכללית של המסך
                            allUsers.Add(_user);
                        }

                        // עדכון שדה הסטטיסטיקה של כמות המשתמשים הכללית על פי גודל הרשימה שחזרה
                        tvTotalUsers.Text = allUsers.Count.ToString();
                        // רענון האדפטר כדי שיצייר מחדש את רשימת המשתמשים המעודכנת על גבי המסך
                        userAdapter.NotifyDataSetChanged();
                    }
                    else
                    {
                        // אם השאילתה חזרה ריקה לגמרי (אין משתמשים במערכת), נציג 0
                        tvTotalUsers.Text = "0";
                    }
                }
                catch (Exception ex)
                {
                    // תפיסה ורישום של שגיאות בזמן עיבוד הנתונים שחזרו מהמאזין
                    Log.Error("AdminActivity", $"Error in real-time listener: {ex.Message}");
                }
            };
        }

        // פונקציה המופעלת בעת לחיצה על שורה (משתמש) כלשהי בתוך הרשימה
        private void UserAdapter_ItemClick(object sender, int position)
        {
            // שליפת אובייקט המשתמש הספציפי שנבחר על פי המיקום שלו ברשימה
            var selectedUser = allUsers[position];

            // הדפסת הודעת דיבאג ל-Logcat לבדיקת תקינות הנתונים וה-ID של המשתמש שנבחר
            Android.Util.Log.Debug("ADMIN_DEBUG", $"Selected User: {selectedUser.FirstName}, ID from list: '{selectedUser.Id}'");

            // יצירת אובייקט Bundle להעברת פרטי המשתמש שנבחר לתוך חלון ה-Fragment הקופץ
            Bundle args = new Bundle();
            args.PutString("UserID", selectedUser.Id);
            args.PutString("FirstName", selectedUser.FirstName);
            args.PutString("LastName", selectedUser.LastName);
            args.PutString("Email", selectedUser.UserEmail);
            args.PutString("Mobile", selectedUser.UserMobile);

            // יצירת מופע של ה-AccountFragment (חלון פרטי החשבון), הזרקת הארגומנטים והצגתו למנהל המערכת
            AccountFragment accountFragment = new AccountFragment();
            accountFragment.Arguments = args;
            accountFragment.Show(SupportFragmentManager, "AccountFragment");
        }

        // פונקציה פנימית לניהול הצגת והעלמת גלגל הטעינה (ProgressBar) בצורה בטוחה
        private void ShowProgressBar(bool show)
        {
            if (show)
            {
                // יצירת חלון הדיאלוג וה-Layout שלו
                mProgressDialog = new Dialog(this, Android.Resource.Style.ThemeNoTitleBar);
                View view = LayoutInflater.From(this).Inflate(Resource.Layout.fb_progressbar, null);
                // הגדרת רקע שקוף לחלון כדי שיוצג בצורה יפה מעל רכיבי המסך
                mProgressDialog.Window.SetBackgroundDrawableResource(Resource.Color.mtrl_btn_transparent_bg_color);
                mProgressDialog.SetContentView(view);
                mProgressDialog.SetCancelable(false); // חסימת אפשרות ביטול הדיאלוג על ידי לחיצה מחוצה לו
                mProgressDialog.Show(); // הצגה בפועל
            }
            else
            {
                // סגירת חלון הדיאלוג והעלמתו מהמסך במידה והוא פעיל
                mProgressDialog?.Dismiss();
            }
        }
    }
}