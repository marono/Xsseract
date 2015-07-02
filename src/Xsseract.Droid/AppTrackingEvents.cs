namespace Xsseract.Droid
{
  public enum AppTrackingEvents
  {
    Startup,
    PrepareTessDataFiles,
    PrepareTessOrientationFiles,
    RestoredOutOfSyncDataFiles,
    RestoredOutOfSyncOrientationFiles,
    ErrorDownloadingDataFiles,
    ErrorDownloadingOrientationFiles,
    ImageParseDuration,
    ImagePreparationDuration,
    Cropping,
    Reimaging,
    Help,
    Tutorial,
    RateNow,
    RateNever,
    RateLater,
    InitialSnapshotCancelled,
    Share,
    CopyToClipboard,
    Accept,
    ResultManuallyEdited,
    InitialTutorialTime,
    PipelineMode,
    ImageDetails
  }

  public static class AppTrackingEventsDataKey
  {
    public const string
      ImageResW = "ImageResW",
      ImageResH = "ImageResH",
      ImageDensity = "ImageDensity",
      OriginalImageResW = "OriginalImageResW",
      OriginalImageResH = "OriginalImageResH",
      HelpPage = "Page",
      ParseSpeedPixelsPerMs = "ParseSpeedPixelsPerMs";
  }
}