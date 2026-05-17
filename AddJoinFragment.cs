// ייבוא ספריות הליבה של אנדרואיד לעבודה עם רכיבי עיצוב, תצוגות (Views) ודיאלוגים
using Android.OS;
using Android.Views;
using Android.Widget;
// ייבוא שכבות הלוגיקה העסקית והשירותים של הפרויקט שלך (כמו מחלקת העזר של Firebase)
using Big17DataFirebase2.BusinessLogic;
using Big17DataFirebase2.Service;
// ייבוא רכיבי Firebase הרשמיים לניהול אימות ומשתמשים (Auth)
using Firebase.Auth;
// ייבוא ספריית ה-Design של גוגל המאפשרת שימוש בתפריט צץ תחתון (BottomSheet)
using Google.Android.Material.BottomSheet;
using System;

namespace Big17DataFirebase2
{
    // הגדרת המחלקה כיורשת של BottomSheetDialogFragment - מה שגורם לה להיפתח כחלון החלקה מלמטה למעלה
    public class AddJoinFragment : BottomSheetDialogFragment
    {
        // הגדרת רכיבי ממשק המשתמש (UI) המופיעים בתוך התפריט התחתון
        private EditText etListName, etJoinCode; // שדות טקסט להקלדת שם רשימה או קוד הצטרפות
        private Spinner spinnerType;             // תיבת בחירה נפתחת (דרופדאון) לבחירת סוג הרשימה
        private Button btnCreate, btnJoin;       // כפתורי פעולה ליצירה או הצטרפות

        // פונקציית מחזור החיים האחראית על טעינת ה-Layout הויזואלי וקישור הרכיבים שלו
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // טעינת קובץ ה-XML הויזואלי (add_join_layout) והפיכתו לאובייקט View בקוד
            var view = inflater.Inflate(Resource.Layout.add_join_layout, container, false);

            // === 1. אתחול וקישור רכיבי אזור ההצטרפות (Join Section) ===
            etJoinCode = view.FindViewById<EditText>(Resource.Id.etJoinCode);
            btnJoin = view.FindViewById<Button>(Resource.Id.btnJoin);

            // === 2. אתחול וקישור רכיבי אזור היצירה (Create Section) ===
            etListName = view.FindViewById<EditText>(Resource.Id.etListName);
            spinnerType = view.FindViewById<Spinner>(Resource.Id.spinnerType);
            btnCreate = view.FindViewById<Button>(Resource.Id.btnCreate);

            // === 3. הגדרה ואתחול של רכיב ה-Spinner (תיבת הבחירה) ===
            // יצירת מערך מחרוזות שמכיל את סוגי הרשימות האפשריים באפליקציה שלך
            var types = new string[] { "Standard", "Shopping", "Work", "Home" };

            // יצירת אדפטר פשוט המתווך בין מערך הסטרינגים לצורה הגרפית שבה הם יוצגו ב-Spinner
            var adapter = new ArrayAdapter<string>(Context, Android.Resource.Layout.SimpleSpinnerItem, types);
            // הגדרת העיצוב הויזואלי של הרשימה כשהיא נפתחת כלפי מטה
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            // הצבת האדפטר המעודכן בתוך ה-Spinner
            spinnerType.Adapter = adapter;

            // === 4. טיפול באירוע לחיצה על כפתור הצטרפות (btnJoin) ===
            btnJoin.Click += async (s, e) =>
            {
                // שליפת הקוד שהוקלד, ניקוי רווחים מיותרים והמרה לאותיות גדולות (Caps Lock) להתאמה בבסיס הנתונים
                string code = etJoinCode.Text.Trim().ToUpper();

                // בדיקה שהמשתמש אכן הקליד קוד ולא השאיר שדה ריק
                if (!string.IsNullOrEmpty(code))
                {
                    // שימוש ב-Polymorphism (רב-צורתיות) כדי לקבל גישה למסך הבית (HomeActivity) שמארח את ה-Fragment הזה
                    var home = Activity as HomeActivity;
                    if (home != null)
                    {
                        // הפעלת הפונקציה האסינכרונית שנמצאת ב-HomeActivity להצטרפות לרשימה לפי הקוד
                        await home.JoinListByCode(code);
                        // סגירה והעלמה אוטומטית של תפריט ה-BottomSheet החלקלק מהמסך לאחר סיום הפעולה
                        Dismiss();
                    }
                }
                else
                {
                    // במידה והשדה ריק, נציג הודעת אזהרה קופצת (Toast)
                    Toast.MakeText(Context, "Please enter a code", ToastLength.Short).Show();
                }
            };

            // === 5. טיפול באירוע לחיצה על כפתור יצירת רשימה חדשה (btnCreate) ===
            btnCreate.Click += async (s, e) =>
            {
                // שליפת השם שהוקלד לרשימה וניקוי רווחים בקצוות
                string name = etListName.Text.Trim();
                // שליפת הערך שנבחר כרגע מתוך תיבת ה-Spinner (סוג הרשימה)
                string type = spinnerType.SelectedItem.ToString();

                // בדיקה שהמשתמש אכן הזין שם לרשימה החדשה
                if (!string.IsNullOrEmpty(name))
                {
                    // שליפת ה-UID הייחודי של המשתמש המחובר כרגע ב-Firebase Auth כדי להגדיר אותו כיוצר הרשימה
                    string uid = FirebaseAuth.Instance.CurrentUser.Uid;

                    // קריאה לפונקציה אסינכרונית בתוך מחלקת העזר (FireBaseHelper) ליצירת הרשימה החדשה בשרת ה-Firestore
                    bool success = await FireBaseHelper.CreateList(name, uid, type);

                    // אם היצירה בשרת הצליחה בהצלחה
                    if (success)
                    {
                        // הצגת הודעת הצלחה למשתמש
                        Toast.MakeText(Context, "List Created!", ToastLength.Short).Show();

                        // קבלת התייחסות למסך הבית (HomeActivity) כדי לבקש ממנו לרענן את רשימת התצוגה שלו
                        var home = Activity as HomeActivity;
                        if (home != null)
                        {
                            // הפעלת פונקציית הטעינה מחדש ב-HomeActivity כדי שהרשימה החדשה תופיע מיד ב-RecyclerView
                            await home.LoadUserLists();
                        }
                        // סגירה של תפריט ה-BottomSheet
                        Dismiss();
                    }
                }
                else
                {
                    // במידה ושם הרשימה נשאר ריק, נציג הודעה מתאימה
                    Toast.MakeText(Context, "Please enter a list name", ToastLength.Short).Show();
                }
            };

            // החזרת ה-View המוכן והשלם למערכת של אנדרואיד כדי שתציג אותו על המסך
            return view;
        }
    }
}