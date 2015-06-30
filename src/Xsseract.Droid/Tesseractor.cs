#region

using System;
using System.Threading.Tasks;
using Android.Graphics;
using Com.Googlecode.Leptonica.Android;
using Com.Googlecode.Tesseract.Android;
using Java.IO;

#endregion

namespace Xsseract.Droid
{
  public class Tesseractor : IDisposable
  {
    #region Fields

    //private static string[] cubeDataFiles =
    //{
    //  ".cube.bigrams",
    //  ".cube.fold",
    //  ".cube.lm",
    //  ".cube.nn",
    //  ".cube.params",
    //  //".cube.size", // This file is not available for Hindi
    //  ".cube.word-freq",
    //  ".tesseract_cube.nn",
    //  ".traineddata"
    //};
    private readonly string baseDir;

    private static string languageCode = "eng";
    private int ocrEngineMode = TessBaseAPI.OemTesseractOnly;
    private TessBaseAPI tesseract;

    #endregion

    public event EventHandler<EventArgs> DownloadingDataFiles;
    public event EventHandler<EventArgs> DataFilesDownloadDone;

    #region .ctors

    public Tesseractor(string baseDir)
    {
      if (String.IsNullOrWhiteSpace(baseDir))
      {
        throw new ArgumentException("The detination dir must be specified.", nameof(baseDir));
      }

      this.baseDir = baseDir;
    }

    #endregion

    #region Public methods

    public void Dispose()
    {
      tesseract.End();
      tesseract.Dispose();
    }

    public async Task<bool> InitializeAsync(AppContext context)
    {
      return await Task.Factory.StartNew(() => Initialize(context));
    }

    public bool Initialize(AppContext context)
    {
      var tessDataInit = new TessDataInitializer(context);
      bool downloading = false;
      tessDataInit.DownloadingDataFiles +=
        (sender, e) =>
        {
          downloading = true;
          DownloadingDataFiles?.Invoke(this, EventArgs.Empty);
        };
      try
      {
        tessDataInit.Initialize();
      }
      finally
      {
        if (downloading)
        {
          DataFilesDownloadDone?.Invoke(this, EventArgs.Empty);
        }
      }

      tesseract = new TessBaseAPI();
      return tesseract.Init(baseDir + File.Separator, languageCode, ocrEngineMode);
    }

    public async Task<string> RecognizeAsync(Bitmap bitmap, Rect target)
    {
      return await Task.Factory.StartNew(
          () =>
          {
            tesseract.SetImage(ReadFile.ReadBitmap(bitmap));
            tesseract.SetRectangle(target);
            return tesseract.UTF8Text;
          });
    }

    #endregion
  }
}
