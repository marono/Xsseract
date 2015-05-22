using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Com.Googlecode.Tesseract.Android;
using System.Threading.Tasks;
using Java.IO;
using Com.Googlecode.Leptonica.Android;
using Android.Graphics;

namespace Xsseract.Droid
{
  public class Tesseractor : IDisposable
  {
    private TessBaseAPI tesseract;
    private int ocrEngineMode = TessBaseAPI.OemTesseractOnly;
    private string destinationDirBase;

    private static string[] CUBE_DATA_FILES = {
      ".cube.bigrams",
      ".cube.fold", 
      ".cube.lm", 
      ".cube.nn", 
      ".cube.params", 
      //".cube.size", // This file is not available for Hindi
      ".cube.word-freq", 
      ".tesseract_cube.nn", 
      ".traineddata" };

    private static string languageCode = "eng";

    public Tesseractor(string destinationDirBase)
    {
      if (String.IsNullOrWhiteSpace(destinationDirBase))
      {
        throw new ArgumentException("The detination dir must be specified.", "destinationDirBase");
      }

      this.destinationDirBase = destinationDirBase;
    }

    public async Task<bool> InitializeAsync()
    {
      return await Task.Factory.StartNew(() =>
      {
        tesseract = new TessBaseAPI();
        return tesseract.Init(destinationDirBase + File.Separator, languageCode, ocrEngineMode);
      });
    }

    public async Task<string> RecognizeAsync(Bitmap bitmap)
    {
      return await Task.Factory.StartNew(() =>
      {
        tesseract.SetImage(ReadFile.ReadBitmap(bitmap));
        return tesseract.UTF8Text;
      });
    }

    public void Dispose()
    {
      tesseract.Dispose();
    }
  }
}