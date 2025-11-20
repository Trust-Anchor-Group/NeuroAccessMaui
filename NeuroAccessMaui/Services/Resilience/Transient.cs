using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NeuroAccessMaui.Services.Resilience
{
    public static class Transient
    {
        /// <summary>
        /// Heuristic transient error classification for network-bound work.
        /// </summary>
        public static bool IsTransient(Exception ex)
        {
            return ex is HttpRequestException
                || ex is SocketException
                || ex is TaskCanceledException
                || ex is TimeoutException;
        }
    }
}

