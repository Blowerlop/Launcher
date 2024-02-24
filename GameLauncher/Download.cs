using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace GameLauncher;

public static class Download
{
    public enum Status
    {
        Success,
        Fail
    }
    
    public static Action? onDownloadStarted;
    public static Action<long?, long, double?>? onDownloadProgress;
    public static Action<Status>? onDownloadFinished;

    public static Action? onUnzipStarted;
    public static Action? onUnzipFinished;
    
    public static async Task TryDownloadFileFromUrl(string url, string pathToWriteTo)
    {
        if (Network.HasInternetConnection() == false)
        {
            Console.WriteLine("Download failed, no connection.");
            OnDownloadFinished(url, Status.Fail);
        }
           
        CheckIfPathValidAndFixedIt();
        await DownloadFileFromUrl(url, pathToWriteTo);
    }

    private static void OnDownloadStarted(string url)
    {
        onDownloadStarted?.Invoke();
    }

    private static void OnDownloadProgress(string url, long? totalFileSize, long totalBytesDownloaded, double? progressPercentage)
    {
        onDownloadProgress?.Invoke(totalFileSize, totalBytesDownloaded, progressPercentage);
    }

    private static void OnDownloadFinished(string url, Status status)
    {
        onDownloadFinished?.Invoke(status);
    }
    
    public static async Task DownloadFileFromUrl(string url, string pathToWriteTo)
    {
        OnDownloadStarted(url);
        
        using HttpClientDownloadWithProgress httpClientDownloadWithProgress = new HttpClientDownloadWithProgress(url, pathToWriteTo);
        httpClientDownloadWithProgress.ProgressChanged += (size, downloaded, percentage) => OnDownloadProgress(url, size, downloaded, percentage);
        httpClientDownloadWithProgress.FileDownloaded += path => OnDownloadFinished(url, Status.Success);
        await httpClientDownloadWithProgress.StartDownload();

        OnDownloadFinished(url, Status.Success);
    }
    
    public static void CheckIfPathValidAndFixedIt()
    {
        if (Directory.Exists(Application.downloadDirectoryPath))
        {   
            Directory.Delete(Application.downloadDirectoryPath, true);
        }

        Directory.CreateDirectory(Application.downloadDirectoryPath);
    } 
    
    public static void UnzipFile(string sourceArchiveFileName, string destinationDirectoryName, bool destroySourceArchiveFileOnUnzip)
    {
        Console.WriteLine("Start unzip");
        onUnzipStarted?.Invoke();
        
        ZipFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, true);

        if (destroySourceArchiveFileOnUnzip)
        {
            File.Delete(sourceArchiveFileName);
        }
        
        onUnzipFinished?.Invoke();
        
        Console.WriteLine("Unzip finished");
    }
}