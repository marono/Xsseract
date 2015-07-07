#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Java.Lang;
using Xamarin;
using Xsseract.Droid.Extensions;
using Xsseract.Droid.Fragments;
using Environment = System.Environment;
using Exception = System.Exception;
using File = Java.IO.File;
using Path = System.IO.Path;
using String = System.String;
using Uri = Android.Net.Uri;

#endregion

namespace Xsseract.Droid
{
  // TODO: android.view.WindowLeaked: Activity md5b71b1bed33a31f85ecaffba202309a1f.ResultActivity has leaked window com.android.internal.policy.impl.PhoneWindow$DecorView{43b5f40 G.E..... R.....ID 0,0-1026,348} that was originally added here
  [Activity(WindowSoftInputMode = SoftInput.AdjustResize, Theme = "@style/AppTheme")]
  public class ResultActivity : ContextualHelpActivity
  {
    #region Fields

    private Bitmap cropped;
    private Rect cropRect;
    private ImageView imgResult;
    private bool pipeResult;
    private bool ratingAskedForThisImage;
    private string result;
    private Func<Task> toDoOnResume;
    private EditText txtEditResult;
    private TextView txtViewResult;

    #endregion

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

    public override bool OnKeyDown([GeneratedEnum] Keycode keyCode, KeyEvent e)
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

    protected override async void OnResume()
    {
      base.OnResume();

      var cropRectString = Intent.GetStringExtra(Constants.CropRect).Split(',');
      cropRect = new Rect(Int32.Parse(cropRectString[0]), Int32.Parse(cropRectString[1]), Int32.Parse(cropRectString[2]), Int32.Parse(cropRectString[3]));

      pipeResult = Intent.GetBooleanExtra(Constants.PipeResult, false);

      txtEditResult.Visibility = ViewStates.Gone;

      if (!pipeResult)
      {
        Toolbar.ShowResultTools(true);
      }
      else
      {
        Toolbar.ShowResultToolsNoShare(true);
      }

      if (null == cropped)
      {
        ITrackHandle handle = null;
        var sw = new Stopwatch();
        try
        {
          DisplayProgress(Resources.GetString(Resource.String.progress_OCR));

          handle = XsseractContext.LogTimedEvent(AppTrackingEvents.ImageParseDuration);
          sw.Start();
          handle.Start();
          await PerformOcrAsync();

          XsseractContext.IncrementSuccessCounter();
          handle.Stop();
          sw.Stop();

          var data = new Dictionary<string, string>
          {
            { AppTrackingEventsDataKey.ImageResW, cropped.Width.ToString() },
            { AppTrackingEventsDataKey.ImageResH, cropped.Height.ToString() },
            { AppTrackingEventsDataKey.ImageDensity, cropped.Density.ToString() },
            { AppTrackingEventsDataKey.OriginalImageResW, cropped.Width.ToString() },
            { AppTrackingEventsDataKey.OriginalImageResH, cropped.Height.ToString() }
          };
          if (0 != sw.ElapsedMilliseconds)
          {
            data.Add(AppTrackingEventsDataKey.ParseSpeedPixelsPerMs, ((cropped.Width * cropped.Height) / sw.ElapsedMilliseconds).ToString());
          }

          XsseractContext.LogEvent(AppTrackingEvents.ImageDetails, data);

          HideProgress();
        }
        catch(Exception e)
        {
          handle.DisposeIfRunning();
          sw.Stop();

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
        Toast t = Toast.MakeText(this, Resource.String.toast_TapToEditResult, ToastLength.Short);
        t.Show();
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

    #region Private Methods

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

    private void CopyToClipboard()
    {
      var service = (ClipboardManager)GetSystemService(ClipboardService);
      var data = ClipData.NewPlainText(Resources.GetString(Resource.String.text_ClipboardLabel),
        $"{Resources.GetString(Resource.String.text_SendToSubject)}: {Environment.NewLine}{result}");

      service.PrimaryClip = data;

      var toast = Toast.MakeText(this, Resource.String.toast_CopiedToClipboard, ToastLength.Long);
      toast.Show();
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
            catch(OutOfMemoryError)
            {
              scale += 1;
            }
          } while(null == res);

          return res;
        });
    }

