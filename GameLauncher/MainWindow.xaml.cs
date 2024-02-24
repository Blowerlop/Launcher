using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace GameLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Height = Screen.PrimaryScreen.Bounds.Height * (2.0f / 3.0f);// / (GetWindowsScaling() / 100.0f);
            Width = ResizeImage(Height);

            Application.onStart += OnApplicationStart_UpdateButtonText;
            Application.onStop += OnApplicationStop_UpdateButtonText;
            
            Download.onDownloadStarted += OnDownloadStarted_UpdateButtonText;
            Download.onDownloadProgress += OnDownloadProgress_UpdateProgressBar;
            Download.onDownloadFinished += OnDownloadFinished_UpdateButtonText;
            
            Download.onUnzipStarted += OnUnzipStarted_UpdateButtonText;
            Download.onUnzipFinished += OnUnzipFinished_UpdateButtonText;

            InitButton();
        }

        private void OnUnzipStarted_UpdateButtonText()
        {
            UpdateButtonText("Unzip");
        }
        
        private void OnUnzipFinished_UpdateButtonText()
        {
            UpdateButtonText("Play");
        }


        private void OnApplicationStart_UpdateButtonText()
        {
            UpdateButtonText("Application is running");
        }

        private async void OnApplicationStop_UpdateButtonText()
        {
            if (await Application.IsUpToDate())
            {
                UpdateButtonText("Play");
            }
            else
            {
                UpdateButtonText("Update");
            }
        }

        private void OnDownloadStarted_UpdateButtonText()
        {
            UpdateButtonText("Downloading");
            ProgressBar.Visibility = Visibility.Visible;
        }
        
        private void OnDownloadProgress_UpdateProgressBar(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage)
        {
            if (progressPercentage != null) ProgressBar.Value = progressPercentage.Value;
        }
        
        private void OnDownloadFinished_UpdateButtonText(Download.Status status)
        {
            UpdateButtonText("Play");
            ProgressBar.Visibility = Visibility.Hidden;
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
            UpdateButtonText("Waiting for connection...");
            await Application.IsUpToDate();
            if (await Application.CanStart())
            {
                UpdateButtonText("Play");
                ProgressBar.Visibility = Visibility.Hidden;
            }
            else
            {
                UpdateButtonText(Application.IsApplicationRunning() ? "Application is running" : "Download");
            }

            MainButton.Click += OnMainButtonClick;
        }

        private async void OnMainButtonClick(object sender, RoutedEventArgs e)
        {
            MainButton.Click -= OnMainButtonClick;
            
            if (await Application.IsUpToDate())
            {
                Application.Start();
            }
            else
            {
                
                await Application.Download();
                
            }
            
            MainButton.Click += OnMainButtonClick;
        }
        
        private void UpdateButtonText(string text)
        {
            Console.WriteLine("Update text: " + text);
            Dispatcher.BeginInvoke(() => MainButton.Content = text);
        }
    }
}