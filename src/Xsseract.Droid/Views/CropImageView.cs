#region

using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;

#endregion

namespace Xsseract.Droid.Views
{
  public class CropImageView : ImageViewTouchBase
  {
    #region Fields

    private readonly List<HighlightView> hightlightViews = new List<HighlightView>();
    private float mLastX;
    private float mLastY;
    private HighlightView mMotionHighlightView;
    private HighlightView.HitPosition motionEdge;

    #endregion

    #region .ctors

    public CropImageView(Context context, IAttributeSet attrs)
      : base(context, attrs)
    {
      SetLayerType(LayerType.Software, null);
    }

    #endregion

    public void AddHighlightView(HighlightView hv)
    {
      hightlightViews.Add(hv);
      Invalidate();
    }

    public void ClearHighlightViews()
    {
      hightlightViews.Clear();
    }

    public override bool OnTouchEvent(MotionEvent ev)
    {
      switch(ev.Action)
      {
        case MotionEventActions.Down:

          for(int i = 0; i < hightlightViews.Count; i++)
          {
            HighlightView hv = hightlightViews[i];
            var edge = hv.GetHit(ev.GetX(), ev.GetY());
            if (edge != HighlightView.HitPosition.None)
            {
              motionEdge = edge;
              mMotionHighlightView = hv;
              mLastX = ev.GetX();
              mLastY = ev.GetY();
              mMotionHighlightView.Mode =
                (edge == HighlightView.HitPosition.Move)
                  ? HighlightView.ModifyMode.Move
                  : HighlightView.ModifyMode.Grow;

              break;
            }
          }
          break;

        case MotionEventActions.Up:
          if (mMotionHighlightView != null)
          {
            CenterBasedOnHighlightView(mMotionHighlightView);
            mMotionHighlightView.Mode = HighlightView.ModifyMode.None;
          }

          mMotionHighlightView = null;
          motionEdge = HighlightView.HitPosition.None;
          break;

        case MotionEventActions.Move:
          if (mMotionHighlightView != null)
          {
            mMotionHighlightView.HandleMotion(motionEdge,
              ev.GetX() - mLastX,
              ev.GetY() - mLastY);
            mLastX = ev.GetX();
            mLastY = ev.GetY();

            if (true)
            {
              // This section of code is optional. It has some user
              // benefit in that moving the crop rectangle against
              // the edge of the screen causes scrolling but it means
              // that the crop rectangle is no longer fixed under
              // the user's finger.
              EnsureVisible(mMotionHighlightView);
            }
          }
          break;
      }

      switch(ev.Action)
      {
        case MotionEventActions.Up:
          Center(true, true);
          break;
        case MotionEventActions.Move:
          // if we're not zoomed then there's no point in even allowing
          // the user to move the image around.  This call to center puts
          // it back to the normalized location (with false meaning don't
          // animate).
          if (GetScale().Equals(1F))
          {
            Center(true, true);
          }
          break;
      }

      return true;
    }

    protected override void OnDraw(Canvas canvas)
    {
      base.OnDraw(canvas);

      foreach(HighlightView t in hightlightViews)
      {
        t.Draw(canvas);
      }
    }

    protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
    {
      base.OnLayout(changed, left, top, right, bottom);

      if (BitmapDisplayed.Bitmap != null)
      {
        foreach(var hv in hightlightViews)
        {
          hv.Matrix.Set(ImageMatrix);
          hv.Invalidate();

          if (hv.Focused)
          {
            CenterBasedOnHighlightView(hv);
          }
        }
      }
    }

    protected override void PostTranslate(float deltaX, float deltaY)
    {
      base.PostTranslate(deltaX, deltaY);
      for(int i = 0; i < hightlightViews.Count; i++)
      {
        HighlightView hv = hightlightViews[i];
        hv.Matrix.PostTranslate(deltaX, deltaY);
        hv.Invalidate();
      }
    }

    protected override void ZoomIn()
    {
      base.ZoomIn();
      foreach(var hv in hightlightViews)
      {
        hv.Matrix.Set(ImageMatrix);
        hv.Invalidate();
      }
    }

    protected override void ZoomOut()
    {
      base.ZoomOut();
      foreach(var hv in hightlightViews)
      {
        hv.Matrix.Set(ImageMatrix);
        hv.Invalidate();
      }
    }

    protected override void ZoomTo(float scale, float centerX, float centerY)
    {
      base.ZoomTo(scale, centerX, centerY);

      foreach(var hv in hightlightViews)
      {
        hv.Matrix.Set(ImageMatrix);
        hv.Invalidate();
      }
    }

    #region Private Methods

    // Pan the displayed image to make sure the cropping rectangle is visible.

    // If the cropping rectangle's size changed significantly, change the
    // view's center and scale according to the cropping rectangle.
    private void CenterBasedOnHighlightView(HighlightView hv)
    {
      Rect drawRect = hv.DrawRect;

      float width = drawRect.Width();
      float height = drawRect.Height();

      float thisWidth = Width;
      float thisHeight = Height;

      float z1 = thisWidth / width * .6F;
      float z2 = thisHeight / height * .6F;

      float zoom = Math.Min(z1, z2);
      zoom = zoom * GetScale();
      zoom = Math.Max(1F, zoom);
      if ((Math.Abs(zoom - GetScale()) / zoom) > .1)
      {
        var coordinates = new float[]
        {
          hv.CropRect.CenterX(),
          hv.CropRect.CenterY()
        };

        ImageMatrix.MapPoints(coordinates);
        ZoomTo(zoom, coordinates[0], coordinates[1], 300F);
      }

      EnsureVisible(hv);
    }

    private void EnsureVisible(HighlightView hv)
    {
      Rect r = hv.DrawRect;

      int panDeltaX1 = Math.Max(0, IvLeft - r.Left);
      int panDeltaX2 = Math.Min(0, IvRight - r.Right);

      int panDeltaY1 = Math.Max(0, IvTop - r.Top);
      int panDeltaY2 = Math.Min(0, IvBottom - r.Bottom);

      int panDeltaX = panDeltaX1 != 0 ? panDeltaX1 : panDeltaX2;
      int panDeltaY = panDeltaY1 != 0 ? panDeltaY1 : panDeltaY2;

      if (panDeltaX != 0 || panDeltaY != 0)
      {
        PanBy(panDeltaX, panDeltaY);
      }
    }

    #endregion
  }
}
