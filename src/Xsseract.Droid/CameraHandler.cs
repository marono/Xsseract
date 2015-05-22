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
using Android.Hardware.Camera2;
using System.Threading.Tasks;

namespace Xsseract.Droid
{
  public class CameraHandler
  {
    private CameraDevice camera;
    //private AutoFocusManager
    private readonly ISurfaceHolder previewHolder;
    private readonly Android.Content.Context context;
    private bool previewing;

    public CameraHandler(ISurfaceHolder previewHolder, Android.Content.Context context)
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

    public async Task OpenAsync()
    {
      await Task.Factory.StartNew(() => {
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

      await Task.Factory.StartNew(() => {
        //camera.StartPreview();
        previewing = true;
      });
    }

    private void CameraCallback()
    { }
  }
}