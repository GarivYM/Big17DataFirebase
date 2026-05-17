// ייבוא ספריות בסיסיות של מערכת ההפעלה אנדרואיד לרכיבי UI, לוגים וניהול מסכים
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
// ייבוא ספריית התאימות לאחור (AppCompat) המאפשרת עיצובים מודרניים
using AndroidX.AppCompat.App;
// ייבוא מחלקת הלוגיקה העסקית של הפרויקט שלך (לצורך בדיקת מצב דיבאג)
using Big17DataFirebase2.BusinessLogic;
using System;

namespace Big17DataFirebase2
{
    // הגדרת תכונות המסך: שם האפליקציה שנלקח מקובץ הסטרינגים, ערכת העיצוב (Theme), 
    // והגדרת MainLauncher = false (המסך הזה אינו נקודת הכניסה הראשית, כיוון ששינית את זה ל-SignInActivity).
    // המחלקה מיישמת את הממשק IOnClickListener כדי לטפל בלחיצות באופן מרוכז.
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = false)]
    public class MainActivity : AppCompatActivity, Android.Views.View.IOnClickListener
    {
        // הגדרת משתנים עבור רכיבי הטקסט הלחיצים במסך (כפתורי מעבר)
        TextView tvSignIn, tvSignUp;
        // משתנה קבוע (מחרוזת) המגדיר את תגית הזיהוי של הלוגים עבור מסך זה
        private readonly string TAG = "YAIRAPP";

        // פונקציית מחזור החיים המרכזית שמופעלת כאשר המסך נוצר לראשונה
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // אתחול ספריית Xamarin.Essentials המאפשרת גישה לרכיבי מערכת (כמו רשת, סוללה, מיקום וכו')
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            // קישור הקוד הנוכחי לקובץ ה-XML של העיצוב הראשי (activity_main)
            SetContentView(Resource.Layout.activity_main);

            // קריאה לפונקציה פנימית שמקשרת את רכיבי ה-UI ומגדירה מאזיני לחיצות
            InitialViews();
            // הדפסת שורת מעקב ל-Logcat כדי לדעת שמסך ה-MainActivity עלה בהצלחה
            Log.Debug(TAG, $"MainActivity: OnCreate()");

            // מצב בדיקה (Debug Mode): אם המערכת במצב בדיקות, האפליקציה תדלג אוטומטית למסך אחר
            if (ProManager.DebugMode)
                // כרגע הקוד מוגדר לעבור אוטומטית למסך ההרשמה (SignUpActivity)
                StartActivity(typeof(SignUpActivity));
            // השורה הבאה חסומה בהערה (קומנט) - שימשה בעבר למעבר אוטומטי למסך ההתחברות
            //StartActivity(typeof(SignInActivity));
        }

        // פונקציה פנימית למציאת הרכיבים הגרפיים ב-XML וחיבורם לקוד
        private void InitialViews()
        {
            // קישור משתני הטקסט הלחיצים לרכיבים ב-XML על פי ה-ID הייחודי שלהם
            tvSignIn = FindViewById<TextView>(Resource.Id.tvSignIn);
            tvSignUp = FindViewById<TextView>(Resource.Id.tvSignUp);

            // הגדרת מאזין לחיצות עבור שני רכיבי הטקסט. 
            // המילה keyword "this" אומרת שכאשר ילחצו עליהם, תופעל פונקציית OnClick שנמצאת כאן למטה
            tvSignIn.SetOnClickListener(this);
            tvSignUp.SetOnClickListener(this);
        }

        // פונקציה מובנית של אנדרואיד שמופעלת אוטומטית לאחר שהמשתמש מאשר או דוחה בקשת הרשאה (כמו מצלמה, מיקום וכו')
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            // העברת התוצאה לספריית Xamarin.Essentials כדי שהיא תדע לנהל את הסטטוס הפנימי שלה
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            // קריאה לפונקציית הבסיס של אנדרואיד כדי להמשיך את זרימת המערכת הרגילה
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        // הפונקציה המרכזית שמטפלת בכל אירועי הלחיצה של הרכיבים שנרשמו אליה
        public void OnClick(View v)
        {
            // בדיקה: האם הרכיב שנלחץ הוא טקסט ההתחברות (tvSignIn)
            if (v == tvSignIn)
            {
                // מעבר למסך ההתחברות (SignInActivity) באמצעות יצירת אובייקט Intent מפורש
                StartActivity(new Intent(this, typeof(SignInActivity)));
            }
            // בדיקה: האם הרכיב שנלחץ הוא טקסט ההרשמה (tvSignUp)
            else if (v == tvSignUp)
            {
                // מעבר למסך ההרשמה (SignUpActivity) באמצעות העברת טיפוס המחלקה (דרך קצרה יותר)
                StartActivity(typeof(SignUpActivity));
            }
        }
    }
}