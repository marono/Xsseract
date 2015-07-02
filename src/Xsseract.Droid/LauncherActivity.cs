#region

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;

#endregion

namespace Xsseract.Droid
{
  // TODO: Investigate white screen before splashscreen.
  [Activity(Name = "xsseract.droid.Launcher", Icon = "@drawable/icon", NoHistory = true)]
  public class LauncherActivity : ActivityBase
  {
    private TextView txtViewDescription;
    #region Protected methods

    protected override void OnCreate(Bundle bundle)
    {
      base.OnCreate(bundle);

      if (XsseractContext.State == XsseractContext.AppContextState.None)
      {
        SetContentView(Resource.Layout.Launcher);
      }

      txtViewDescription = FindViewById<TextView>(Resource.Id.txtViewDescription);
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
    {
      base.OnActivityResult(requestCode, resultCode, data);
      Finish();
    }

    protected async override void OnResume()
    {
      base.OnResume();

      // TODO: Investigate exceptions on Tasks when the return is void!!!
      if (XsseractContext.State != XsseractContext.AppContextState.None)
      {
        switch (XsseractContext.State)
        {
          case XsseractContext.AppContextState.Initialized:
            ResumeApplication();
            break;
          case XsseractContext.AppContextState.InitializationErrors:
            DisplayAlert(Resources.GetString(Resource.String.label_InitializeFailedOnPrevAttempt),
              () =>
              {
                SetResult(Result.Canceled);
                Finish();
              });
            return;
        }
        return;
      }

      try
      {
        XsseractContext.FirstTimeInitialize += AppContext_FirstTimeInitialize;
        XsseractContext.MeteredConnectionPermissionCallback = MeteredConnectionPermissionCallback;
        await PerformInit();
      }
      catch (Exception e)
      {
        DisplayError(e,
          () =>
          {
            SetResult(Result.Canceled);
            Finish();
          });

        return;
      }
      finally
      {
        XsseractContext.MeteredConnectionPermissionCallback = null;
        XsseractContext.FirstTimeInitialize -= AppContext_FirstTimeInitialize;
      }

      ResumeApplication();
    }

    private bool MeteredConnectionPermissionCallback()
    {
      ManualResetEvent waitHandle = new ManualResetEvent(false);
      bool result = false;
      RunOnUiThread(
        () =>
        {
          new AlertDialog.Builder(this)
        .SetTitle(Resource.String.AlertTitle)
        .SetMessage(Resources.GetString(Resource.String.message_MobileDataDownload))
        .SetPositiveButton(Resource.String.action_Yes, delegate
        {
          result = true;
          waitHandle.Set();
        })
        .SetNegativeButton(Resource.String.action_No, delegate
        {
          result = false;
          waitHandle.Set();
        })
        .Show();
        });
      waitHandle.WaitOne();

      return result;
    }

    private void ResumeApplication()
    {
      bool pipeResult = Intent.GetBooleanExtra("PipeResult", false);

      var activityToStart = typeof(CaptureActivity);
      if (XsseractContext.IsFirstRun)
      {
        activityToStart = typeof(HelpActivity);
      }

      var intent = new Intent(this, activityToStart);
      if (pipeResult)
      {
        intent.PutExtra(CaptureActivity.Constants.PipeResult, true);
        intent.AddFlags(ActivityFlags.ForwardResult);
      }

      StartActivity(intent);
      if (null != XsseractApplication.AppStartupTracker)
      {
        XsseractApplication.AppStartupTracker.Stop();
        var span = XsseractApplication.AppStartupTracker.Elapsed;
        XsseractApplication.AppStartupTracker = null;

        LogAppStatupTime(span);
      }

      Finish();
    }

    private void AppContext_FirstTimeInitialize(object sender, EventArgs eventArgs)
    {
      RunOnUiThread(() => txtViewDescription.Text = Resources.GetString(Resource.String.label_FirstTimeInitialize));
    }

    #endregion

    private async Task PerformInit()
    {
      await Task.Factory.StartNew(
        () =>
        {
          XsseractContext.Initialize();
          // TODO: Handle error during init.
        });
    }

    private void LogAppStatupTime(TimeSpan span)
    {
      XsseractContext.LogEvent(AppTrackingEvents.Startup,
        new Dictionary<string, string> {
          { "Duration", span.TotalMilliseconds.ToString () },
          { "Device", XsseractContext.DeviceName }
        });
    }
  }
}
