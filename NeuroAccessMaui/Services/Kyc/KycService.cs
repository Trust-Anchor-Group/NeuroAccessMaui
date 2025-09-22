using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services.Kyc.Domain;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.Kyc.ViewModels;
using NeuroAccessMaui.Services.Resilience;
using NeuroAccessMaui.Services.Resilience.Dispatch;
using NeuroAccessMaui.Services.Fetch;
using NeuroAccessMaui.UI.MVVM;
using NeuroAccessMaui.UI.MVVM.Building;
using NeuroAccessMaui.UI.MVVM.Policies;
using Waher.Runtime.Inventory;
using Waher.Persistence;
using SkiaSharp;
using Waher.Networking.XMPP.Contracts;
using System.Diagnostics;
using System.Security.Cryptography;

namespace NeuroAccessMaui.Services.Kyc
{
	/// <summary>
	/// Service providing KYC process loading, validation, snapshotting and persistence.
	/// Implements ordered, serialized snapshot persistence to avoid data races.
	/// </summary>
	[Singleton]
	public class KycService : IKycService, IDisposable
	{
		private static readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
		private static readonly string backupKyc = "TestKYCNeuro.xml";

		private readonly System.Collections.Concurrent.ConcurrentDictionary<string, AsyncLock> referenceLocks = new();
		private readonly Channel<string> autosaveChannel;
		private readonly System.Collections.Concurrent.ConcurrentDictionary<string, AutosaveEntry> pendingAutosave = new();
		private readonly CancellationTokenSource autosaveCts = new();
		private readonly Task autosaveWorkerTask;

		// Snapshot metrics (exposed for future diagnostics)
		private long snapshotsPersisted = 0;
		private long snapshotsSkipped = 0;

		private sealed class AutosaveEntry
		{
			public AutosaveEntry(KycReference reference, KycReferenceSnapshot snapshot)
			{
				this.Reference = reference;
				this.Snapshot = snapshot;
			}
			public KycReference Reference { get; }
			public KycReferenceSnapshot Snapshot { get; }
		}

		private bool disposedValue;

		/// <summary>
		/// Creates a new instance of the <see cref="KycService"/> class.
		/// Initializes the autosave channel and background worker.
		/// </summary>
		public KycService()
		{
			this.autosaveChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
			{
				SingleReader = true,
				SingleWriter = false,
				AllowSynchronousContinuations = false
			});
			this.autosaveWorkerTask = Task.Run(this.AutosaveWorkerLoop);
		}

		/// <summary>
		/// Background worker that serially persists the latest pending snapshot per reference.
		/// </summary>
		private async Task AutosaveWorkerLoop()
		{
			ChannelReader<string> reader = this.autosaveChannel.Reader;
			while (await reader.WaitToReadAsync(this.autosaveCts.Token).ConfigureAwait(false))
			{
				while (reader.TryRead(out string? key))
				{
					if (this.autosaveCts.IsCancellationRequested)
						return;
					if (!this.pendingAutosave.TryRemove(key, out AutosaveEntry? entry))
						continue; // Coalesced away
					try
					{
						await this.SaveSnapshotAsync(entry.Reference, entry.Snapshot, false).ConfigureAwait(false);
					}
					catch (OperationCanceledException)
					{
						return;
					}
					catch (Exception ex)
					{
						ServiceRef.LogService.LogException(ex, new KeyValuePair<string, object?>("Operation", "KYC.AutosaveWorker"));
					}
				}
			}
		}

		#region Loading & Persistence

