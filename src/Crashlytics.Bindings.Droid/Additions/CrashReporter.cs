using System;
using Android.Content;
using Android.Runtime;
using Java.Lang;
using Xamarin.Android.Crashlytics;
using JavaException = Java.Lang.Exception;

namespace Crashlytics.Bindings.Droid
{
  public static class CrashReporter
  {
    public static void StartWithMonoHook(Context context, bool callJavaDefaultUncaughtExceptionHandler)
    {
      if (context == null)
        throw new ArgumentNullException("context");
      Fabric.With(context, new Crashlytics());
      AndroidEnvironment.UnhandledExceptionRaiser +=
          (sender, args) => AndroidEnvironmentOnUnhandledExceptionRaiser(args, callJavaDefaultUncaughtExceptionHandler);
    }

    private static void AndroidEnvironmentOnUnhandledExceptionRaiser(RaiseThrowableEventArgs eventArgs,
            bool callJavaDefaultUncaughtExceptionHandler)
    {
      JavaException exception = MonoException.Create(eventArgs.Exception);

      if (callJavaDefaultUncaughtExceptionHandler && Thread.DefaultUncaughtExceptionHandler != null)
        Thread.DefaultUncaughtExceptionHandler.UncaughtException(Thread.CurrentThread(), exception);
      else
        Crashlytics.LogException(exception);
    }
  }
}