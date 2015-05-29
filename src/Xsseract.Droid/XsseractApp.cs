#region

using System;
using System.IO;
using Android.App;
using Android.Runtime;
using Android.Util;
//using Crashlytics.Bindings.Droid;
using Java.IO;
using Java.Util.Zip;
using Xamarin;

#endregion

namespace Xsseract.Droid
{
  [Application(Icon = "@drawable/icon")]
  public class XsseractApp : Application
  {
    private readonly ParserContext parserContext = new ParserContext();
    private readonly AppContext appContext;

    #region Properties

    public string DestinationDirBase { get; private set; }
    public ParserContext ParserContext
    {
      get { return parserContext; }
    }
    public AppContext AppContext
    {
      get { return appContext; }
    }

    #endregion

    #region .ctors

    protected XsseractApp(IntPtr javaReference, JniHandleOwnership transfer)
      : base(javaReference, transfer)
    {
      appContext = new AppContext(this);
    }

    #endregion

    #region Public methods
    public override void OnCreate()
    {
      base.OnCreate();
      //We won't be using Crashlytics for now, we've switched to Xamarin Insights.
      //CrashReporter.StartWithMonoHook(this, true);

      if(!String.IsNullOrWhiteSpace(AppContext.Settings.InsightsKey))
      {
        Insights.HasPendingCrashReport += (sender, isStartupCrash) =>
        {
          if (isStartupCrash)
          {
            Insights.PurgePendingCrashReports().Wait();
          }
        };

        Insights.Initialize(AppContext.Settings.InsightsKey, this);
      }
    }
    #endregion

    #region Private Methods

    public void InitializeTesseract()
    {
      DestinationDirBase = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
      string tessDataFolder = Path.Combine(DestinationDirBase, "tessdata");

      if (Directory.Exists(tessDataFolder))
      {
        return;
      }

      Directory.CreateDirectory(tessDataFolder);

      using (var file = new ZipInputStream(Resources.Assets.Open("tessData.zip")))
      {
        var buffer = new byte[2048];
        int count;
        ZipEntry entry;
        while ((entry = file.NextEntry) != null)
        {
          var fos = new FileStream(Path.Combine(tessDataFolder, entry.Name), FileMode.CreateNew, FileAccess.Write);
          var dest = new BufferedOutputStream(fos, buffer.Length);

          do
          {
            count = file.Read(buffer, 0, buffer.Length);
            if (count > 0)
            {
              dest.Write(buffer, 0, count);
            }
          } while (count > 0);
        }
      }
    }

    #endregion
  }
}
