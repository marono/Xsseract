#region

using System;
using Android.OS;
using Android.Support.V4.App;
using Android.Views;
using Android.Webkit;
using Android.Widget;

#endregion

namespace Xsseract.Droid.Fragments
{
  public abstract class HtmlFragmentBase : Fragment
  {
    #region Fields

    private WebView webView;

    #endregion

    public override void OnViewCreated(View view, Bundle savedInstanceState)
    {
      base.OnViewCreated(view, savedInstanceState);

      var container = view.FindViewWithTag("helpContents") as ViewGroup;
      if (container != null)
      {
        webView = new WebView(view.Context);

#if DEBUG
        WebView.SetWebContentsDebuggingEnabled(true);
#endif

        webView.CanScrollHorizontally(0);
        webView.CanScrollVertically(0);
        webView.LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
        webView.SetBackgroundColor(Resources.GetColor(Resource.Color.black));
        webView.SetBackgroundResource(Resource.Color.black);

        webView.LoadDataWithBaseURL(null, GetWrappedHtml(), "text/html;", "utf-8", null);

        container.AddView(webView);
      }
    }

    protected abstract string GetHtml();

    #region Private Methods

    private string GetWrappedHtml()
    {
      return String.Format("<html><body style=\"width: 95%; color: white; text-align: justify;\">{0}</body></html>", GetHtml());
    }

    #endregion
  }
}
