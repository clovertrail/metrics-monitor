using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace AzSignalR.Monitor
{
    public static class Utils
    {
        public static int Parallellism
        {
            get
            {
                // In debuger, it's better to run single threaded otherwise it's difficult to trace execution flow.
                return Debugger.IsAttached ? 1 : Environment.ProcessorCount;
            }
        }

        public static string InversedTimeKey(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }
            dateTime = dateTime.ToUniversalTime();

            // the max value is a number like xxx9999999, add 1 so we get a clear representation when the given dateTime is rounded.
            return (DateTime.MaxValue.Ticks - dateTime.Ticks + 1).ToString("d19");
        }

        public static Stream GenerateStreamFromString(string s)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(s ?? ""));
        }

        public static List<T> Shuffle<T>(IEnumerable<T> items)
        {
            var list = new List<T>(items);
            var rand = new Random();
            for (int i = 0; i < list.Count; ++i)
            {
                var pos = i + rand.Next(list.Count - i);
                var tmp = list[i];
                list[i] = list[pos];
                list[pos] = tmp;
            }
            return list;
        }
    }

    public class MixedTimeoutTokenSource : IDisposable
    {
        private readonly CancellationTokenSource _timeoutSource;
        private readonly CancellationTokenSource _linkedSource;
        private CancellationToken TimeoutToken => _timeoutSource.Token;
        public CancellationToken Token => _linkedSource?.Token ?? TimeoutToken;

        public TimeSpan Timeout { get; }
        public bool IsTimeout => TimeoutToken.IsCancellationRequested;

        public MixedTimeoutTokenSource(TimeSpan timeout, CancellationToken cancellationToken = default(CancellationToken))
        {
            Timeout = timeout;
            _timeoutSource = new CancellationTokenSource(timeout);
            if (cancellationToken != default(CancellationToken))
            {
                _linkedSource = CancellationTokenSource.CreateLinkedTokenSource(TimeoutToken, cancellationToken);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _timeoutSource.Dispose();
                    _linkedSource?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
