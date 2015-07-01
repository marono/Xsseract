#region

using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Xsseract.Droid.Fragments;
using AlertDialog = Android.App.AlertDialog;
using Environment = System.Environment;

#endregion

namespace Xsseract.Droid
{
  public class ActivityBase : Android.Support.V4.App.FragmentActivity
  {
    private ProgressDialog progressDialog;
    private ToolbarFragment toolbar;

    #region Properties

    protected XsseractApp ApplicationContext => (XsseractApp)BaseContext.ApplicationContext;

    protected ToolbarFragment Toolbar
    {
      get
      {
        if (null != toolbar)
        {
          return toolbar;
        }

        toolbar = SupportFragmentManager.FindFragmentById(Resource.Id.toolbar) as ToolbarFragment;
        return toolbar;
      }
    }

    #endregion

    #region Protected methods
    public override void SetContentView(int layoutResID)
    {
      base.SetContentView(layoutResID);

      var host = FindViewById(Resource.Id.optionsMenuHost);
      if (null != host)
      {
        Toolbar.EnableOptionsMenu(host);
      }
    }

    public override bool OnContextItemSelected(IMenuItem item)
    {
      switch (item.ItemId)
      {
        case Resource.Id.tutorial:
          var intent = new Intent(this, typeof(HelpActivity));
          intent.PutExtra(HelpActivity.Constants.FinishOnClose, true);
          StartActivity(intent);
          break;
        case Resource.Id.rateUs:
          StartRateApplicationActivity();
          break;
        case Resource.Id.feedback:
          Intent feedbackIntent = new Intent(Intent.ActionSendto, Android.Net.Uri.FromParts("mailto", ApplicationContext.AppContext.Settings.FeedbackEmailAddress, null));
          feedbackIntent.PutExtra(Intent.ExtraSubject, Resources.GetString(Resource.String.text_FeedbackEmailSubject));

          StartActivity(Intent.CreateChooser(feedbackIntent, Resources.GetString(Resource.String.text_FeedbackChooserTitle)));
          break;
        case Resource.Id.about:
          StartActivity(typeof(AboutActivity));
          break;
      }
      return base.OnContextItemSelected(item);
    }

    public override void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
    {
      MenuInflater.Inflate(Resource.Layout.OptionsMenu, menu);
    }

    public override bool OnKeyDown([GeneratedEnum]Keycode keyCode, KeyEvent e)
    {
      if (keyCode == Keycode.Menu)
      {
        var host = FindViewById(Resource.Id.optionsMenuHost);
        if (null != host)
        {
          OpenContextMenu(host);
          return true;
        }
      }
      return base.OnKeyDown(keyCode, e);
    }

    protected void DisplayAlert(string message, Action callback)
    {
      new AlertDialog.Builder(this)
        .SetTitle(Resource.String.AlertTitle)
        .SetMessage(message)
        .SetPositiveButton(Android.Resource.String.Ok, delegate { callback?.Invoke(); })
        .Show();
    }

    protected void DisplayAlert(string message, Func<Task> callback)
    {
      new AlertDialog.Builder(this)
        .SetTitle(Resource.String.AlertTitle)
        .SetMessage(message)
        .SetPositiveButton(Android.Resource.String.Ok,
          async (sender, e) =>
          {
            if (null != callback)
            {
              await callback();
            }
          })
        .Show();
    }

    protected void DisplayError(Exception e)
    {
      DisplayError(e, null);
    }

    protected void DisplayError(Exception exception, Action dismissDelegate)
    {
      string message = $"{exception.Message}{Environment.NewLine}{exception.StackTrace[0]}";

      new AlertDialog.Builder(this)
        .SetTitle(Resource.String.ErrorTitle)
        .SetMessage(message)
        .SetPositiveButton(Android.Resource.String.Ok, (sender, e) => dismissDelegate?.Invoke())
        .Show();
    }

    protected void DisplayError(string error)
    {
      new AlertDialog.Builder(this)
        .SetTitle(Resource.String.ErrorTitle)
        .SetMessage(error)
        .SetPositiveButton(Android.Resource.String.Ok, (IDialogInterfaceOnClickListener)null)
        .Show();
    }

    protected void DisplayProgress(string message)
    {
      if (null != progressDialog)
      {
        throw new InvalidOperationException("A background operation is already in progress.");
      }

      progressDialog = new ProgressDialog(this);
      progressDialog.SetCancelable(false);
      progressDialog.SetMessage(message);
      progressDialog.Show();
    }

    protected void HideProgress()
    {
      progressDialog.Hide();
      progressDialog = null;
    }
    #endregion

    #region Public methods

    public void LogDebug(string message)
    {
      ApplicationContext.AppContext.LogDebug(message);
    }

    public void LogDebug(string format, params object[] args)
    {
      ApplicationContext.AppContext.LogDebug(format, args);
    }

    public void LogError(Exception e)
    {
      ApplicationContext.AppContext.LogError(e);
    }

    #endregion

    private void StartRateApplicationActivity()
    {
      try
      {
        StartActivity(new Intent(Intent.ActionView, Android.Net.Uri.Parse("market://details?id=" + PackageName)));
      }
      catch (ActivityNotFoundException e)
      {
        StartActivity(new Intent(Intent.ActionView, Android.Net.Uri.Parse("https://play.google.com/store/apps/details?id=" + PackageName)));
      }
    }
  }
}
