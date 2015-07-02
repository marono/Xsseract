using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Android.Content;
using Android.OS;
using Android.Util;
using Newtonsoft.Json;
using Xamarin;
using File = Java.IO.File;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Net;

namespace Xsseract.Droid
{
  // TODO: Detect version changes when initializing (store current version in the shared prefs).
  public class XsseractContext
  {
    private const string tag = "Xsseract.App";

    #region Inner classes
    private class InstallationDetails
    {
      public string InstallationId { get; set; }
    }

    public enum AppContextState
    {
      None,
      Initializing,
      Initialized,
      InitializationErrors
    }
    #endregion Inner classes

    private readonly XsseractApp context;
    public const string InstallationFile = "INSTALLATION";
    private const string preferencesFile = "Prefs";

    private static class PreferencesKeys
    {
      public const string
        IsFirstRun = "IsFirstRun",
        SuccessfullImages = "SuccessfullImages",
        DontRate = "DontRate";
    }

    private string installationId;
    private AppSettings settings;
    private ISharedPreferences preferences;
    private Image image;
    private Tesseractor tesseractor;

    public event EventHandler<EventArgs> FirstTimeInitialize;

    public Func<bool> MeteredConnectionPermissionCallback;

    public AppContextState State { get; private set; }

    public string DeviceId => Android.Provider.Settings.Secure.AndroidId;

    public string DeviceName { get; private set; }

    public string InstallationId
    {
      get
      {
        EnsureAppContextInitialized();
        return installationId;
      }
    }

    public bool IsFirstRun
    {
      get
      {
#if DEBUG
        // ReSharper disable once ConvertPropertyToExpressionBody
        return false;
#else
        return Preferences.GetBoolean(PreferencesKeys.IsFirstRun, true);
#endif
      }
    }

    public ISharedPreferences Preferences
    {
      get
      {
        return preferences = preferences ?? context.GetSharedPreferences(preferencesFile, FileCreationMode.Private);
      }
    }

    public AppSettings Settings => settings ?? (settings = GetAppSettings());
    public File PublicFilesPath => new File(Android.OS.Environment.ExternalStorageDirectory, System.IO.Path.Combine("Xsseract", "files"));
    public File TessDataFilesPath => new File(PublicFilesPath, "tessdata");
    public bool HasImage => image != null;

    public XsseractContext(XsseractApp underlyingContext)
    {
      if (null == underlyingContext)
      {
        throw new ArgumentNullException(nameof(underlyingContext));
      }

      context = underlyingContext;
      DeviceName = GetDeviceName();
    }

    public void Initialize()
    {
      if(State != AppContextState.None)
      {
        return;
      }

      try
      {
        EnsureAppContextInitialized();

        tesseractor = new Tesseractor(PublicFilesPath.AbsolutePath);
        tesseractor.DownloadingDataFiles += (sender, e) => FirstTimeInitialize?.Invoke(this, EventArgs.Empty);
        tesseractor.Initialize(this);
        State = AppContextState.Initialized;
      }
      catch(WebException e)
      {
        State = AppContextState.InitializationErrors;
        // TODO: To resources.
        throw new DataConnectionException("Could not connect to the server for the tess data files download.", e);
      }
      catch(Exception)
      {
        State = AppContextState.InitializationErrors;
        // TODO: Log error.
        throw;
      }
    }

    public async Task<Tesseractor> GetTessInstanceAsync()
    {
      if (null != tesseractor)
      {
        await Task.Yield();
        return tesseractor;
      }

      tesseractor = new Tesseractor(PublicFilesPath.AbsolutePath);
      var result = await tesseractor.InitializeAsync(this);
      if (!result)
      {
        throw new ApplicationException("Error initializing tesseract.");
      }

      return tesseractor;
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
      Log.Debug(tag, message);
    }

    public void LogDebug(string format, params object[] args)
    {
      Log.Debug(tag, format, args);
    }

    public void LogInfo(string message)
    {
      Log.Info(tag, message);
    }

    public void LogInfo(string format, params object[] args)
    {
      Log.Info(tag, format, args);
    }

    public void LogWarn(string message)
    {
      Log.Warn(tag, message);
    }

    public void LogWarn(string format, params object[] args)
    {
      Log.Warn(tag, format, args);
    }

    public void LogError(string message)
    {
      Log.Error(tag, message);
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
      Log.Error(tag, e.ToString());
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
      return image?.Bitmap;
    }

    public string GetImageUri()
    {
      return image?.Path;
    }

    public bool IsDataConnectionAvailable()
    {
      ConnectivityManager cm = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService);
      return cm.ActiveNetworkInfo?.IsConnected == true;
    }

    public void AskForMeteredConnectionPermission()
    {
      ConnectivityManager cm = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService);
      if(!cm.IsActiveNetworkMetered)
      {
        return;
      }

      if(null == MeteredConnectionPermissionCallback)
      {
        // TODO: To resources.
        throw new DataConnectionException("A metered data connection is active. Xsseract needs to download some rather large files, come back when you've hit WiFi.");
      }

      if(!MeteredConnectionPermissionCallback())
      {
        // TODO: To resources.
        throw new DataConnectionException("User denied data usage over a metered connection.");
      }
    }

    public void IncrementSuccessCounter()
    {
      var trans = Preferences.Edit();

      int val = Preferences.GetInt(PreferencesKeys.SuccessfullImages, 0);
      trans.PutInt(PreferencesKeys.SuccessfullImages, val + 1);

      trans.Commit();
    }

    public void SetDontRateFlag()
    {
      var trans = Preferences.Edit();
      trans.PutBoolean(PreferencesKeys.DontRate, true);
      trans.Commit();
    }

    public bool ShouldAskForRating()
    {
      if(Preferences.GetBoolean(PreferencesKeys.DontRate, false) || 0 == Settings.SuccessCountForRatingPrompt)
      {
        return false;
      }

      var count = Preferences.GetInt(PreferencesKeys.SuccessfullImages, 0);
      return 0 != count && 0 == count % Settings.SuccessCountForRatingPrompt;
    }

    private void EnsureAppContextInitialized()
    {
      if (State != AppContextState.None)
      {
        return;
      }

      State = AppContextState.Initializing;
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

    private void CreateInstalationFile(File installation)
    {
#if DEBUG
      // For insights reporting during debug.
      var iid = Guid.Empty;
#else
      var iid = Guid.NewGuid();
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
#if DEBUG
      string fileName = "Settings.DEBUG.json";
#else
      string fileName = "Settings.RELEASE.json";
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