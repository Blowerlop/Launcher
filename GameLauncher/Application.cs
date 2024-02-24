using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameLauncher.Utilities;
using Octokit;

namespace GameLauncher;

public static class Application
{
    private const string _URL = "https://github.com/Blowerlop/TBD/releases/latest/download/TBD.zip";
    private const string _FILENAME = "/TBD.zip";
    private static Process? _process;
    
    public static string downloadDirectoryPath => Directory.GetCurrentDirectory() + "/Data";
    private static string versionDirectoryPath => downloadDirectoryPath;
    private static string versionFilePath => versionDirectoryPath + "/Version.txt";

    public static Action? onStart;
    public static Action? onStop;


    public static void Init()
    {
        
    }
    
    public static async void Start()
    {
        if (await CanStart() == false) return;
        
        if (FileUtilities.GetFilesWithExtensionNonAlloc(downloadDirectoryPath, ".exe", out string[] files))
        {
            int length = files.Length;
            if (files.Length > 2 || files.Length == 1)
            {
                MessageBox.Show($"Anormal number of .exe in the path : Found {length}");
                return;
            }

            for (int i = 0; i < length; i++)
            {
                if (files[i] == downloadDirectoryPath + @"\UnityCrashHandler64.exe") continue;

                _process = Process.Start(files[i]);
                break;
            }
        }

        if (_process == null)
        {
            Console.WriteLine("Process is null");
            return;
        }
        
        OnStart();
    }

    public static async Task<bool> CanStart()
    {
        return _process == null && await IsUpToDate();
    }

    private static void OnStart()
    {
        if (_process == null)
        {
            Console.WriteLine("OnStart but application process is null");
            return;
        }
        
        _process.EnableRaisingEvents = true;
        _process.Exited += OnProcessExited;

        onStart?.Invoke();
        
        Console.WriteLine("Application started");
    }

    public static void OnStop()
    {
        _process = null;
        onStop?.Invoke();
        
        Console.WriteLine("Application stopped");
    }
    
    private static void OnProcessExited(object? _, EventArgs __)
    {
        Console.WriteLine("Application process exit");
        OnStop();
    }
    
    public static bool IsApplicationRunning()
    {
        string? applicationProcessName = GetApplicationProcessName();
        
        if (applicationProcessName == null) return false;

        var process = Process.GetProcessesByName(applicationProcessName);

        if (process.Length >= 1)
        {
            for (int i = 1; i < process.Length; i++)
            {
                process[i].Kill();
            }

            if (_process == null)
            {
                _process = process[0];
                _process.EnableRaisingEvents = true;
                _process.Exited += OnProcessExited;
            }
                
            return true;
        }

        return false;
    }
    
    private static string? GetApplicationProcessName()
    {
        if (FileUtilities.GetFilesWithExtensionNonAlloc(downloadDirectoryPath, ".exe", out string[] files))
        {
            int length = files.Length;
            if (files.Length > 2 || files.Length == 1)
            {
                MessageBox.Show($"Anormal number of .exe in the path : Found {length}");
                return null;
            }

            for (int i = 0; i < length; i++)
            {
                if (files[i] == downloadDirectoryPath + @"\UnityCrashHandler64.exe") continue;

                string[] strings = files[i].Split(@"\");
                return strings[^1].Replace(".exe", "");

            }
        }
        return null;
    }

    public static async Task Download()
    {
        if (Directory.Exists(downloadDirectoryPath) == false)
        {
            Directory.CreateDirectory(downloadDirectoryPath);
        }
        
        await GameLauncher.Download.TryDownloadFileFromUrl(_URL, downloadDirectoryPath + _FILENAME);
        GameLauncher.Download.UnzipFile(downloadDirectoryPath + _FILENAME, downloadDirectoryPath, true);
        string? githubVersion = await GetGithubReleaseVersion();
        if (githubVersion != null) await UpdateFileVersion(githubVersion);
    }

    public static async Task<bool> IsUpToDate()
    {
        bool result = await AreVersionEqual();
        return result;
    }
    
    private static async Task<bool> AreVersionEqual()
    {
        if (Network.HasInternetConnection() == false)
        {
            throw new OutOfMemoryException();
        }

        string? currentFileVersion = null;

        if (File.Exists(versionFilePath) == false)
        {
            GameLauncher.Download.CheckIfPathValidAndFixedIt();
            FileStream versionFilePath = File.Create(Application.versionFilePath);
            await versionFilePath.DisposeAsync();
        }

        using StreamReader streamReader = new StreamReader(versionFilePath);
        currentFileVersion = await streamReader.ReadLineAsync();

        string? githubFileVersion = await GetGithubReleaseVersion();
        
        if (currentFileVersion == null) return false;
        
        return currentFileVersion == githubFileVersion;
    }
    
    private static async Task<string?> GetGithubReleaseVersion()
    {
        //Get all releases from GitHub
        //Source: https://octokitnet.readthedocs.io/en/latest/getting-started/
        
        GitHubClient client = new GitHubClient(new ProductHeaderValue("SomeName"));
        IReadOnlyList<Release> releases = await client.Repository.Release.GetAll("Blowerlop", "TBD");

        //Setup the versions
        if (releases.Count == 0)
        {
            return null;
        }
        return releases[0].TagName;

    }
    
    private static async Task UpdateFileVersion(string newVersion)
    {
        await using StreamWriter streamWriter = new StreamWriter(versionFilePath, false);
        await streamWriter.WriteAsync(newVersion);
    }
}