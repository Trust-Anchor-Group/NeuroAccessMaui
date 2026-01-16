using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Waher.Runtime.Settings;
using Waher.Runtime.Inventory;

#if IOS
using CoreFoundation;
using CoreNFC;
using Foundation;
using NeuroAccessMaui.IosPlatform.Nfc;
#endif

namespace NeuroAccessMaui.Services.Nfc
{
    /// <summary>
    /// Default implementation of a user-initiated NFC passport scan flow.
    /// </summary>
    [Singleton]
    public class PassportNfcScanService : IPassportNfcScanService
    {
        private readonly INfcService nfcService = ServiceRef.Provider.GetRequiredService<INfcService>();

        private const string PassportScanRequestedKey = "NFC.Passport.ScanRequested";
        private const string PassportBacOkKey = "NFC.Passport.BacOk";
        private const string PassportLastBacAtKey = "NFC.Passport.LastBacAt";

        /// <inheritdoc />
        public Task<bool> ScanPassportAsync(string Prompt, CancellationToken CancellationToken)
        {
#if IOS
			return this.ScanPassportIosAsync(Prompt, CancellationToken);
#else
            return this.ScanPassportPassiveAsync(CancellationToken);
#endif
        }

#if !IOS
        private async Task<bool> ScanPassportPassiveAsync(CancellationToken CancellationToken)
        {
            await RuntimeSettings.SetAsync(PassportScanRequestedKey, true);
            await RuntimeSettings.DeleteAsync(PassportBacOkKey);
            await RuntimeSettings.DeleteAsync(PassportLastBacAtKey);

            try
            {
                DateTimeOffset StartedAt = DateTimeOffset.UtcNow;
                TimeSpan Timeout = TimeSpan.FromSeconds(30);

                while (!CancellationToken.IsCancellationRequested)
                {
                    object? BacOkValue = await RuntimeSettings.GetAsync(PassportBacOkKey, (object?)null);
                    if (BacOkValue is bool BacOk)
                        return BacOk;

                    if (DateTimeOffset.UtcNow - StartedAt > Timeout)
                        return false;

                    try
                    {
                        await Task.Delay(250, CancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        return false;
                    }
                }

                return false;
            }
            finally
            {
                await RuntimeSettings.DeleteAsync(PassportScanRequestedKey);
            }
        }
#endif

#if IOS
		private Task<bool> ScanPassportIosAsync(string Prompt, CancellationToken CancellationToken)
		{
			if (!NFCTagReaderSession.ReadingAvailable)
				return Task.FromResult(false);

			TaskCompletionSource<bool> TaskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

			_ = RuntimeSettings.SetAsync(PassportScanRequestedKey, true);
			_ = RuntimeSettings.DeleteAsync(PassportBacOkKey);
			_ = RuntimeSettings.DeleteAsync(PassportLastBacAtKey);

			PassportTagSessionDelegate Delegate = new(this, TaskSource);
			NFCTagReaderSession Session = new(NFCPollingOption.Iso14443, Delegate, DispatchQueue.MainQueue)
			{
				AlertMessage = Prompt ?? string.Empty
			};

			Delegate.SetSession(Session);

			if (CancellationToken.CanBeCanceled)
			{
				CancellationToken.Register(() =>
				{
					try { Session.InvalidateSession(); } catch { }
					TaskSource.TrySetResult(false);
				});
			}

			Session.BeginSession();
			return TaskSource.Task;
		}

		private sealed class PassportTagSessionDelegate : NFCTagReaderSessionDelegate
		{
			private readonly PassportNfcScanService owner;
			private readonly TaskCompletionSource<bool> taskSource;
			private NFCTagReaderSession? session;
			private int completed;

			public PassportTagSessionDelegate(PassportNfcScanService Owner, TaskCompletionSource<bool> TaskSource)
			{
				this.owner = Owner;
				this.taskSource = TaskSource;
			}

			public void SetSession(NFCTagReaderSession Session)
			{
				this.session = Session;
			}

			public override void DidInvalidate(NFCTagReaderSession Session, NSError Error)
			{
				this.taskSource.TrySetResult(false);
			}

			public override void DidDetectTags(NFCTagReaderSession Session, INFCTag[] Tags)
			{
				if (Tags is null || Tags.Length == 0)
					return;

				if (Interlocked.Exchange(ref this.completed, 1) != 0)
					return;

				_ = this.HandleTagAsync(Session, Tags[0]);
			}

			private async Task HandleTagAsync(NFCTagReaderSession Session, INFCTag Tag)
			{
				try
				{
					await this.ConnectAsync(Session, Tag);

					if (Tag.Type != NFCTagType.Iso7816Compatible)
					{
						try { Session.InvalidateSession("Unsupported tag."); } catch { }
						this.taskSource.TrySetResult(false);
						return;
					}

					INFCIso7816Tag? iso7816 = Tag.AsNFCIso7816Tag;
					if (iso7816 is null)
					{
						try { Session.InvalidateSession("Unsupported tag."); } catch { }
						this.taskSource.TrySetResult(false);
						return;
					}

					IosIsoDepTag nfcTag = new(iso7816, Session); // <-- update ctor type if needed

					using (nfcTag)
					{
						await this.owner.nfcService.TagDetected(nfcTag);
					}

					bool BacOk = await this.TryGetLastBacOkAsync();

					try { Session.InvalidateSession(); } catch { }
					this.taskSource.TrySetResult(BacOk);
				}
				catch
				{
					try { Session.InvalidateSession(); } catch { }
					this.taskSource.TrySetResult(false);
				}
				finally
				{
					try { await RuntimeSettings.DeleteAsync(PassportScanRequestedKey); } catch { }
				}
			}

			private Task ConnectAsync(NFCTagReaderSession Session, INFCTag Tag)
			{
				TaskCompletionSource<bool> TaskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
				Session.ConnectTo(Tag, (NSError? Error) =>
				{
					if (Error is not null)
						TaskSource.TrySetException(new NSErrorException(Error));
					else
						TaskSource.TrySetResult(true);
				});
				return TaskSource.Task;
			}

			private async Task<bool> TryGetLastBacOkAsync()
			{
				try
				{
					object? BacOkValue = await RuntimeSettings.GetAsync(PassportBacOkKey, (object?)null);
					return BacOkValue is bool BacOk && BacOk;
				}
				catch
				{
					return false;
				}
			}

		}
#endif
    }
}
