﻿using System.Collections.Concurrent;
using System.Diagnostics;
using SequenceMediaDownloader.Common;

namespace SequenceMediaDownloader.Core;

public class DownloadManager
{
    // Singleton
    private static readonly Lazy<DownloadManager> LazyInstance = new(() => new DownloadManager());
    public static DownloadManager Instance => LazyInstance.Value;

    private readonly ConcurrentDictionary<string, WrapperTask> _wrapperDict = [];

    public virtual void StartDownload(string url, string filePath)
    {
        string id;
        do
        {
            id = Commons.RandomId(12, Commons.AutoGenerated);
        } while (_wrapperDict.ContainsKey(id));

        var wrapperTask = new WrapperTask(id);
        _wrapperDict.TryAdd(id, wrapperTask);
        wrapperTask.Task = RunAsync(id, url, filePath);
    }

    public virtual async Task<double> WaitForDownloadsCompletion()
    {
        var tasks = new ConcurrentBag<WrapperTask>(_wrapperDict.Values
                .Where(wrapperTask => wrapperTask.IsRunning()))
            .Select(wrapperTask => wrapperTask.Task);

        var cts = new CancellationTokenSource();
        var displayTask = Displayer.MonitorDownloadProgress(cts);
        await Task.WhenAll(tasks);
        await cts.CancelAsync();
        var ret = await displayTask;
        Clear();
        return ret;
    }

    public virtual void Clear()
    {
        foreach (var task in _wrapperDict.Values)
        {
            task.Task.Dispose();
        }

        _wrapperDict.Clear();
        Displayer.Completed = 0;
    }

    private async Task RunAsync(string id, string url, string path)
    {
        var content = await DownloadAsync(id, url);
        await WriteToFile(path, content);
    }

    private async Task<byte[]> DownloadAsync(string id, string url)
    {
        if (!_wrapperDict.TryGetValue(id, out var wrapperTask)) throw new Exception("123");
        try
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            using var stream = await response.Content.ReadAsStreamAsync();

            // calculate buffer size
            var size = response.Content.Headers.ContentLength;
            wrapperTask.TotalBytes = size ?? -1;
            byte[] buffer = new byte[CalculateBufferSize(response.Content.Headers.ContentLength)];
            int bytesRead;

            using var memoryStream = new MemoryStream();
            // Read bytes from the stream in chunks and write to memory stream
            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
                var data = buffer.AsMemory(0, bytesRead);
                await memoryStream.WriteAsync(data);
                wrapperTask.AddBytesDownloaded(data.Length * sizeof(byte));
            }

            Displayer.Completed++;

            // Return the byte array from memory stream
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while downloading {url}: {ex}");
            return await Task.FromException<byte[]>(ex);
        }
    }

    private int CalculateBufferSize(long? contentLength)
    {
        const int defaultBufferSize = 8192; // Default buffer size (8 KB)
        const int smallFileSizeThreshold = 1024 * 1024; // Threshold for small files (1 MB)
        const int smallBufferSize = 4096; // Small buffer size (4 KB)
        const int largeBufferSize = 8192 * 8; // Large buffer size (64 KB)

        if (contentLength == null) return defaultBufferSize;
        return contentLength switch
        {
            < 0 => defaultBufferSize,
            < smallFileSizeThreshold => smallBufferSize,
            _ => largeBufferSize
        };
    }

    private async Task WriteToFile(string filePath, byte[] mediaBytes)
    {
        // var dataSize = mediaBytes.Length * sizeof(byte);
        // var bufferSize = dataSize switch
        // {
        //     <= 1024000 => 4096,
        //     < 10240000 => 81920,
        //     _ => 122880
        // };
        //
        // await using var file = File.Create(filePath, bufferSize, (FileOptions)FileMode.Create);
        // await file.WriteAsync(mediaBytes);
        await File.WriteAllBytesAsync(filePath, mediaBytes);
    }

    private static class Displayer
    {
        public static int Completed;
        private static double _total;
        private static double _totalBytesDownloaded;
        private static double _percent;

        internal static async Task<double> MonitorDownloadProgress(CancellationTokenSource cts)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            var fullTasks = -1;
            var ct = cts.Token;
            while (!ct.IsCancellationRequested)
            {
                var tasks = new ConcurrentBag<WrapperTask>(Instance._wrapperDict.Values
                    .Where(wrapperTask => wrapperTask.IsRunning()));
                if (fullTasks < 0) fullTasks = tasks.Count;
                _total = tasks.Select(task => task.TotalBytes).Aggregate((current, next) => current + next);
                // Calculate and display download speed
                double speed = await CalculateDownloadSpeedAsync(tasks, stopwatch.Elapsed);
                _percent = _totalBytesDownloaded / _total * 100;

                // Move cursor to beginning of the line you want to delete
                Console.SetCursorPosition(0, Console.CursorTop - 1);

                Console.WriteLine($"Average download speed of all Url(s): {speed:F2} byte/s or {(speed * Math.Pow(10, -6)):F2} MB/s, {Completed}/{fullTasks}, {_percent:F2}%...");

                // Wait for a short interval before updating speed again
                await Task.Delay(2000); // 2 second
            }

            stopwatch.Stop();
            return stopwatch.Elapsed.TotalSeconds;
        }

        private static async Task<double> CalculateDownloadSpeedAsync(ConcurrentBag<WrapperTask> tasks, TimeSpan elapsedTime)
        {
            // Calculate download speed (bytes per second), wrapped in double
            _totalBytesDownloaded = tasks.Select(task => task.BytesDownloaded).Aggregate((current, next) => current + next);
            var avgSpeed = _totalBytesDownloaded / elapsedTime.TotalSeconds;
            return await Task.FromResult(avgSpeed);
        }
    }
}