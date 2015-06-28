using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.View;
using Xsseract.Droid.Fragments;
using Xsseract.Droid.Views;

namespace Xsseract.Droid
{
  // TODO: Add Toolbar for About and Tutorial actions.
  [Activity(NoHistory = true)]
  public class HelpActivity : ActivityBase
  {
    private GenericFragmentPagerAdaptor pageViewAdapter;
    protected override void OnCreate(Bundle bundle)
    {
      base.OnCreate(bundle);

      SetContentView(Resource.Layout.Help);
      InitializePaging();
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

    private void finishFrag_GotIt(object sender, EventArgs eventArgs)
    {
      ApplicationContext.AppContext.MarkHelpScreenCompleted();
      var intent = new Intent(this, typeof(CaptureActivity));

      if(null != Intent && null != Intent.Extras)
      {
        intent.PutExtras(Intent.Extras);
        intent.AddFlags(ActivityFlags.ForwardResult);
      }

      StartActivity(intent);
    }
  }
}