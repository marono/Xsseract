#region

using System;
using Android.Graphics;
using Android.Graphics.Drawables;

#endregion

namespace Xsseract.Droid.Extensions
{
  public static class DrawableExtensions
  {
    public static void SetBounds(this Drawable drawable, Rect boundingRect)
    {
      if (null == drawable)
      {
        throw new ArgumentNullException("drawable");
      }

      drawable.SetBounds(boundingRect.Left, boundingRect.Top, boundingRect.Right, boundingRect.Bottom);
    }
  }
}
