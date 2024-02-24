using System.Globalization;
using System.Net;

namespace GameLauncher;

public static class Network
{
    public static bool HasInternetConnection(int timeoutMs = 10000, string? url = null)
    {
        try
        {
            url ??= CultureInfo.InstalledUICulture switch
            {
                // { Name: var n } when n.StartsWith("fa") => // Iran
                //     "http://www.aparat.com",
                // { Name: var n } when n.StartsWith("zh") => // China
                //     "http://www.baidu.com",
                _ =>
                    "https://google.com",
            };

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.KeepAlive = false;
            request.Timeout = timeoutMs;
            using HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return true;
        }
        catch
        {
            return false;
        }
    }
}