using System;

namespace TEKLauncher.SteamInterop.Network
{
    internal class ValidatorException : Exception
    {
        internal ValidatorException() : base() { }
        internal ValidatorException(string Message) : base(Message) { }
    }
}