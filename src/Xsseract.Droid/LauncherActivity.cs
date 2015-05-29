#region

using System;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Widget;

#endregion

namespace Xsseract.Droid
{
  [Activity(MainLauncher = true)]
  public class LauncherActivity: ActivityBase
  {
    private ProgressBar progressView;

    #region Protected methods

    protected override void OnCreate(Bundle bundle)
    {
      base.OnCreate(bundle);

      if (!ApplicationContext.AppContext.Initialized)
      {
        SetContentView(Resource.Layout.Launcher);
        progressView = FindViewById<ProgressBar>(Resource.Id.progressView);
      }
    }

    protected async override void OnResume()
    {
      base.OnResume();

      if(!ApplicationContext.AppContext.Initialized)
      {
        await PerformInit();
        StartActivity(typeof(CaptureActivity));
      }
      else
      {
        StartActivity(typeof(CaptureActivity));
      }
    }

    #endregion
    private async Task PerformInit()
    {
      var t1 = Task.Factory.StartNew(
        () =>
        {
          ApplicationContext.AppContext.Initialize();
          if(ApplicationContext.AppContext.InitializeError)
          {
            // TODO: Handle error during init.
          }
        });
      var t2 = Task.Factory.StartNew(
        () =>
        {
          ApplicationContext.InitializeTesseract();
          // TODO: Handle error.
        });

      await Task.WhenAll(t1, t2);
    }
  }
}
