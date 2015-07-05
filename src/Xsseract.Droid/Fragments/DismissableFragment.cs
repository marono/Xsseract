#region

using System;
using Android.OS;
using Android.Views;
using Android.Widget;

#endregion

namespace Xsseract.Droid.Fragments
{
  public abstract class DismissableFragment : HtmlFragmentBase
  {
    #region Fields

    private readonly bool allowDismissal;
    private Button btnGotIt;

    #endregion

    public event EventHandler<EventArgs> Dismissed;

    #region .ctors

    protected DismissableFragment(bool allowDismissal)
    {
      this.allowDismissal = allowDismissal;
    }

    #endregion

    public override void OnViewCreated(View view, Bundle savedInstanceState)
    {
      base.OnViewCreated(view, savedInstanceState);
      btnGotIt = view.FindViewWithTag("dismissAction") as Button;
      if (null != btnGotIt)
      {
        if (!allowDismissal)
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
      Dismissed?.Invoke(this, e);
    }
  }
}
