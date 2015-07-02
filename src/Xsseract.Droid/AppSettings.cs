using System.Collections.Generic;

namespace Xsseract.Droid
{
  public class AppSettings
  {
    private List<TessDataFile> tessDataFiles = new List<TessDataFile>();

    public string InsightsKey { get; set; }
    public string TessOsdUrl { get; set; }
    
    public List<TessDataFile> TessDataFiles
    {
      get { return tessDataFiles; }
      set
      {
        if (null == value)
        {
          value = new List<TessDataFile>();
        }

        tessDataFiles = value;
      }
    }

    public string FeedbackEmailAddress { get; set; }
    public int SuccessCountForRatingPrompt { get; set; }
  }

  public class TessDataFile
  {
    public string Culture { get; set; }
    public string Url { get; set; }
  }
}