using System;
using Android.Views;
using com.refractored.fab;

namespace Xsseract.Droid.Extensions
{
  public static class FloatingActionButtonExtensions
  {
    public static bool WillShow(this FloatingActionButton button, ViewStates requestedState)
    {
      if(null == button)
      {
        throw new ArgumentNullException("button");
      }

      return !button.Visible && requestedState == ViewStates.Visible;
    }

    public static bool WillHide(this FloatingActionButton button, ViewStates requestedState)
    {
      if (null == button)
      {
        throw new ArgumentNullException("button");
      }

      return button.Visible && requestedState != ViewStates.Visible;
    }
  }
}