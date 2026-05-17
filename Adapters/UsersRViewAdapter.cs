// ייבוא רכיבי הליבה של אנדרואיד ומערכת ההפעלה
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
// ייבוא רכיב ה-RecyclerView של AndroidX להצגת רשימות מתקדמות ויעילות בזיכרון
using AndroidX.RecyclerView.Widget;
// ייבוא מודל המשתמש (User) מהפרויקט שלך
using Big17DataFirebase2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Big17DataFirebase2.Adapters
{
    // אדאפטר (מתאם) ה-RecyclerView האחראי על חיבור רשימת המשתמשים מ-C# לתצוגה הגרפית באנדרואיד
    public class UsersRViewAdapter : RecyclerView.Adapter
    {
        Context context; // קונטקסט האפליקציה (בדרך כלל ה-Activity המציגה)
        List<User> users; // רשימת המקור שמכילה את נתוני המשתמשים שיש להציג

        // הגדרת אירוע (Event) של סישארפ שיופעל בעת לחיצה על שורה, ומחזיר את ה-Position (מיקום השורה)
        public event EventHandler<int> ItemClick;
        // NEW click for the delete button -> הערה שלך כהכנה או לוגיקה נפרדת לכפתור המחיקה

        // בנאי המקבל את הקונטקסט ואת רשימת המשתמשים ומאתחל את שדות המחלקה
        public UsersRViewAdapter(Context context, List<User> users)
        {
            this.context = context;
            this.users = users;
        }

        // מאפיין (Property) המחזיר ל-RecyclerView את כמות הפריטים הכוללת שיש ברשימה
        public override int ItemCount => users.Count;

        // פונקציה המחברת (Bind) את הנתונים של משתמש ספציפי לרכיבי ה-UI בשורה הנוכחית
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            // בדיקה בטוחה שה-ViewHolder הוא אכן מסוג UserViewHolder שנתמך באדאפטר זה
            if (holder is UserViewHolder userViewHolder)
            {
                var user = users[position]; // שליפת המשתמש המתאים לפי המיקום (Index) שלו ברשימה

                // השמת הנתונים מהמודל ישירות לתוך פקדי הטקסט והתמונה שבשורה
                userViewHolder.firstName.Text = user.FirstName;
                userViewHolder.lastName.Text = user.LastName;
                userViewHolder.ivAvatar.SetImageResource(user.ImageId);
            }
        }

        // פונקציה המופעלת על ידי אנדרואיד כדי ליצור לראשונה את מבנה השורה (ViewHolder) בזיכרון
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            // טעינה וניפוח (Inflate) של קובץ ה-Layout של השורה הבודדת מה-Resources
            View layout = LayoutInflater.From(context).Inflate(Resource.Layout.usercard_layout, parent, false);

            // יצירת מופע חדש של ViewHolder והעברת ה-Layout שלו יחד עם פונקציית הלמבדה שמפעילה את ה-ItemClick
            return new UserViewHolder(layout, (pos) => ItemClick?.Invoke(this, pos));
        }

        // מחלקה פנימית המייצגת את מחזיק הרכיבים (ViewHolder) של שורת משתמש יחידה
        public class UserViewHolder : RecyclerView.ViewHolder
        {
            // הגדרת משתנים עבור פקדי התצוגה המופיעים בתוך השורה
            public TextView firstName, lastName;
            public ImageView ivAvatar;

            // הבנאי של ה-ViewHolder - מוצא את הפקדים בתוך ה-View ומגדיר את ההאזנה ללחיצות
            public UserViewHolder(View itemView, Action<int> listener) : base(itemView)
            {
                // קישור משתני ה-C# לרכיבים הגרפיים שבקובץ ה-XML באמצעות ה-ID שלהם
                firstName = itemView.FindViewById<TextView>(Resource.Id.tvFirstName);
                lastName = itemView.FindViewById<TextView>(Resource.Id.tvLastName);
                ivAvatar = itemView.FindViewById<ImageView>(Resource.Id.ivAvatar);

                // רישום לאירוע לחיצה (Click) על השורה כולה (itemView)
                // בעת לחיצה, מופעל ה-listener (הנציג) ונשלח אליו ה-LayoutPosition הדינמי והעדכני של השורה
                itemView.Click += (sender, e) => listener(LayoutPosition);
            }
        }
    }
}