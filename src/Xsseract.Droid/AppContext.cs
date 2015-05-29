using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Android.Content;
using Android.Util;
using Newtonsoft.Json;
using Xamarin;
using File = Java.IO.File;

namespace Xsseract.Droid
{
  public class AppContext
  {
    private const string TAG = "Xsseract.App";

    #region Inner classes
    private class InstallationDetails
    {
      public string InstallationId { get; set; }
    }

    public class AppSettings
    {
      public string InsightsKey { get; set; }
    }
    #endregion Inner classes

    private readonly XsseractApp context;
    public static readonly string InstallationFile = "INSTALLATION";

    private string installationId;
    private AppSettings settings;

    public bool Initialized { get; private set; }

    public string DeviceId
    {
      get
      {
        return Android.Provider.Settings.Secure.AndroidId;
      }
    }

    public string InstallationId
    {
      get
      {
        EnsureAppContextInitialized();
        return installationId;
      }
    }

    public bool InitializeError { get; private set; }

    public AppSettings Settings
    {
      get { return settings ?? (settings = GetAppSettings()); }
    }

    public AppContext(XsseractApp underlyingContext)
    {
      if (null == underlyingContext)
      {
        throw new ArgumentNullException("underlyingContext");
      }

      context = underlyingContext;
    }

    public void Initialize()
    {
      EnsureAppContextInitialized();
    }

    public void LogDebug(string message)
    {
      Log.Debug(TAG, message);
    }

    public void LogDebug(string format, params object[] args)
    {
      Log.Debug(TAG, format, args);
    }

    public void LogError(string message)
    {
      Log.Error(TAG, message);
      Insights.Report(new ApplicationException(message), Insights.Severity.Warning);
    }

    public void LogError(Exception e)
    {
      if (null == e)
      {
        LogError("Unspecified exception occured.");
        return;
      }

      // TODO: Disable DEBUG logging by configuration.
      Log.Error(TAG, e.ToString());
      Insights.Report(e, Insights.Severity.Warning);
    }

    private void EnsureAppContextInitialized()
    {
      if (Initialized || InitializeError)
      {
        return;
      }

      try
      {
        using (File f = new File(context.FilesDir, InstallationFile))
        {
          if (!f.Exists())
          {
            CreateInstalationFile(f);
          }
          else
          {
            ReadInstallationFile(f);
          }
        }

        Insights.Identify(installationId, "UserId", String.Empty);
      }
      catch (Exception e)
      {
        InitializeError = true;
        // TODO: Log error.
        //throw new ApplicationException("Could not initialize installation: " + e.Message, e);
      }
    }

    private void CreateInstalationFile(File installation)
    {
      var details = new InstallationDetails
      {
        InstallationId = Guid.NewGuid().ToString()
      };

      var s = new JsonSerializer();
      using (var sw = new StreamWriter(installation.AbsolutePath, false, Encoding.UTF8))
      {
        s.Serialize(sw, details);
      }

      installationId = details.InstallationId;
    }

    private void ReadInstallationFile(File installation)
    {
      var s = new JsonSerializer();
      InstallationDetails details;
      using (var sw = new StreamReader(installation.AbsolutePath, Encoding.UTF8))
      {
        details = (InstallationDetails)s.Deserialize(sw, typeof(InstallationDetails));
      }

      installationId = details.InstallationId;
    }

    private AppSettings GetAppSettings()
    {
      string fileName = "Settings.RELEASE.json";
#if DEBUG
      fileName = "Settings.DEBUG.json";
#endif
      var serializer = new JsonSerializer();
      using (var file = context.Assets.Open(fileName))
      using (var stream = new StreamReader(file))
      {
        return (AppSettings)serializer.Deserialize(stream, typeof(AppSettings));
      }
    }
  }
}