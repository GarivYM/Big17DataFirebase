// ייבוא רכיבי הליבה הגרפיים של אנדרואיד (תצוגות, פקדים וחלונות)
using Android.Views;
using Android.Widget;
// ייבוא רכיב ה-RecyclerView של AndroidX לניהול רשימות ארוכות ויעילות בזיכרון
using AndroidX.RecyclerView.Widget;
// ייבוא מחלקות המודל של הפרויקט שלך (שם נמצא אובייקט הנתונים List)
using Big17DataFirebase2.Model;
using System;
using System.Collections.Generic;

namespace Big17DataFirebase2.Adapters
{
    // אדאפטר האחראי על תיווך והצגת קולקציית הרשימות (Lists) בתוך ה-RecyclerView
    public class ListsRViewAdapter : RecyclerView.Adapter
    {
        // הגדרת אירוע (Event) שמאפשר ל-Activity להאזין ולדעת מתי המשתמש לחץ על שורה מסוימת
        public event EventHandler<int> ItemClick;

        // משתנה פרטי השומר את רשימת המקור שמגיעה בבנאי
        private List<List> lists;

        // הבנאי של האדאפטר - מקבל מבחוץ את רשימת הנתונים ומאתחל את השדה המקומי
        public ListsRViewAdapter(List<List> lists)
        {
            this.lists = lists;
        }

        // מאפיין (Property) שמדווח ל-RecyclerView כמה פריטים קיימים בסך הכל ברשימה
        public override int ItemCount => lists.Count;

        // פונקציה המחברת (Bind) את הנתונים מהמודל לפי המיקום (Position) לרכיבי ה-UI בשורה
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            // המרה (Casting) של ה-ViewHolder הכללי לסוג הספציפי שלנו: ListsViewHolder
            ListsViewHolder vh = holder as ListsViewHolder;

            // גישה לשדה ה-Title של הרשימה במיקום הנוכחי, והשמת הטקסט בתוך פקד ה-TextView
            vh.Title.Text = lists[position].Title;
        }

        // פונקציה המופעלת על ידי אנדרואיד כדי לייצר לראשונה את תבנית השורה בזיכרון
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            // טעינה וניפוח (Inflate) של קובץ ה-XML המייצג שורת רשימה בודדת (list_row)
            View itemView = LayoutInflater.From(parent.Context)
                .Inflate(Resource.Layout.list_row, parent, false);

            // יצירת ה-ViewHolder החדש, והעברת ה-View יחד עם פונקציית הלחיצה הסטנדרטית OnClick
            ListsViewHolder vh = new ListsViewHolder(itemView, OnClick);
            return vh;
        }

        // מתודת עזר פנימית שמקבלת את מיקום השורה שנלחצה, ומקפיצה (Invoke) את האירוע החוצה אל ה-Activity
        void OnClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }
    }

    // מחלקה המייצגת את מחזיק הרכיבים הגרפיים (ViewHolder) של שורת רשימה בודדת
    public class ListsViewHolder : RecyclerView.ViewHolder
    {
        // הגדרת מאפיין לפקד הטקסט שמציג את כותרת הרשימה
        public TextView Title { get; set; }

        // הבנאי של ה-ViewHolder - מוצא את הפקדים בתוך ה-View ומגדיר את האזנת הלחיצה
        public ListsViewHolder(View view, Action<int> clickListener) : base(view)
        {
            // קישור משתנה ה-C# לפקד ה-TextView שבקובץ ה-XML באמצעות ה-ID שלו
            Title = view.FindViewById<TextView>(Resource.Id.tvListTitle);

            // רישום לאירוע לחיצה על השורה כולה (view)
            // בעת לחיצה, מופעל ה-clickListener ונשלח אליו ה-AdapterPosition הדינמי והעדכני של השורה
            view.Click += (s, e) => clickListener(AdapterPosition);
        }
    }
}