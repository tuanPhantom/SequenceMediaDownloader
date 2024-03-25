using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace SequenceMediaDownloader.Core;

public static class Parser
{
    public static async Task<ConcurrentBag<string>> ParseLink(string filePath)
    {
        var ret = new ConcurrentBag<string>();
        try
        {
            // Check if the file exists
            if (File.Exists(filePath))
            {
                using var streamReader = new StreamReader(filePath);
                string line;
                while ((line = await streamReader.ReadLineAsync()) != null)
                {
                    // Process each line as needed
                    ret.Add(line);
                }
            }
            else
            {
                Console.WriteLine("File does not exist: " + filePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex);
        }

        return ret;
    }

    public static string ExtractInteger(string input)
    {
        // Define the regular expression pattern to match an integer
        Regex regex = new Regex(@"https://www\.yourregexgoeshere\.com/video/(.+)/stream2/(\d+)\.vod/");

        // Find the first match in the input string
        Match match = regex.Match(input);

        if (match.Success)
        {
            Group group = match.Groups[2]; // Group 1 contains the captured (\d+) part
            if (group.Success)
            {
                string intString = group.Value;
                return intString;
            }
        }
        
        return string.Empty;
    }
}