using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
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
using NeuroAccessMaui.Services.Xmpp;
using NeuroAccessMaui.UI.MVVM;
using NeuroAccessMaui.UI.MVVM.Building;
using NeuroAccessMaui.UI.MVVM.Policies;
using Waher.Runtime.Inventory;
using Waher.Persistence;
using SkiaSharp;
using Waher.Networking.XMPP.Contracts;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Xml;
using Microsoft.Maui.Storage;
using Waher.Networking.XMPP.PubSub;
using Waher.Networking.XMPP.ResultSetManagement;

namespace NeuroAccessMaui.Services.Kyc
{
	/// <summary>
	/// Service providing KYC process loading, validation, snapshotting and persistence.
	/// Implements ordered, serialized snapshot persistence to avoid data races.
	/// </summary>
	[Singleton]
	public class KycService : IKycService, IDisposable
	{
		private static readonly string backupKyc = "TestKYCNeuro.xml";
		private const string KycTemplateNodeId = "NeuroAccessKyc";
		private const int DefaultTemplatePageSize = 20;

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
			ChannelReader<string> Reader = this.autosaveChannel.Reader;
			while (await Reader.WaitToReadAsync(this.autosaveCts.Token).ConfigureAwait(false))
			{
				while (Reader.TryRead(out string? Key))
				{
					if (this.autosaveCts.IsCancellationRequested)
						return;
					if (!this.pendingAutosave.TryRemove(Key, out AutosaveEntry? Entry))
						continue; // Coalesced away
					try
					{
						await this.SaveSnapshotAsync(Entry.Reference, Entry.Snapshot, false).ConfigureAwait(false);
					}
					catch (OperationCanceledException)
					{
						return;
					}
					catch (Exception Ex)
					{
						ServiceRef.LogService.LogException(Ex, new KeyValuePair<string, object?>("Operation", "KYC.AutosaveWorker"));
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
		public async Task<KycReference> LoadKycReferenceAsync(string? Lang = null, KycApplicationTemplate? Template = null)
		{
			KycReference? LoadedReference;
			try
			{
				LoadedReference = await ServiceRef.StorageService.FindFirstDeleteRest<KycReference>();
			}
			catch (Exception FindEx)
			{
				ServiceRef.LogService.LogException(FindEx, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				LoadedReference = null;
			}

			if (LoadedReference is null || string.IsNullOrEmpty(LoadedReference.ObjectId))
			{
				LoadedReference = new KycReference
				{
					CreatedUtc = DateTime.UtcNow
				};
				try
				{
					await ServiceRef.StorageService.Insert(LoadedReference);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}
			}

			KycApplicationTemplate? TemplateToApply = Template;
			if (TemplateToApply is null && string.IsNullOrEmpty(LoadedReference.KycXml))
			{
				try
				{
					KycApplicationPage TemplatePage = await this.LoadKycApplicationsPageAsync(null, null, null, 1, Lang).ConfigureAwait(false);
					TemplateToApply = TemplatePage.Templates.FirstOrDefault();
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}
			}

			if (TemplateToApply is not null)
			{
				try
				{
					await this.ApplyTemplateToReferenceAsync(LoadedReference, TemplateToApply, Lang).ConfigureAwait(false);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}
			}

			if (string.IsNullOrEmpty(LoadedReference.KycXml))
			{
				try
				{
					await this.ApplyBundledTemplateAsync(LoadedReference, Lang).ConfigureAwait(false);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}
			}

			// Localize friendly name if available
			try
			{
				KycProcess? Process = await LoadedReference.GetProcess(Lang).ConfigureAwait(false);
				if (Process?.Name is not null)
				{
					string NewName = Process.Name.Text;
					if (!string.Equals(LoadedReference.FriendlyName, NewName, StringComparison.Ordinal))
					{
						LoadedReference.FriendlyName = NewName;
						try { await ServiceRef.StorageService.Update(LoadedReference); } catch { }
					}
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
			}

			return LoadedReference;
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
				KycApplicationPage Page = await this.LoadKycApplicationsPageAsync(null, null, null, null, Lang).ConfigureAwait(false);
				List<KycReference> References = new List<KycReference>();
				foreach (KycApplicationTemplate Template in Page.Templates)
				{
					References.Add(Template.Reference);
				}
				return References;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				return Array.Empty<KycReference>();
			}
		}

		public async Task<KycApplicationPage> LoadKycApplicationsPageAsync(string? After = null, string? Before = null, int? Index = null, int? Max = null, string? Lang = null, CancellationToken CancellationToken = default)
		{
			int PageSize = Max.HasValue && Max.Value > 0 ? Max.Value : DefaultTemplatePageSize;
			string Domain = ServiceRef.TagProfile.Domain ?? string.Empty;
			string? ServiceAddress = ServiceRef.TagProfile.PubSubJid;

			try
			{
				PubSubPageResult? PageResult = await ServiceRef.XmppService.GetItemsPageAsync(KycTemplateNodeId, ServiceAddress, After, Before, Index, PageSize).ConfigureAwait(false);
				if (PageResult is null && !string.IsNullOrWhiteSpace(ServiceAddress))
				{
					PageResult = await ServiceRef.XmppService.GetItemsPageAsync(KycTemplateNodeId, null, After, Before, Index, PageSize).ConfigureAwait(false);
				}
				if (PageResult is null)
				{
					IReadOnlyList<KycApplicationTemplate> FallbackTemplates = await this.LoadFallbackTemplatesAsync(Lang, CancellationToken).ConfigureAwait(false);
					return new KycApplicationPage(FallbackTemplates, null, true);
				}

				IReadOnlyList<KycApplicationTemplate> Templates = await this.ConvertToTemplatesAsync(PageResult.Items, Domain, Lang, CancellationToken).ConfigureAwait(false);
				if (Templates.Count == 0)
				{
					IReadOnlyList<KycApplicationTemplate> FallbackTemplates = await this.LoadFallbackTemplatesAsync(Lang, CancellationToken).ConfigureAwait(false);
					return new KycApplicationPage(FallbackTemplates, PageResult.ResultPage, true);
				}

				return new KycApplicationPage(Templates, PageResult.ResultPage, false);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				IReadOnlyList<KycApplicationTemplate> FallbackTemplates = await this.LoadFallbackTemplatesAsync(Lang, CancellationToken).ConfigureAwait(false);
				return new KycApplicationPage(FallbackTemplates, null, true);
			}
		}

		private async Task<IReadOnlyList<KycApplicationTemplate>> ConvertToTemplatesAsync(PubSubItem[]? Items, string Domain, string? Lang, CancellationToken CancellationToken)
		{
			List<KycApplicationTemplate> Templates = new List<KycApplicationTemplate>();
			if (Items is null || Items.Length == 0)
				return Templates;

			foreach (PubSubItem Item in Items)
			{
				CancellationToken.ThrowIfCancellationRequested();

				if (!KycApplicationItem.TryCreate(Item, out KycApplicationItem? Application) || Application is null)
					continue;

				try
				{
					KycApplicationTemplate? Template = await this.CreateTemplateAsync(Application, Application.ProcessXml, Domain, Lang, CancellationToken).ConfigureAwait(false);
					if (Template is not null)
						Templates.Add(Template);
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}
			}

			return Templates;
		}

		private async Task<KycApplicationTemplate?> CreateTemplateAsync(KycApplicationItem? Application, string Xml, string Domain, string? Lang, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();
			bool IsValid = await this.ValidateKycXmlAsync(string.IsNullOrWhiteSpace(Domain) ? "unknown" : Domain, Xml).ConfigureAwait(false);
			if (!IsValid)
				return null;

			CancellationToken.ThrowIfCancellationRequested();
			KycProcess Process = await KycProcessParser.LoadProcessAsync(Xml, Lang).ConfigureAwait(false);
				string? FriendlyName = Process.Name?.PrimaryText;
				if (string.IsNullOrWhiteSpace(FriendlyName) && Application is not null)
					FriendlyName = Application.PrimaryDisplayName ?? Application.DisplayName;
				if (string.IsNullOrWhiteSpace(FriendlyName) && Application is not null)
					FriendlyName = Application.ItemId;
			if (string.IsNullOrWhiteSpace(FriendlyName))
				FriendlyName = "KYC Application";

			KycReference Reference = KycReference.FromProcess(Process, Xml, FriendlyName);
			return new KycApplicationTemplate(Reference, Application);
		}

		private async Task<IReadOnlyList<KycApplicationTemplate>> LoadFallbackTemplatesAsync(string? Lang, CancellationToken CancellationToken)
		{
			List<KycApplicationTemplate> Templates = new List<KycApplicationTemplate>();
			try
			{
				string Xml = await this.LoadBundledKycXmlAsync(CancellationToken).ConfigureAwait(false);
				KycApplicationTemplate? Template = await this.CreateTemplateAsync(null, Xml, "fallback", Lang, CancellationToken).ConfigureAwait(false);
				if (Template is not null)
					Templates.Add(Template);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
			}

			return Templates;
		}

		private async Task<string> LoadBundledKycXmlAsync(CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();
			using Stream Stream = await FileSystem.OpenAppPackageFileAsync(backupKyc);
			using StreamReader Reader = new(Stream);
			string Xml = await Reader.ReadToEndAsync(CancellationToken).ConfigureAwait(false);
			return Xml;
		}

		private async Task<bool> ValidateKycXmlAsync(string Domain, string Xml)
		{
			try
			{
				bool Valid = await ServiceRef.XmlSchemaValidationService.ValidateAsync(Constants.Schemes.NeuroAccessKycProcessUrl, Xml).ConfigureAwait(false);
				if (Valid)
				{
					ServiceRef.LogService.LogDebug("KycXmlValidationPrimarySuccess", new KeyValuePair<string, object?>("Domain", Domain));
					return true;
				}

				bool LegacyValid = await ServiceRef.XmlSchemaValidationService.ValidateAsync(Constants.Schemes.KYCProcess, Xml).ConfigureAwait(false);
				if (LegacyValid)
				{
					ServiceRef.LogService.LogInformational(
						"KycXmlValidationFallbackSuccess",
						new KeyValuePair<string, object?>("Domain", Domain),
						new KeyValuePair<string, object?>("LegacyKey", Constants.Schemes.KYCProcess));
					return true;
				}

				ServiceRef.LogService.LogWarning(
					"KycXmlValidationBothFailed",
					new KeyValuePair<string, object?>("Domain", Domain),
					new KeyValuePair<string, object?>("PrimaryKey", Constants.Schemes.NeuroAccessKycProcessUrl),
					new KeyValuePair<string, object?>("LegacyKey", Constants.Schemes.KYCProcess));
				return false;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				return false;
			}
		}

		private async Task ApplyTemplateToReferenceAsync(KycReference Reference, KycApplicationTemplate Template, string? Lang)
		{
			if (Template.Reference.KycXml is null)
				return;

			string Xml = Template.Reference.KycXml;
			KycProcess? Process = await Template.Reference.GetProcess(Lang).ConfigureAwait(false);
			if (Process is null)
				Process = await KycProcessParser.LoadProcessAsync(Xml, Lang).ConfigureAwait(false);

			Reference.SetProcess(Process, Xml, DateTime.UtcNow, DateTime.UtcNow);
				string? FriendlyName = Process.Name?.PrimaryText ?? Template.Reference.FriendlyName;
				if (!string.IsNullOrWhiteSpace(FriendlyName))
					Reference.FriendlyName = FriendlyName;
		}

		private async Task ApplyBundledTemplateAsync(KycReference Reference, string? Lang)
		{
			string Xml = await this.LoadBundledKycXmlAsync(default).ConfigureAwait(false);
			KycProcess Process = await KycProcessParser.LoadProcessAsync(Xml, Lang).ConfigureAwait(false);
			Reference.SetProcess(Process, Xml, DateTime.UtcNow, DateTime.UtcNow);
			string? FriendlyName = Process.Name?.Text;
			if (!string.IsNullOrWhiteSpace(FriendlyName))
				Reference.FriendlyName = FriendlyName;
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
			IEnumerable<ObservableKycField> Fields = Page.VisibleFields;
			ReadOnlyObservableCollection<KycSection> Sections = Page.VisibleSections;
			if (Sections is not null)
				Fields = Fields.Concat(Sections.SelectMany(S => S.VisibleFields));
			bool Ok = true;
			List<Task> Tasks = new();
			foreach (ObservableKycField Field in Fields)
			{
				Field.ForceSynchronousValidation();
				Field.ValidationTask.Run();
				Task T = MainThread.InvokeOnMainThreadAsync(async () =>
				{
					await Field.ValidationTask.WaitAllAsync();
					if (!Field.IsValid)
						Ok = false;
				});
				Tasks.Add(T);
			}
			await Task.WhenAll(Tasks);
			return Ok;
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
				KycPage Page = Process.Pages[i];
				if (!Page.IsVisible(Process.Values))
					continue;
				if (!await this.ValidatePageAsync(Page))
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
			List<Property> Mapped = new();
			List<LegalIdentityAttachment> Attachments = new();
			foreach (KycPage Page in Process.Pages)
			{
				if (!Page.IsVisible(Process.Values))
					continue;
				foreach (ObservableKycField Field in Page.VisibleFields)
				{
					if (this.CheckAndHandleFile(Process, Field, Attachments))
						continue;
					foreach (Property P in await this.BuildPropertiesFromFieldAsync(Process, Field, CancellationToken))
						Mapped.Add(P);
				}
				foreach (KycSection Section in Page.AllSections)
				{
					foreach (ObservableKycField Field in Section.VisibleFields)
					{
						if (this.CheckAndHandleFile(Process, Field, Attachments))
							continue;
						foreach (Property P in await this.BuildPropertiesFromFieldAsync(Process, Field, CancellationToken))
							Mapped.Add(P);
					}
				}
			}
			KycOrderingComparer Comparer = KycOrderingComparer.Create(Process);
			Mapped.Sort(Comparer.PropertyComparer);
			return (Mapped, Attachments);
		}

		/// <summary>
		/// Captures a snapshot and schedules an asynchronous persistence operation (coalescing by reference).
		/// </summary>
		public Task ScheduleSnapshotAsync(KycReference Reference, KycProcess Process, KycNavigationSnapshot Navigation, double Progress, string? CurrentPageId)
		{
			if (Reference is null || Process is null)
				return Task.CompletedTask;
			AsyncLock Lock = this.GetLockFor(Reference);
			return this.ScheduleSnapshotCoreAsync(Lock, Reference, Process, Navigation, Progress, CurrentPageId);
		}

		private async Task ScheduleSnapshotCoreAsync(AsyncLock Lock, KycReference Reference, KycProcess Process, KycNavigationSnapshot Navigation, double Progress, string? CurrentPageId)
		{
			await using (await Lock.LockAsync().ConfigureAwait(false))
			{
				KycReferenceSnapshot Snapshot = CreateSnapshot(Reference, Process, Navigation, Progress, CurrentPageId);
				string Key = Reference.ObjectId ?? string.Empty;
				this.pendingAutosave[Key] = new AutosaveEntry(Reference, Snapshot); // coalesce to latest
				_ = this.autosaveChannel.Writer.TryWrite(Key);
			}
		}

		/// <summary>
		/// Captures a snapshot and persists it immediately (bypasses the autosave queue).
		/// </summary>
		public async Task FlushSnapshotAsync(KycReference Reference, KycProcess Process, KycNavigationSnapshot Navigation, double Progress, string? CurrentPageId)
		{
			if (Reference is null || Process is null)
				return;
			AsyncLock Lock = this.GetLockFor(Reference);
			KycReferenceSnapshot Snapshot;
			await using (await Lock.LockAsync().ConfigureAwait(false))
			{
				Snapshot = CreateSnapshot(Reference, Process, Navigation, Progress, CurrentPageId);
			}
			try
			{
				await this.SaveSnapshotAsync(Reference, Snapshot, true).ConfigureAwait(false);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex, new KeyValuePair<string, object?>("Operation", "KYC.ImmediateAutosave"));
			}
			string Key = Reference.ObjectId ?? string.Empty;
			this.pendingAutosave.TryRemove(Key, out _);
		}

		/// <summary>
		/// Records submission data (identity id + state) and persists the reference.
		/// </summary>
		public async Task ApplySubmissionAsync(KycReference Reference, LegalIdentity Identity)
		{
			if (Reference is null || Identity is null)
				return;
			AsyncLock Lock = this.GetLockFor(Reference);
			await using (await Lock.LockAsync().ConfigureAwait(false))
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
			AsyncLock Lock = this.GetLockFor(Reference);
			await using (await Lock.LockAsync().ConfigureAwait(false))
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
			AsyncLock Lock = this.GetLockFor(Reference);
			await using (await Lock.LockAsync().ConfigureAwait(false))
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
			AsyncLock Lock = this.GetLockFor(Reference);
			await using (await Lock.LockAsync().ConfigureAwait(false))
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
					Reference.Fields = SeedFields.Select(Field => new KycFieldValue(Field.FieldId, Field.Value)).ToArray();
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
				catch (Exception Exception)
				{
					ServiceRef.LogService.LogException(Exception, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}
			}
		}

		/// <summary>
		/// Creates an immutable snapshot of current process state and updates the mutable reference for in-process readers.
		/// </summary>
		private static KycReferenceSnapshot CreateSnapshot(KycReference Reference, KycProcess Process, KycNavigationSnapshot Navigation, double Progress, string? CurrentPageId)
		{
			Reference.Version++;
			KycFieldValue[] Fields = [.. Process.Values.Select(Pair => new KycFieldValue(Pair.Key, Pair.Value))];
			string? LastVisitedPageId = ResolveLastVisitedPageId(Reference, Process, Navigation, CurrentPageId);
			string Mode = ResolveMode(Navigation);
			DateTime Now = DateTime.UtcNow;
			Reference.UpdatedUtc = Now;
			Reference.Fields = Fields;
			Reference.Progress = Progress;
			Reference.LastVisitedPageId = LastVisitedPageId;
			Reference.LastVisitedMode = Mode;

			KycReferenceSnapshot Snapshot = new KycReferenceSnapshot(
				Reference.ObjectId,
				Reference.Version,
				Fields,
				Progress,
				LastVisitedPageId,
				Mode,
				Reference.RejectionMessage,
				Reference.RejectionCode,
				Reference.InvalidClaims,
				Reference.InvalidPhotos,
				Reference.CreatedUtc,
				Now);

			try
			{
				ServiceRef.LogService.LogDebug("KycSnapshotCreated",
					new KeyValuePair<string, object?>("ReferenceId", Reference.ObjectId ?? string.Empty),
					new KeyValuePair<string, object?>("Version", Snapshot.Version),
					new KeyValuePair<string, object?>("FieldCount", Fields.Length));
			}
			catch { }

			return Snapshot;
		}

		private AsyncLock GetLockFor(KycReference Reference)
		{
			string Key = Reference.ObjectId ?? string.Empty;
			return this.referenceLocks.GetOrAdd(Key, _ => new AsyncLock());
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
			Stopwatch Sw = Stopwatch.StartNew();
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
				string Hash = ComputeFieldsHash(Snapshot.Fields);
				ServiceRef.LogService.LogInformational("KycSnapshotPersisted",
					new KeyValuePair<string, object?>("ReferenceId", Reference.ObjectId ?? string.Empty),
					new KeyValuePair<string, object?>("Version", Snapshot.Version),
					new KeyValuePair<string, object?>("FieldCount", Snapshot.Fields?.Length ?? 0),
					new KeyValuePair<string, object?>("DurationMs", Sw.ElapsedMilliseconds),
					new KeyValuePair<string, object?>("Hash", Hash));
			}
			finally
			{
				Sw.Stop();
			}
		}

		private static string ComputeFieldsHash(KycFieldValue[]? Fields)
		{
			if (Fields is null || Fields.Length == 0)
				return "0";
			try
			{
				using SHA256 Sha = SHA256.Create();
				foreach (KycFieldValue F in Fields.OrderBy(f => f.FieldId, StringComparer.Ordinal))
				{
					string Line = F.FieldId + "=" + (F.Value ?? string.Empty) + "\n";
					byte[] Bytes = Encoding.UTF8.GetBytes(Line);
					Sha.TransformBlock(Bytes, 0, Bytes.Length, null, 0);
				}
				Sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
				return Convert.ToHexString(Sha.Hash);
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
			List<Property> Result = new();
			if (Field.Mappings.Count == 0)
				return Result;
			if (Field.Condition is not null && !Field.Condition.Evaluate(Process.Values))
				return Result;
			string BaseValue = Field.StringValue?.Trim() ?? string.Empty;
			if (string.IsNullOrEmpty(BaseValue))
				return Result;
			foreach (KycMapping Map in Field.Mappings)
			{
				if (string.IsNullOrEmpty(Map.Key))
					continue;
				string Current = BaseValue;
				foreach (string Name in Map.TransformNames)
				{
					if (string.IsNullOrWhiteSpace(Name))
						continue;
					if (NeuroAccessMaui.Services.Kyc.Transforms.KycTransformRegistry.TryGet(Name, out NeuroAccessMaui.Services.Kyc.Transforms.IKycTransform Transform))
					{
						try { Current = await Transform.ApplyAsync(Field, Process, Current, Ct); }
						catch (Exception Ex2) { ServiceRef.LogService.LogException(Ex2); }
					}
					if (string.IsNullOrEmpty(Current))
						break;
				}
				if (!string.IsNullOrEmpty(Current))
					Result.Add(new Property(Map.Key, Current));
			}
			return Result;
		}

		private bool CheckAndHandleFile(KycProcess Process, ObservableKycField Field, List<LegalIdentityAttachment> List)
		{
			if (Field.Condition is not null && (Field.Mappings.Count == 0 || !Field.Condition.Evaluate(Process.Values)))
				return false;
			if (!string.IsNullOrEmpty(Field.StringValue) && Field is ObservableImageField ImageField)
			{
				byte[]? Data = ImageField.StringValue is null ? null : this.CompressImage(this.Base64ToStream(ImageField.StringValue));
				if (Data is not null)
					List.Add(new LegalIdentityAttachment(ImageField.Mappings.First().Key + ".jpg", Constants.MimeTypes.Jpeg, Data));
				return true;
			}
			return false;
		}

		private MemoryStream Base64ToStream(string Base64)
		{
			byte[] Bytes = Convert.FromBase64String(Base64);
			return new MemoryStream(Bytes);
		}

		private byte[]? CompressImage(Stream InputStream)
		{
			try
			{
				using SKManagedStream Ms = new(InputStream);
				using SKData ImgData = SKData.Create(Ms);
				using SKCodec Codec = SKCodec.Create(ImgData);
				SKBitmap Bmp = SKBitmap.Decode(ImgData);
				Bmp = this.HandleOrientation(Bmp, Codec.EncodedOrigin);
				bool Resize = false;
				int H = Bmp.Height; int W = Bmp.Width;
				if (W >= H && W > 1920) { H = (int)(H * (1920.0 / W) + 0.5); W = 1920; Resize = true; }
				else if (H > W && H > 1920) { W = (int)(W * (1920.0 / H) + 0.5); H = 1920; Resize = true; }
				if (Resize)
				{
					SKImageInfo Info = Bmp.Info;
					SKImageInfo Ni = new(W, H, Info.ColorType, Info.AlphaType, Info.ColorSpace);
					SKBitmap? Resized = Bmp.Resize(Ni, SKFilterQuality.High);
					if (Resized is not null) { Bmp.Dispose(); Bmp = Resized; }
				}
				byte[] Bytes2;
				using (SKData Encoded = Bmp.Encode(SKEncodedImageFormat.Jpeg, 80)) Bytes2 = Encoded.ToArray();
				Bmp.Dispose();
				return Bytes2;
			}
			catch (Exception Ex) { ServiceRef.LogService.LogException(Ex); return null; }
		}

		private SKBitmap HandleOrientation(SKBitmap Bmp, SKEncodedOrigin O)
		{
			SKBitmap Rotated;
			switch (O)
			{
				case SKEncodedOrigin.BottomRight:
					Rotated = new SKBitmap(Bmp.Width, Bmp.Height);
					using (SKCanvas Canvas = new(Rotated)) { Canvas.RotateDegrees(180, Bmp.Width / 2, Bmp.Height / 2); Canvas.DrawBitmap(Bmp, 0, 0); }
					break;
				case SKEncodedOrigin.RightTop:
					Rotated = new SKBitmap(Bmp.Height, Bmp.Width);
					using (SKCanvas Canvas = new(Rotated)) { Canvas.Translate(Rotated.Width, 0); Canvas.RotateDegrees(90); Canvas.DrawBitmap(Bmp, 0, 0); }
					break;
				case SKEncodedOrigin.LeftBottom:
					Rotated = new SKBitmap(Bmp.Height, Bmp.Width);
					using (SKCanvas Canvas = new(Rotated)) { Canvas.Translate(0, Rotated.Height); Canvas.RotateDegrees(270); Canvas.DrawBitmap(Bmp, 0, 0); }
					break;
				default: return Bmp;
			}
			return Rotated;
		}

		#endregion // Data Preparation

		/// <summary>
		/// Flushes any pending coalesced snapshots by forcing immediate persistence.
		/// </summary>
		public async Task FlushAsync(CancellationToken cancellationToken = default)
		{
			foreach (KeyValuePair<string, AutosaveEntry> Pair in this.pendingAutosave.ToArray())
			{
				if (cancellationToken.IsCancellationRequested)
					break;
				if (this.pendingAutosave.TryRemove(Pair.Key, out AutosaveEntry? Entry))
				{
					try { await this.SaveSnapshotAsync(Entry.Reference, Entry.Snapshot, true).ConfigureAwait(false); }
					catch (Exception Ex) { ServiceRef.LogService.LogException(Ex, new KeyValuePair<string, object?>("Operation", "KYC.FlushAutosave")); }
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
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
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
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
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
