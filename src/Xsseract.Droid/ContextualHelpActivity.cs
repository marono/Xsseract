using System;
using Android.Views;
using Android.Widget;
using Xsseract.Droid.Fragments;

namespace Xsseract.Droid
{
  public abstract class ContextualHelpActivity : ActivityBase
  {
    private FrameLayout frmCaptureHelp;
    private DismissableFragment helpFragment;

    protected override void OnResume()
    {
      base.OnResume();

      Toolbar.Help -= Toolbar_Help;
      Toolbar.Help += Toolbar_Help;
      frmCaptureHelp = FindViewById<FrameLayout>(Resource.Id.frmCaptureHelp);
    }

    protected abstract DismissableFragment GetHelpFragment();

    private void Toolbar_Help(object sender, EventArgs e)
    {
      helpFragment = GetHelpFragment();
      var trans = SupportFragmentManager.BeginTransaction();
      trans.Add(frmCaptureHelp.Id, helpFragment);
      trans.Commit();

      helpFragment.Dismissed += BtnGotIt_Click;

      frmCaptureHelp.Clickable = true;
      frmCaptureHelp.Visibility = ViewStates.Visible;
    }

    private void BtnGotIt_Click(object sender, EventArgs eventArgs)
    {
      helpFragment.Dismissed -= BtnGotIt_Click;
      frmCaptureHelp.Visibility = ViewStates.Gone;
      frmCaptureHelp.RemoveAllViews();
      helpFragment = null;
    }
  }
}