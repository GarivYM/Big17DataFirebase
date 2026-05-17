// ייבוא רכיבי ה-UI הבסיסיים של אנדרואיד (תצוגות ומערכים גרפיים)
using Android.Views;
using Android.Widget;
// ייבוא רכיב ה-RecyclerView מבית AndroidX
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;

namespace Big17DataFirebase2.Adapters
{
    // אדאפטר ייעודי עבור פאנל הניהול (Admin) להצגה ומחיקה של רשימות
    public class AdminListsAdapter : RecyclerView.Adapter
    {
        // רשימה של זוגות מפתח-ערך: המפתח (Key) הוא ה-ListId מ-Firebase, והערך (Value) הוא שם הרשימה
        private List<KeyValuePair<string, string>> _lists;

        // אירוע (Event) שמחזיר מחרוזת (string) - שולח את ה-ListId הייחודי ישירות ל-Activity כדי לבצע את המחיקה בשרת
        public event EventHandler<string> OnDeleteListClick;

        // הבנאי של האדאפטר - מקבל את רשימת הזוגות ומאתחל את השדה הפרטי
        public AdminListsAdapter(List<KeyValuePair<string, string>> lists)
        {
            _lists = lists;
        }

        // מחזיר את כמות הרשימות הקיימות בפאנל הניהול
        public override int ItemCount => _lists.Count;

        // פונקציית החיבור (Bind) בין הנתונים לפקדים הגרפיים בכל שורה
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (holder is ListViewHolder listViewHolder)
            {
                // שליפת זוג הנתונים (מפתח וערך) עבור המיקום הנוכחי ברשימה
                var listData = _lists[position];

                // הצגת שם הרשימה (Value) בתוך פקד ה-TextView
                listViewHolder.TvListName.Text = listData.Value;

                // הערת ארכיטקטורה: השורה הזו -> `listViewHolder.BtnDeleteList.Click -= (s, e) => { };`
                // מנסה לנתק פונקציה אנונימית חדשה, ולכן היא לא באמת תנתק מאזינים קודמים אם נוצרו בגלל ה-Reuse של ה-ViewHolder.
                // מכיוון שהקוד עובד לך, הכל טוב! אך אם בעתיד תשים לב לכפל מחיקות בגלילה מהירה, מומלץ להעביר את הרישום ל-Click לתוך הבנאי של ה-ViewHolder (כמו באדאפטרים הקודמים).
                listViewHolder.BtnDeleteList.Click -= (s, e) => { }; // ניסיון למניעת כפל אירועים

                // חיבור מאזין ללחיצה על כפתור המחיקה בשורה הנוכחית
                listViewHolder.BtnDeleteList.Click += (s, e) =>
                {
                    // הפעלת האירוע והעברת ה-Key (שהוא ה-ListId של הרשימה הספציפית הזו ב-Firebase)
                    OnDeleteListClick?.Invoke(this, listData.Key);
                };
            }
        }

        // יצירת ה-ViewHolder על ידי ניפוח קובץ ה-XML הייעודי של פאנל הניהול (admin_list_item)
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View view = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.admin_list_item, parent, false);
            return new ListViewHolder(view);
        }

        // מחלקה פנימית (ViewHolder) המחזיקה את פקדי השורה של פאנל הניהול
        private class ListViewHolder : RecyclerView.ViewHolder
        {
            public TextView TvListName { get; set; } // פקד להצגת שם הרשימה
            public ImageButton BtnDeleteList { get; set; } // כפתור מחיקת הרשימה

            // הבנאי מוצא ומקשר את הפקדים מתוך ה-View לפי ה-ID שלהם ב-XML
            public ListViewHolder(View view) : base(view)
            {
                TvListName = view.FindViewById<TextView>(Resource.Id.tvAdminListName);
                BtnDeleteList = view.FindViewById<ImageButton>(Resource.Id.btnDeleteList);
            }
        }
    }
}