using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Provider;
using Android.Util;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Com.Googlecode.Tesseract.Android;
using com.refractored.monodroidtoolkit;
using Java.IO;
using Xsseract.Droid.Controls;
using Environment = Android.OS.Environment;

namespace Xsseract.Droid
{
  [Activity]
  public class CaptureActivity : ActivityBase
  {
    private Tesseractor tesseractor;
    private Button btnTest;
    private Button btnRead;
    //private CameraHandler cameraHandler;
    //private SurfaceView srfViewPreview;
    private CropImageView imgPreview;
    private Bitmap image;
    private Android.Net.Uri imageUri;
    private Android.Net.Uri nextImageUri;
    private HighlightView crop;

    

    protected override void OnCreate(Bundle bundle)
    {
      base.OnCreate(bundle);

      tesseractor = new Tesseractor(ApplicationContext.DestinationDirBase);

      SetContentView(Resource.Layout.Capture);
      btnTest = FindViewById<Button>(Resource.Id.btnTest);
      btnRead = FindViewById<Button>(Resource.Id.btnRead);
      //viewFinder = FindViewById<ViewGroup>(Resource.Id.viewFinder);
      //srfViewPreview = FindViewById<SurfaceView>(Resource.Id.srfViewPreview);
      //cameraHandler = new CameraHandler(srfViewPreview.Holder, BaseContext);
      imgPreview = FindViewById<CropImageView>(Resource.Id.imgPreview);

      btnTest.Click += btnTest_Click;
      btnRead.Click += btnRead_Click;
    }

    protected async override void OnResume()
    {
      base.OnResume();

      await tesseractor.InitializeAsync();

      if (null != nextImageUri)
      {
        // Don't take another snap, as one is already present.
        return;
      }

      //StartCameraActivity();
      nextImageUri = Android.Net.Uri.FromFile(new File("/storage/emulated/0/Pictures/Xsseract/Xsseract_3b8c746e-0822-4f77-8476-cd1d9a3f3958.jpg"));
      //nextImageUri = Android.Net.Uri.FromFile(new File("/storage/emulated/0/Pictures/Xsseract/Xsseract_82f8d342-e04c-4ccb-8f4c-c2d63b004d0c.jpg"));
      await ProcessAndDisplayImage();
    }

    protected async override void OnActivityResult(int requestCode, Result resultCode, Intent data)
    {
      base.OnActivityResult(requestCode, resultCode, data);

      switch (requestCode)
      {
        case 1:
          if (resultCode != Result.Ok)
          {
            nextImageUri = null;
            return;
          }

          await ProcessAndDisplayImage();
          break;
      }
    }

    private async Task ProcessAndDisplayImage()
    {
      imgPreview.SetImageBitmap(null);
      if (null != image)
      {
        image.Recycle();
        image.Dispose();
        image = null;
      }

      var newImage = await Task.Factory.StartNew(
        () =>
        {
          string path = nextImageUri.Path;
          var original = BitmapFactory.DecodeFile(path);

          var width = original.Width;
          var height = original.Height;

          ExifInterface exif = new ExifInterface(path); //Since API Level 5
          var exifOrientation = exif.GetAttributeInt(ExifInterface.TagOrientation, 0);

          Matrix matrix = new Matrix();
          LogDebug("Image is in '{0}'.", (Android.Media.Orientation)exifOrientation);
          switch ((Android.Media.Orientation)exifOrientation)
          {
            case Android.Media.Orientation.Normal:
            case Android.Media.Orientation.Rotate90:
              matrix = new Matrix();
              matrix.PostRotate(90);
              break;
            case Android.Media.Orientation.Rotate270:
              matrix = new Matrix();
              matrix.PostRotate(90);
              break;
            case Android.Media.Orientation.Rotate180:
              matrix = new Matrix();
              matrix.PostRotate(180);
              break;
          }

          if (width > 4096 || height > 4096)
          {
            float scaleFactorW = (float)width / 4096;
            float scaleFactorH = (float)height / 4096;
            float scaleFactor = scaleFactorW > scaleFactorH ? scaleFactorW : scaleFactorH;

            matrix.PostScale(1 / scaleFactor, 1 / scaleFactor);
          }

          var rotated = Bitmap.CreateBitmap(original, 0, 0, original.Width, original.Height, matrix, true);
          original.Recycle();
          original.Dispose();

          return rotated;
        });

      imageUri = nextImageUri;
      image = newImage;

      imgPreview.SetImageBitmapResetBase(image, true);
      AddHighlightView();
    }

