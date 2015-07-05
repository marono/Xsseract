#region

using Android.OS;
using Android.Views;

#endregion

namespace Xsseract.Droid.Fragments
{
  public class HelpWelcomeFragment : HtmlFragmentBase
  {
    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
      return inflater.Inflate(Resource.Layout.HelpWelcome, null);
    }

    protected override string GetHtml()
    {
      return Resources.GetString(Resource.String.html_HelpWelcome);
    }
  }
}
