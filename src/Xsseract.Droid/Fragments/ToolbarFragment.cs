using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using com.refractored.fab;
using Xsseract.Droid.Extensions;

namespace Xsseract.Droid.Fragments
{
  // TODO: Icons not suggestive enough.
  public class ToolbarFragment : Fragment
  {
    private FloatingActionButton fabCrop;
    private FloatingActionButton fabCamera;
    private FloatingActionButton fabAccept;
    private FloatingActionButton fabToClipboard;
    private FloatingActionButton fabShare;
    private LinearLayout acceptActionsContainer;

    private List<FloatingActionButton> allFabs;
    private List<FloatingActionButton> acceptActions;

    public event EventHandler<EventArgs> Camera;
    public event EventHandler<EventArgs> Crop;
    public event EventHandler<EventArgs> CopyToClipboard;
    public event EventHandler<EventArgs> Share;

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
      acceptActionsContainer = view.FindViewById<LinearLayout>(Resource.Id.acceptActionsContainer);

      HideFab(fabToClipboard, false);
      HideFab(fabShare, false);
      //acceptActionsContainer.BringToFront();
      acceptActionsContainer.Visibility = ViewStates.Gone;

      allFabs = new List<FloatingActionButton> { fabCamera, fabCrop, fabAccept };
      acceptActions = new List<FloatingActionButton> { fabToClipboard, fabShare };

      fabCamera.Click += fabCamera_Click;
      fabCrop.Click += fabCrop_Click;
      fabAccept.Click += fabAccept_Click;
      fabToClipboard.Click += (sender, e) => OnCopyToClipboard(EventArgs.Empty);
      fabShare.Click += (sender, e) => OnShare(EventArgs.Empty);
    }

    public void ShowCroppingTools(bool animate)
    {
      SetVisibleFabs(fabCrop, fabCamera);
      acceptActionsContainer.Visibility = ViewStates.Gone;
    }

    public void ShowResultTools(bool animate)
    {
      acceptActionsContainer.Visibility = ViewStates.Visible;
      SetVisibleFabs(fabCrop, fabCamera, fabAccept);
    }

    public void HideAll()
    {
      HideFabs(acceptActions, true);
      acceptActionsContainer.Visibility = ViewStates.Gone;
      SetVisibleFabs();
    }

    protected void OnCrop(EventArgs e)
    {
      var handler = Crop;
      if (null != handler)
      {
        handler(this, e);
      }
    }

    protected void OnCamera(EventArgs e)
    {
      var handler = Camera;
      if (null != handler)
      {
        handler(this, e);
      }
    }

    protected void OnShare(EventArgs e)
    {
      var handler = Share;
      if(null != handler)
      {
        handler(this, e);
      }
    }

    protected void OnCopyToClipboard(EventArgs e)
    {
      var handler = CopyToClipboard;
      if(null != handler)
      {
        handler(this, e);
      }
    }

    private void fabCrop_Click(object sender, EventArgs eventArgs)
    {
      OnCrop(EventArgs.Empty);
    }

    private void fabCamera_Click(object sender, EventArgs e)
    {
      OnCamera(EventArgs.Empty);
    }

    private void fabAccept_Click(object sender, EventArgs eventArgs)
    {
      if (fabToClipboard.Visible)
      {
        HideFabs(acceptActions, true);
      }
      else
      {
        ShowFabs(acceptActions, true);
        //acceptActions.ForEach(f => f.BringToFront());
      }
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

    private void SetFabsState(IEnumerable<FloatingActionButton> buttons, ViewStates visibility, bool animate)
    {
      foreach (var button in buttons)
      {
        if (button.WillShow(visibility))
        {
          button.Visibility = visibility;
          button.Show(animate);

          continue;
        }

        if(button.WillHide(visibility))
        {
          button.Hide(animate);
          button.Visibility = visibility;
        }
      }
    }

    private void ShowFabs(IEnumerable<FloatingActionButton> buttons, bool animate)
    {
      foreach (var button in buttons)
      {
        if (!button.Visible)
        {
          button.Visibility = ViewStates.Visible;
          button.Show(animate);
        }
      }
    }

    private void HideFabs(IEnumerable<FloatingActionButton> buttons, bool animate)
    {
      foreach (var button in buttons)
      {
        if (button.Visible)
        {
          button.Visibility = ViewStates.Gone;
          button.Hide(animate);
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
  }
}