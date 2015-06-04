#region

using System;
using Android.App;
using Android.Content;
using Xsseract.Droid.Fragments;
using AlertDialog = Android.Support.V7.App.AlertDialog;
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

        toolbar = FragmentManager.FindFragmentById<ToolbarFragment>(Resource.Id.toolbar);
        return toolbar;
      }
    }

    #endregion

    #region Protected methods

    protected void DisplayAlert(string message)
    {
      new AlertDialog.Builder(this)
        .SetTitle(Resource.String.AlertTitle)
        .SetMessage(message)
        .SetPositiveButton(Android.Resource.String.Ok, (IDialogInterfaceOnClickListener)null)
        .Show();
    }

    protected void DisplayError(Exception e)
    {
      string message = String.Format("{0}{1}{2}", e.Message, Environment.NewLine, e.StackTrace[0]);

      new AlertDialog.Builder(this)
        .SetTitle(Resource.String.ErrorTitle)
        .SetMessage(message)
        .SetPositiveButton(Android.Resource.String.Ok, (IDialogInterfaceOnClickListener)null)
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
