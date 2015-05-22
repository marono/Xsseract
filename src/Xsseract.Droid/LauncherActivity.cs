#region

using Android.App;
using Android.OS;

#endregion

namespace Xsseract.Droid
{
  [Activity(MainLauncher = true)]
  public class LauncherActivity: ActivityBase
  {
    #region Protected methods

    protected override void OnCreate(Bundle bundle)
    {
      base.OnCreate(bundle);
    }

    protected override void OnResume()
    {
      base.OnRestart();

      StartActivity(typeof(CaptureActivity));
    }

    #endregion
  }
}
