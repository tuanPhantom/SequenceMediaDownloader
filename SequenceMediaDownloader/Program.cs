using SequenceMediaDownloader.Core;

namespace SequenceMediaDownloader;

public class Program
{
    private const string Path = @"C:\Users\UserName\Desktop\dess\";

    public static async Task Main(string[] args)
    {
        var links = await Parser.ParseLink(@"C:\Users\UserName\Desktop\link.txt");
        try
        {
            Console.WriteLine("starting download, please wait...\n");

            foreach (var url in links)
            {
                var parsed = Parser.ExtractInteger(url);
                if (string.IsNullOrWhiteSpace(parsed)) continue;
                var filePath = Path + parsed + ".jpeg";
                DownloadManager.Instance.StartDownload(url, filePath);
            }
            
            var timeElapsedInSeconds = await DownloadManager.Instance.WaitForDownloadsCompletion();
            var timeElapsedInMinutes = timeElapsedInSeconds / 60;
            Console.WriteLine($"finish after: {timeElapsedInMinutes:F2} minutes");
            Console.WriteLine("download complete!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex}");
        }
    }
}