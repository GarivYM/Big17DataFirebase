// ייבוא ספריות בסיסיות של מערכת ההפעלה אנדרואיד לעבודה עם רכיבים גרפיים ודיאלוגים
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
// ייבוא ספריית תאימות לאחור של אנדרואיד (AppCompat) התומכת ברכיבי עיצוב מתקדמים
using AndroidX.AppCompat.App;
// ייבוא מחלקות פנימיות של הפרויקט שלך (לוגיקה עסקית ושירותי קישור לשרת)
using Big17DataFirebase2.BusinessLogic;
using Big17DataFirebase2.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Big17DataFirebase2
{
    // הגדרת המחלקה כפעילות מסוג AppCompatActivity. 
    // המאפיין MainLauncher = true קובע שזהו המסך הראשון שייפתח כשהאפליקציה עולה בטלפון.
    // המחלקה מיישמת את הממשק IOnClickListener כדי לטפל בלחיצות על כפתורים בצורה מרכזית.
    [Activity(Label = "SmartList", Icon = "@drawable/smart_list_icon", MainLauncher = true)]
    public class SignInActivity : AppCompatActivity, Android.Views.View.IOnClickListener
    {
        // הגדרת משתני רכיבי הקלט (תיבות הטקסט לאימייל וסיסמה)
        private EditText etEmail, etPass;
        // הגדרת משתנה עבור כפתור ההתחברות
        private Button btnSignIn;
        // הגדרת משתנה עבור קישור מעבר למסך ההרשמה (מיוצג כטקסט לחיץ)
        private TextView btnSighUp;
        // הגדרת משתנה לחלון קופץ (Dialog) שיציג את פס ההתקדמות בזמן פעולת ההתחברות
        private Dialog mProgressDialog;

        // פונקציית מחזור החיים שרצה אוטומטית בעת יצירת המסך בפעם הראשונה
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // קישור הקוד לקובץ ה-XML הוויזואלי שמגדיר את עיצוב מסך הלינקוג' (signin_layout)
            SetContentView(Resource.Layout.signin_layout);

            // קריאה לפונקציה שמאתחלת ומקשרת את רכיבי המסך
            InitilizeViews();
            // כתיבת שורת מעקב ל-Logcat (חלון הפלט של ה-Debug) כדי לדעת שהמסך נוצר בהצלחה
            Log.Debug(ProManager.TAG, $"SignInActivity: OnCreate()");
        }

        // פונקציה פנימית לקישור רכיבי ה-UI מה-XML לקוד והגדרת מאזינים ללחיצות
        private void InitilizeViews()
        {
            // מציאת הרכיבים הגרפיים בתוך קובץ ה-XML לפי ה-ID הייחודי שלהם
            etEmail = FindViewById<EditText>(Resource.Id.et_email2);
            etPass = FindViewById<EditText>(Resource.Id.et_password2);
            btnSignIn = FindViewById<Button>(Resource.Id.btn_login2);
            btnSighUp = FindViewById<TextView>(Resource.Id.btn_sign_up);

            // הגדרת מאזין הלחיצות (OnClickListener) עבור הכפתורים. 
            // המילה keyword "this" אומרת שהלחיצות יופנו לפונקציה OnClick שנמצאת במחלקה זו.
            btnSignIn.SetOnClickListener(this);
            btnSighUp.SetOnClickListener(this);

            // מצב בדיקה (Debug Mode): אם המערכת מוגדרת במצב בדיקות, נבצע לוגין אוטומטי ללא צורך בהקלדה
            if (ProManager.DebugMode)
            {
                // השמת אימייל וסיסמה קבועים מראש בשדות הטקסט
                etEmail.Text = "user@gmail.com";
                etPass.Text = "123456";
                // הצגת גלגל הטעינה על המסך
                ShowProgressBar(true);
                // הרצת פונקציית ההתחברות ישירות
                SignInWithEmailAndPassword();
            }
        }

        // פונקציה אסינכרונית (רצה ברקע) שמבצעת את אימות המשתמש מול ה-Firebase Authentication
        private async void SignInWithEmailAndPassword()
        {
            // שליחת האימייל והסיסמה לפונקציית העזר של Firebase. 
            // ה-await ממתין לתשובת השרת ומחזיר את מפתח ה-UID הייחודי של המשתמש אם הוא קיים.
            string userAuthID = await FireBaseHelper.SignInUserAsync(etEmail.Text, etPass.Text);

            // אם השרת החזיר מפתח תקין (ההתחברות הצליחה)
            if (userAuthID != null)
            {
                // הדפסת הודעת הצלחה ב-Log לצרכי פיתוח
                Log.Debug(ProManager.TAG, $"Firebase Auth SignIn success: {etEmail.Text} {etPass.Text}");
                // קריאה לפונקציה הבאה כדי למשוך את שאר נתוני המשתמש מתוך מסד הנתונים (Firestore)
                GetCurrentUserFromDB(userAuthID);
            }
            else // אם השרת החזיר null (פרטים שגויים או שגיאת רשת)
            {
                // העלמת גלגל הטעינה כדי שהמסך לא יישאר חסום
                ShowProgressBar(false);
                // הדפסת שורת שגיאה ב-Log
                Log.Debug(ProManager.TAG, $"Firebase Auth SignIn Failed: {etEmail.Text} {etPass.Text}");
                // הצגת הודעת שגיאה קופצת (Toast) קצרה למשתמש על המסך
                Toast.MakeText(this, "SignIn Process failed", ToastLength.Short).Show();
            }
        }

        // פונקציה אסינכרונית שמושכת את נתוני המשתמש המלאים מקולקציית הנתונים ב-Database לפי ה-UID שלו
        private async void GetCurrentUserFromDB(string userAuthID)
        {
            // פנייה ל-Database שליפת מסמך המשתמש על בסיס ה-ID שלו
            var userfromDB = await FireBaseHelper.GetUserById(userAuthID);

            // אפשרות 1: המשתמש נמצא בבסיס הנתונים והוא משתמש רגיל (IsAdmin הוא false)
            if (userfromDB != null && !userfromDB.IsAdmin)
            {
                // שמירת אובייקט המשתמש בזיכרון הגלובלי של האפליקציה כדי שכל המסכים הבאים יכירו אותו
                ProManager.CurrentUser = userfromDB;
                // העלמת גלגל הטעינה
                ShowProgressBar(false);

                // יצירת Intent (בקשת מעבר) כדי לעבור למסך הבית (HomeActivity)
                Intent intent = new Intent(this, typeof(HomeActivity));
                // שימוש בדגלים קריטיים: מנקים את כל היסטוריית המסכים הקודמים (clear stack), כדי שלא יוכלו ללחוץ "חזור" ולהגיע שוב למסך ההתחברות
                intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
                // מעבר למסך החדש
                StartActivity(intent);
                // סגירת המסך הנוכחי לצמיתות
                Finish();
            }
            // אפשרות 2: המשתמש נמצא בבסיס הנתונים והוא מנהל מערכת (IsAdmin הוא true)
            else if (userfromDB != null && userfromDB.IsAdmin)
            {
                // שמירת אובייקט המשתמש בזיכרון הגלובלי של האפליקציה
                ProManager.CurrentUser = userfromDB;
                // העלמת גלגל הטעינה
                ShowProgressBar(false);

                // יצירת Intent מעבר למסך הניהול הייעודי (AdminActivity)
                Intent intent = new Intent(this, typeof(AdminActivity));
                // ניקוי מחסנית המסכים הישנים
                intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
                // מעבר למסך החדש
                StartActivity(intent);
                // סגירת המסך הנוכחי
                Finish();
            }
            // אפשרות 3: מזהה האימות קיים, אך לא נמצא מסמך תואם עבור ה-ID הזה בקולקציית המשתמשים ב-Database
            else
            {
                // העלמת גלגל הטעינה
                ShowProgressBar(false);
                // רישום שגיאה ב-Logcat
                Log.Debug(ProManager.TAG, "SighIn: Failed get user from DB");
                // הצגת הודעה למשתמש שהתהליך נכשל
                Toast.MakeText(this, "SignIn Process failed", ToastLength.Short).Show();
            }
        }

        // פונקציה מרכזית שמטפלת בכל אירועי הלחיצה של הרכיבים שנרשמו אליה
        public void OnClick(View v)
        {
            // בדיקה: האם הרכיב שנלחץ הוא כפתור ההתחברות (btnSignIn)
            if (v == btnSignIn)
            {
                // הרצת פונקציית הוולידציה (בדיקת תקינות קלט)
                if (Validate())
                {
                    // הצגת גלגל הטעינה לפני הפנייה לשרת
                    ShowProgressBar(true);
                    // קריאה לפונקציה שמבצעת את ההתחברות מול ה-Firebase
                    SignInWithEmailAndPassword();
                }
            }
            // בדיקה: האם הרכיב שנלחץ הוא קישור ההרשמה (btnSighUp)
            else if (v == btnSighUp)
            {
                // מעבר רגיל למסך ההרשמה (SignUpActivity) ללא ניקוי היסטוריה (כך שהמשתמש יוכל לחזור אחורה אם ירצה)
                StartActivity(typeof(SignUpActivity));
            }
        }

        // פונקציה שאחראית על יצירה, הצגה או העלמה של גלגל הטעינה (ProgressBar) מהמסך
        private void ShowProgressBar(bool show)
        {
            // אם המשתנה show הוא true - יוצרים ומציגים את הדיאלוג
            if (show)
            {
                // יצירת חלון דיאלוג ריק ללא כותרת מובנית של מערכת ההפעלה
                mProgressDialog = new Dialog(this, Android.Resource.Style.ThemeNoTitleBar);
                // הזרקת (Inflate) קובץ ה-XML של העיצוב (fb_progressbar) לתוך רכיב תצוגה
                View view = LayoutInflater.From(this).Inflate(Resource.Layout.fb_progressbar, null);
                // הגדרת חלון הדיאלוג כבעל רקע שקוף לחלוטין כדי שיופיע רק גלגל האנימציה עצמו
                mProgressDialog.Window.SetBackgroundDrawableResource(Resource.Color.mtrl_btn_transparent_bg_color);
                // השמת התצוגה המוזרקת לתוך חלון הדיאלוג
                mProgressDialog.SetContentView(view);
                // חסימת האפשרות של המשתמש לבטל או לסגור את חלון הטעינה על ידי לחיצה מחוץ לגבולותיו
                mProgressDialog.SetCancelable(false);
                // הצגת החלון בפועל מעל המסך הנוכחי
                mProgressDialog.Show();
            }
            // אם המשתנה show הוא false - סוגרים ומעלימים את החלון
            else
            {
                // סגירת הדיאלוג בצורה בטוחה
                mProgressDialog.Dismiss();
            }
        }

        // פונקציה לבדיקת תקינות הקלט (למשל: האם השדות ריקים, האם המבנה של האימייל תקין וכדומה)
        private bool Validate()
        {
            // כרגע הפונקציה מחזירה תמיד אמת (true) כברירת מחדל, ניתן להוסיף כאן לוגיקת בדיקות בעתיד
            return true;
        }
    }
}