using System;
using Android.Graphics;

namespace Xsseract.Droid
{
  public class ParserContext
  {
    public Uri OriginalImageUri { get; set; }
    public Bitmap OriginalImage { get; set; }
    public Bitmap CroppedImage { get; set; }
    public CaptureStates CaptureState { get; set; }
  }
}