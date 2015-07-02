using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Java.Lang;
using Java.Util.Zip;
using Org.Xeustechnologies.Jtar;
using Xamarin;
using Xsseract.Droid.Extensions;
using Exception = System.Exception;
using File = Java.IO.File;
using String = System.String;

namespace Xsseract.Droid
{
  public class TessDataInitializer
  {
    private readonly XsseractContext context;

    public event EventHandler<EventArgs> DownloadingDataFiles;

    public Func<bool> RestrictedUseAcknowledgeCallback;

    private File publicFilesPath;

    public TessDataInitializer(XsseractContext context)
    {
      if (null == context)
      {
        throw new ArgumentNullException(nameof(context));
      }

      this.context = context;
    }

    public void Initialize()
    {
      DoInitialize();
    }

    protected void OnFirstTimeInitialize(EventArgs e)
    {
      DownloadingDataFiles?.Invoke(this, e);
    }

    private void DoInitialize()
    {
      publicFilesPath = context.PublicFilesPath;

      try
      {
        if (!publicFilesPath.Exists())
        {
          publicFilesPath.Mkdirs();
        }
      }
      catch (Exception)
      {
        throw new ApplicationException("Could not create the data folder on external storage.");
      }

      CheckTargetPathWritable();
      var tessDataSrc = new Uri(EnDataFileUrl());
      var tessDataFileName = tessDataSrc.GetFileName();
      var tessOrientationSrc = new Uri(context.Settings.TessOsdUrl);
      var tessOrientationFileName = tessOrientationSrc.GetFileName();
      var destination = context.TessDataFilesPath;

      var tessDataFiles = GetSrcDataFiles(destination, tessDataFileName);
      context.LogInfo("Source files indicates the following data files: {0}", String.Join(",", tessDataFiles.Select(f => f.Name)));
      var tessOrientationFiles = GetSrcDataFiles(destination, tessOrientationFileName);
      context.LogInfo("Source files indicates the following orientation files: {0}", String.Join(",", tessOrientationFiles.Select(f => f.Name)));

      if (0 == (tessDataFiles?.Count ?? 0) || 0 == (tessOrientationFiles?.Count ?? 0))
      {
        OnFirstTimeInitialize(EventArgs.Empty);
      }

      bool shouldRefreshDataFiles = (tessDataFiles?.Count ?? 0) == 0 || !IsFileCollectionSane(tessDataFiles);
      if (shouldRefreshDataFiles)
      {
        context.LogWarn("Some tess data files are missing. Redeploying ...");
        RemoveFiles(tessDataFiles ?? new List<File>());
      }

      bool shouldRefreshOrientationFiles = (tessOrientationFiles?.Count ?? 0) == 0 || !IsFileCollectionSane(tessOrientationFiles);
      if (shouldRefreshOrientationFiles)
      {
        context.LogWarn("Some tess orientation files are missing. Redeploying ...");
        RemoveFiles(tessOrientationFiles ?? new List<File>());
      }

      var feq = new FileEqualityComparer();
      var dstFiles = destination.ListFiles();
      if (null != dstFiles)
      {
        foreach (var f in dstFiles)
        {
          if (tessDataFiles?.Contains(f, feq) == false && tessOrientationFiles?.Contains(f, feq) == false)
          {
            context.LogWarn("Removing '{0}' because is not part of any of any source.", f.Name);
            f.Delete();
          }
        }
      }

      if(shouldRefreshDataFiles || shouldRefreshOrientationFiles)
      {
        if(!context.IsDataConnectionAvailable())
        {
          // TODO: To resources.
          throw new ApplicationException("There's no data connection. Can't do first-time initialize without an active data connection.");
        }
        context.AskForMeteredConnectionPermission();
      }

      if (shouldRefreshDataFiles)
      {
        ITrackHandle handle = null;
        try
        {
          handle = context.LogTimedEvent(AppTrackingEvents.PrepareTessDataFiles);
          handle.Start();
          EnsureTessDataFiles(tessDataSrc, destination);

          handle?.Stop();
          if(tessDataFiles?.Count > 0 || dstFiles?.Length > 0)
          {
            context.LogEvent(AppTrackingEvents.RestoredOutOfSyncDataFiles);
          }
        }
        catch
        {
          handle.DisposeIfRunning();
          context.LogEvent(AppTrackingEvents.ErrorDownloadingDataFiles);
          throw;
        }
      }

      if (shouldRefreshOrientationFiles)
      {
        ITrackHandle handle = null;
        try
        {
          handle = context.LogTimedEvent(AppTrackingEvents.PrepareTessOrientationFiles);
          handle.Start();
          EnsureTessDataFiles(tessOrientationSrc, destination);

          handle?.Stop();
          if(tessOrientationFiles?.Count > 0 || dstFiles?.Length > 0)
          {
            context.LogEvent(AppTrackingEvents.RestoredOutOfSyncOrientationFiles);
          }
        }
        catch
        {
          handle.DisposeIfRunning();
          context.LogEvent(AppTrackingEvents.ErrorDownloadingOrientationFiles);
          throw;
        }
      }
    }

