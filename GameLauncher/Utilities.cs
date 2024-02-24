
namespace GameLauncher
{
    namespace Utilities
    {
        public static class FileUtilities
        {
            public static string[] GetFilesWithExtension(string directoryPath, string extension)
            {
                return System.IO.Directory.GetFiles(directoryPath, $"*.{extension}");
            }

            public static bool GetFilesWithExtensionNonAlloc(string directoryPath, string extension, out string[] files)
            {
                files = System.IO.Directory.GetFiles(directoryPath, $"*{extension}");
                return (files.Length != 0);
            }

            public static bool GetFilesNonAlloc(string directoryPath, out string[] files)
            {
                files = System.IO.Directory.GetFiles(directoryPath);
                return (files.Length != 0);
            }
        }
    }
    
}
