#region

using System.Collections.Generic;
using Android.Support.V4.App;

#endregion

namespace Xsseract.Droid
{
  public class GenericFragmentPagerAdaptor : FragmentPagerAdapter
  {
    #region Fields

    private readonly List<Fragment> fragments = new List<Fragment>();

    #endregion

    public override int Count
    {
      get { return fragments.Count; }
    }

    #region .ctors

    public GenericFragmentPagerAdaptor(FragmentManager fm)
      : base(fm) {}

    #endregion

    public override Fragment GetItem(int position)
    {
      return fragments[position];
    }

    public void AddFragment(Fragment fragment)
    {
      fragments.Add(fragment);
    }
  }
}
