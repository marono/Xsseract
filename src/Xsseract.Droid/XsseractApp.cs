#region

using System;
using System.Diagnostics;
using Android.App;
using Android.Runtime;
using Xamarin;

#endregion

namespace Xsseract.Droid
{
  // TODO: Cleanup unused files.
  [Application(Icon = "@drawable/icon")]
  public class XsseractApp : Application
  {
    public string DestinationDirBase { get; private set; }
    public XsseractContext XsseractContext { get; }
    public Stopwatch AppStartupTracker { get; set; }

    #region .ctors

    protected XsseractApp(IntPtr javaReference, JniHandleOwnership transfer)
      : base(javaReference, transfer)
    {
      XsseractContext = new XsseractContext(this);
    }

    #endregion

    public override void OnCreate()
    {
      AppStartupTracker = Stopwatch.StartNew();
      if (!String.IsNullOrWhiteSpace(XsseractContext.Settings.InsightsKey))
      {
        Insights.HasPendingCrashReport += (sender, isStartupCrash) =>
                                          {
                                            if (isStartupCrash)
                                            {
                                              Insights.PurgePendingCrashReports().Wait();
                                            }
                                          };

        Insights.Initialize(XsseractContext.Settings.InsightsKey, this);
      }

      base.OnCreate();
    }
  }
}
