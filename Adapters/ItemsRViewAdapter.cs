using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Big17DataFirebase2.Model;
using System;
using System.Collections.Generic;


namespace Big17DataFirebase2.Adapters
{
    public class ItemsRViewAdapter : RecyclerView.Adapter
    {
        List<Item> items;
        public event EventHandler<int> ItemClick;
        public event EventHandler<int> CheckChanged; // To handle ticking off items

        public ItemsRViewAdapter(List<Item> items)
        {
            this.items = items;
        }

        public override int ItemCount => items.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (holder is ItemViewHolder itemViewHolder)
            {
                var item = items[position];
                itemViewHolder.tvItemName.Text = item.Name;
                itemViewHolder.cbIsChecked.Checked = item.IsChecked;

                // Clear listener before setting checked state to avoid recursion
                itemViewHolder.cbIsChecked.CheckedChange -= (s, e) => CheckChanged?.Invoke(this, position);
                itemViewHolder.cbIsChecked.Checked = item.IsChecked;
                itemViewHolder.cbIsChecked.CheckedChange += (s, e) => CheckChanged?.Invoke(this, position);
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            // Create a new layout file called item_row_layout.xml
            View layout = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.item_row_layout, parent, false);
            return new ItemViewHolder(layout, (pos) => ItemClick?.Invoke(this, pos));
        }

        public class ItemViewHolder : RecyclerView.ViewHolder
        {
            public TextView tvItemName;
            public CheckBox cbIsChecked;

            public ItemViewHolder(View itemView, Action<int> clickListener) : base(itemView)
            {
                tvItemName = itemView.FindViewById<TextView>(Resource.Id.tvItemName);
                cbIsChecked = itemView.FindViewById<CheckBox>(Resource.Id.cbIsChecked);

                itemView.Click += (s, e) => clickListener(LayoutPosition);
            }
        }
    }
}