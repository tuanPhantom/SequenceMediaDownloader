namespace SequenceMediaDownloader.Core;

public class WrapperTask
{
    private string _id;
    private Task _task;
    private double _bytesDownloaded;
    private double _totalBytes;

    public string Id
    {
        get => _id;
        set => _id ??= value;
    }

    public Task Task
    {
        get => _task;
        set
        {
            if (value != null)
            {
                _task = value;
            }
        }
    }

    public double BytesDownloaded
    {
        get => _bytesDownloaded;
        set
        {
            if (ValidateBytesDownloaded(value))
            {
                _bytesDownloaded = value;
            }
        }
    }

    public virtual void AddBytesDownloaded(double byteDownloaded)
    {
        if (ValidateBytesDownloaded(byteDownloaded))
        {
            _bytesDownloaded += byteDownloaded;
        }
    }

    public double TotalBytes
    {
        get => _totalBytes;
        set
        {
            if (ValidateBytesDownloaded(value))
            {
                _totalBytes = value;
            }
        }
    }

    private bool ValidateBytesDownloaded(double byteDownloaded)
    {
        return byteDownloaded > 0;
    }

    public WrapperTask(string id)
    {
        Id = id;
    }

    public virtual bool IsRunning()
    {
        return Task is { IsCompleted: false };
    }
}