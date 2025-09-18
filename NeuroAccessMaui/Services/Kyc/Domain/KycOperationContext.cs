using System;
using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.Services.Kyc.Domain
{
    /// <summary>
    /// Provides debounced operation scheduling with cancellation and optional flushing.
    /// </summary>
    internal sealed class KycOperationContext : IDisposable
    {
        private readonly object syncRoot = new();
        private readonly TimeSpan delay;
        private CancellationTokenSource? pendingCts;
        private Task? pendingTask;
        private bool disposed;

        public KycOperationContext(string operationName, TimeSpan delay)
        {
            this.OperationName = operationName;
            this.delay = delay;
            this.CorrelationId = Guid.NewGuid().ToString("N");
        }

        public string OperationName { get; }

        public string CorrelationId { get; }

        public Task ScheduleAsync(Func<CancellationToken, Task> work)
        {
            if (work is null)
            {
                throw new ArgumentNullException(nameof(work));
            }

            CancellationTokenSource localCts;
            lock (this.syncRoot)
            {
                this.pendingCts?.Cancel();
                localCts = new CancellationTokenSource();
                this.pendingCts = localCts;
                this.pendingTask = this.ExecuteAsync(localCts, work);
            }

            return Task.CompletedTask;
        }

        public async Task FlushAsync(Func<CancellationToken, Task>? immediateWork)
        {
            CancellationTokenSource? local;
            Task? pending;
            lock (this.syncRoot)
            {
                local = this.pendingCts;
                pending = this.pendingTask;
                this.pendingCts = null;
                this.pendingTask = null;
            }

            try { local?.Cancel(); } catch { }

            if (pending is not null)
            {
                try { await pending.ConfigureAwait(false); } catch (TaskCanceledException) { }
            }

            if (immediateWork is not null)
            {
                using CancellationTokenSource immediate = new();
                try
                {
                    await immediateWork(immediate.Token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception ex)
                {
                    ServiceRef.LogService.LogException(ex, new KeyValuePair<string, object?>("Operation",$"KYC.Operation.{this.OperationName}.Immediate"));
                }
            }
        }

        private async Task ExecuteAsync(CancellationTokenSource cts, Func<CancellationToken, Task> work)
        {
            try
            {
                if (this.delay > TimeSpan.Zero)
                {
                    await Task.Delay(this.delay, cts.Token).ConfigureAwait(false);
                }

                if (cts.IsCancellationRequested)
                {
                    return;
                }

                await work(cts.Token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                ServiceRef.LogService.LogException(ex, new KeyValuePair<string, object?>("Operaiton",$"KYC.Operation.{this.OperationName}"));
            }
            finally
            {
                lock (this.syncRoot)
                {
                    if (this.pendingCts == cts)
                    {
                        this.pendingCts = null;
                        this.pendingTask = null;
                    }
                }

                cts.Dispose();
            }
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            CancellationTokenSource? local;
            Task? pending;
            lock (this.syncRoot)
            {
                local = this.pendingCts;
                pending = this.pendingTask;
                this.pendingCts = null;
                this.pendingTask = null;
            }

            try { local?.Cancel(); } catch { }
            local?.Dispose();
            // Pending task will observe cancellation; no need to await during dispose.
        }
    }
}
