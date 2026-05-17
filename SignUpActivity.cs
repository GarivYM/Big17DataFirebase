// ספריות בסיסיות של מערכת ההפעלה אנדרואיד
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
// ספריות פנימיות של הפרויקט שלך (לוגיקה, מודלים ושירותים)
using Big17DataFirebase2.BusinessLogic;
using Big17DataFirebase2.Model;
using Big17DataFirebase2.Service;
using System;
using System.Collections.Generic;

namespace Big17DataFirebase2
{
    // הגדרת המחלקה כרקע פעילות (Activity) באנדרואיד ושם המסך במערכת
    [Activity(Label = "SignUpActivity")]
    public class SignUpActivity : Activity
    {
        // הגדרת משתנים עבור שדות קלט הטקסט (תיבות הטקסט שהמשתמש ממלא)
        EditText _firstName, _lastName, _userEmail, _userPassword, _userMobile;

        // הגדרת משתנה עבור כפתור ההרשמה
        Button _btnSignUp;

        // דיאלוג (חלון קופץ) שישמש להצגת מדד התקדמות (ProgressBar) בזמן הטעינה
        Dialog mProgressDialog;

        // אובייקט מסוג משתמש (User) שיחזיק את הנתונים שהוזנו במסך
        Model.User _user;

        // הפונקציה הראשית שרצה ברגע שהמסך נוצר ומותחל
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // קישור הקוד לקובץ ה-XML הוויזואלי של מסך ההרשמה
            SetContentView(Resource.Layout.signup_layout);

            // קריאה לפונקציה שמקשרת את רכיבי המסך לקוד
            InitializeViews();
        }

        // פונקציה שתפקידה למצוא את רכיבי ה-UI מה-XML ולחבר אותם למשתנים בקוד
        private void InitializeViews()
        {
            // קישור כל משתנה לרכיב המתאים לו ב-XML לפי ה-ID שלו
            _firstName = FindViewById<EditText>(Resource.Id.et_first_name);
            _lastName = FindViewById<EditText>(Resource.Id.et_last_name);
            _userEmail = FindViewById<EditText>(Resource.Id.et_email);
            _userPassword = FindViewById<EditText>(Resource.Id.et_password);
            _userMobile = FindViewById<EditText>(Resource.Id.et_mobile);
            _btnSignUp = FindViewById<Button>(Resource.Id.btn_register);

            // רישום לאירוע לחיצה: ברגע שלוחצים על כפתור ההרשמה, תופעל הפונקציה BtnSignUp_Click
            _btnSignUp.Click += BtnSignUp_Click;

            // מצב דיבאג (בדיקות): אם מנהל הפרויקט הגדיר שאנחנו במצב בדיקה, נמלא נתוני ברירת מחדל אוטומטית
            if (ProManager.DebugMode)
            {
                _firstName.Text = "FName";
                _lastName.Text = "LName";
                _userEmail.Text = "user@gmail.com";
                _userPassword.Text = "123456";
                _userMobile.Text = "051015";
            }
        }

        // הפונקציה שמופעלת מיד כשלוחצים על כפתור הרישום
        private void BtnSignUp_Click(object sender, EventArgs e)
        {
            // יצירת מופע חדש של משתמש ומילוי השדות שלו מהטקסט שכתב המשתמש במסך
            _user = new Model.User()
            {
                FirstName = _firstName.Text,
                LastName = _lastName.Text,
                UserEmail = _userEmail.Text,
                UserPass = _userPassword.Text,
                UserMobile = _userMobile.Text,
                // בדיקה: אם האימייל הוא של האדמין, המשתמש יוגדר כמנהל מערכת (true), אחרת לא (false)
                IsAdmin = (_userEmail.Text == "admin@gmail.com")
            };

            // קריאה לפונקציה שמבצעת את הרישום בפועל מול השרת
            RegisterNewUser();
        }

        // פונקציה אסינכרונית (רצה ברקע) שמבצעת את הרישום ל-Firebase
        private async void RegisterNewUser()
        {
            // הצגת חלון הטעינה (פס התקדמות) כדי שהמשתמש יבין שהאפליקציה עובדת
            ShowProgressBar(true);

            try
            {
                // שליחת אובייקט המשתמש למחלקת העזר של Firebase ושמירתו בבסיס הנתונים.
                // הפעולה מחזירה את ה-ID הייחודי שהשרת יצר למשתמש זה.
                _user.Id = await FireBaseHelper.InsertAsync(_user);

                // העלמת חלון הטעינה בסיום הפעולה בהצלחה
                ShowProgressBar(false);

                // הצגת הודעת פופ-אפ קטנה (Toast) על המסך שההרשמה הצליחה
                Toast.MakeText(this, $"SignUp succeeded!", ToastLength.Short).Show();

                // שמירת המשתמש הנוכחי בזיכרון הגלובלי של האפליקציה (ProManager)
                ProManager.CurrentUser = _user;

                // ניתוב המשתמש למסך המתאים לפי סוג החשבון שלו:
                if (_user.IsAdmin)
                {
                    // אם הוא מנהל - פתח את מסך הניהול (AdminActivity)
                    StartActivity(typeof(AdminActivity));
                }
                else
                {
                    // אם הוא משתמש רגיל - פתח את מסך הבית (HomeActivity)
                    StartActivity(typeof(HomeActivity));
                }

                // סגירת מסך ההרשמה הנוכחי כדי שהמשתמש לא יוכל ללחוץ "אחורה" בטלפון ולחזור אליו
                Finish();
            }
            catch (Exception ex)
            {
                // בלוק זה יופעל רק אם קרתה שגיאה במהלך התקשורת עם השרת
                // העלמת חלון הטעינה כדי שהאפליקציה לא תיתקע
                ShowProgressBar(false);

                // רישום השגיאה ב-Logcat (חלון הפלט של המפתח) לצורכי ניפוי שגיאות
                Log.Error("SignUp", ex.Message);

                // הצגת הודעה למשתמש על המסך עם סיבת הכישלון
                Toast.MakeText(this, $"SignUp failed: {ex.Message}", ToastLength.Short).Show();
            }
        }

        // פונקציה שמנהלת את הצגת והעלמת חלון הטעינה (ProgressBar)
        private void ShowProgressBar(bool show)
        {
            if (show)
            {
                // יצירת חלון דיאלוג חדש ללא שורת כותרת
                mProgressDialog = new Dialog(this, Android.Resource.Style.ThemeNoTitleBar);

                // טעינת עיצוב ה-XML הייעודי של פס ההתקדמות (fb_progressbar)
                View view = LayoutInflater.From(this).Inflate(Resource.Layout.fb_progressbar, null);

                // הגדרת רקע שקוף לחלון כדי שיראו רק את גלגל הטעינה עצמו
                mProgressDialog.Window.SetBackgroundDrawableResource(Resource.Color.mtrl_btn_transparent_bg_color);

                // השמת העיצוב לתוך חלון הדיאלוג
                mProgressDialog.SetContentView(view);

                // חסימת האפשרות של המשתמש לבטל את החלון על ידי לחיצה מחוץ לו
                mProgressDialog.SetCancelable(false);

                // הצגת החלון על המסך
                mProgressDialog.Show();
            }
            else
            {
                // אם המשתנה show הוא false - סגור והעלם את חלון הטעינה מהמסך
                mProgressDialog.Dismiss();
            }
        }
    }
}