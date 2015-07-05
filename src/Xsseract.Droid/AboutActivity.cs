#region

using Android.App;
using Android.OS;
using Android.Widget;

#endregion

namespace Xsseract.Droid
{
  [Activity(Label = "AboutActivity", Theme = "@style/AppTheme")]
  public class AboutActivity : ActivityBase
  {
    protected override void OnCreate(Bundle bundle)
    {
      base.OnCreate(bundle);

      SetContentView(Resource.Layout.About);

      var package = PackageManager.GetPackageInfo(PackageName, 0);

      FindViewById<TextView>(Resource.Id.txtVersion).Text = $"{package.VersionName} ({package.VersionCode})";
      FindViewById<TextView>(Resource.Id.txtInstanceId).Text = XsseractContext.InstallationId;
    }
  }
}
