using Android.OS;
using Android.Views;

namespace Xsseract.Droid.Fragments
{
  public class HelpResultsPagerFragment : DismissableFragment
  {
    public HelpResultsPagerFragment(bool allowDismissal)
      : base(allowDismissal) {}

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
      return inflater.Inflate(Resource.Layout.HelpResult, null);
    }

    protected override string GetHtml()
    {
      return Resources.GetString(Resource.String.label_HelpResultDescr);
    }
  }
}