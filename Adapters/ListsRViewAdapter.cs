using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Big17DataFirebase2.Model;
using System;
using System.Collections.Generic;

namespace Big17DataFirebase2.Adapters
{
    public class ListsRViewAdapter : RecyclerView.Adapter
    {
        public event EventHandler<int> ItemClick;

        private List<List> lists;

        public ListsRViewAdapter(List<List> lists)
        {
            this.lists = lists;
        }

        public override int ItemCount => lists.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            ListsViewHolder vh = holder as ListsViewHolder;

            vh.Title.Text = lists[position].Title;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context)
                .Inflate(Resource.Layout.list_row, parent, false);

            ListsViewHolder vh = new ListsViewHolder(itemView, OnClick);
            return vh;
        }

        void OnClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }
    }

    public class ListsViewHolder : RecyclerView.ViewHolder
    {
        public TextView Title { get; private set; }

        public ListsViewHolder(View itemView, Action<int> listener) : base(itemView)
        {
            Title = itemView.FindViewById<TextView>(Resource.Id.tvListTitle);

            itemView.Click += (sender, e) => listener(AdapterPosition);
        }
    }
}