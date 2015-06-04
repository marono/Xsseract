using System;

namespace Xsseract.Droid.Extensions
{
  public static class UriExtensions
  {
    public static string GetFileName(this Uri uri)
    {
      if(null == uri)
      {
        throw new ArgumentNullException("uri");
      }

      return uri.Segments[uri.Segments.Length - 1];
    }
  }
}