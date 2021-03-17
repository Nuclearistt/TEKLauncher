using System;
using TEKLauncher.SteamInterop.Network;
using TEKLauncher.Utils;
using static System.GC;
using static System.IntPtr;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.Net.HTTPClient;
using static TEKLauncher.SteamInterop.Network.Logger;
using static TEKLauncher.Utils.WinAPI;

namespace TEKLauncher.Net
{
    internal class HTTPConnection
    {
        internal HTTPConnection(long Index, IntPtr Handle)
        {
            this.Index = Index;
            this.Handle = Handle;
        }
        ~HTTPConnection() => Close();
        private bool Closed;
        private int BytesRead;
        private uint ErrorCode, Status;
        private readonly long Index;
        private readonly IntPtr Handle;
        private readonly ThreadLock Lock = new ThreadLock();
        private static readonly string[] MIMETypes = new[] { "application/x-steam-chunk\0", null };
        private string GetErrorMessage(uint ErrorCode)
        {
            string Message;
            switch (ErrorCode)
            {
                case 12002U: Message = LocString(LocCode.HTTPConnTimeout); break;
                case 12007U: Message = $"{LocString(LocCode.HTTPConnTimeout)} (12007)"; break;
                case 12029U: Message = LocString(LocCode.HTTPConnFail); break;
                case 12030U: Message = LocString(LocCode.HTTPConnLost); break;
                default: Message = string.Format(LocString(LocCode.HTTPConnErrorCode), ErrorCode); break;
            }
            return Message;
        }
        internal void Close()
        {
            if (Closed)
                return;
            CloseWinHTTPHandle(Handle);
            Lock.Close();
            Closed = true;
        }
        internal void ProcessCallback(uint Status, int BytesRead, ref WinHTTPAsyncResult Result)
        {
            this.BytesRead = BytesRead;
            this.Status = Status;
            if (Status == 0x200000U)
                ErrorCode = Result.Error;
            Lock.Unlock();
        }
        internal byte[] DownloadChunk(string Object, int Size)
        {
            IntPtr Request = CreateHTTPRequest(Handle, null, Object, null, null, MIMETypes, 0x800000U);
            if (Request.ToInt64() == 0L)
            {
                string ErrorCode = GetLastError().ToString();
                Log($"Failed to create HTTP request for {Object}, error code: {ErrorCode}");
                throw new ValidatorException(ErrorCode);
            }
            if (!SendHTTPRequest(Request, null, 0U, Zero, 0U, 0U, Index))
            {
                string Message = GetErrorMessage(GetLastError());
                CloseWinHTTPHandle(Request);
                throw new ValidatorException(Message);
            }
            Lock.Lock();
            if (Status == 0x200000U)
            {
                CloseWinHTTPHandle(Request);
                throw new ValidatorException(GetErrorMessage(ErrorCode));
            }
            if (Status == 0x400000U)
            {
                if (!ReceiveHTTPResponse(Request, Zero))
                {
                    string Message = GetErrorMessage(GetLastError());
                    CloseWinHTTPHandle(Request);
                    throw new ValidatorException(Message);
                }
            }
            else
            {
                CloseWinHTTPHandle(Request);
                throw new ValidatorException(string.Format(LocString(LocCode.HTTPConnUnknownStatus), $"0x{Status:X}"));
            }
            Lock.Lock();
            byte[] Buffer;
            if (Status == 0x200000U)
            {
                CloseWinHTTPHandle(Request);
                throw new ValidatorException(GetErrorMessage(ErrorCode));
            }
            if (Status == 0x20000U)
            {
                Buffer = new byte[Size];
                int Offset = 0;
                while (Offset < Size)
                {
                    int BytesToRead = Size - Offset;
                    if (BytesToRead > 8192)
                        BytesToRead = 8192;
                    if (!ReadHTTPData(Request, ref Buffer[Offset], BytesToRead, Zero))
                    {
                        string Message = GetErrorMessage(GetLastError());
                        CloseWinHTTPHandle(Request);
                        throw new ValidatorException(Message);
                    }
                    Lock.Lock();
                    Offset += BytesRead;
                }
            }
            else
            {
                CloseWinHTTPHandle(Request);
                throw new ValidatorException(string.Format(LocString(LocCode.HTTPConnUnknownStatus), $"0x{Status:X}"));
            }
            KeepAlive(CallbackObject);
            CloseWinHTTPHandle(Request);
            return Buffer;
        }
    }
}