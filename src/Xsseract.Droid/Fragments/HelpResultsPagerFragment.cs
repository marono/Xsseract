#region

using Android.OS;
using Android.Views;

#endregion

namespace Xsseract.Droid.Fragments
{
  public class HelpResultsPagerFragment : DismissableFragment
  {
    #region .ctors

    public HelpResultsPagerFragment(bool allowDismissal)
      : base(allowDismissal) {}

    #endregion

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
      return inflater.Inflate(Resource.Layout.HelpResult, null);
    }

    protected override string GetHtml()
    {
      return Resources.GetString(Resource.String.html_HelpResultDescr);
    }
  }
}
