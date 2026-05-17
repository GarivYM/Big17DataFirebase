// ייבוא רכיבי הליבה של אנדרואיד לעבודה עם חלונות, תצוגות ופקדים
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
// ייבוא רכיב ה-RecyclerView מבית AndroidX
using AndroidX.RecyclerView.Widget;
// ייבוא מודל הפריט (Item) מהפרויקט שלך
using Big17DataFirebase2.Model;
using System;
using System.Collections.Generic;

namespace Big17DataFirebase2.Adapters
{
    // האדאפטר האחראי על ניהול והצגת רשימת הפריטים (למשל, מוצרים בתוך רשימת קניות)
    public class ItemsRViewAdapter : RecyclerView.Adapter
    {
        List<Item> items; // רשימת הפריטים המקומית (המודל)
        public event EventHandler<int> ItemClick; // אירוע לחיצה על השורה כולה
        public event EventHandler<int> CheckChanged; // אירוע ייעודי המופעל בעת סימון/הסרת סימון מה-CheckBox

        // הבנאי של האדאפטר - מקבל את רשימת הפריטים ומציב אותה במשתנה המקומי
        public ItemsRViewAdapter(List<Item> items)
        {
            this.items = items;
        }

        // מאפיין המחזיר את כמות הפריטים הכוללת שיש להציג ברשימה
        public override int ItemCount => items.Count;

        // פונקציית החיבור (Bind) - מופעלת בכל פעם ששורה נכנסת למסך ומציגה נתונים
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (holder is ItemViewHolder itemViewHolder)
            {
                var item = items[position];

                // 1. עדכון ה-ViewHolder באיזה מיקום (position) הוא נמצא כרגע.
                // קריטי ביותר כדי שמתודת ה-OnCheckedChange תדע בדיוק איזה פריט לעדכן במערך.
                itemViewHolder.CurrentPosition = position;

                // 2. פתרון באג הגלילה הידוע של אנדרואיד: נתק לחלוטין את ה-Event של ה-CheckBox.
                // מונע מצב שבו שינוי הסטטוס הוויזואלי בשורה הבאה יפעיל בטעות את הלוגיקה של הפריט הקודם שהשתמש ב-ViewHolder הזה!
                itemViewHolder.cbIsChecked.CheckedChange -= itemViewHolder.OnCheckedChange;

                // 3. עדכון הנתונים הויזואליים של השורה הנוכחית מתוך המודל
                itemViewHolder.tvItemName.Text = item.Name;
                itemViewHolder.cbIsChecked.Checked = item.IsChecked;

                // 4. חבר מחדש את המאזין הבטוח - מעכשיו כל לחיצה של המשתמש תאזין ותפעל בצורה נכונה
                itemViewHolder.cbIsChecked.CheckedChange += itemViewHolder.OnCheckedChange;
            }
        }

        // יצירת ה-ViewHolder לראשונה על ידי ניפוח ה-XML של השורה (item_row_layout)
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View layout = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.item_row_layout, parent, false);

            // יצירת ה-ViewHolder והעברת רפרנס של האדאפטר הנוכחי (this) בשביל לאפשר הפעלת אירועים
            return new ItemViewHolder(layout, this);
        }

        // מחלקה פנימית המייצגת ומחזיקה את הפקדים הגרפיים של שורת פריט בודדת
        public class ItemViewHolder : RecyclerView.ViewHolder
        {
            public TextView tvItemName;
            public CheckBox cbIsChecked;

            // מאפיין השומר את המיקום הדינמי הנוכחי של ה-ViewHolder בזמן הגלילה
            public int CurrentPosition { get; set; }
            private ItemsRViewAdapter adapter;

            // הבנאי של ה-ViewHolder - מקשר רכיבים ומאזין ללחיצה על השורה
            public ItemViewHolder(View itemView, ItemsRViewAdapter adapter) : base(itemView)
            {
                this.adapter = adapter;

                // קישור פקדי ה-XML למשתני ה-C#
                tvItemName = itemView.FindViewById<TextView>(Resource.Id.tvItemName);
                cbIsChecked = itemView.FindViewById<CheckBox>(Resource.Id.cbIsChecked);

                // האזנה ללחיצה על השורה כולה ושימוש ב-LayoutPosition הבטוח לקבלת המיקום המדויק
                itemView.Click += (s, e) => adapter.ItemClick?.Invoke(adapter, LayoutPosition);
            }

            // מתודה קבועה שאינה אנונימית (מתודה רגילה עם שם) - חיונית ביותר!
            // רק בזכות העובדה שיש לה שם, ניתן להסיר (=-) ולחבר (+=) אותה בתוך OnBindViewHolder באופן בטוח ב-C#
            public void OnCheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
            {
                // בדיקת הגנה: מוודאים שהמיקום תקין ונמצא בתוך גבולות הרשימה
                if (CurrentPosition >= 0 && CurrentPosition < adapter.items.Count)
                {
                    // עדכון המודל המקומי ב-C# מיד ברגע הלחיצה כדי לשמור על סנכרון ב-UI
                    adapter.items[CurrentPosition].IsChecked = e.IsChecked;
                }

                // הפעלת האירוע החיצוני (מקפיץ את המידע החוצה ל-ListActivity כדי שיעדכן את השרת של Firestore)
                adapter.CheckChanged?.Invoke(adapter, CurrentPosition);
            }
        }
    }
}