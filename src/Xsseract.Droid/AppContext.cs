using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Android.Content;
using Android.OS;
using Android.Util;
using Java.Util.Prefs;
using Newtonsoft.Json;
using Xamarin;
using File = Java.IO.File;
using System.Threading.Tasks;
using Android.Graphics;

namespace Xsseract.Droid
{
  // TODO: Detect version changes when initializing (store current version in the shared prefs).
  public class AppContext
  {
    private const string TAG = "Xsseract.App";

    #region Inner classes
    private class InstallationDetails
    {
      public string InstallationId { get; set; }
    }

    #endregion Inner classes

    private readonly XsseractApp context;
    public const string InstallationFile = "INSTALLATION";
    private const string PreferencesFile = "Prefs";

    private static class PreferencesKeys
    {
      public const string
        IsFirstRun = "IsFirstRun";
    }

    private string installationId;
    private AppSettings settings;
    private ISharedPreferences preferences;
    private Image image;

    public bool Initialized { get; private set; }

    public string DeviceId
    {
      get
      {
        return Android.Provider.Settings.Secure.AndroidId;
      }
    }

    public string DeviceName { get; private set; }

    public string InstallationId
    {
      get
      {
        EnsureAppContextInitialized();
        return installationId;
      }
    }

    public bool InitializeError { get; private set; }

    public bool IsFirstRun
    {
      get
      {
#if DEBUG
        return false;
#endif
        return Preferences.GetBoolean(PreferencesKeys.IsFirstRun, true);
      }
    }

    public ISharedPreferences Preferences
    {
      get
      {
        return preferences = preferences ?? context.GetSharedPreferences(PreferencesFile, FileCreationMode.Private);
      }
    }

    public AppSettings Settings
    {
      get { return settings ?? (settings = GetAppSettings()); }
    }

    public File PublicFilesPath
    {
      get { return new File(Android.OS.Environment.ExternalStorageDirectory, System.IO.Path.Combine("Xsseract", "files")); }
    }

    public File TessDataFilesPath
    {
      get
      {
        return new File(PublicFilesPath, "tessdata");
      }
    }

    public bool HasImage
    {
      get { return image != null; }
    }

    public AppContext(XsseractApp underlyingContext)
    {
      if (null == underlyingContext)
      {
        throw new ArgumentNullException("underlyingContext");
      }

      context = underlyingContext;
      DeviceName = GetDeviceName();
    }

    public void Initialize()
    {
      EnsureAppContextInitialized();
    }

    public void LogEvent(AppTrackingEvents @event)
    {
      Insights.Track(@event.ToString());
    }

    public void LogEvent(AppTrackingEvents @event, Dictionary<string, string> extraData)
    {
      Insights.Track(@event.ToString(), extraData);
    }

    public ITrackHandle LogTimedEvent(AppTrackingEvents @event)
    {
      return Insights.TrackTime(@event.ToString());
    }

    public void MarkHelpScreenCompleted()
    {
      var trans = Preferences.Edit();
      trans.PutBoolean(PreferencesKeys.IsFirstRun, false);
      trans.Commit();
    }

    public void LogDebug(string message)
    {
      Log.Debug(TAG, message);
    }

    public void LogDebug(string format, params object[] args)
    {
      Log.Debug(TAG, format, args);
    }

    public void LogInfo(string message)
    {
      Log.Info(TAG, message);
    }

    public void LogInfo(string format, params object[] args)
    {
      Log.Info(TAG, format, args);
    }

    public void LogWarn(string message)
    {
      Log.Warn(TAG, message);
    }

    public void LogWarn(string format, params object[] args)
    {
      Log.Warn(TAG, format, args);
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

    public async Task<Bitmap> LoadImageAsync(string path, float rotation)
    {
      await DisposeImageAsync();
      var tmpImg = await Task.Factory.StartNew(
        () =>
        {
          var img = new Image(path, rotation);
          return img;

        });

      this.image = tmpImg;
      LogInfo("Image sampling is {0}", image.SampleSize);
      return image.Bitmap;
    }

    public Bitmap LoadImage(string path, float rotation)
    {
      image?.Dispose();

      image = new Image(path, rotation);
      LogInfo("Image sampling is {0}", image.SampleSize);
      return image.Bitmap;
    }

    public async Task DisposeImageAsync()
    {
      if (null == image)
      {
        await Task.Yield();
        return;
      }

      await Task.Factory.StartNew(
        () =>
        {
          image.Dispose();
          image = null;
        });
    }

    public Bitmap GetBitmap()
    {
      if (null == image)
      {
        return null;
      }

      return image.Bitmap;
    }

    public string GetImageUri()
    {
      if (null == image)
      {
        return null;
      }

      return image.Path;
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
      var iid = Guid.NewGuid();
#if DEBUG
      // For insights reporting during debug.
      iid = Guid.Empty;
#endif
      var details = new InstallationDetails
      {
        InstallationId = iid.ToString()
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

    /** Returns the consumer friendly device name */
    public static String GetDeviceName()
    {
      String manufacturer = Build.Manufacturer;
      String model = Build.Model;
      if (model.StartsWith(manufacturer))
      {
        return Capitalize(model);
      }
      if (0 == String.Compare(manufacturer, "HTC", StringComparison.OrdinalIgnoreCase))
      {
        // make sure "HTC" is fully capitalized.
        return "HTC " + model;
      }
      return Capitalize(manufacturer) + " " + model;
    }

    private static String Capitalize(String str)
    {
      if (String.IsNullOrWhiteSpace(str))
      {
        return str;
      }

      var capitalizeNext = true;
      String phrase = "";
      foreach (char c in str)
      {
        if (capitalizeNext && Char.IsLetter(c))
        {
          phrase += Char.ToUpper(c);
          capitalizeNext = false;
          continue;
        }

        if (Char.IsWhiteSpace(c))
        {
          capitalizeNext = true;
        }

        phrase += c;
      }
      return phrase;
    }
  }
}