#region

using System;
using Android.Content;
using Android.Support.V7.App;

#endregion

namespace Xsseract.Droid
{
  public class ActivityBase: AppCompatActivity
  {
    #region Properties

    protected XsseractApp ApplicationContext
    {
      get { return (XsseractApp)BaseContext.ApplicationContext; }
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

    #endregion

    #region Public methods

    public void LogDebug(string message)
    {
      ApplicationContext.LogDebug(message);
    }

    public void LogDebug(string format, params object[] args)
    {
      ApplicationContext.LogDebug(format, args);
    }

    public void LogError(Exception e)
    {
      ApplicationContext.LogError(e);
    }

    #endregion
  }
}
