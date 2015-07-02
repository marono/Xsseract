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

    private Rect cropRect;
    private bool pipeResult;
    private Func<Task> toDoOnResume;

    private ImageView imgResult;
    private TextView txtViewResult;
    private EditText txtEditResult;
    private Bitmap cropped;
    private string result;
    private bool ratingAskedForThisImage;

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

      var cropRectString = Intent.GetStringExtra(Constants.CropRect).Split(',');
      cropRect = new Rect(Int32.Parse(cropRectString[0]), Int32.Parse(cropRectString[1]), Int32.Parse(cropRectString[2]), Int32.Parse(cropRectString[3]));

      pipeResult = Intent.GetBooleanExtra(Constants.PipeResult, false);

      txtEditResult.Visibility = ViewStates.Gone;

      if (!pipeResult)
        Toolbar.ShowResultTools(true);
      else
        Toolbar.ShowResultToolsNoShare(true);

      if (null == cropped)
      {
        try
        {
          DisplayProgress(Resources.GetString(Resource.String.progress_OCR));
          await PerformOcrAsync();

          XsseractContext.IncrementSuccessCounter();
          HideProgress();
        }
        catch (Exception e)
        {
          HideProgress();

          LogError(e);
          DisplayError(e);
          Finish();

          return;
        }
      }

      imgResult.SetImageBitmap(cropped);
      txtViewResult.Text = result;

      if (null != toDoOnResume)
      {
        await toDoOnResume.Invoke();
        toDoOnResume = null;
      }
      else
      {
        Toast t = Toast.MakeText(this, Resource.String.label_TapToEditResult, ToastLength.Short);
        t.Show();
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
        ResumeResultView();
      }

      return base.OnTouchEvent(e);
    }

    public override bool OnKeyDown([GeneratedEnum]Keycode keyCode, KeyEvent e)
    {
      if (keyCode != Keycode.Back)
      {
        return base.OnKeyDown(keyCode, e);
      }

      if (txtEditResult.Visibility == ViewStates.Visible)
      {
        ResumeResultView();
        return true;
      }
      else
      {
        AskForRating(async () =>
         {
           await Task.Yield();
           SetResult(Result.Canceled);
           Finish();
         });

        return true;
      }
    }

    protected override void OnDestroy()
    {
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

    private async Task PerformOcrAsync()
    {
      cropped = await GetImageAsync();
      var tess = await XsseractContext.GetTessInstanceAsync();
      result = await tess.RecognizeAsync(XsseractContext.GetBitmap(), cropRect);
    }

    private async Task<Bitmap> GetImageAsync()
    {
      return await Task.Factory.StartNew(
        () =>
        {
          var image = XsseractContext.GetBitmap();
          float scale = 1;
          Bitmap res = null;

          do
          {
            try
            {
              Bitmap tmp = Bitmap.CreateBitmap((int)(cropRect.Width() / scale), (int)(cropRect.Height() / scale), Bitmap.Config.Argb8888);
              {
                var canvas = new Canvas(tmp);
                var dstRect = new RectF(0, 0, cropRect.Width(), cropRect.Height());

                canvas.DrawBitmap(image, cropRect, dstRect, null);
              }

              res = tmp;
            }
            catch (Java.Lang.OutOfMemoryError)
            {
              scale += 1;
            }
          } while (null == res);

          return res;
        });
    }

    private async Task<File> GetImageAttachmentAsync()
    {
      return await Task.Factory.StartNew(
        () =>
        {
          var imagePath = XsseractContext.GetImageUri();
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
      var data = ClipData.NewPlainText(Resources.GetString(Resource.String.label_ClipboardLabel), $"{Resources.GetString(Resource.String.text_SendToSubject)}: {System.Environment.NewLine}{result}");

      service.PrimaryClip = data;

      var toast = Toast.MakeText(this, Resource.String.label_CopiedToClipboard, ToastLength.Long);
      toast.Show();
    }

    private void ResumeResultView()
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

    private void AskForRating(Func<Task> callback)
    {
      if (ratingAskedForThisImage || !XsseractContext.ShouldAskForRating())
      {
        callback?.Invoke();
        return;
      }

      ratingAskedForThisImage = true;
      ShowRatingDialog(callback);
      return;
    }

    private void ShowRatingDialog(Func<Task> callback)
    {
      Dialog d = new Dialog(this);
      d.SetTitle(Resource.String.text_RatingPromptTitle);
      d.SetContentView(Resource.Layout.RatingDialog);

      var btnNow = d.FindViewById<Button>(Resource.Id.btnNow);
      var btnLater = d.FindViewById<Button>(Resource.Id.btnLater);
      var btnNever = d.FindViewById<Button>(Resource.Id.btnNever);

      btnNow.Click += (sender, e) =>
      {
        toDoOnResume = callback;
        d.Hide();
        StartRateApplicationActivity();
      };
      btnLater.Click += async (sender, e) =>
      {
        d.Hide();
        await callback?.Invoke();
      };
      btnNever.Click += async (sender, e) =>
      {
        XsseractContext.SetDontRateFlag();

        d.Hide();
        await callback?.Invoke();
      };

      d.Show();
    }

    private async Task ShareResult()
    {
      CopyToClipboard();
      DisplayProgress(Resources.GetString(Resource.String.label_PrepareShare));
      var attachment = await GetImageAttachmentAsync();

      HideProgress();

      var sendIntent = new Intent(Intent.ActionSendMultiple);
      sendIntent.SetType("image/*");
      sendIntent.PutExtra(Intent.ExtraSubject, Resource.String.text_SendToSubject);
      sendIntent.PutExtra(Intent.ExtraText, $"{Resources.GetString(Resource.String.text_SendToSubject)}: {System.Environment.NewLine}{result}");
      sendIntent.PutParcelableArrayListExtra(Intent.ExtraStream, new JavaList<IParcelable>
            {
              Android.Net.Uri.FromFile(attachment)
            });
      sendIntent.AddFlags(ActivityFlags.GrantReadUriPermission);
      StartActivity(Intent.CreateChooser(sendIntent, Resources.GetString(Resource.String.text_SendToTitle)));
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
      AskForRating(async () =>
      {
        await Task.Yield();
        CopyToClipboard();
      });
    }

    private async void Toolbar_Share(object sender, EventArgs eventArgs)
    {
      AskForRating(async () =>
      {
        await ShareResult();
      });
    }

    private void Toolbar_Accept(object sender, EventArgs eventArgs)
    {
      AskForRating(async () =>
      {
        await Task.Yield();

        var intent = new Intent();
        intent.PutExtra(Constants.Accept, true);
        intent.PutExtra(Constants.Result, result);
        SetResult(Result.Ok, intent);

        Finish();
      });
    }
  }
}