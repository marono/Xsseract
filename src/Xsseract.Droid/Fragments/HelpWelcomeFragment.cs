using Android.OS;
using Android.Views;

namespace Xsseract.Droid.Fragments
{
  public class HelpWelcomeFragment : BaseHtmlFragment
  {
    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
      return inflater.Inflate(Resource.Layout.HelpWelcome, null);
    }

    protected override string GetHtml()
    {
      return Resources.GetString(Resource.String.label_helpWelcome);
    }
  }
}