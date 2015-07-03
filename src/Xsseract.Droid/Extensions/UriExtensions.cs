using System;

namespace Xsseract.Droid.Extensions
{
  public static class UriExtensions
  {
    public static string GetFileName(this Uri uri)
    {
      if (null == uri)
      {
        throw new ArgumentNullException(nameof(uri));
      }

      return uri.Segments[uri.Segments.Length - 1];
    }

    public static bool IsAsset(this Uri uri)
    {
      if (null == uri)
      {
        throw new ArgumentNullException(nameof(uri));
      }

      return
        0 == String.CompareOrdinal(uri.Scheme.ToLower(), "file") &&
        true == uri.AbsolutePath.ToLower().StartsWith("/android_asset");
    }
  }
}