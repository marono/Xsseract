#region

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Bluetooth;
using Android.OS;
using Android.Widget;

#endregion

namespace Xsseract.Droid
{
  // TODO: Investigate white screen before splashscreen.
  [Activity(MainLauncher = true)]
  public class LauncherActivity : ActivityBase
  {
    #region Protected methods

    protected override void OnCreate(Bundle bundle)
    {
      base.OnCreate(bundle);

      // TODO: Review this code, tesseract init should only happen when it hasn't already been initialized.
      if (!ApplicationContext.AppContext.Initialized)
      {
        SetContentView(Resource.Layout.Launcher);
      }
    }

    protected async override void OnResume()
    {
      base.OnResume();

      // TODO: Investigate exceptions on Tasks when the return is void!!!
      
      if (!ApplicationContext.AppContext.Initialized)
      {
        await PerformInit();
      }

      var tessInitializer = new TessDataInitializer(ApplicationContext.AppContext);
      await tessInitializer.InitializeAsync();

      StartActivity(typeof(CaptureActivity));
      if(null != ApplicationContext.AppStartupTracker)
      {
        ApplicationContext.AppStartupTracker.Stop();
        var span = ApplicationContext.AppStartupTracker.Elapsed;
        ApplicationContext.AppStartupTracker = null;

        LogAppStatupTime(span);
      }
    }

    #endregion

    private async Task PerformInit()
    {
      var t1 = Task.Factory.StartNew(
        () =>
        {
          ApplicationContext.AppContext.Initialize();
          if (ApplicationContext.AppContext.InitializeError)
          {
            // TODO: Handle error during init.
          }
        });

      await Task.WhenAll(t1);
    }

    private void LogAppStatupTime(TimeSpan span)
    {
      ApplicationContext.AppContext.LogEvent(AppTrackingEvents.Startup,
        new Dictionary<string, string>
        {
          { "Duration", span.TotalMilliseconds.ToString() },
          { "Device", ApplicationContext.AppContext.DeviceName }
        });
    }
  }
}
