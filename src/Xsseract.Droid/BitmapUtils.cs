#region

using Android.Graphics;
using Android.Media;
using Java.Lang;

#endregion

namespace Xsseract.Droid
{
  public static class BitmapUtils
  {
    public static void GetImageSize(string imagePath, out int width, out int height)
    {
      var options = GetBitmapOptionsNoLoad(imagePath);
      width = options.OutWidth;
      height = options.OutHeight;
    }

    public static int CalculateSampleSize(BitmapFactory.Options options, int reqWidth, int reqHeight)
    {
      // Raw height and width of image
      int height = options.OutHeight;
      int width = options.OutWidth;

      double ratioW = (double)width / reqWidth;
      double ratioH = (double)height / reqHeight;

      var sampleSize = (int)Math.Round(Math.Min(ratioW, ratioH));
      return sampleSize != 0 ? sampleSize : 1;
    }

    public static BitmapFactory.Options GetBitmapOptionsNoLoad(string imagePath)
    {
      var options = new BitmapFactory.Options
      {
        InJustDecodeBounds = true
      };

      //Returns null, sizes are in the options variable
      BitmapFactory.DecodeFile(imagePath, options);
      return options;
    }

    public static float GetRotationAngle(Orientation orientation)
    {
      switch(orientation)
      {
        case Orientation.Normal:
          return 0;
        case Orientation.Rotate90:
          return 90;
        case Orientation.Rotate270:
          return 90;
        case Orientation.Rotate180:
          return 180;
      }

      return 0;
    }

    public static Matrix GetOrientationMatrix(Orientation orientation, float px, float py)
    {
      var matrix = new Matrix();
      switch(orientation)
      {
        case Orientation.Normal:
          break;
        case Orientation.Rotate90:
          matrix.PostRotate(90, px, py);
          break;
        case Orientation.Rotate270:
          matrix.PostRotate(90, px, py);
          break;
        case Orientation.Rotate180:
          matrix.PostRotate(180, px, py);
          break;
      }

      return matrix;
    }
  }
}
