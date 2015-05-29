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
using Android.Views.InputMethods;
using Android.Widget;
using com.refractored.fab;
using Java.IO;
using Xsseract.Droid.Controls;
using Environment = Android.OS.Environment;
using Orientation = Android.Media.Orientation;
using Uri = Android.Net.Uri;

#endregion

namespace Xsseract.Droid
{
  [Activity]
  public class CaptureActivity : ActivityBase
  {
    #region Fields

    private FloatingActionButton fabCrop;
    private FloatingActionButton fabCamera;
    private FloatingActionButton fabAccept;
    private RelativeLayout containerResult;
    private TextView txtViewResult;
    private EditText txtEditResult;
    private HighlightView crop;
    private Bitmap image;
    private Bitmap cropped;
    private string result;
    private Uri imageUri;
    private CropImageView imgPreview;
    private ImageView imgResult;
    private Uri nextImageUri;
    private Tesseractor tesseractor;

    #endregion

    #region Protected methods

    protected override async void OnActivityResult(int requestCode, Result resultCode, Intent data)
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

    protected override void OnCreate(Bundle bundle)
    {
      base.OnCreate(bundle);

      SetContentView(Resource.Layout.Capture);
      fabCamera = FindViewById<FloatingActionButton>(Resource.Id.fabCamera);
      fabCrop = FindViewById<FloatingActionButton>(Resource.Id.fabCrop);
      fabAccept = FindViewById<FloatingActionButton>(Resource.Id.fabAccept);
      imgPreview = FindViewById<CropImageView>(Resource.Id.imgPreview);
      txtViewResult = FindViewById<TextView>(Resource.Id.txtViewResult);
      containerResult = FindViewById<RelativeLayout>(Resource.Id.containerResult);
      imgResult = FindViewById<ImageView>(Resource.Id.imgResult);
      txtEditResult = FindViewById<EditText>(Resource.Id.txtEditResult);

      fabCamera.Click += fabCamera_Click;
      fabCrop.Click += fabCrop_Click;
      txtViewResult.Click += txtViewResult_Click;
      imgResult.Focusable = true;
    }

    protected override void OnDestroy()
    {
      if (null != tesseractor)
      {
        tesseractor.Dispose();
        tesseractor = null;
      }

      base.OnDestroy();
    }

    protected override async void OnResume()
    {
      base.OnResume();

      if (null == tesseractor)
      {
        tesseractor = new Tesseractor(ApplicationContext.DestinationDirBase);
        await tesseractor.InitializeAsync();
      }

      if (null != nextImageUri)
      {
        // Don't take another snap, as one is already present.
        return;
      }

      switch (ApplicationContext.ParserContext.CaptureState)
      {
        case CaptureStates.None:
          //StartCameraActivity();
          nextImageUri = Uri.FromFile(new File("/storage/emulated/0/Pictures/Xsseract/Xsseract_3b8c746e-0822-4f77-8476-cd1d9a3f3958.jpg"));
          //nextImageUri = Android.Net.Uri.FromFile(new File("/storage/emulated/0/Pictures/Xsseract/Xsseract_82f8d342-e04c-4ccb-8f4c-c2d63b004d0c.jpg"));
          SetStateCrop();
          await ProcessAndDisplayImage();
          break;
        case CaptureStates.Capture:
          //StartCameraActivity();
          nextImageUri = Uri.FromFile(new File("/storage/emulated/0/Pictures/Xsseract/Xsseract_3b8c746e-0822-4f77-8476-cd1d9a3f3958.jpg"));
          //nextImageUri = Android.Net.Uri.FromFile(new File("/storage/emulated/0/Pictures/Xsseract/Xsseract_82f8d342-e04c-4ccb-8f4c-c2d63b004d0c.jpg"));
          SetStateCrop();
          await ProcessAndDisplayImage();
          break;
        case CaptureStates.Crop:
          nextImageUri = Uri.FromFile(new File("/storage/emulated/0/Pictures/Xsseract/Xsseract_3b8c746e-0822-4f77-8476-cd1d9a3f3958.jpg"));
          SetStateCrop();
          await ProcessAndDisplayImage();
          break;
        case CaptureStates.Parsed:
          SetStateParsed();
          break;
      }
    }

