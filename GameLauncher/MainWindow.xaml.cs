using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using System.Windows.Xps.Packaging;

namespace GameLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string _URL = "https://github.com/Blowerlop/TBD/releases/latest/download/TBD.zip";
        private const string _FILENAME = "/TBD.zip";
        private string _downloadDirectoryPath => Directory.GetCurrentDirectory() + "/Data";

        private string _versionDirectoryPath => _downloadDirectoryPath;
        private string _versionFilePath => _versionDirectoryPath + "/Version.txt";

        private bool _equalVersion;
        private string? _version;

        private Action<string> _onPlayEvent;
        private Action _onDownloadedEvent;

        private Process? _gameApplicationProcess = null;


        public MainWindow()
        {
            InitializeComponent();
            Height = (double)(Screen.PrimaryScreen.Bounds.Height * (2.0f / 3.0f));// / (GetWindowsScaling() / 100.0f);
            Width = (double)ResizeImage(Height);
            //MessageBox.Show(Height.ToString());

            _onPlayEvent += UpdateButtonText;
            _onDownloadedEvent += OnDownload;

            if (Directory.Exists(_downloadDirectoryPath) == false)
            {
                Directory.CreateDirectory(_downloadDirectoryPath);
            }

            ApplicationAlreadyRunning();

            InitButton();
        }

        private void OnDownload()
        {
            //_equalVersion = true;
            UpdateButtonText("Play");
            ProgressBar.Visibility = Visibility.Hidden;
        }

        private double ResizeImage(int originalWidth)
        {
            return (double)(originalWidth * 1.777f);
        }

        private double ResizeImage(float originalWidth)
        {
            return (double)(originalWidth * 1.777f);
        }

        private double ResizeImage(double originalWidth)
        {
            return (double)(originalWidth * 1.777f);
        }


        public static float GetWindowsScaling()
        {
            return (float)(100 * Screen.PrimaryScreen.Bounds.Width / SystemParameters.PrimaryScreenWidth);
        }

        private async void InitButton()
        {
            await AreVersionEqual();
            if (_equalVersion )
            {
                if (ApplicationAlreadyRunning())
                {
                    UpdateButtonText("Application is running");
                }
                else
                {
                    UpdateButtonText("Play");
                }

                ProgressBar.Visibility = Visibility.Hidden;
            }
            else
            {
                UpdateButtonText("Download");
            }

            MainButton.Click += OnMainButtonClick;
        }

        private async void OnMainButtonClick(object sender, RoutedEventArgs e)
        {
            //await AreVersionEqual();
            if (_equalVersion)
            {
                if (ApplicationAlreadyRunning())
                {
                    UpdateButtonText("Application is running");
                    return;
                }
            }

            try
            {
                MainButton.Click -= OnMainButtonClick;
                if (_equalVersion)
                {
                    Play();
                }
                else
                {
                    await TryDownloadFileFromUrl();
                    //UpdateButtonContent("Play");
                }
                MainButton.Click += OnMainButtonClick;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                MainButton.Click += OnMainButtonClick;
            } 
        }

        private bool ApplicationAlreadyRunning()
        {
            string applicationProcessname = GetApplicationProcessName();
            /*if (applicationProcessname == null)
            {
                throw new NullReferenceException();
            }*/
            if (applicationProcessname == null) return false;

            var process = Process.GetProcessesByName(applicationProcessname);

            if (process.Length >= 1)
            {
                for (int i = 1; i < process.Length; i++)
                {
                    process[i].Kill();
                }

                if (_gameApplicationProcess == null)
                {
                    _gameApplicationProcess = process[0];
                    _gameApplicationProcess.EnableRaisingEvents = true;
                    _gameApplicationProcess.Exited += _gameApplicationProcess_Exited;
                }
                
                return true;
            }

            return false;
        }


        private async Task TryDownloadFileFromUrl()
        {
            if (HasInternetConnection() == false)
            {
                throw new OutOfMemoryException();
            }
           
            CheckIfPathValidAndFixedIt();
            await DownloadFileFromUrl(_URL, _downloadDirectoryPath + _FILENAME);
            await UpdateFileVersion();
            _onDownloadedEvent?.Invoke();
        }

        private async Task UpdateFileVersion()
        {
            using StreamWriter streamWriter = new StreamWriter(_versionFilePath, false);
            //MessageBox.Show(_version);
            await streamWriter.WriteAsync(_version);
            _equalVersion = true;
            
        }

        public async Task DownloadFileFromUrl(string url, string pathToWriteTo)
        {
            using HttpClientDownloadWithProgress httpClientDownloadWithProgress = new HttpClientDownloadWithProgress(url, pathToWriteTo);
            httpClientDownloadWithProgress.ProgressChanged += UpdateProgressBar;
            httpClientDownloadWithProgress.FileDownloaded += InitPlayBehaviour;
            await httpClientDownloadWithProgress.StartDownload();
        }

        private void UpdateProgressBar(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage)
        {
            if (progressPercentage.HasValue == false) return;

            ProgressBar.Value = progressPercentage.Value;
        }

        private void InitPlayBehaviour(string destinationFilePath)
        {
            UpdateButtonText("Unziping file...");
            UnzipFile(destinationFilePath, _downloadDirectoryPath, true);
            //MainButton.Content = "Play";
            //MainButton.Click += PlayButton;
        }

        private void CheckIfPathValidAndFixedIt()
        {
            if (Directory.Exists(_downloadDirectoryPath))
            {   
                Directory.Delete(_downloadDirectoryPath, true);
            }

            Directory.CreateDirectory(_downloadDirectoryPath);
        }    
        
        

        private async Task AreVersionEqual()
        {
            if (HasInternetConnection() == false)
            {
                throw new OutOfMemoryException();
            }

            string? currentFileVersion = null;

            if (File.Exists(_versionFilePath) == false)
            {
                CheckIfPathValidAndFixedIt();
                FileStream versionFilePath = File.Create(_versionFilePath);
                versionFilePath.Dispose();
            }

            using StreamReader streamReader = new StreamReader(_versionFilePath);
            currentFileVersion = streamReader.ReadLine();

            string? githubFileVersion = await GetGithubReleaseVersion();
            if (currentFileVersion == null)
            {
                _equalVersion = false;
                _version = githubFileVersion;
            }
            else
            {
                _equalVersion = currentFileVersion == githubFileVersion;
                _version = githubFileVersion;
            }
        }
        

        private string? GetApplicationProcessName()
        {
            //CheckIfPathValidAndFixedIt();

            if (GetFilesWithExtensionNonAlloc(_downloadDirectoryPath, ".exe", out string[] files))
            {
                int length = files.Length;
                if (files.Length > 2 || files.Length == 1)
                {
                    MessageBox.Show($"Anormal number of .exe in the path : Found {length}");
                    return null;
                }

                for (int i = 0; i < length; i++)
                {
                    if (files[i] == _downloadDirectoryPath + @"\UnityCrashHandler64.exe") continue;

                    var a = files[i].Split(@"\");
                    return a[a.Length - 1].Replace(".exe", "");

                }
            }
            return null;
        }


        private void Play()
        {
            if (GetFilesWithExtensionNonAlloc(_downloadDirectoryPath, ".exe", out string[] files))
            {
                int length = files.Length;
                if (files.Length > 2 || files.Length == 1)
                {
                    MessageBox.Show($"Anormal number of .exe in the path : Found {length}");
                    return;
                }

                for (int i = 0; i < length; i++)
                {
                    if (files[i] == _downloadDirectoryPath + @"\UnityCrashHandler64.exe") continue;

                    _gameApplicationProcess = Process.Start(files[i]);
                    _gameApplicationProcess.EnableRaisingEvents = true;
                    _gameApplicationProcess.Exited += _gameApplicationProcess_Exited;
                    _onPlayEvent?.Invoke("Application is running");
                    return;
                }
            }
        }

        private void _gameApplicationProcess_Exited(object? sender, EventArgs e)
        {
            UpdateButtonText("Play");
        }

        private void UpdateButtonText(string text)
        {
            Dispatcher.BeginInvoke(() => MainButton.Content = text);
        }

        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private bool HasInternetConnection(int timeoutMs = 10000, string? url = null)
        {
            try
            {
                url ??= CultureInfo.InstalledUICulture switch
                {
                    { Name: var n } when n.StartsWith("fa") => // Iran
                        "http://www.aparat.com",
                    { Name: var n } when n.StartsWith("zh") => // China
                        "http://www.baidu.com",
                    _ =>
                        "http://www.gstatic.com/generate_204",
                };

                var request = (HttpWebRequest)WebRequest.Create(url);
                request.KeepAlive = false;
                request.Timeout = timeoutMs;
                using (var response = (HttpWebResponse)request.GetResponse())
                    return true;
            }
            catch
            {
                return false;
            }
        }

        private void UnzipFile(string sourceArchiveFileName, string destinationDirectoryName, bool destroySourceArchiveFileOnUnzip)
        {
            UpdateButtonText("Unziping file...");
            ZipFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, true);

            if (destroySourceArchiveFileOnUnzip)
            {
                File.Delete(sourceArchiveFileName);
            }
        }

        private string[] GetFilesWithExtension(string directoryPath, string extension)
        {
            return System.IO.Directory.GetFiles(directoryPath, $"*.{extension}");
        }

        private bool GetFilesWithExtensionNonAlloc(string directoryPath, string extension, out string[] files)
        {
            files = System.IO.Directory.GetFiles(directoryPath, $"*{extension}");
            return (files.Length != 0);
        }

        private bool GetFilesNonAlloc(string directoryPath, out string[] files)
        {
            files = System.IO.Directory.GetFiles(directoryPath);
            return (files.Length != 0);
        }

        private async Task<string?> GetGithubReleaseVersion()
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
            //Version latestGitHubVersion = new Version(releases[0].TagName);
            //Version localVersion = new Version("X.X.X"); //Replace this with your local version. 
        }
    }
}