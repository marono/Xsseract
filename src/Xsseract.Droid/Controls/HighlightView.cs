#region

using System;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;

#endregion

namespace Xsseract.Droid.Controls
{
  public class HighlightView
  {
    // The View displaying the image.

    #region Fields

    private readonly View context;

    private RectF cropRect; // in image space

    private readonly Paint focusPaint = new Paint();
    private RectF imageRect; // in image space
    private float initialAspectRatio;
    private bool maintainAspectRatio = false;
    public Matrix matrix;
    private ModifyMode mode = ModifyMode.None;
    private readonly Paint noFocusPaint = new Paint();
    private readonly Paint outlinePaint = new Paint();
    private Drawable resizeDrawableHeight;
    private Drawable resizeDrawableWidth;

    #endregion

    #region Properties

    public Rect CropRect
    {
      get
      {
        return new Rect((int)cropRect.Left, (int)cropRect.Top,
          (int)cropRect.Right, (int)cropRect.Bottom);
      }
    }
    public Rect DrawRect // in screen space
    { get; private set; }

    public bool Focused { get; set; }

    public bool Hidden { get; set; }

    public ModifyMode Mode
    {
      get { return mode; }
      set
      {
        if(value != mode)
        {
          mode = value;
          context.Invalidate();
        }
      }
    }

    #endregion

    // Handles motion (dx, dy) in screen space.
    // The "edge" parameter specifies which edges the user is dragging.

    #region Public methods

    public void Draw(Canvas canvas)
    {
      if(Hidden)
      {
        return;
      }

      canvas.Save();

      //if (!Focused)
      //{
      //  outlinePaint.Color = Color.White;
      //  canvas.DrawRect(DrawRect, outlinePaint);
      //}
      //else
      //{
      var viewDrawingRect = new Rect();
      context.GetDrawingRect(viewDrawingRect);

      outlinePaint.Color = Color.White; // new Color(0XFF, 0xFF, 0x8A, 0x00);
      focusPaint.Color = new Color(50, 50, 50, 125);

      var path = new Path();
      path.AddRect(new RectF(DrawRect), Path.Direction.Cw);

      canvas.ClipPath(path, Region.Op.Difference);
      canvas.DrawRect(viewDrawingRect, focusPaint);

      canvas.Restore();
      canvas.DrawPath(path, outlinePaint);

      int left = DrawRect.Left + 1;
      int right = DrawRect.Right + 1;
      int top = DrawRect.Top + 4;
      int bottom = DrawRect.Bottom + 3;

      int widthWidth = resizeDrawableWidth.IntrinsicWidth / 2;
      int widthHeight = resizeDrawableWidth.IntrinsicHeight / 2;
      int heightHeight = resizeDrawableHeight.IntrinsicHeight / 2;
      int heightWidth = resizeDrawableHeight.IntrinsicWidth / 2;

      int xMiddle = DrawRect.Left + ((DrawRect.Right - DrawRect.Left) / 2);
      int yMiddle = DrawRect.Top + ((DrawRect.Bottom - DrawRect.Top) / 2);

      resizeDrawableWidth.SetBounds(left - widthWidth, yMiddle - widthHeight, left + widthWidth, yMiddle + widthHeight);
      resizeDrawableWidth.Draw(canvas);

      resizeDrawableWidth.SetBounds(right - widthWidth, yMiddle - widthHeight, right + widthWidth, yMiddle + widthHeight);
      resizeDrawableWidth.Draw(canvas);

      resizeDrawableHeight.SetBounds(xMiddle - heightWidth, top - heightHeight, xMiddle + heightWidth, top + heightHeight);
      resizeDrawableHeight.Draw(canvas);

      resizeDrawableHeight.SetBounds(xMiddle - heightWidth, bottom - heightHeight, xMiddle + heightWidth, bottom + heightHeight);
      resizeDrawableHeight.Draw(canvas);
    }