    public override bool OnTouchEvent(MotionEvent e)
    {
      if (ApplicationContext.ParserContext.CaptureState != CaptureStates.Parsed || txtEditResult.Visibility != ViewStates.Visible)
      {
        return base.OnTouchEvent(e);
      }

      var coords = new int[2];
      txtEditResult.GetLocationOnScreen(coords);
      var rect = new Rect(coords[0], coords[1], coords[0] + txtEditResult.Width, coords[1] + txtEditResult.Height);

      if (!rect.Contains((int)e.RawX, (int)e.RawY))
      {
        InputMethodManager imm = (InputMethodManager)GetSystemService(Context.InputMethodService);
        imm.HideSoftInputFromWindow(txtEditResult.WindowToken, 0);

        txtViewResult.Text = txtEditResult.Text;
        txtViewResult.Visibility = ViewStates.Visible;
        txtEditResult.Visibility = ViewStates.Gone;
      }

      return base.OnTouchEvent(e);
    }
    #endregion

    #region Private Methods

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

      //imgPreview.ClearHighlightViews();
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

    private Bitmap GetSelectedRegion()
    {
      var r = crop.CropRect;

      int width = r.Width();
      int height = r.Height();

      Bitmap croppedImage = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);
      {
        var canvas = new Canvas(croppedImage);
        var dstRect = new Rect(0, 0, width, height);
        canvas.DrawBitmap(image, r, dstRect, null);
      }

      return croppedImage;
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
          string path = nextImageUri.Path;
          var original = BitmapFactory.DecodeFile(path);

          var width = original.Width;
          var height = original.Height;

          var exif = new ExifInterface(path); //Since API Level 5
          var exifOrientation = exif.GetAttributeInt(ExifInterface.TagOrientation, 0);

          var matrix = new Matrix();
          LogDebug("Image is in '{0}'.", (Orientation)exifOrientation);
          switch ((Orientation)exifOrientation)
          {
            case Orientation.Normal:
            case Orientation.Rotate90:
              matrix = new Matrix();
              matrix.PostRotate(90);
              break;
            case Orientation.Rotate270:
              matrix = new Matrix();
              matrix.PostRotate(90);
              break;
            case Orientation.Rotate180:
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
      SetStateCrop();
    }

    private async Task ProcessAndDisplayResult()
    {
      if (null == cropped)
      {
        cropped = GetSelectedRegion();
      }
      if (null == result)
      {
        result = await tesseractor.RecognizeAsync(cropped);
      }

      imgResult.SetImageBitmap(cropped);
      txtViewResult.Text = result;
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

      nextImageUri = Uri.FromFile(imageFile);
      intent.PutExtra(MediaStore.ExtraOutput, nextImageUri);

      StartActivityForResult(intent, 1);
    }

    private void SetStateCapture()
    {
      fabAccept.Hide(false);
      fabAccept.Visibility = ViewStates.Gone;
      fabCrop.Hide(false);
      fabCrop.Visibility = ViewStates.Gone;
      fabCamera.Visibility = ViewStates.Visible;
      fabCamera.Show(true);

      ApplicationContext.ParserContext.CaptureState = CaptureStates.Capture;
    }

