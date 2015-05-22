using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Support.V7.App;

namespace Xsseract.Droid
{
  public class ActivityBase : AppCompatActivity
  {
    protected XsseractApp ApplicationContext
    {
      get { return (XsseractApp)this.BaseContext.ApplicationContext; }
    }

    protected void DisplayAlert(string message)
    {
      new Android.Support.V7.App.AlertDialog.Builder(this)
        .SetTitle(Resource.String.AlertTitle)
        .SetMessage(message)
        .SetPositiveButton(Android.Resource.String.Ok, (IDialogInterfaceOnClickListener)null)
        .Show();
    }

    protected void DisplayError(Exception e)
    {
      string message = String.Format("{0}{1}{2}", e.Message, System.Environment.NewLine, e.StackTrace[0]);

      new Android.Support.V7.App.AlertDialog.Builder(this)
        .SetTitle(Resource.String.ErrorTitle)
        .SetMessage(message)
        .SetPositiveButton(Android.Resource.String.Ok, (IDialogInterfaceOnClickListener)null)
        .Show();
    }

    protected void DisplayError(string error)
    {
      new Android.Support.V7.App.AlertDialog.Builder(this)
        .SetTitle(Resource.String.ErrorTitle)
        .SetMessage(error)
        .SetPositiveButton(Android.Resource.String.Ok, (IDialogInterfaceOnClickListener)null)
        .Show();
    }

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
  }
}