    private void EnsureTessDataFiles(Uri sourceUri, File destination)
    {
      var dataFileName = sourceUri.Segments[sourceUri.Segments.Length - 1];
      var dataFile = new File(publicFilesPath, dataFileName);

      if (dataFile.Exists())
      {
        dataFile.Delete();
      }

      DownloadFile(sourceUri, dataFile.AbsolutePath);
      ExtractTo(dataFile, destination);
    }

    private string GetTempDownloadFileName(string fileName)
    {
      return $"{fileName}.download";
    }

    private string EnDataFileUrl()
    {
      var file = context.Settings.TessDataFiles.FirstOrDefault(f => 0 == String.CompareOrdinal(f.Culture.ToLower(), "en"));
      if (null == file)
      {
        throw new ApplicationException("The tesseract eng data file has not been specified in the configuration.");
      }

      return file.Url;
    }

    private void CheckTargetPathWritable()
    {
      var specialFile = new File(publicFilesPath, "write_access");
      try
      {
        if (specialFile.Exists())
        {
          specialFile.Delete();
        }

        if (!specialFile.CreateNewFile())
        {
          throw new ApplicationException("Can't write to external media.");
        }
      }
      catch (ApplicationException)
      {
        throw;
      }
      catch (Exception)
      {
        throw new ApplicationException("Can't write to external media.");
      }
    }

    private void DownloadFile(Uri uri, string destination)
    {
      var tempDest = new File(GetTempDownloadFileName(destination));

      try
      {
        if (tempDest.Exists())
        {
          context.LogDebug("Removing previously existing download file '{0}'.", tempDest.AbsolutePath);
          tempDest.Delete();
        }

        context.LogInfo("Downloading file from '{0}' to '{1}'.", uri, tempDest.AbsolutePath);
        WebRequest request = WebRequest.Create(uri);
        request.Method = "GET";
        request.Timeout = 10000;

        var response = request.GetResponse();
        if(((HttpWebResponse)response).StatusCode != HttpStatusCode.OK)
        {
          throw new ApplicationException("Could not download Tess data files.");
        }

        var stream = response.GetResponseStream();
        CopyStreamContentToFile(stream, tempDest.AbsolutePath);

        tempDest.RenameTo(new File(destination));
      }
      finally
      {
        if (tempDest.Exists())
        {
          tempDest.Delete();
        }
      }
    }

