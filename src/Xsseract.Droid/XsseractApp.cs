#region

using System;
using System.IO;
using Android.App;
using Android.Runtime;
using Android.Util;
using Java.IO;
using Java.Util.Zip;

#endregion

namespace Xsseract.Droid
{
  [Application(Icon = "@drawable/icon")]
  public class XsseractApp: Application
  {
    #region Properties

    public string DestinationDirBase { get; private set; }

    #endregion

    #region .ctors

    protected XsseractApp(IntPtr javaReference, JniHandleOwnership transfer)
      : base(javaReference, transfer) {}

    #endregion

    #region Public methods

    public void LogDebug(string message)
    {
      Log.Debug(Tag, message);
    }

    public void LogDebug(string format, params object[] args)
    {
      Log.Debug(Tag, format, args);
    }

    public void LogError(Exception e)
    {
      Log.Error(Tag, e.ToString());
    }

    public override void OnCreate()
    {
      base.OnCreate();

      // TODO: Move to launcher ???
      InitializeTessData();
    }

    #endregion

    #region Private Methods

    private void InitializeTessData()
    {
      DestinationDirBase = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
      string tessDataFolder = Path.Combine(DestinationDirBase, "tessdata");

      if(Directory.Exists(tessDataFolder))
      {
        return;
      }

      Directory.CreateDirectory(tessDataFolder);

      using(var file = new ZipInputStream(Resources.Assets.Open("tessData.zip")))
      {
        var buffer = new byte[2048];
        int count;
        ZipEntry entry;
        while((entry = file.NextEntry) != null)
        {
          var fos = new FileStream(Path.Combine(tessDataFolder, entry.Name), FileMode.CreateNew, FileAccess.Write);
          var dest = new BufferedOutputStream(fos, buffer.Length);

          do
          {
            count = file.Read(buffer, 0, buffer.Length);
            if(count > 0)
            {
              dest.Write(buffer, 0, count);
            }
          } while(count > 0);
        }
      }
    }

    #endregion

    public const string Tag = "XsseractApp";
  }
}
