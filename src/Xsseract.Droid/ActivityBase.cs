#region

using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
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

    protected XsseractApp ApplicationContext
    {
      get { return (XsseractApp)BaseContext.ApplicationContext; }
    }

    protected ToolbarFragment Toolbar
    {
      get
      {
        if(null != toolbar)
        {
          return toolbar;
        }

        toolbar = SupportFragmentManager.FindFragmentById(Resource.Id.toolbar) as ToolbarFragment;
        return toolbar;
      }
    }

    #endregion

    #region Protected methods
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
            if(null != callback)
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
  }
}