    // Determines which edges are hit by touching at (x, y).
    public HitPosition GetHit(float x, float y)
    {
      Rect r = computeLayout();
      float hysteresis = Math.Max(
        Math.Max(resizeDrawableWidth.IntrinsicWidth / 2, resizeDrawableWidth.IntrinsicHeight / 2),
        Math.Max(resizeDrawableHeight.IntrinsicWidth / 2, resizeDrawableHeight.IntrinsicHeight / 2));
      var retval = HitPosition.None;

      // verticalCheck makes sure the position is between the top and
      // the bottom edge (with some tolerance). Similar for horizCheck.
      bool verticalCheck = (y >= r.Top - hysteresis) && (y < r.Bottom + hysteresis);
      bool horizCheck = (x >= r.Left - hysteresis) && (x < r.Right + hysteresis);

      // Check whether the position is near some edge(s).
      if((Math.Abs(r.Left - x) < hysteresis) && verticalCheck)
      {
        retval |= HitPosition.GrowLeftEdge;
      }

      if((Math.Abs(r.Right - x) < hysteresis) && verticalCheck)
      {
        retval |= HitPosition.GrowRightEdge;
      }

      if((Math.Abs(r.Top - y) < hysteresis) && horizCheck)
      {
        retval |= HitPosition.GrowTopEdge;
      }

      if((Math.Abs(r.Bottom - y) < hysteresis) && horizCheck)
      {
        retval |= HitPosition.GrowBottomEdge;
      }

      // Not near any edge but inside the rectangle: move.
      if(retval == HitPosition.None && r.Contains((int)x, (int)y))
      {
        retval = HitPosition.Move;
      }

      return retval;
    }

    public void HandleMotion(HitPosition edge, float dx, float dy)
    {
      Rect r = computeLayout();
      if(edge == HitPosition.None)
      {
        return;
      }
      else if(edge == HitPosition.Move)
      {
        // Convert to image space before sending to moveBy().
        moveBy(dx * (cropRect.Width() / r.Width()),
          dy * (cropRect.Height() / r.Height()));
      }
      else
      {
        if(!edge.HasFlag(HitPosition.GrowLeftEdge) && !edge.HasFlag(HitPosition.GrowRightEdge))
        {
          dx = 0;
        }

        if(!edge.HasFlag(HitPosition.GrowTopEdge) && !edge.HasFlag(HitPosition.GrowBottomEdge))
        {
          dy = 0;
        }

        // Convert to image space before sending to growBy().
        float xDelta = dx * (cropRect.Width() / r.Width());
        float yDelta = dy * (cropRect.Height() / r.Height());

        growBy(edge, xDelta, yDelta);
      }
    }

    public void Invalidate()
    {
      DrawRect = computeLayout();
    }

    public void Setup(Matrix m, Rect imageRect, RectF cropRect, bool maintainAspectRatio)
    {
      matrix = new Matrix(m);

      this.cropRect = cropRect;
      this.imageRect = new RectF(imageRect);
      this.maintainAspectRatio = maintainAspectRatio;

      initialAspectRatio = cropRect.Width() / cropRect.Height();
      DrawRect = computeLayout();

      focusPaint.SetARGB(125, 50, 50, 50);
      noFocusPaint.SetARGB(125, 50, 50, 50);
      outlinePaint.StrokeWidth = 3;
      outlinePaint.SetStyle(Paint.Style.Stroke);
      outlinePaint.AntiAlias = true;

      mode = ModifyMode.None;
      init();
    }

    #endregion

    #region Private helpers

    private Rect computeLayout()
    {
      var r = new RectF(cropRect.Left, cropRect.Top,
        cropRect.Right, cropRect.Bottom);
      matrix.MapRect(r);
      return new Rect((int)Math.Round(r.Left), (int)Math.Round(r.Top),
        (int)Math.Round(r.Right), (int)Math.Round(r.Bottom));
    }