		/// <summary>
		/// Loads the single persisted <see cref="KycReference"/> (creating a new one if none exists) and ensures KYC XML is available.
		/// Optionally localizes the friendly name using the provided language.
		/// </summary>
		/// <param name="Lang">Optional language code for process localization.</param>
		/// <returns>The loaded reference.</returns>
		public async Task<KycReference> LoadKycReferenceAsync(string? Lang = null)
		{
			KycReference? reference;
			try
			{
				reference = await ServiceRef.StorageService.FindFirstDeleteRest<KycReference>();
			}
			catch (Exception findEx)
			{
				ServiceRef.LogService.LogException(findEx, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				reference = null;
			}

			if (reference is null || string.IsNullOrEmpty(reference.ObjectId))
			{
				reference = new KycReference
				{
					CreatedUtc = DateTime.UtcNow
				};
				try
				{
					await ServiceRef.StorageService.Insert(reference);
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}

				string? xml = await this.TryFetchKycXmlFromProvider();
				if (string.IsNullOrEmpty(xml))
				{
					using Stream stream = await FileSystem.OpenAppPackageFileAsync(backupKyc);
					using StreamReader reader = new(stream);
					xml = await reader.ReadToEndAsync().ConfigureAwait(false);
				}
				reference.KycXml = xml;
				reference.UpdatedUtc = DateTime.UtcNow;
				reference.FetchedUtc = DateTime.UtcNow;
			}

			// Localize friendly name if available
			try
			{
				KycProcess? process = await reference.GetProcess(Lang).ConfigureAwait(false);
				if (process?.Name is not null)
				{
					string newName = process.Name.Text;
					if (!string.Equals(reference.FriendlyName, newName, StringComparison.Ordinal))
					{
					reference.FriendlyName = newName;
						try { await ServiceRef.StorageService.Update(reference); } catch { }
					}
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
			}

			return reference;
		}

		/// <summary>
		/// Persists an existing <see cref="KycReference"/> instance (insert or update).
		/// </summary>
		/// <param name="Reference">Reference to persist.</param>
		public async Task SaveKycReferenceAsync(KycReference Reference)
		{
			try
			{
				if (string.IsNullOrEmpty(Reference.ObjectId))
					await ServiceRef.StorageService.Insert(Reference);
				else
					await ServiceRef.StorageService.Update(Reference);
			}
			catch (KeyNotFoundException)
			{
				await ServiceRef.StorageService.Insert(Reference);
			}
		}

		/// <summary>
		/// Loads available KYC references from the provider; falls back to local bundled test definition.
		/// Always returns a list (empty on failure).
		/// </summary>
		/// <param name="Lang">Optional language code.</param>
		public async Task<IReadOnlyList<KycReference>> LoadAvailableKycReferencesAsync(string? Lang = null)
		{
			try
			{
				string? xml = await this.TryFetchKycXmlFromProvider();
				if (string.IsNullOrEmpty(xml))
				{
					using Stream stream = await FileSystem.OpenAppPackageFileAsync(backupKyc);
					using StreamReader reader = new(stream);
					xml = await reader.ReadToEndAsync().ConfigureAwait(false);
				}

				KycProcess process = await KycProcessParser.LoadProcessAsync(xml, Lang).ConfigureAwait(false);
				KycReference reference = KycReference.FromProcess(process, xml, process.Name?.Text);
				return new List<KycReference> { reference };
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				return Array.Empty<KycReference>();
			}
		}

		private async Task<string?> TryFetchKycXmlFromProvider()
		{
			try
			{
				string? domain = ServiceRef.TagProfile.Domain;
				if (string.IsNullOrWhiteSpace(domain))
					return null;

				Uri uri = new($"https://{domain}/PubSub/NeuroAccessKyc/KycProcess");
				IResourceFetcher fetcher = new ResourceFetcher();
				ResourceFetchOptions options = new() { ParentId = $"KycProcess:{domain}", Permanent = false };
				ResourceResult<byte[]> result = await fetcher.GetBytesAsync(uri, options).ConfigureAwait(false);
				byte[]? bytes = result.Value;
				if (bytes is null || bytes.Length == 0)
					return null;
				return Encoding.UTF8.GetString(bytes);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				return null;
			}
 		}

		#endregion

		#region Validation
		/// <summary>
		/// Validates all visible fields on a page (including sections) using synchronous and asynchronous validators.
		/// </summary>
		/// <param name="Page">Page to validate.</param>
		/// <returns>True if all visible fields validate; otherwise false.</returns>
		public async Task<bool> ValidatePageAsync(KycPage Page)
		{
			if (Page is null)
				return false;
			IEnumerable<ObservableKycField> fields = Page.VisibleFields;
			ReadOnlyObservableCollection<KycSection> sections = Page.VisibleSections;
			if (sections is not null)
				fields = fields.Concat(sections.SelectMany(s => s.VisibleFields));
			bool ok = true;
			List<Task> tasks = new();
			foreach (ObservableKycField f in fields)
			{
				f.ForceSynchronousValidation();
				f.ValidationTask.Run();
				Task t = MainThread.InvokeOnMainThreadAsync(async () =>
				{
					await f.ValidationTask.WaitAllAsync();
					if (!f.IsValid)
						ok = false;
				});
				tasks.Add(t);
			}
			await Task.WhenAll(tasks);
			return ok;
		}

		/// <summary>
		/// Finds the first visible page index that fails validation, or -1 if all visible pages are valid.
		/// </summary>
		/// <param name="Process">KYC process.</param>
		public async Task<int> GetFirstInvalidVisiblePageIndexAsync(KycProcess Process)
		{
			if (Process is null)
				return -1;
			for (int i = 0; i < Process.Pages.Count; i++)
			{
				KycPage page = Process.Pages[i];
				if (!page.IsVisible(Process.Values))
					continue;
				if (!await this.ValidatePageAsync(page))
					return i;
			}
			return -1;
		}
		#endregion

		#region Data Preparation
		/// <summary>
		/// Builds identity properties and attachments (images) from a process, respecting visibility and transforms.
		/// </summary>
		public async Task<(IReadOnlyList<Property> Properties, IReadOnlyList<LegalIdentityAttachment> Attachments)> PreparePropertiesAndAttachmentsAsync(KycProcess Process, CancellationToken CancellationToken)
		{
			List<Property> mapped = new();
			List<LegalIdentityAttachment> attachments = new();
			foreach (KycPage page in Process.Pages)
			{
				if (!page.IsVisible(Process.Values))
					continue;
				foreach (ObservableKycField field in page.VisibleFields)
				{
					if (this.CheckAndHandleFile(Process, field, attachments))
						continue;
					foreach (Property p in await this.BuildPropertiesFromFieldAsync(Process, field, CancellationToken))
						mapped.Add(p);
				}
				foreach (KycSection section in page.AllSections)
				{
					foreach (ObservableKycField field in section.VisibleFields)
					{
						if (this.CheckAndHandleFile(Process, field, attachments))
							continue;
						foreach (Property p in await this.BuildPropertiesFromFieldAsync(Process, field, CancellationToken))
							mapped.Add(p);
					}
				}
			}
			KycOrderingComparer comparer = KycOrderingComparer.Create(Process);
			mapped.Sort(comparer.PropertyComparer);
			return (mapped, attachments);
		}

		/// <summary>
		/// Captures a snapshot and schedules an asynchronous persistence operation (coalescing by reference).
		/// </summary>
		public Task ScheduleSnapshotAsync(KycReference Reference, KycProcess Process, KycNavigationSnapshot Navigation, double Progress, string? CurrentPageId)
		{
			if (Reference is null || Process is null)
				return Task.CompletedTask;
			AsyncLock l = this.GetLockFor(Reference);
			return this.ScheduleSnapshotCoreAsync(l, Reference, Process, Navigation, Progress, CurrentPageId);
		}

		private async Task ScheduleSnapshotCoreAsync(AsyncLock l, KycReference reference, KycProcess process, KycNavigationSnapshot navigation, double progress, string? currentPageId)
		{
			await using (await l.LockAsync().ConfigureAwait(false))
			{
				KycReferenceSnapshot snapshot = CreateSnapshot(reference, process, navigation, progress, currentPageId);
				string key = reference.ObjectId ?? string.Empty;
				this.pendingAutosave[key] = new AutosaveEntry(reference, snapshot); // coalesce to latest
				_ = this.autosaveChannel.Writer.TryWrite(key);
			}
		}

		/// <summary>
		/// Captures a snapshot and persists it immediately (bypasses the autosave queue).
		/// </summary>
		public async Task FlushSnapshotAsync(KycReference Reference, KycProcess Process, KycNavigationSnapshot Navigation, double Progress, string? CurrentPageId)
		{
			if (Reference is null || Process is null)
				return;
			AsyncLock l = this.GetLockFor(Reference);
			KycReferenceSnapshot snapshot;
			await using (await l.LockAsync().ConfigureAwait(false))
			{
				snapshot = CreateSnapshot(Reference, Process, Navigation, Progress, CurrentPageId);
			}
			try
			{
				await this.SaveSnapshotAsync(Reference, snapshot, true).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex, new KeyValuePair<string, object?>("Operation", "KYC.ImmediateAutosave"));
			}
			string key = Reference.ObjectId ?? string.Empty;
			this.pendingAutosave.TryRemove(key, out _);
		}

		/// <summary>
		/// Records submission data (identity id + state) and persists the reference.
		/// </summary>
		public async Task ApplySubmissionAsync(KycReference Reference, LegalIdentity Identity)
		{
			if (Reference is null || Identity is null)
				return;
			AsyncLock l = this.GetLockFor(Reference);
			await using (await l.LockAsync().ConfigureAwait(false))
			{
				Reference.CreatedIdentityId = Identity.Id;
				Reference.CreatedIdentityState = Identity.State;
				Reference.Version++;
				Reference.UpdatedUtc = DateTime.UtcNow;
				await SaveReferenceAsync(Reference);
			}
		}

		/// <summary>
		/// Clears stored submission information from the reference.
		/// </summary>
		public async Task ClearSubmissionAsync(KycReference Reference)
		{
			if (Reference is null)
				return;
			AsyncLock l = this.GetLockFor(Reference);
			await using (await l.LockAsync().ConfigureAwait(false))
			{
				Reference.CreatedIdentityId = null;
				Reference.CreatedIdentityState = null;
				Reference.Version++;
				Reference.UpdatedUtc = DateTime.UtcNow;
				await SaveReferenceAsync(Reference);
			}
		}

		/// <summary>
		/// Records rejection metadata and invalid claim/photo information.
		/// </summary>
		public async Task ApplyRejectionAsync(KycReference Reference, string Message, string[] InvalidClaims, string[] InvalidPhotos, string? Code)
		{
			if (Reference is null)
				return;
			AsyncLock l = this.GetLockFor(Reference);
			await using (await l.LockAsync().ConfigureAwait(false))
			{
				Reference.RejectionMessage = Message;
				Reference.RejectionCode = Code;
				Reference.InvalidClaims = InvalidClaims;
				Reference.InvalidPhotos = InvalidPhotos;
				Reference.Version++;
				Reference.UpdatedUtc = DateTime.UtcNow;
				await SaveReferenceAsync(Reference);
			}
		}

		/// <summary>
		/// Resets a reference for a new application session, optionally seeding field values.
		/// </summary>
		public async Task PrepareReferenceForNewApplicationAsync(KycReference Reference, string? Language, IReadOnlyList<KycFieldValue>? SeedFields)
		{
			if (Reference is null)
				return;
			AsyncLock l = this.GetLockFor(Reference);
			await using (await l.LockAsync().ConfigureAwait(false))
			{
				Reference.LastVisitedMode = "Form";
				Reference.LastVisitedPageId = null;
				Reference.RejectionMessage = null;
				Reference.RejectionCode = null;
				Reference.InvalidClaims = null;
				Reference.InvalidPhotos = null;
				Reference.InvalidClaimDetails = null;
				Reference.InvalidPhotoDetails = null;
				Reference.CreatedIdentityId = null;
				Reference.CreatedIdentityState = null;
				if (SeedFields is not null && SeedFields.Count > 0)
					Reference.Fields = SeedFields.Select(field => new KycFieldValue(field.FieldId, field.Value)).ToArray();
				else
					Reference.Fields = null;
				Reference.Version++;
				Reference.UpdatedUtc = DateTime.UtcNow;
				await SaveReferenceAsync(Reference);
			}

			if (SeedFields is not null && SeedFields.Count > 0 && !string.IsNullOrWhiteSpace(Language))
			{
				try
				{
					await Reference.ApplyFieldsToProcessAsync(Language);
				}
				catch (Exception exception)
				{
					ServiceRef.LogService.LogException(exception, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}
			}
		}

		/// <summary>
		/// Creates an immutable snapshot of current process state and updates the mutable reference for in-process readers.
		/// </summary>
		private static KycReferenceSnapshot CreateSnapshot(KycReference Reference, KycProcess Process, KycNavigationSnapshot Navigation, double Progress, string? CurrentPageId)
		{
			Reference.Version++;
			KycFieldValue[] fields = [.. Process.Values.Select(pair => new KycFieldValue(pair.Key, pair.Value))];
			string? lastVisitedPageId = ResolveLastVisitedPageId(Reference, Process, Navigation, CurrentPageId);
			string mode = ResolveMode(Navigation);
			DateTime now = DateTime.UtcNow;
			Reference.UpdatedUtc = now;
			Reference.Fields = fields;
			Reference.Progress = Progress;
			Reference.LastVisitedPageId = lastVisitedPageId;
			Reference.LastVisitedMode = mode;

			KycReferenceSnapshot snapshot = new KycReferenceSnapshot(
				Reference.ObjectId,
				Reference.Version,
				fields,
				Progress,
				lastVisitedPageId,
				mode,
				Reference.RejectionMessage,
				Reference.RejectionCode,
				Reference.InvalidClaims,
				Reference.InvalidPhotos,
				Reference.CreatedUtc,
				now);

			try
			{
				ServiceRef.LogService.LogDebug("KycSnapshotCreated",
					new KeyValuePair<string, object?>("ReferenceId", Reference.ObjectId ?? string.Empty),
					new KeyValuePair<string, object?>("Version", snapshot.Version),
					new KeyValuePair<string, object?>("FieldCount", fields.Length));
			}
			catch { }

			return snapshot;
		}

		private AsyncLock GetLockFor(KycReference Reference)
		{
			string key = Reference.ObjectId ?? string.Empty;
			return this.referenceLocks.GetOrAdd(key, _ => new AsyncLock());
		}

		private static string ResolveMode(KycNavigationSnapshot Navigation)
		{
			if (Navigation.State == KycFlowState.Summary || Navigation.State == KycFlowState.PendingSummary || Navigation.State == KycFlowState.RejectedSummary)
				return "Summary";
			return "Form";
		}

		private static string? ResolveLastVisitedPageId(KycReference Reference, KycProcess Process, KycNavigationSnapshot Navigation, string? CurrentPageId)
		{
			if (!string.IsNullOrWhiteSpace(CurrentPageId))
				return CurrentPageId;
			if (Navigation.CurrentPageIndex >= 0 && Navigation.CurrentPageIndex < Process.Pages.Count)
				return Process.Pages[Navigation.CurrentPageIndex].Id;
			return Reference.LastVisitedPageId;
		}

		/// <summary>
		/// Persists a snapshot if not stale, updating the mutable reference. Logs timing and hash metrics.
		/// </summary>
		private async Task SaveSnapshotAsync(KycReference Reference, KycReferenceSnapshot Snapshot, bool IsImmediate)
		{
			Stopwatch sw = Stopwatch.StartNew();
			try
			{
				ServiceRef.LogService.LogDebug("KycSnapshotPersistAttempt",
					new KeyValuePair<string, object?>("ReferenceId", Reference.ObjectId ?? string.Empty),
					new KeyValuePair<string, object?>("Version", Snapshot.Version),
					new KeyValuePair<string, object?>("IsImmediate", IsImmediate));

				// Stale check
				if (Snapshot.Version < Reference.Version)
				{
					Interlocked.Increment(ref this.snapshotsSkipped);
					ServiceRef.LogService.LogDebug("KycSnapshotStaleSkipped",
						new KeyValuePair<string, object?>("ReferenceId", Reference.ObjectId ?? string.Empty),
						new KeyValuePair<string, object?>("SnapshotVersion", Snapshot.Version),
						new KeyValuePair<string, object?>("CurrentVersion", Reference.Version));
					return;
				}

				if (Reference.Fields != Snapshot.Fields)
					Reference.Fields = Snapshot.Fields;
				Reference.Progress = Snapshot.Progress;
				Reference.LastVisitedPageId = Snapshot.LastVisitedPageId;
				Reference.LastVisitedMode = Snapshot.LastVisitedMode;
				Reference.RejectionMessage = Snapshot.RejectionMessage;
				Reference.RejectionCode = Snapshot.RejectionCode;
				Reference.InvalidClaims = Snapshot.InvalidClaims;
				Reference.InvalidPhotos = Snapshot.InvalidPhotos;
				Reference.UpdatedUtc = Snapshot.UpdatedUtc;
				await SaveReferenceAsync(Reference).ConfigureAwait(false);

				Interlocked.Increment(ref this.snapshotsPersisted);
				string hash = ComputeFieldsHash(Snapshot.Fields);
				ServiceRef.LogService.LogInformational("KycSnapshotPersisted",
					new KeyValuePair<string, object?>("ReferenceId", Reference.ObjectId ?? string.Empty),
					new KeyValuePair<string, object?>("Version", Snapshot.Version),
					new KeyValuePair<string, object?>("FieldCount", Snapshot.Fields?.Length ?? 0),
					new KeyValuePair<string, object?>("DurationMs", sw.ElapsedMilliseconds),
					new KeyValuePair<string, object?>("Hash", hash));
			}
			finally
			{
				sw.Stop();
			}
		}

		private static string ComputeFieldsHash(KycFieldValue[]? Fields)
		{
			if (Fields is null || Fields.Length == 0)
				return "0";
			try
			{
				using SHA256 sha = SHA256.Create();
				foreach (KycFieldValue f in Fields.OrderBy(f => f.FieldId, StringComparer.Ordinal))
				{
					string line = f.FieldId + "=" + (f.Value ?? string.Empty) + "\n";
					byte[] bytes = Encoding.UTF8.GetBytes(line);
					sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
				}
				sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
				return Convert.ToHexString(sha.Hash);
			}
			catch
			{
				return "ERR";
			}
		}

		private static async Task SaveReferenceAsync(KycReference Reference)
		{
			try
			{
				if (string.IsNullOrEmpty(Reference.ObjectId))
				{
					await ServiceRef.StorageService.Insert(Reference);
				}
				else
				{
					await ServiceRef.StorageService.Update(Reference);
				}
			}
			catch (KeyNotFoundException)
			{
				await ServiceRef.StorageService.Insert(Reference);
			}
		}

		private async Task<List<Property>> BuildPropertiesFromFieldAsync(KycProcess Process, ObservableKycField Field, CancellationToken Ct)
		{
			List<Property> result = new();
			if (Field.Mappings.Count == 0)
				return result;
			if (Field.Condition is not null && !Field.Condition.Evaluate(Process.Values))
				return result;
			string baseValue = Field.StringValue?.Trim() ?? string.Empty;
			if (string.IsNullOrEmpty(baseValue))
				return result;
			foreach (KycMapping map in Field.Mappings)
			{
				if (string.IsNullOrEmpty(map.Key))
					continue;
				string current = baseValue;
				foreach (string name in map.TransformNames)
				{
					if (string.IsNullOrWhiteSpace(name))
						continue;
					if (NeuroAccessMaui.Services.Kyc.Transforms.KycTransformRegistry.TryGet(name, out NeuroAccessMaui.Services.Kyc.Transforms.IKycTransform transform))
					{
						try { current = await transform.ApplyAsync(Field, Process, current, Ct); }
						catch (Exception ex2) { ServiceRef.LogService.LogException(ex2); }
					}
					if (string.IsNullOrEmpty(current))
						break;
				}
				if (!string.IsNullOrEmpty(current))
					result.Add(new Property(map.Key, current));
			}
			return result;
		}

		private bool CheckAndHandleFile(KycProcess Process, ObservableKycField Field, List<LegalIdentityAttachment> List)
		{
			if (Field.Condition is not null && (Field.Mappings.Count == 0 || !Field.Condition.Evaluate(Process.Values)))
				return false;
			if (!string.IsNullOrEmpty(Field.StringValue) && Field is ObservableImageField imageField)
			{
				byte[]? data = imageField.StringValue is null ? null : this.CompressImage(this.Base64ToStream(imageField.StringValue));
				if (data is not null)
					List.Add(new LegalIdentityAttachment(imageField.Mappings.First().Key + ".jpg", Constants.MimeTypes.Jpeg, data));
				return true;
			}
			return false;
		}

		private MemoryStream Base64ToStream(string Base64)
		{
			byte[] bytes = Convert.FromBase64String(Base64);
			return new MemoryStream(bytes);
		}

		private byte[]? CompressImage(Stream InputStream)
		{
			try
			{
				using SKManagedStream ms = new(InputStream);
				using SKData imgData = SKData.Create(ms);
				using SKCodec codec = SKCodec.Create(imgData);
				SKBitmap bmp = SKBitmap.Decode(imgData);
				bmp = this.HandleOrientation(bmp, codec.EncodedOrigin);
				bool resize = false;
				int h = bmp.Height; int w = bmp.Width;
				if (w >= h && w > 1920) { h = (int)(h * (1920.0 / w) + 0.5); w = 1920; resize = true; }
				else if (h > w && h > 1920) { w = (int)(w * (1920.0 / h) + 0.5); h = 1920; resize = true; }
				if (resize)
				{
					SKImageInfo info = bmp.Info;
					SKImageInfo ni = new(w, h, info.ColorType, info.AlphaType, info.ColorSpace);
					SKBitmap? resized = bmp.Resize(ni, SKFilterQuality.High);
					if (resized is not null) { bmp.Dispose(); bmp = resized; }
				}
				byte[] bytes;
				using (SKData encoded = bmp.Encode(SKEncodedImageFormat.Jpeg, 80)) bytes = encoded.ToArray();
				bmp.Dispose();
				return bytes;
			}
			catch (Exception ex) { ServiceRef.LogService.LogException(ex); return null; }
		}

		private SKBitmap HandleOrientation(SKBitmap bmp, SKEncodedOrigin o)
		{
			SKBitmap rotated;
			switch (o)
			{
				case SKEncodedOrigin.BottomRight:
					rotated = new SKBitmap(bmp.Width, bmp.Height);
					using (SKCanvas canvas = new(rotated)) { canvas.RotateDegrees(180, bmp.Width / 2, bmp.Height / 2); canvas.DrawBitmap(bmp, 0, 0); }
					break;
				case SKEncodedOrigin.RightTop:
					rotated = new SKBitmap(bmp.Height, bmp.Width);
					using (SKCanvas canvas = new(rotated)) { canvas.Translate(rotated.Width, 0); canvas.RotateDegrees(90); canvas.DrawBitmap(bmp, 0, 0); }
					break;
				case SKEncodedOrigin.LeftBottom:
					rotated = new SKBitmap(bmp.Height, bmp.Width);
					using (SKCanvas canvas = new(rotated)) { canvas.Translate(0, rotated.Height); canvas.RotateDegrees(270); canvas.DrawBitmap(bmp, 0, 0); }
					break;
				default: return bmp;
			}
			return rotated;
		}

		#endregion // Data Preparation

		/// <summary>
		/// Flushes any pending coalesced snapshots by forcing immediate persistence.
		/// </summary>
		public async Task FlushAsync(CancellationToken cancellationToken = default)
		{
			foreach (KeyValuePair<string, AutosaveEntry> pair in this.pendingAutosave.ToArray())
			{
				if (cancellationToken.IsCancellationRequested)
					break;
				if (this.pendingAutosave.TryRemove(pair.Key, out AutosaveEntry? entry))
				{
					try { await this.SaveSnapshotAsync(entry.Reference, entry.Snapshot, true).ConfigureAwait(false); }
					catch (Exception ex) { ServiceRef.LogService.LogException(ex, new KeyValuePair<string, object?>("Operation", "KYC.FlushAutosave")); }
				}
			}
		}

		/// <summary>
		/// Performs an orderly shutdown of the service, stopping the worker and flushing snapshots.
		/// </summary>
		public async Task ShutdownAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				this.autosaveCts.Cancel();
				this.autosaveChannel.Writer.TryComplete();
				await Task.WhenAny(this.autosaveWorkerTask, Task.Delay(TimeSpan.FromSeconds(2), cancellationToken)).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
			await this.FlushAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Releases resources used by the service.
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			if (this.disposedValue)
				return;
			if (disposing)
			{
				try
				{
					this.autosaveCts.Cancel();
					this.autosaveChannel.Writer.TryComplete();
					this.autosaveWorkerTask.Wait(TimeSpan.FromSeconds(2));
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}
			}
			this.disposedValue = true;
		}

		/// <summary>
		/// Disposes the service.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Returns current snapshot persistence metrics.
		/// </summary>
		internal (long Persisted, long Skipped) GetSnapshotMetrics() => (Interlocked.Read(ref this.snapshotsPersisted), Interlocked.Read(ref this.snapshotsSkipped));
	}
}
