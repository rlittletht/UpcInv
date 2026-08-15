using Android.Views;
using UpcShared;
using Exception = Java.Lang.Exception;

namespace DroidUpc2;

public class UpcTypeSpinnerAdapter(Activity m_activity) : BaseAdapter
{
    public override int Count => (int)UpcInvCore.ADAS.Max;

    public override Java.Lang.Object? GetItem(int position)
    {
        return null;
    }

    public override long GetItemId(int position)
    {
        return 0;
    }

    public override View GetView(int position, View? convertView, ViewGroup? parent)
    {
        View view = convertView ?? m_activity.LayoutInflater.Inflate(Resource.Layout.spinner_type, parent, false) ?? throw new Exception("Can't get view");

        using ImageView? image = view.FindViewById<ImageView>(Resource.Id.TypeImage);

        switch (position)
        {
            case (int)UpcInvCore.ADAS.Book:
                image?.SetImageResource(Resource.Drawable.books);
                break;
            case (int)UpcInvCore.ADAS.DVD:
                image?.SetImageResource(Resource.Drawable.dvds);
                break;
            case (int)UpcInvCore.ADAS.Generic:
                image?.SetImageResource(Resource.Drawable.upc);
                break;
            case (int)UpcInvCore.ADAS.Wine:
                image?.SetImageResource(Resource.Drawable.wine);
                break;
            case (int)UpcInvCore.ADAS.WineRack:
                image?.SetImageResource(Resource.Drawable.racks);
                break;
        }

        return view;
    }
}