    private void growBy(HitPosition position, float dx, float dy)
    {
      if(maintainAspectRatio)
      {
        if(dx != 0)
        {
          dy = dx / initialAspectRatio;
        }
        else if(dy != 0)
        {
          dx = dy * initialAspectRatio;
        }
      }

      // Don't let the cropping rectangle grow too fast.
      // Grow at most half of the difference between the image rectangle and
      // the cropping rectangle.
      var r = new RectF(cropRect);
      if(dx > 0F && r.Width() + 2 * dx > imageRect.Width())
      {
        float adjustment = (imageRect.Width() - r.Width()) / 2F;
        dx = adjustment;
        if(maintainAspectRatio)
        {
          dy = dx / initialAspectRatio;
        }
      }
      if(dy > 0F && r.Height() + 2 * dy > imageRect.Height())
      {
        float adjustment = (imageRect.Height() - r.Height()) / 2F;
        dy = adjustment;
        if(maintainAspectRatio)
        {
          dx = dy * initialAspectRatio;
        }
      }

      //r.Inset(-dx, -dy);
      switch(position)
      {
        case HitPosition.GrowLeftEdge:
          r.Left += dx;
          break;
        case HitPosition.GrowRightEdge:
          r.Right += dx;
          break;
        case HitPosition.GrowBottomEdge:
          r.Bottom += dy;
          break;
        case HitPosition.GrowTopEdge:
          r.Top += dy;
          break;
      }

      // Don't let the cropping rectangle shrink too fast.
      float widthCap = 25F;
      if(r.Width() < widthCap)
      {
        r.Inset(-(widthCap - r.Width()) / 2F, 0F);
      }
      float heightCap = maintainAspectRatio
        ? (widthCap / initialAspectRatio)
        : widthCap;
      if(r.Height() < heightCap)
      {
        r.Inset(0F, -(heightCap - r.Height()) / 2F);
      }

      // Put the cropping rectangle inside the image rectangle.
      if(r.Left < imageRect.Left)
      {
        r.Offset(imageRect.Left - r.Left, 0F);
      }
      else if(r.Right > imageRect.Right)
      {
        r.Offset(-(r.Right - imageRect.Right), 0);
      }
      if(r.Top < imageRect.Top)
      {
        r.Offset(0F, imageRect.Top - r.Top);
      }
      else if(r.Bottom > imageRect.Bottom)
      {
        r.Offset(0F, -(r.Bottom - imageRect.Bottom));
      }

      cropRect.Set(r);
      DrawRect = computeLayout();
      context.Invalidate();
    }

    private void init()
    {
      var resources = context.Resources;

      resizeDrawableWidth = resources.GetDrawable(Resource.Drawable.camera_crop_width);
      resizeDrawableHeight = resources.GetDrawable(Resource.Drawable.camera_crop_height);
    }

    // Grows the cropping rectange by (dx, dy) in image space.
    private void moveBy(float dx, float dy)
    {
      var invalRect = new Rect(DrawRect);

      cropRect.Offset(dx, dy);

      // Put the cropping rectangle inside image rectangle.
      cropRect.Offset(
        Math.Max(0, imageRect.Left - cropRect.Left),
        Math.Max(0, imageRect.Top - cropRect.Top));

      cropRect.Offset(
        Math.Min(0, imageRect.Right - cropRect.Right),
        Math.Min(0, imageRect.Bottom - cropRect.Bottom));

      DrawRect = computeLayout();
      invalRect.Union(DrawRect);
      invalRect.Inset(-10, -10);
      context.Invalidate(invalRect);
    }

    // Grows the cropping rectange by (dx, dy) in image space.

    #endregion

    public enum ModifyMode
    {
      None,
      Move,
      Grow
    }

    [Flags]
    public enum HitPosition
    {
      None,
      GrowLeftEdge,
      GrowRightEdge,
      GrowTopEdge,
      GrowBottomEdge,
      Move
    }

    #region Constructor

    public HighlightView(View ctx)
    {
      context = ctx;
    }

    #endregion
  }
}
