using System;

namespace Chronex.Services.Networking
{
    public sealed class NetworkServiceException : Exception
    {
        public NetworkServiceException(string message) : base(message) { }
        public NetworkServiceException(string message, Exception inner) : base(message, inner) { }
    }
}