    protected override void OnDestroy()
    {
      base.OnDestroy();
      tesseractor.Dispose();
    }

    private async void btnTest_Click(object sender, System.EventArgs e)
    {
      nextImageUri = null;
      //StartCameraActivity();
      nextImageUri = Android.Net.Uri.FromFile(new File("/storage/emulated/0/Pictures/Xsseract/Xsseract_3b8c746e-0822-4f77-8476-cd1d9a3f3958.jpg"));
      //nextImageUri = Android.Net.Uri.FromFile(new File("/storage/emulated/0/Pictures/Xsseract/Xsseract_82f8d342-e04c-4ccb-8f4c-c2d63b004d0c.jpg"));
      await ProcessAndDisplayImage();
    }

    private async void btnRead_Click(object sender, EventArgs eventArgs)
    {
      //var cropped = GetSelectedRegion();

      LogDebug("Image translation is {0}, {1}", imgPreview.TranslateX, imgPreview.TranslateY);
      LogDebug("Image scale is {0}", imgPreview.Scale);
      LogDebug("Crop rect is {0},{1}/{2},{3}", crop.CropRect.Left, crop.CropRect.Top, crop.CropRect.Width(), crop.CropRect.Height());
      LogDebug("Draw rect is at {0},{1}/{2},{3}", crop.DrawRect.Left, crop.DrawRect.Top, crop.DrawRect.Width(), crop.DrawRect.Height());

      float cropX = crop.CropRect.Left;
      float cropY = crop.CropRect.Top;
      float cropW = crop.CropRect.Width();
      float cropH = crop.CropRect.Height();
      LogDebug("Cropping at {0},{1}/{2},{3}", cropX, cropY, cropW, cropH);
      LogDebug("Image size is {0},{1}", image.Width, image.Height);

      var cropped = Bitmap.CreateBitmap(image, (int)cropX, (int)cropY, (int)cropW, (int)cropH, imgPreview.Matrix, true);
      imgPreview.SetImageBitmapResetBase(cropped, true);
      imgPreview.ClearHighlightViews();

      try
      {
        var result = await tesseractor.RecognizeAsync(cropped);
        DisplayAlert(result);
      }
      catch (Exception e)
      {
        LogError(e);
        DisplayError(e);
      }
    }

    private void StartCameraActivity()
    {
      if (!IsThereACameraAppAvailable())
      {
        // TODO: To resources.
        DisplayError("There's no camera app to take snaps. Get one from the store ...");
        return;
      }

      var mediaPath = CreateDirectoryForPictures();

      Intent intent = new Intent(MediaStore.ActionImageCapture);
      File imageFile = new File(mediaPath, String.Format("Xsseract_{0}.jpg", Guid.NewGuid()));
      imageFile.DeleteOnExit();

      nextImageUri = Android.Net.Uri.FromFile(imageFile);
      intent.PutExtra(MediaStore.ExtraOutput, nextImageUri);

      StartActivityForResult(intent, 1);
    }

    private File CreateDirectoryForPictures()
    {
      var mediaPath = new File(Environment.GetExternalStoragePublicDirectory(Environment.DirectoryPictures), Resources.GetString(Resource.String.ApplicationName));
      if (!mediaPath.Exists())
      {
        mediaPath.Mkdirs();
      }

      return mediaPath;
    }

