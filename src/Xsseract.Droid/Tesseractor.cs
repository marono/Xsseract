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

        private static string[] CUBE_DATA_FILES =
        {
      ".cube.bigrams",
      ".cube.fold",
      ".cube.lm",
      ".cube.nn",
      ".cube.params",
            //".cube.size", // This file is not available for Hindi
            ".cube.word-freq",
      ".tesseract_cube.nn",
      ".traineddata"
    };
        private readonly string destinationDirBase;

        private static string languageCode = "eng";
        private int ocrEngineMode = TessBaseAPI.OemTesseractOnly;
        private TessBaseAPI tesseract;

        #endregion

        #region .ctors

        public Tesseractor(string destinationDirBase)
        {
            if (String.IsNullOrWhiteSpace(destinationDirBase))
            {
                throw new ArgumentException("The detination dir must be specified.", "destinationDirBase");
            }

            this.destinationDirBase = destinationDirBase;
        }

        #endregion

        #region Public methods

        public void Dispose()
        {
            tesseract.End();
            tesseract.Dispose();
        }

        public async Task<bool> InitializeAsync()
        {
            return await Task.Factory.StartNew(() =>
                                               {
                                                   tesseract = new TessBaseAPI();
                                                   return tesseract.Init(destinationDirBase + File.Separator, languageCode, ocrEngineMode);
                                               });
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
