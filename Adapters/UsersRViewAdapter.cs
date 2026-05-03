using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Big17DataFirebase2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Big17DataFirebase2.Adapters
{
    public class UsersRViewAdapter : RecyclerView.Adapter
    {
        Context context;
        List<User> users;

        // Existing click for the row
        public event EventHandler<int> ItemClick;
        // NEW click for the delete button

        public UsersRViewAdapter(Context context, List<User> users)
        {
            this.context = context;
            this.users = users;
        }

        public override int ItemCount => users.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (holder is UserViewHolder userViewHolder)
            {
                var user = users[position];
                userViewHolder.firstName.Text = user.FirstName;
                userViewHolder.lastName.Text = user.LastName;
                userViewHolder.ivAvatar.SetImageResource(user.ImageId);

                
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            // Inflate your usercard_layout
            View layout = LayoutInflater.From(context).Inflate(Resource.Layout.usercard_layout, parent, false);
            return new UserViewHolder(layout, (pos) => ItemClick?.Invoke(this, pos));
        }

        // UPDATED ViewHolder
        public class UserViewHolder : RecyclerView.ViewHolder
        {
            public TextView firstName, lastName;
            public ImageView ivAvatar;


            public UserViewHolder(View itemView, Action<int> listener) : base(itemView)
            {
                firstName = itemView.FindViewById<TextView>(Resource.Id.tvFirstName);
                lastName = itemView.FindViewById<TextView>(Resource.Id.tvLastName);
                ivAvatar = itemView.FindViewById<ImageView>(Resource.Id.ivAvatar);
                

                // Row click listener
                itemView.Click += (sender, e) => listener(LayoutPosition);
            }
        }
    }
}