    private bool IsThereACameraAppAvailable()
    {
      var intent = new Intent(MediaStore.ActionImageCapture);
      IList<ResolveInfo> availableActivities = PackageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
      return availableActivities != null && availableActivities.Count > 0;
    }

    private void AddHighlightView()
    {
      crop = new HighlightView(imgPreview);

      int width = image.Width;
      int height = image.Height;

      Rect imageRect = new Rect(0, 0, width, height);

      // make the default size about 4/5 of the width or height
      int cropWidth = Math.Min(width, height) * 4 / 5;
      int cropHeight = cropWidth;

      int x = (width - cropWidth) / 2;
      int y = (height - cropHeight) / 2;

      RectF cropRect = new RectF(x, y, x + cropWidth, y + cropHeight);
      crop.Setup(imgPreview.ImageMatrix, imageRect, cropRect, false);

      //imgPreview.ClearHighlightViews();
      crop.Focused = true;
      imgPreview.AddHighlightView(crop);
    }

    private Bitmap GetSelectedRegion()
    {
      var r = crop.CropRect;

      int width = r.Width();
      int height = r.Height();

      Bitmap croppedImage = Bitmap.CreateBitmap(width, height, Bitmap.Config.Rgb565);
      {
        Canvas canvas = new Canvas(croppedImage);
        Rect dstRect = new Rect(0, 0, width, height);
        canvas.DrawBitmap(image, r, dstRect, null);
      }

      return croppedImage;

      // If the output is required to a specific size then scale or fill
      //if (outputX != 0 && outputY != 0)
      //{
      //  if (scale)
      //  {
      //    // Scale the image to the required dimensions
      //    Bitmap old = croppedImage;
      //    croppedImage = Util.transform(new Matrix(),
      //                                  croppedImage, outputX, outputY, scaleUp);
      //    if (old != croppedImage)
      //    {
      //      old.Recycle();
      //    }
      //  }
      //  else
      //  {
      //    // Don't scale the image crop it to the size requested.
      //    // Create an new image with the cropped image in the center and
      //    // the extra space filled.              
      //    Bitmap b = Bitmap.CreateBitmap(outputX, outputY,
      //                                   Bitmap.Config.Rgb565);
      //    Canvas canvas = new Canvas(b);

      //    Rect srcRect = crop.CropRect;
      //    Rect dstRect = new Rect(0, 0, outputX, outputY);

      //    int dx = (srcRect.Width() - dstRect.Width()) / 2;
      //    int dy = (srcRect.Height() - dstRect.Height()) / 2;

      //    // If the srcRect is too big, use the center part of it.
      //    srcRect.Inset(Math.Max(0, dx), Math.Max(0, dy));

      //    // If the dstRect is too big, use the center part of it.
      //    dstRect.Inset(Math.Max(0, -dx), Math.Max(0, -dy));

      //    // Draw the cropped bitmap in the center
      //    canvas.DrawBitmap(image, srcRect, dstRect, null);

      //    // Set the cropped bitmap as the new bitmap
      //    croppedImage.Recycle();
      //    croppedImage = b;
      //  }
      //}

      // Return the cropped image directly or save it to the specified URI.
      Bundle myExtras = Intent.Extras;

      //if (myExtras != null &&
      //    (myExtras.GetParcelable("data") != null || myExtras.GetBoolean("return-data")))
      //{
      //  Bundle extras = new Bundle();
      //  extras.PutParcelable("data", croppedImage);
      //  SetResult(Result.Ok,
      //            (new Intent()).SetAction("inline-data").PutExtras(extras));
      //  Finish();
      //}
      //else
      //{
      //  Bitmap b = croppedImage;
      //  BackgroundJob.StartBackgroundJob(this, null, "Saving image", () => saveOutput(b), mHandler);
      //}
    }
  }
}

