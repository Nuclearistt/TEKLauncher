using System.IO.Pipes;
using System.Threading;

namespace TEKLauncher.Steam;

/// <summary>Manages connection between the launcher and TEK Injector in game process.</summary>
static class TEKInjectorInterop
{
    /// <summary>Downloads/updates specified mod and reports download progress to TEK Injector over the named pipe.</summary>
    /// <param name="obj">Thread parameter, for this method it's a tuple of <see cref="ulong"/> and <see cref="NamedPipeServerStream"/>.</param>
    static void TaskProcedure(object? obj)
    {
        if (obj is not (ulong id, NamedPipeServerStream pipe))
            throw new ArgumentException("Task thread parameter must be a tuple of ulong and NamedPipeServerStream", nameof(obj));
        try
        {
            var details = CM.Client.GetModDetails(id);
            if (details.Length != 0)
            {
                long currentProgress = 0;
                bool watchProgress = false; //Non-binary progresses should not be reported
                byte[] buffer = new byte[17];
                Client.RunTasks(346110, Tasks.GetUpdateData | Tasks.ReserveDiskSpace | Tasks.Download | Tasks.Install | Tasks.FinishModInstall, new()
                {
                    PrepareProgress = (mode, total) =>
                    {
                        watchProgress = mode;
                        if (mode)
                        {
                            currentProgress = 0;
                            BitConverter.TryWriteBytes(new Span<byte>(buffer, 8, 8), total);
                            buffer[16] = 1;
                        }
                    },
                    UpdateProgress = increment =>
                    {
                        if (watchProgress)
                        {
                            currentProgress += increment;
                            BitConverter.TryWriteBytes(new Span<byte>(buffer, 0, 8), currentProgress);
                            pipe.Write(buffer);
                        }
                    }
                }, default, in details[0]);
            }
        }
        catch { }
        try
        {
            pipe.Write(stackalloc byte[0]);
            pipe.Disconnect();
        }
        catch { }
    }
    /// <summary>Listens the named pipe for incoming connections in a loop.</summary>
    /// <param name="obj">Thread parameter, for this method it's a <see cref="NamedPipeServerStream"/>.</param>
    public static void ListenPipe(object? obj)
    {
        if (obj is not NamedPipeServerStream pipe)
            throw new ArgumentException("Pipe listener thread parameter must be a NamedPipeServerStream", nameof(obj));
        Span<byte> buffer = stackalloc byte[8];
        for(;;)
        {
            try { pipe.WaitForConnection();}
            catch { break; }
            try
            {
                ulong modId;
                {
                    string filePath = string.Concat(Path.GetTempPath(), "TEKLauncherModId");
                    if (!File.Exists(filePath))
                    {
                        pipe.WriteByte(0);
                        pipe.Disconnect();
                    }
                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    stream.Read(buffer);
                    modId = BitConverter.ToUInt64(buffer);
                }
                pipe.WriteByte((byte)(modId == 0 ? 0 : 1));
                if (modId == 0)
                    pipe.Disconnect();
                else
                {
                    var thread = new Thread(TaskProcedure);
                    thread.Start((modId, pipe));
                    thread.Join();
                }
            }
            catch { }
        }
    }
}