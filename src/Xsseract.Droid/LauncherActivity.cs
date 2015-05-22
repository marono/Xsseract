using System;
using System.Collections.Generic;
using System.IO;

using Android.App;
using Android.OS;

namespace Xsseract.Droid
{
  [Activity(MainLauncher = true)]
  public class LauncherActivity : ActivityBase
  {
    protected override void OnCreate(Bundle bundle)
    {
      base.OnCreate(bundle);
    }

    protected override void OnResume()
    {
      base.OnRestart();

      StartActivity(typeof(CaptureActivity));
    }
  }
}