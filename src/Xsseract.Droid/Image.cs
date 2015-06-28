using System;
using Android.Graphics;

namespace Xsseract.Droid
{
  internal class Image : IDisposable
  {
    public string Path { get; private set; }
    public float Rotation { get; private set; }
    public Bitmap Bitmap { get; private set; }
    public int SampleSize { get; private set; }

    public Image(string path, float rotation)
    {
      if (String.IsNullOrWhiteSpace(path))
      {
        throw new ArgumentException("The path must be specified.");
      }

      Path = path;
      Rotation = rotation;
      LoadImage();
    }

    private void LoadImage()
    {
      Bitmap loaded;
      BitmapFactory.Options opts = new BitmapFactory.Options();
      opts.InSampleSize = 1;

      while (!TryLoadImageAndTransform(opts, Rotation, out loaded) && opts.InSampleSize < 32)
      {
        opts.InSampleSize++;
      }

      if (null == loaded)
      {
        // TODO: To Resources.
        throw new ApplicationException("Could not allocate memory to load the image, even with downsampling. The application will not work properly on this device.");
      }

      Bitmap = loaded;
    }

    private bool TryLoadImageAndTransform(BitmapFactory.Options options, float rotation, out Bitmap image)
    {
      Bitmap loaded;
      Bitmap raw;

      if (!TryLoadImage(options, out raw))
      {
        image = null;
        return false;
      }

      float scale = (float)300 / raw.Density;
      var matrix = new Matrix();
      if (scale < 1)
      {
        matrix.PostScale(scale, scale, raw.Width / 2f, raw.Height / 2f);
      }
      matrix.PostRotate(rotation, raw.Width / 2f, raw.Height / 2f);

      RectF outline = new RectF(0, 0, raw.Width, raw.Height);
      matrix.MapRect(outline);

      Bitmap transformed = null;
      try
      {
        transformed = Bitmap.CreateBitmap(raw, 0, 0, raw.Width, raw.Height, matrix, true);
        raw.Recycle();
        raw.Dispose();

        loaded = transformed;
      }
      catch (Java.Lang.OutOfMemoryError)
      {
        if (null != transformed)
        {
          transformed.Recycle();
          transformed.Dispose();
        }

        raw.Recycle();
        raw.Dispose();

        image = null;
        return false;
      }

      SampleSize = options.InSampleSize;
      image = loaded;
      return loaded != null;
    }

    private bool TryLoadImage(BitmapFactory.Options options, out Bitmap image)
    {
      Bitmap loaded = null;
      try
      {
        loaded = BitmapFactory.DecodeFile(Path, options);
        image = loaded;
        return true;
      }
      catch (Java.Lang.OutOfMemoryError)
      {
        if (null != loaded)
        {
          try
          {
            loaded.Recycle();
            loaded.Dispose();
          }
          // ReSharper disable once EmptyGeneralCatchClause
          // Try and recycle whatever still possible to recycle.
          catch { }
        }

        image = null;
        return false;
      }
    }

    public void Dispose()
    {
      if (null != Bitmap)
      {
        if (!Bitmap.IsRecycled)
        {
          Bitmap.Recycle();
          Bitmap.Dispose();
        }
        Bitmap = null;
      }
    }
  }
}