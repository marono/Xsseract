# Xsseract

This is a Xamarin based implmentation of an OCR app.
I'm using the Tesseract engine for the actual OCR task (https://code.google.com/p/tesseract-ocr/).

The app has been published on the Google Play store at https://play.google.com/store/apps/details?id=ro.onos.xsseract

The special bit about this implementation is that you can interact with it from other apps via the **ro.onos.xsseract.ResolveImage** intent.
Because of what I think is a bug in either the Xamarin platform or in Android itself, when using the above intent you also need to specify a 
boolean extra, named **PipeResult** with a value of *true* (I'll investigate this later on).

When the user accepts the OCR result, the parsed text will be returned via the **Result** extra.

#### Example

To invoke the app:

```
Intent ocrIntent = new Intent(XsseractResolveImageIntent);
ocrIntent.PutExtra("PipeResult", true);
StartActivityForResult(ocrIntent, 2);
```

And to retrieve the result:
```
protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
{
  base.OnActivityResult(requestCode, resultCode, data);
  switch (requestCode)
  {
    ...
    case 2:
      if(resultCode == Result.Ok)
      {
        var result = data.GetStringExtra("Result");
        ...
      }
      break;
  }
}
```

#### To contribute
The solution is currently missing two files for obvious reasons:
>src\Xsseract.Droid\Assets\Settings.DEBUG.json

>src\Xsseract.Droid\Assets\Settings.RELEASE.json

After downloading the files, you'll need to add them yourselves. The structure is as follows:

```
{
  "InsightsKey": "<value or empty to disable Xamarin Insights analytics>",
  "TessDataFiles": [
    { "culture": "en", "url": "file:///android_asset/tesseract-ocr-3.02.eng.tar.gz" }
  ],
  "TessOsdUrl": "file:///android_asset/tesseract-ocr-3.01.osd.tar.gz",
  "FeedbackEmailAddress": "<fill in only if using the feedback action>",
  "SuccessCountForRatingPrompt": 1
}
```

The `SuccessCountForRatingPrompt` value indicates how many successful parses the application will 
perform before asking the user to rate the app.

That's it!
Good luck cracking!
