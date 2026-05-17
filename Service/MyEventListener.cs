// ייבוא ספריות הבסיס של אנדרואיד ומערכת ההפעלה
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
// ייבוא ספריית ה-SDK הרשמית של Firebase Firestore עבור אנדרואיד
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Big17DataFirebase2.Service
{
    // מחלקה זו משמשת כמתרגם/מגשר (Adapter) בין עולם ה-Java של אנדרואיד לעולם ה-C# של .NET.
    // היא חייבת לרשת מ-Java.Lang.Object ולממש את ה-Interface של ה-IEventListener של Firestore,
    // כדי שמערכת אנדרואיד תוכל להזריק אליה את הנתונים בזמן אמת מהשרת.
    public class MyEventListener : Java.Lang.Object, IEventListener
    {
        // הגדרת משתנה פרטי מסוג Action (נציג/Delegate) שיכול להחזיק מתודה (פונקציה) מ-C#.
        // המתודה הזו תקבל את הנתונים הגולמיים מ-Java (אובייקט התוצאה) ואת השגיאה במידה וקיימת.
        private readonly Action<Java.Lang.Object, FirebaseFirestoreException> _onEvent;

        // הבנאי (Constructor) של המחלקה - מקבל כפרמטר את הלוגיקה של ה-C# (מתודת הטיפול שנכתבה במסכים השונים)
        // ושומר אותה בתוך המשתנה הפרטי _onEvent.
        public MyEventListener(Action<Java.Lang.Object, FirebaseFirestoreException> onEvent)
        {
            _onEvent = onEvent;
        }

        // פונקציית המטרה (Callback) של Firebase אנדרואיד. 
        // בכל פעם שיש שינוי בבסיס הנתונים (למשל: משתמש התעדכן, רשימה נוספה), מערכת ה-Java של אנדרואיד 
        // תקרא באופן אוטומטי לפונקציה הזו ותעביר אליה את המידע החדש (value) או את השגיאה (error).
        public void OnEvent(Java.Lang.Object value, FirebaseFirestoreException error)
        {
            // בדיקה בטוחה: במידה והשדכן ה-C#-אי שלנו (_onEvent) אינו ריק והוגדרה לו פונקציית יעד,
            // אנו מפעילים (Invoke) אותה ומעבירים אליה את הנתונים כדי שהמסך (כמו AdminActivity) יוכל לעבד אותם.
            _onEvent?.Invoke(value, error);
        }
    }
}