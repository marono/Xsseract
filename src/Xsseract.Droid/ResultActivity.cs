using System;
using System.IO;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Text.Method;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Xsseract.Droid.Fragments;
using ClipboardManager = Android.Content.ClipboardManager;
using File = Java.IO.File;

namespace Xsseract.Droid
{
  // TODO: android.view.WindowLeaked: Activity md5b71b1bed33a31f85ecaffba202309a1f.ResultActivity has leaked window com.android.internal.policy.impl.PhoneWindow$DecorView{43b5f40 G.E..... R.....ID 0,0-1026,348} that was originally added here
  [Activity(WindowSoftInputMode = SoftInput.AdjustResize)]
  public class ResultActivity : ContextualHelpActivity
  {
    public static class Constants
    {
      public const string
        ImagePath = "ImagePath",
        CropRect = "CropRect",
        PipeResult = "PipeResult",
        Rotation = "Rotation",
        Accept = "Accept",
        Result = "Result";
    }

    private Tesseractor tesseractor;
    private string imagePath;
    private Rect cropRect;
    private float rotation;
    private bool pipeResult;

    private ImageView imgResult;
    private TextView txtViewResult;
    private EditText txtEditResult;
    private Bitmap cropped;
    private string result;

    protected override void OnCreate(Bundle bundle)
    {
      base.OnCreate(bundle);

      SetContentView(Resource.Layout.Result);

      txtViewResult = FindViewById<TextView>(Resource.Id.txtViewResult);
      imgResult = FindViewById<ImageView>(Resource.Id.imgResult);
      txtEditResult = FindViewById<EditText>(Resource.Id.txtEditResult);

      txtViewResult.MovementMethod = new ScrollingMovementMethod();

      txtViewResult.LongClick += txtViewResult_Click;
      imgResult.Focusable = true;

      Toolbar.CopyToClipboard += Toolbar_CopyToClipboard;
      Toolbar.Share += Toolbar_Share;
      Toolbar.Accept += Toolbar_Accept;
    }

    protected async override void OnResume()
    {
      base.OnResume();

      imagePath = Intent.GetStringExtra(Constants.ImagePath);
      var cropRectString = Intent.GetStringExtra(Constants.CropRect).Split(',');
      cropRect = new Rect(Int32.Parse(cropRectString[0]), Int32.Parse(cropRectString[1]), Int32.Parse(cropRectString[2]), Int32.Parse(cropRectString[3]));

      rotation = Intent.GetFloatExtra(Constants.Rotation, 0);
      pipeResult = Intent.GetBooleanExtra(Constants.PipeResult, false);

      txtEditResult.Visibility = ViewStates.Gone;

      if (!pipeResult)
        Toolbar.ShowResultTools(true);
      else
        Toolbar.ShowResultToolsNoShare(true);

      try
      {
        DisplayProgress(Resources.GetString(Resource.String.progress_OCR));
        await InitializeTesseractAsync();

        if (null == cropped)
        {
          await PerformOcrAsync();
        }

        imgResult.SetImageBitmap(cropped);
        txtViewResult.Text = result;
        HideProgress();

        Toast t = Toast.MakeText(this, Resource.String.label_TapToEditResult, ToastLength.Short);
        t.Show();
      }
      catch (Exception e)
      {
        HideProgress();

        LogError(e);
        DisplayError(e);
      }
    }

    public override bool OnTouchEvent(MotionEvent e)
    {
      if (txtEditResult.Visibility != ViewStates.Visible)
      {
        return base.OnTouchEvent(e);
      }

      var coords = new int[2];
      txtEditResult.GetLocationOnScreen(coords);
      var rect = new Rect(coords[0], coords[1], coords[0] + txtEditResult.Width, coords[1] + txtEditResult.Height);

      if (!rect.Contains((int)e.GetX(), (int)e.GetY()))
      {
        var imm = (InputMethodManager)GetSystemService(InputMethodService);
        imm.HideSoftInputFromWindow(txtEditResult.WindowToken, 0);

        txtViewResult.Text = result = txtEditResult.Text;
        txtViewResult.Visibility = ViewStates.Visible;
        txtEditResult.Visibility = ViewStates.Gone;

        if (!pipeResult)
          Toolbar.ShowResultTools(false);
        else
          Toolbar.ShowResultToolsNoShare(false);
      }

      return base.OnTouchEvent(e);
    }

    protected override void OnDestroy()
    {
      // TODO: Move tesseractor to the App so that we don't need to reinitialize it each time, try initializing it on start-up(?).
      if (null != tesseractor)
      {
        tesseractor.Dispose();
        tesseractor = null;
      }

      if (null != cropped)
      {
        imgResult.SetImageBitmap(null);
        cropped.Recycle();
        cropped.Dispose();
        cropped = null;

        result = null;
      }

      base.OnDestroy();
    }

    protected override DismissableFragment GetHelpFragment()
    {
      return new HelpResultsPagerFragment(true);
    }

    private async Task InitializeTesseractAsync()
    {
      if (null == tesseractor)
      {
        tesseractor = new Tesseractor(ApplicationContext.AppContext.PublicFilesPath.AbsolutePath);
        await tesseractor.InitializeAsync();
      }
    }

