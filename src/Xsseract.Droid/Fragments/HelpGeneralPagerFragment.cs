using Android.OS;
using Android.Views;


namespace Xsseract.Droid.Fragments
{
  public class HelpGeneralPagerFragment : BaseHtmlFragment
  {
    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
      return inflater.Inflate(Resource.Layout.HelpGeneral, null);
    }

    protected override string GetHtml()
    {
      return Resources.GetString(Resource.String.label_helpImageNotice);
    }
  }
}