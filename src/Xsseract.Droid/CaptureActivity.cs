#region

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
using Android.Views;
using Android.Widget;
using Java.IO;
using Xsseract.Droid.Fragments;
using Xsseract.Droid.Views;
using Environment = Android.OS.Environment;
using Orientation = Android.Media.Orientation;
using Uri = Android.Net.Uri;

#endregion

namespace Xsseract.Droid
{
  // TODO: Add help screen.
  // TODO: Add toasts explaining what to do on each screen.
  // TODO: Memory issue probably due to poor image manipulation.
  // TODO: Hitting back should exit the app somehow (double-tap?).
  [Activity]
  public class CaptureActivity : ActivityBase
  {
    internal enum RequestCode
    {
      Image = 1,
      Parse = 2
    }

    public static class Constants
    {
      public const string PipeResult = "PipeResult",
        Result = "Result";
    }

    #region Fields
    private HighlightView crop;
    private Bitmap image;
    private float rotation;
    private Uri imageUri;
    private Uri prospectiveUri;
    private CropImageView imgPreview;
    private FrameLayout frmCaptureHelp;
    private HelpCapturePagerFragment helpFragment;
    private int imageSamplingRatio;
    private bool pipeResult;
    #endregion

    #region Protected methods
    protected override void OnCreate(Bundle bundle)
    {
      base.OnCreate(bundle);

      SetContentView(Resource.Layout.Capture);
      imgPreview = FindViewById<CropImageView>(Resource.Id.imgPreview);
      frmCaptureHelp = FindViewById<FrameLayout>(Resource.Id.frmCaptureHelp);

      pipeResult = Intent.GetBooleanExtra(Constants.PipeResult, false);

      Toolbar.Crop += Toolbar_Crop;
      Toolbar.Camera += Toolbar_Camera;
      Toolbar.Help += Toolbar_Help;
    }

    protected override async void OnActivityResult(int requestCode, Result resultCode, Intent data)
    {
      base.OnActivityResult(requestCode, resultCode, data);

      switch ((RequestCode)requestCode)
      {
        case RequestCode.Image:
          if (resultCode != Result.Ok)
          {
            prospectiveUri = null;
            return;
          }

          await ProcessAndDisplayImage();
          break;
        case RequestCode.Parse:
          if (resultCode == Result.Canceled)
          {
            if (null == data)
            {
              return;
            }
          }

          bool accept = data.GetBooleanExtra(ResultActivity.Constants.Accept, false);
          if (accept)
          {
            var resultIntent = new Intent();
            resultIntent.PutExtra(Constants.Result, data.GetStringExtra(ResultActivity.Constants.Result));
            SetResult(Result.Ok, resultIntent);
            Finish();
          };
          break;
      }
    }

    protected override async void OnResume()
    {
      base.OnResume();

      Toolbar.ShowCroppingTools(false);
      if (null != imageUri || null != prospectiveUri)
      {
        // Don't take another snap, as one is already present.
        return;
      }

      await AcquireNewImage();
    }
    #endregion

    #region Private Methods
    private void ResetHighlightView()
    {
      imgPreview.ClearHighlightViews();
      crop = null;

      AddHighlightView();
    }

    private void AddHighlightView()
    {
      if (null != crop)
      {
        return;
      }

      crop = new HighlightView(imgPreview);

      int width = image.Width;
      int height = image.Height;

      var imageRect = new Rect(0, 0, width, height);

      // make the default size about 4/5 of the width or height
      int cropWidth = Math.Min(width, height) * 4 / 5;
      int cropHeight = cropWidth;

      int x = (width - cropWidth) / 2;
      int y = (height - cropHeight) / 2;

      var cropRect = new RectF(x, y, x + cropWidth, y + cropHeight);
      crop.Setup(imgPreview.ImageMatrix, imageRect, cropRect, false);

      crop.Focused = true;
      imgPreview.AddHighlightView(crop);
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
          string path = prospectiveUri.Path;
          var options = BitmapUtils.GetBitmapOptionsNoLoad(path);

          options.InSampleSize = imageSamplingRatio = BitmapUtils.CalculateSampleSize(options, Resources.DisplayMetrics.WidthPixels, Resources.DisplayMetrics.HeightPixels);
          options.InJustDecodeBounds = false;
          var original = BitmapFactory.DecodeFile(path, options);

          var exif = new ExifInterface(path); //Since API Level 5
          var exifOrientation = exif.GetAttributeInt(ExifInterface.TagOrientation, 0);

          LogDebug("Image is in '{0}'.", (Orientation)exifOrientation);
          rotation = BitmapUtils.GetRotationAngle((Orientation)exifOrientation);
          var matrix = new Matrix();
          matrix.PostRotate(rotation, original.Width / 2f, original.Height / 2f);

          var rotated = Bitmap.CreateBitmap(original, 0, 0, original.Width, original.Height, matrix, true);
          original.Recycle();
          original.Dispose();

          return rotated;
        });

