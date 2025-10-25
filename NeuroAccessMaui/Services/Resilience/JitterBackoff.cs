using System;

namespace NeuroAccessMaui.Services.Resilience
{
    public static class JitterBackoff
    {
        private static readonly Random Rng = new Random();

        /// <summary>
        /// Decorrelated jitter backoff ("full jitter") based on attempt number.
        /// </summary>
        public static TimeSpan DecorrelatedJitter(TimeSpan baseDelay, int attempt, TimeSpan? maxDelay = null)
        {
            if (attempt < 1) attempt = 1;
            double capMs = (maxDelay ?? TimeSpan.FromSeconds(30)).TotalMilliseconds;
            double sleepMs = Math.Min(capMs, baseDelay.TotalMilliseconds * Math.Pow(2, attempt));
            double jitter = Rng.NextDouble() * sleepMs;
            return TimeSpan.FromMilliseconds(jitter);
        }
    }
}

