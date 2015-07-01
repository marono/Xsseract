using System;

using Android.OS;
using Android.Views;
using Android.Widget;

namespace Xsseract.Droid.Fragments
{
  public abstract class DismissableFragment : HtmlFragmentBase
  {
    private Button btnGotIt;
    private readonly bool allowDismissal;

    public event EventHandler<EventArgs> Dismissed;

    protected DismissableFragment(bool allowDismissal)
    {
      this.allowDismissal = allowDismissal;
    }

    public override void OnViewCreated(View view, Bundle savedInstanceState)
    {
      base.OnViewCreated(view, savedInstanceState);
      btnGotIt = view.FindViewWithTag("dismissAction") as Button;
      if (null != btnGotIt)
      {
        if(!allowDismissal)
        {
          btnGotIt.Visibility = ViewStates.Gone;
        }
        else
        {
          btnGotIt.Click += (sender, e) => OnDismissed(EventArgs.Empty);
        }
      }
    }

    protected void OnDismissed(EventArgs e)
    {
      var handler = Dismissed;
      if(null != handler)
      {
        handler(this, e);
      }
    }
  }
}