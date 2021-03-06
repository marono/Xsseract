#region

using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.V4.View;
using Xamarin;
using Xsseract.Droid.Fragments;
using Xsseract.Droid.Views;

#endregion

namespace Xsseract.Droid
{
  [Activity(NoHistory = true, Theme = "@style/AppTheme", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
  public class HelpActivity : ActivityBase
  {
    #region Fields

    private bool firstTimeEntered = true;
    private GenericFragmentPagerAdaptor pageViewAdapter;
    private ITrackHandle timeTracker;

    #endregion

    protected override void OnCreate(Bundle bundle)
    {
      base.OnCreate(bundle);

      SetContentView(Resource.Layout.Help);
      InitializePaging();
    }

    protected override void OnResume()
    {
      base.OnResume();

      if (firstTimeEntered)
      {
        firstTimeEntered = false;
        timeTracker = XsseractContext.LogTimedEvent(AppTrackingEvents.InitialTutorialTime);
        timeTracker.Start();
      }
    }

    #region Private Methods

    private void finishFrag_GotIt(object sender, EventArgs eventArgs)
    {
      XsseractContext.MarkHelpScreenCompleted();

      if (Intent.GetBooleanExtra(Constants.FinishOnClose, false))
      {
        Finish();
        return;
      }

      // This assumes that when the tutorial page is called as an user request, it will always go via the FinishOnClose branch above.
      // We only want to track how much time the user spent on the tutorial page.
      timeTracker?.Stop();
      var intent = new Intent(this, typeof(CaptureActivity));

      if (null != Intent && null != Intent.Extras)
      {
        intent.PutExtras(Intent.Extras);
        intent.AddFlags(ActivityFlags.ForwardResult);
      }

      StartActivity(intent);
    }

    private void InitializePaging()
    {
      pageViewAdapter = new GenericFragmentPagerAdaptor(SupportFragmentManager);
      pageViewAdapter.AddFragment(new HelpWelcomeFragment());
      pageViewAdapter.AddFragment(new HelpGeneralPagerFragment());

      var helpCapture = new HelpCapturePagerFragment(false);
      pageViewAdapter.AddFragment(helpCapture);

      var helpResults = new HelpResultsPagerFragment(false);
      pageViewAdapter.AddFragment(helpResults);

      var finishFrag = new HelpFinishPagerFragment();
      pageViewAdapter.AddFragment(finishFrag);

      var pager = FindViewById<ViewPager>(Resource.Id.viewPager);
      pager.Adapter = pageViewAdapter;

      var indicator = FindViewById<CirclePageIndicator>(Resource.Id.indicator);
      indicator.SetViewPager(pager);

      finishFrag.GotIt += finishFrag_GotIt;
    }

    #endregion

    #region Inner Classes/Enums

    public static class Constants
    {
      public const string
        FinishOnClose = "FinishOnClose";
    }

    #endregion
  }
}