      imageUri = prospectiveUri;
      image = newImage;

      imgPreview.SetImageBitmapResetBase(image, true);
      ResetHighlightView();
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

      var intent = new Intent(MediaStore.ActionImageCapture);
      var imageFile = new File(mediaPath, String.Format("Xsseract_{0}.jpg", Guid.NewGuid()));
      imageFile.DeleteOnExit();

      prospectiveUri = Uri.FromFile(imageFile);
      intent.PutExtra(MediaStore.ExtraOutput, prospectiveUri);

      StartActivityForResult(intent, (int)RequestCode.Image);
    }

    private async Task AcquireNewImage()
    {
      try
      {
        DisplayProgress(Resources.GetString(Resource.String.progress_ImageAdjust));
        prospectiveUri = null;
        //StartCameraActivity();
        //prospectiveUri = Uri.FromFile(new File("/storage/emulated/0/Pictures/Xsseract/Xsseract_3b8c746e-0822-4f77-8476-cd1d9a3f3958.jpg"));
        prospectiveUri = Uri.FromFile(new File("/storage/sdcard1/DCIM/Camera/IMG_20150609_172808052.jpg"));
        await ProcessAndDisplayImage();

        HideProgress();
      }
      catch (Exception e)
      {
        HideProgress();
        LogError(e);
        DisplayError(e);
      }
    }

    private void Toolbar_Crop(object sender, EventArgs eventArgs)
    {
      var intent = new Intent(this, typeof(ResultActivity));

      intent.PutExtra(ResultActivity.Constants.ImagePath, imageUri.Path);
      var cropRect = new RectF(crop.CropRect);

      var matrix = new Matrix();
      matrix.PostRotate(-1 * rotation, image.Width / 2f, image.Height / 2f);
      var imgRect = new RectF(0, 0, image.Width, image.Height);
      matrix.MapRect(imgRect);
      matrix.PostTranslate(-1 * imgRect.Left, -1 * imgRect.Top);
      matrix.MapRect(cropRect);

      intent.PutExtra(ResultActivity.Constants.CropRect, String.Format("{0},{1},{2},{3}", cropRect.Left * imageSamplingRatio, cropRect.Top * imageSamplingRatio, cropRect.Right * imageSamplingRatio, cropRect.Bottom * imageSamplingRatio));
      intent.PutExtra(ResultActivity.Constants.Rotation, rotation);
      intent.PutExtra(ResultActivity.Constants.PipeResult, pipeResult);
      StartActivityForResult(intent, (int)RequestCode.Parse);
    }

    private async void Toolbar_Camera(object sender, EventArgs eventArgs)
    {
      await AcquireNewImage();
    }

    private void Toolbar_Help(object sender, EventArgs e)
    {
      helpFragment = new HelpCapturePagerFragment(true);
      var trans = SupportFragmentManager.BeginTransaction();
      trans.Add(frmCaptureHelp.Id, helpFragment);
      trans.Commit();

      helpFragment.Dismissed += BtnGotIt_Click;

      frmCaptureHelp.Clickable = true;
      frmCaptureHelp.Visibility = ViewStates.Visible;
    }

    private void BtnGotIt_Click(object sender, EventArgs eventArgs)
    {
      helpFragment.Dismissed -= BtnGotIt_Click;
      frmCaptureHelp.Visibility = ViewStates.Gone;
      frmCaptureHelp.RemoveAllViews();
      helpFragment = null;
    }

    #endregion
  }
}