    private void CopyStreamContentToFile(Stream stream, string destinationPath)
    {
      using (var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
      {
        var buffer = new byte[1024];
        int count;
        do
        {
          count = stream.Read(buffer, 0, buffer.Length);
          if (count != 0)
          {
            fs.Write(buffer, 0, count);
          }

        } while (count != 0);
      }
    }

    private void ExtractTo(File archive, File destinationDir)
    {
      if (!destinationDir.Exists())
      {
        context.LogInfo("Creating folder structure for '{0}'.", destinationDir.AbsolutePath);
        destinationDir.Mkdirs();
      }

      context.LogInfo("Extracting archive at '{0}' to '{1}'.", archive.AbsolutePath, destinationDir.AbsolutePath);
      var tarFile = Unzip(archive, destinationDir);
      var files = Untar(tarFile, destinationDir);

      using (var sw = new StreamWriter(Path.Combine(destinationDir.AbsolutePath, GetSrcFileName(archive.Name)), false))
      {
        files.ForEach(sw.WriteLine);
      }

      context.LogDebug("Removing tar file at '{0}'.", tarFile.AbsolutePath);
      tarFile.Delete();
      context.LogDebug("Removing archive file at '{0}'.", archive.AbsolutePath);
      archive.Delete();
    }

    private File Unzip(File zipFile, File destinationDir)
    {
      var destinationFile = new File(destinationDir, Path.GetFileNameWithoutExtension(zipFile.Name));
      using (var zip = new GZIPInputStream(new FileStream(zipFile.AbsolutePath, FileMode.Open, FileAccess.Read)))
      using (var fs = new FileStream(destinationFile.AbsolutePath, FileMode.Create, FileAccess.Write))
      {
        int count;
        var buffer = new byte[8196];
        while ((count = zip.Read(buffer, 0, buffer.Length)) > 0)
        {
          fs.Write(buffer, 0, count);
        }
      }

      return destinationFile;
    }

    private List<string> Untar(File tarFile, File destinationDir)
    {
      context.LogDebug("Untarring '{0}' to '{1}'...", tarFile.AbsolutePath, destinationDir.AbsolutePath);

      var result = new List<string>();
      // Extract all the files
      using (var tarInputStream = new TarInputStream(new FileStream(tarFile.AbsolutePath, FileMode.Open, FileAccess.Read)))
      {
        TarEntry entry;
        while ((entry = tarInputStream.NextEntry) != null)
        {
          var buffer = new byte[8192];
          string fileName = entry.Name.Substring(entry.Name.LastIndexOf('/') + 1);
          using (var fs = new FileStream(Path.Combine(destinationDir.AbsolutePath, fileName), FileMode.Create, FileAccess.Write))
          using (var outputStream = new BufferedStream(fs))
          {
            context.LogDebug("Writing '{0}' ...", fileName);
            int count;
            while ((count = tarInputStream.Read(buffer, 0, buffer.Length)) != -1)
            {
              outputStream.Write(buffer, 0, count);
            }

            result.Add(fileName);
          }
        }
      }

      return result;
    }

    private List<File> GetSrcDataFiles(File dataFilesDir, string dataFileName)
    {
      var srcFile = new File(dataFilesDir.AbsolutePath, GetSrcFileName(dataFileName));
      if (!srcFile.Exists())
      {
        return new List<File>();
      }

      var files = new List<File>();
      using (var sr = new StreamReader(srcFile.AbsolutePath))
      {
        while (!sr.EndOfStream)
        {
          var l = sr.ReadLine();
          if (!String.IsNullOrWhiteSpace(l))
          {
            files.Add(new File(dataFilesDir, l));
          }
        }
      }
      files.Add(srcFile);
      return files;
    }

    private void RemoveFiles(IEnumerable<File> files)
    {
      foreach (var f in files)
      {
        if (f.Exists())
        {
          f.Delete();
        }
      }
    }

    private string GetSrcFileName(string fileName)
    {
      return $"{fileName}.src";
    }

    private bool IsFileCollectionSane(IEnumerable<File> files)
    {
      return files.All(f => f.Exists());
    }

    private class FileEqualityComparer : IEqualityComparer<File>
    {
      public bool Equals(File x, File y)
      {
        return 0 == String.CompareOrdinal(x.AbsolutePath, y.AbsolutePath);
      }

      public int GetHashCode(File obj)
      {
        return obj.GetHashCode();
      }
    }

  }
}