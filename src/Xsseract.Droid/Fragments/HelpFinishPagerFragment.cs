using System;

using Android.OS;
using Android.Views;
using Android.Widget;

namespace Xsseract.Droid.Fragments
{
  public class HelpFinishPagerFragment : HtmlFragmentBase
  {
    private Button btnGotIt;

    public event EventHandler<EventArgs> GotIt;

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
      var view = inflater.Inflate(Resource.Layout.HelpFinish, null);
      btnGotIt = view.FindViewById<Button>(Resource.Id.btnGoIt);
      btnGotIt.Click += (sender, e) => OnGotIt(EventArgs.Empty);

      return view;
    }

    protected override string GetHtml()
    {
      return Resources.GetString(Resource.String.label_helpHelp);
    }

    protected void OnGotIt(EventArgs e)
    {
      var handler = GotIt;
      if(null != handler)
      {
        handler(this, e);
      }
    }
  }
}