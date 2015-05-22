#region

using Android.Graphics;

#endregion

namespace Xsseract.Droid.Controls
{
  public class RotateBitmap
  {
    #region Properties

    public Bitmap Bitmap { get; set; }

    public int Height
    {
      get
      {
        if(IsOrientationChanged)
        {
          return Bitmap.Width;
        }
        else
        {
          return Bitmap.Height;
        }
      }
    }
    public bool IsOrientationChanged
    {
      get { return (Rotation / 90) % 2 != 0; }
    }
    public int Rotation { get; set; }

    public int Width
    {
      get
      {
        if(IsOrientationChanged)
        {
          return Bitmap.Height;
        }
        else
        {
          return Bitmap.Width;
        }
      }
    }

    #endregion

    #region .ctors

    public RotateBitmap(Bitmap bitmap)
    {
      Bitmap = bitmap;
    }

    public RotateBitmap(Bitmap bitmap, int rotation)
    {
      Bitmap = bitmap;
      Rotation = rotation % 360;
    }

    #endregion

    #region Public methods

    public Matrix GetRotateMatrix()
    {
      // By default this is an identity matrix.
      var matrix = new Matrix();

      if(Rotation != 0)
      {
        // We want to do the rotation at origin, but since the bounding
        // rectangle will be changed after rotation, so the delta values
        // are based on old & new width/height respectively.
        int cx = Bitmap.Width / 2;
        int cy = Bitmap.Height / 2;
        matrix.PreTranslate(-cx, -cy);
        matrix.PostRotate(Rotation);
        matrix.PostTranslate(Width / 2, Height / 2);
      }

      return matrix;
    }

    #endregion

    public const string TAG = "RotateBitmap";

    // TOOD: Recyle
  }
}
