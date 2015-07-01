using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using com.refractored.fab;

namespace Xsseract.Droid.Fragments
{
  // TODO: Icons not suggestive enough.
  public class ToolbarFragment : Android.Support.V4.App.Fragment
  {
    private FloatingActionButton fabCrop;
    private FloatingActionButton fabCamera;
    private FloatingActionButton fabAccept;
    private FloatingActionButton fabToClipboard;
    private FloatingActionButton fabShare;
    private FloatingActionButton fabHelp;
    private ImageButton btnOptions;

    private View contextMenuHost;
    private List<FloatingActionButton> allFabs;

    public event EventHandler<EventArgs> Camera;
    public event EventHandler<EventArgs> Crop;
    public event EventHandler<EventArgs> CopyToClipboard;
    public event EventHandler<EventArgs> Share;
    public event EventHandler<EventArgs> Accept;
    public event EventHandler<EventArgs> Help;

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
      return inflater.Inflate(Resource.Layout.Toolbar, null);
    }

    public override void OnViewCreated(View view, Bundle savedInstanceState)
    {
      base.OnViewCreated(view, savedInstanceState);

      fabCamera = view.FindViewById<FloatingActionButton>(Resource.Id.fabCamera);
      fabCrop = view.FindViewById<FloatingActionButton>(Resource.Id.fabCrop);
      fabAccept = view.FindViewById<FloatingActionButton>(Resource.Id.fabAccept);
      fabToClipboard = view.FindViewById<FloatingActionButton>(Resource.Id.fabToClipboard);
      fabShare = view.FindViewById<FloatingActionButton>(Resource.Id.fabShare);
      fabHelp = view.FindViewById<FloatingActionButton>(Resource.Id.fabHelp);
      btnOptions = view.FindViewById<ImageButton>(Resource.Id.btnOptions);

      HideFab(fabToClipboard, false);
      HideFab(fabShare, false);

      allFabs = new List<FloatingActionButton> { fabCamera, fabCrop, fabAccept, fabToClipboard, fabShare, fabHelp };

      fabCamera.Click += (sender, e) => OnCamera(EventArgs.Empty);
      fabCrop.Click += (sender, e) => OnCrop(EventArgs.Empty);
      fabAccept.Click += (sender, e) => OnAccept(EventArgs.Empty);
      fabToClipboard.Click += (sender, e) => OnCopyToClipboard(EventArgs.Empty);
      fabShare.Click += (sender, e) => OnShare(EventArgs.Empty);
      fabHelp.Click += (sendner, e) => OnHelp(EventArgs.Empty);
      btnOptions.Click += (sender, e) => Activity.OpenContextMenu(contextMenuHost);

      btnOptions.Visibility = contextMenuHost != null ? ViewStates.Visible : ViewStates.Gone;
    }

    public void EnableOptionsMenu(View view)
    {
      contextMenuHost = view;
      RegisterForContextMenu(contextMenuHost);

      ResumeOptionsButtonVisibility();
    }

    public void ShowCroppingTools(bool animate)
    {
      SetVisibleFabs(fabCrop, fabCamera, fabHelp);
      ResumeOptionsButtonVisibility();
    }

    public void ShowResultTools(bool animate)
    {
      SetVisibleFabs(fabToClipboard, fabShare, fabHelp);
      ResumeOptionsButtonVisibility();
    }

    public void ShowResultToolsNoShare(bool animate)
    {
      SetVisibleFabs(fabAccept);
      ResumeOptionsButtonVisibility();
    }

    public void HideAll()
    {
      SetVisibleFabs();
      btnOptions.Visibility = ViewStates.Gone;
    }

    protected void OnCrop(EventArgs e)
    {
      Crop?.Invoke(this, e);
    }

    protected void OnCamera(EventArgs e)
    {
      Camera?.Invoke(this, e);
    }

    protected void OnShare(EventArgs e)
    {
      Share?.Invoke(this, e);
    }

    protected void OnAccept(EventArgs e)
    {
      Accept?.Invoke(this, e);
    }

    protected void OnCopyToClipboard(EventArgs e)
    {
      CopyToClipboard?.Invoke(this, e);
    }

    protected void OnHelp(EventArgs e)
    {
      Help?.Invoke(this, e);
    }

    private void SetVisibleFabs(params FloatingActionButton[] visibleFabs)
    {
      if (null == visibleFabs)
      {
        visibleFabs = new FloatingActionButton[0];
      }

      foreach (var f in allFabs)
      {
        if (!visibleFabs.Contains(f))
        {
          if (f.Visible)
          {
            HideFab(f, true);
          }
        }
        else
        {
          if (!f.Visible)
          {
            ShowFab(f, true);
          }
        }
      }
    }

    private void ShowFab(FloatingActionButton button, bool animate)
    {
      button.Visibility = ViewStates.Visible;
      button.Show(animate);
    }

    private void HideFab(FloatingActionButton button, bool animate)
    {
      button.Hide(animate);
      button.Visibility = ViewStates.Gone;
    }

    private void ResumeOptionsButtonVisibility()
    {
      btnOptions.Visibility = contextMenuHost != null ? ViewStates.Visible : ViewStates.Gone;
    }
  }
}