using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using UpcShared;

namespace DroidUpc
{
    public class UpcTypeSpinnerAdapter : BaseAdapter
    {
        private Activity m_activity;

        public UpcTypeSpinnerAdapter(Activity a)
        {
            m_activity = a;
        }

        public override int Count => (int)UpcInvCore.ADAS.Max;

        public override Java.Lang.Object GetItem(int position)
        {
            return null;
        }

        public override long GetItemId(int position)
        {
            return 0;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = convertView ?? m_activity.LayoutInflater.Inflate(Resource.Layout.spinner_type, parent, false);

            var image = view.FindViewById<ImageView>(Resource.Id.TypeImage);

            switch (position)
            {
                case (int)UpcInvCore.ADAS.Book:
                    image.SetImageResource(Resource.Drawable.books);
                    
                    break;
                case (int)UpcInvCore.ADAS.DVD:
                    image.SetImageResource(Resource.Drawable.dvds);
                    break;
                case (int)UpcInvCore.ADAS.Generic:
                    image.SetImageResource(Resource.Drawable.upc);
                    break;
                case (int)UpcInvCore.ADAS.Wine:
                    image.SetImageResource(Resource.Drawable.wine);
                    break;
            }
            return view;
        }
    }
}