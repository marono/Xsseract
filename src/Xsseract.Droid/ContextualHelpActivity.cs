#region

using System;
using System.Collections.Generic;
using Android.Views;
using Android.Widget;
using Xsseract.Droid.Fragments;

#endregion

namespace Xsseract.Droid
{
  public abstract class ContextualHelpActivity : ActivityBase
  {
    #region Fields

    private FrameLayout frmCaptureHelp;
    private DismissableFragment helpFragment;

    #endregion

    protected override void OnResume()
    {
      base.OnResume();

      Toolbar.Help -= Toolbar_Help;
      Toolbar.Help += Toolbar_Help;
      frmCaptureHelp = FindViewById<FrameLayout>(Resource.Id.frmCaptureHelp);
    }

    protected abstract DismissableFragment GetHelpFragment();

    #region Private Methods

    private void BtnGotIt_Click(object sender, EventArgs eventArgs)
    {
      helpFragment.Dismissed -= BtnGotIt_Click;
      frmCaptureHelp.Visibility = ViewStates.Gone;
      frmCaptureHelp.RemoveAllViews();
      helpFragment = null;
    }

    private void Toolbar_Help(object sender, EventArgs e)
    {
      helpFragment = GetHelpFragment();
      var trans = SupportFragmentManager.BeginTransaction();
      trans.Add(frmCaptureHelp.Id, helpFragment);
      trans.Commit();

      helpFragment.Dismissed += BtnGotIt_Click;

      frmCaptureHelp.Clickable = true;
      frmCaptureHelp.Visibility = ViewStates.Visible;

      XsseractContext.LogEvent(AppTrackingEvents.Help, new Dictionary<string, string>
      {
        { AppTrackingEventsDataKey.HelpPage, this.GetType().Name }
      });
    }

    #endregion
  }
}