    private async Task<File> GetImageAttachmentAsync()
    {
      return await Task.Factory.StartNew(
        () =>
        {
          var imagePath = XsseractContext.GetImageUri();
          var fileName = String.Format("{0}-cropped.png", Path.GetFileNameWithoutExtension(imagePath));
          var path = Path.GetDirectoryName(imagePath);

          var file = new File(Path.Combine(path, fileName));
          if (file.Exists())
          {
            file.Delete();
          }

          using(var fs = new FileStream(file.AbsolutePath, FileMode.Create, FileAccess.Write))
          {
            cropped.Compress(Bitmap.CompressFormat.Png, 100, fs);
          }

          file.DeleteOnExit();

          return file;
        });
    }

    private async Task PerformOcrAsync()
    {
      cropped = await GetImageAsync();
      var tess = await XsseractContext.GetTessInstanceAsync();
      result = await tess.RecognizeAsync(XsseractContext.GetBitmap(), cropRect);
    }

    private void ResumeResultView()
    {
      var imm = (InputMethodManager)GetSystemService(InputMethodService);
      imm.HideSoftInputFromWindow(txtEditResult.WindowToken, 0);

      bool hasChanged = 0 != String.Compare(txtEditResult.Text, result);
      txtViewResult.Text = result = txtEditResult.Text;
      txtViewResult.Visibility = ViewStates.Visible;
      txtEditResult.Visibility = ViewStates.Gone;

      if (!pipeResult)
      {
        Toolbar.ShowResultTools(false);
      }
      else
      {
        Toolbar.ShowResultToolsNoShare(false);
      }

      if (hasChanged)
      {
        XsseractContext.LogEvent(AppTrackingEvents.ResultManuallyEdited);
      }
    }

    private async Task ShareResult()
    {
      CopyToClipboard();
      DisplayProgress(Resources.GetString(Resource.String.progress_PrepareShare));
      var attachment = await GetImageAttachmentAsync();

      HideProgress();

      var sendIntent = new Intent(Intent.ActionSendMultiple);
      sendIntent.SetType("image/*");
      sendIntent.PutExtra(Intent.ExtraSubject, Resource.String.text_SendToSubject);
      sendIntent.PutExtra(Intent.ExtraText, $"{Resources.GetString(Resource.String.text_SendToSubject)}: {Environment.NewLine}{result}");
      sendIntent.PutParcelableArrayListExtra(Intent.ExtraStream, new JavaList<IParcelable>
      {
        Uri.FromFile(attachment)
      });
      sendIntent.AddFlags(ActivityFlags.GrantReadUriPermission);
      StartActivity(Intent.CreateChooser(sendIntent, Resources.GetString(Resource.String.text_SendToTitle)));
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
                                XsseractContext.LogEvent(AppTrackingEvents.RateLater);
                                d.Hide();
                                if(null != callback) {
                                  await callback.Invoke();
                                }
                                else {
                                  await Task.Yield();
                                }
                              };
      btnNever.Click += async (sender, e) =>
                              {
                                XsseractContext.SetDontRateFlag();
                                XsseractContext.LogEvent(AppTrackingEvents.RateNever);

                                d.Hide();
                                if(null != callback) {
                                  await callback.Invoke();
                                }
                                else {
                                  await Task.Yield();
                                }
                              };

      d.Show();
    }

    private void Toolbar_Accept(object sender, EventArgs eventArgs)
    {
      AskForRating(async () =>
                         {
                           await Task.Yield();

                           XsseractContext.LogEvent(AppTrackingEvents.Accept);

                           var intent = new Intent();
                           intent.PutExtra(Constants.Accept, true);
                           intent.PutExtra(Constants.Result, result);
                           SetResult(Result.Ok, intent);

                           Finish();
                         });
    }

    private void Toolbar_CopyToClipboard(object sender, EventArgs eventArgs)
    {
      AskForRating(async () =>
                         {
                           await Task.Yield();
                           CopyToClipboard();

                           XsseractContext.LogEvent(AppTrackingEvents.CopyToClipboard);
                         });
    }

    private void Toolbar_Share(object sender, EventArgs eventArgs)
    {
      AskForRating(async () =>
                         {
                           await ShareResult();
                           XsseractContext.LogEvent(AppTrackingEvents.Share);
                         });
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

    #endregion

    #region Inner Classes/Enums

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

    #endregion
  }
}
