using System.Collections.Generic;

using Android.Support.V4.App;

namespace Xsseract.Droid
{
  public class GenericFragmentPagerAdaptor : FragmentPagerAdapter
  {
    private readonly List<Fragment> fragments = new List<Fragment>();
    public GenericFragmentPagerAdaptor(FragmentManager fm)
      : base(fm) { }

    public override int Count
    {
      get { return fragments.Count; }
    }

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