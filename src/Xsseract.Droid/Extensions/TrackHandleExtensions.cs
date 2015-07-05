#region

using Xamarin;

#endregion

namespace Xsseract.Droid.Extensions
{
  public static class TrackHandleExtensions
  {
    public static void DisposeIfRunning(this ITrackHandle handle)
    {
      if (null == handle)
      {
        return;
      }

      try
      {
        handle.Dispose();
      }
      catch {}
    }
  }
}