    private async Task PerformOcrAsync()
    {
      cropped = await GetImageAsync();
      result = await tesseractor.RecognizeAsync(cropped);
    }

    private async Task<Bitmap> GetImageAsync()
    {
      return await Task.Factory.StartNew(
        () =>
        {
          Bitmap image = BitmapFactory.DecodeFile(imagePath);

          try
          {
            // Rotate the crop rect so that we get the same as we've selected on the previous screent.
            // The rotation needs to happen in the original image space, and not in the cropped one's.
            var transformMatrix = new Matrix();
            transformMatrix.SetRotate(rotation, image.Width / 2f, image.Height / 2f);
            var imgRect = new RectF(0, 0, image.Width, image.Height);
            transformMatrix.MapRect(imgRect);

            transformMatrix.PostTranslate(-1 * imgRect.Left, -1 * imgRect.Top);

            var rf = new RectF(cropRect);
            transformMatrix.MapRect(rf);

            Bitmap croppedImage = Bitmap.CreateBitmap((int)rf.Width(), (int)rf.Height(), Bitmap.Config.Argb8888);
            {
              var canvas = new Canvas(croppedImage);
              var dstRect = new RectF(0, 0, cropRect.Width(), cropRect.Height());
              var dstTrans = new Matrix();
              dstTrans.PostTranslate((croppedImage.Width - cropRect.Width()) / 2f, (croppedImage.Height - cropRect.Height()) / 2f);
              dstTrans.MapRect(dstRect);

              canvas.Rotate(rotation, croppedImage.Width / 2f, croppedImage.Height / 2f);
              canvas.DrawBitmap(image, cropRect, dstRect, null);
            }

            return croppedImage;
          }
          finally
          {
            image.Recycle();
            image.Dispose();
          }
        });
    }

    private async Task<File> GetImageAttachmentAsync()
    {
      return await Task.Factory.StartNew(
        () =>
        {
          var fileName = String.Format("{0}-cropped.png", System.IO.Path.GetFileNameWithoutExtension(imagePath));
          var path = System.IO.Path.GetDirectoryName(imagePath);

          var file = new File(System.IO.Path.Combine(path, fileName));
          if (file.Exists())
          {
            file.Delete();
          }

          using (var fs = new FileStream(file.AbsolutePath, FileMode.Create, FileAccess.Write))
          {
            cropped.Compress(Bitmap.CompressFormat.Png, 100, fs);
          }

          file.DeleteOnExit();

          return file;
        });
    }

    private void CopyToClipboard()
    {
      var service = (ClipboardManager)GetSystemService(ClipboardService);
      var data = ClipData.NewPlainText(Resources.GetString(Resource.String.label_ClipboardLabel), result);

      service.PrimaryClip = data;

      var toast = Toast.MakeText(this, Resource.String.label_CopiedToClipboard, ToastLength.Long);
      toast.Show();
    }

    private void txtViewResult_Click(object sender, EventArgs eventArgs)
    {
      txtEditResult.Text = txtViewResult.Text;
      txtViewResult.Visibility = ViewStates.Gone;
      txtEditResult.Visibility = ViewStates.Visible;

      var inputMethodManager = (InputMethodManager)BaseContext.GetSystemService(InputMethodService);
      inputMethodManager.ShowSoftInput(txtEditResult, ShowFlags.Forced);
      inputMethodManager.ToggleSoftInput(ShowFlags.Forced, HideSoftInputFlags.ImplicitOnly);

      Toolbar.HideAll();
    }

    private void Toolbar_CopyToClipboard(object sender, EventArgs eventArgs)
    {
      CopyToClipboard();
    }

    private void Toolbar_Share(object sender, EventArgs eventArgs)
    {
      DisplayAlert(Resources.GetString(Resource.String.message_ShareInstructions),
        async () =>
        {
          try
          {
            CopyToClipboard();
            DisplayProgress(Resources.GetString(Resource.String.label_PrepareShare));
            var attachment = await GetImageAttachmentAsync();

            HideProgress();

            var sendIntent = new Intent(Intent.ActionSendMultiple);
            sendIntent.SetType("image/*");
            sendIntent.PutExtra(Intent.ExtraSubject, result);
            sendIntent.PutExtra(Intent.ExtraText, result);
            sendIntent.PutParcelableArrayListExtra(Intent.ExtraStream, new JavaList<IParcelable>
            {
              Android.Net.Uri.FromFile(attachment)
            });
            sendIntent.AddFlags(ActivityFlags.GrantReadUriPermission);
            StartActivity(Intent.CreateChooser(sendIntent, Resources.GetString(Resource.String.sendTo)));
          }
          catch (Exception)
          {
            HideProgress();

          }
        });
    }

    private void Toolbar_Accept(object sender, EventArgs eventArgs)
    {
      var intent = new Intent();
      intent.PutExtra(Constants.Accept, true);
      intent.PutExtra(Constants.Result, result);
      SetResult(Result.Ok, intent);

      Finish();
    }
  }
}