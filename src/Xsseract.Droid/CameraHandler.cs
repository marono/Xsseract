#region

using System;
using System.Threading.Tasks;
using Android.Content;
using Android.Hardware.Camera2;
using Android.Views;

#endregion

namespace Xsseract.Droid
{
  public class CameraHandler
  {
    #region Fields

    private CameraDevice camera;
    //private AutoFocusManager
    private readonly Context context;
    private readonly ISurfaceHolder previewHolder;
    private bool previewing;

    #endregion

    #region .ctors

    public CameraHandler(ISurfaceHolder previewHolder, Context context)
    {
      if(null == previewHolder)
      {
        throw new ArgumentNullException("previewHolder");
      }
      if(null == context)
      {
        throw new ArgumentNullException("context");
      }

      this.previewHolder = previewHolder;
      this.context = context;
    }

    #endregion

    #region Public methods

    public async Task OpenAsync()
    {
      await Task.Factory.StartNew(() =>
                                  {
                                    //CameraManager manager = (CameraManager)context.GetSystemService(Android.Content.Context.CameraService);
                                    //var cameras = manager.OpenCamera("0", CameraCallback, previewHolder);

                                    //var c0 = manager.GetCameraCharacteristics("0");
                                    //var c1 = manager.GetCameraCharacteristics("1");

                                    if(null == camera)
                                    {
                                      throw new ApplicationException("Could not open the camera.");
                                    }

                                    //camera.SetPreviewDisplay(previewHolder);
                                  });
    }

    public async Task StartPreviewAsync()
    {
      if(null == camera)
      {
        throw new InvalidOperationException("The camera has not been properly initialized.");
      }
      if(previewing)
      {
        throw new InvalidOperationException("The camera is already in preview mode. Multiple calls to StartPreview are not supported.");
      }

      await Task.Factory.StartNew(() =>
                                  {
                                    //camera.StartPreview();
                                    previewing = true;
                                  });
    }

    #endregion

    #region Private Methods

    private void CameraCallback() {}

    #endregion
  }
}