    private void SetStateCrop()
    {
      switch (ApplicationContext.ParserContext.CaptureState)
      {
        case CaptureStates.None:
          fabAccept.Hide(false);
          fabAccept.Visibility = ViewStates.Gone;
          fabCrop.Visibility = ViewStates.Visible;
          fabCrop.Show(true);
          fabCamera.Visibility = ViewStates.Visible;
          fabCamera.Show(true);
          break;
        case CaptureStates.Crop:
          // Resuming activity
          fabAccept.Hide(false);
          fabAccept.Visibility = ViewStates.Gone;
          fabCrop.Visibility = ViewStates.Visible;
          fabCrop.Show(true);
          fabCamera.Visibility = ViewStates.Visible;
          fabCamera.Show(true);

          containerResult.Visibility = ViewStates.Gone;
          imgPreview.Visibility = ViewStates.Visible;
          break;
        case CaptureStates.Capture:
          fabAccept.Hide(false);
          fabAccept.Visibility = ViewStates.Gone;
          fabCrop.Visibility = ViewStates.Visible;
          fabCrop.Show(true);
          break;
        case CaptureStates.Parsed:
          fabAccept.Hide(true);
          fabAccept.Visibility = ViewStates.Gone;
          fabCrop.Visibility = ViewStates.Visible;
          fabCrop.Show(true);
          containerResult.Visibility = ViewStates.Gone;

          imgResult.Visibility = ViewStates.Gone;
          imgPreview.Visibility = ViewStates.Visible;
          break;
      }

      ApplicationContext.ParserContext.CaptureState = CaptureStates.Crop;

    }

    private async void SetStateParsed()
    {
      switch (ApplicationContext.ParserContext.CaptureState)
      {
        case CaptureStates.Crop:
          fabAccept.Visibility = ViewStates.Visible;
          fabAccept.Show(true);

          fabCrop.Hide(true);
          fabCrop.Visibility = ViewStates.Gone;
          containerResult.Visibility = ViewStates.Visible;

          imgResult.Visibility = ViewStates.Visible;
          imgPreview.Visibility = ViewStates.Gone;
          break;
        case CaptureStates.Parsed:
          // Resume activity.
          fabAccept.Visibility = ViewStates.Visible;
          fabAccept.Show(true);

          fabCamera.Visibility = ViewStates.Visible;
          fabCamera.Show(true);

          fabCrop.Hide(true);
          fabCrop.Visibility = ViewStates.Gone;
          containerResult.Visibility = ViewStates.Visible;

          await ProcessAndDisplayResult();
          imgResult.Visibility = ViewStates.Visible;
          imgPreview.Visibility = ViewStates.Gone;
          break;
      }

      ApplicationContext.ParserContext.CaptureState = CaptureStates.Parsed;

    }

    private async void fabCrop_Click(object sender, EventArgs eventArgs)
    {
      try
      {
        await ProcessAndDisplayResult();
        SetStateParsed();
      }
      catch (Exception e)
      {
        LogError(e);
        DisplayError(e);
      }
    }

    private async void fabCamera_Click(object sender, EventArgs e)
    {
      nextImageUri = null;
      //StartCameraActivity();
      nextImageUri = Uri.FromFile(new File("/storage/emulated/0/Pictures/Xsseract/Xsseract_3b8c746e-0822-4f77-8476-cd1d9a3f3958.jpg"));
      //nextImageUri = Android.Net.Uri.FromFile(new File("/storage/emulated/0/Pictures/Xsseract/Xsseract_82f8d342-e04c-4ccb-8f4c-c2d63b004d0c.jpg"));
      await ProcessAndDisplayImage();
      SetStateCrop();
    }

    private void txtViewResult_Click(object sender, EventArgs eventArgs)
    {
      //containerResult.LayoutParameters.Height = containerResult.Height;
      //txtEditResult.SetHeight(txtViewResult.Height);
      //containerResult.Invalidate();

      txtEditResult.Text = txtViewResult.Text;
      txtViewResult.Visibility = ViewStates.Gone;
      txtEditResult.Visibility = ViewStates.Visible;

      txtEditResult.RequestFocus();
      InputMethodManager inputMethodManager = BaseContext.GetSystemService(Context.InputMethodService) as InputMethodManager;
      inputMethodManager.ShowSoftInput(txtEditResult, ShowFlags.Forced);
      inputMethodManager.ToggleSoftInput(ShowFlags.Forced, HideSoftInputFlags.ImplicitOnly);
    }
    #endregion
  }
}
