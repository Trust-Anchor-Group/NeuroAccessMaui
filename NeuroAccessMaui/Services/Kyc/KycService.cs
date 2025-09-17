using System.Reflection;
using System.Net;
using System.Text;
using System.Collections.ObjectModel;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services.Kyc.Domain;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.Kyc.ViewModels;
using Waher.Runtime.Inventory;
using Waher.Persistence;
using NeuroAccessMaui.Services.Fetch;
using SkiaSharp;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Services.Kyc
{
	/// <summary>
	/// Default implementation of <see cref="IKycService"/>.
	/// </summary>
	[Singleton]
	public class KycService : IKycService, IDisposable
	{
		private static readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
		private static readonly string backupKyc = "TestKYCNeuro.xml";
		private static readonly TimeSpan AutosaveDelay = TimeSpan.FromMilliseconds(800);
		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposedValue)
			{
				if (disposing)
				{
					try { this.autosaveCts?.Cancel(); } catch { }
					this.autosaveCts?.Dispose();
				}
				this.disposedValue = true;
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		private CancellationTokenSource? autosaveCts;
		private readonly object autosaveSync = new();
		private Task? pendingAutosaveTask;

		#region Loading & Persistence

		/// <inheritdoc/>
		public async Task<KycReference> LoadKycReferenceAsync(string? Lang = null)
		{
			KycReference? Reference;

			try
			{
				Reference = await ServiceRef.StorageService.FindFirstDeleteRest<KycReference>();
			}
			catch (Exception FindEx)
			{
				ServiceRef.LogService.LogException(FindEx, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				Reference = null;
			}

			if (Reference is null || string.IsNullOrEmpty(Reference.ObjectId))
			{
				Reference = new KycReference
				{
					CreatedUtc = DateTime.UtcNow
				};
				try
				{
					await ServiceRef.StorageService.Insert(Reference);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}

				// Try to fetch KYC XML from provider first, fallback to embedded resource
				string? Xml = await this.TryFetchKycXmlFromProvider();
				if (string.IsNullOrEmpty(Xml))
				{
					using Stream Stream = await FileSystem.OpenAppPackageFileAsync(backupKyc);
					using StreamReader Reader = new(Stream);
					Xml = await Reader.ReadToEndAsync().ConfigureAwait(false);
				}
				Reference.KycXml = Xml;
				Reference.UpdatedUtc = DateTime.UtcNow;
				Reference.FetchedUtc = DateTime.UtcNow;
			}

			// Populate localized friendly name from process, if available
			try
			{
				KycProcess? Process = await Reference.GetProcess(Lang).ConfigureAwait(false);
				if (Process?.Name is not null)
				{
					string NewName = Process.Name.Text;
					if (!string.Equals(Reference.FriendlyName, NewName, StringComparison.Ordinal))
					{
						Reference.FriendlyName = NewName;
						// Persist update non-critically
						try { await ServiceRef.StorageService.Update(Reference); } catch { /* ignore */ }
					}
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
			}

			// 3) Load default if not available.
			return Reference;
		}

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
		/// Loads available KYC process references from the provider, falling back to the bundled test KYC when unavailable.
		/// </summary>
		/// <param name="Lang">Optional language code used for resolving localized process name.</param>
		/// <returns>A read-only list of <see cref="KycReference"/> items representing available processes.</returns>
		public async Task<IReadOnlyList<KycReference>> LoadAvailableKycReferencesAsync(string? Lang = null)
		{
			try
			{
				string? Xml = await this.TryFetchKycXmlFromProvider();
				if (string.IsNullOrEmpty(Xml))
				{
					using Stream Stream = await FileSystem.OpenAppPackageFileAsync(backupKyc);
					using StreamReader Reader = new(Stream);
					Xml = await Reader.ReadToEndAsync().ConfigureAwait(false);
				}

				KycProcess Process = await KycProcessParser.LoadProcessAsync(Xml, Lang).ConfigureAwait(false);
				KycReference Reference = KycReference.FromProcess(Process, Xml, Process.Name?.Text);
				return new List<KycReference> { Reference };
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				return Array.Empty<KycReference>();
			}
		}

		private static string GetFileName(string Resource)
		{
			int Index = Resource.LastIndexOf("Raw.", StringComparison.OrdinalIgnoreCase);
			return Index >= 0 ? Resource[(Index + 4)..] : Resource;
		}

		private async Task<string?> TryFetchKycXmlFromProvider()
		{
			try
			{
				string? Domain = ServiceRef.TagProfile.Domain;
				if (string.IsNullOrWhiteSpace(Domain))
					return null;

				Uri Uri = new($"https://{Domain}/PubSub/NeuroAccessKyc/KycProcess");
				IResourceFetcher Fetcher = new ResourceFetcher();
				ResourceFetchOptions Options = new() { ParentId = $"KycProcess:{Domain}", Permanent = false };
				ResourceResult<byte[]> Result = await Fetcher.GetBytesAsync(Uri, Options).ConfigureAwait(false);
				byte[]? Bytes = Result.Value;
				if (Bytes is null || Bytes.Length == 0)
					return null;
				return Encoding.UTF8.GetString(Bytes);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				return null;
			}
 		}

		#endregion

		#region Validation

		/// <inheritdoc/>
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
			foreach (ObservableKycField F in Fields)
			{
				F.ForceSynchronousValidation();
				F.ValidationTask.Run();
				Task T = MainThread.InvokeOnMainThreadAsync(async () =>
				{
					await F.ValidationTask.WaitAllAsync();
					if (!F.IsValid)
						Ok = false;
				});
				Tasks.Add(T);
			}
			await Task.WhenAll(Tasks);
			return Ok;
		}

		/// <inheritdoc/>
		public async Task<int> GetFirstInvalidVisiblePageIndexAsync(KycProcess Process)
		{
			if (Process is null)
				return -1;
			for (int I = 0; I < Process.Pages.Count; I++)
			{
				KycPage Page = Process.Pages[I];
				if (!Page.IsVisible(Process.Values))
					continue;
				if (!await this.ValidatePageAsync(Page))
					return I;
			}
			return -1;
		}

		#endregion

		#region Data Preparation

		/// <inheritdoc/>
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

		#region Persistence Helpers (Autosave)

		/// <inheritdoc />
		public void UpdateReferenceFields(KycReference Reference, KycProcess Process, NeuroAccessMaui.Services.Kyc.Domain.KycNavigationSnapshot Navigation, double Progress)
		{
			if (Reference is null || Process is null) return;
			Reference.Fields = [.. Process.Values.Select(p => new KycFieldValue(p.Key, p.Value))];
			Reference.Progress = Progress;
			Reference.LastVisitedPageId = Process.Pages.Count > Navigation.CurrentPageIndex && Navigation.CurrentPageIndex >= 0 ? Process.Pages[Navigation.CurrentPageIndex].Id : Reference.LastVisitedPageId;
			Reference.LastVisitedMode = Navigation.State.ToString();
			Reference.UpdatedUtc = DateTime.UtcNow;
		}

		/// <inheritdoc />
		public Task ScheduleAutosaveAsync(KycReference Reference)
		{
			CancellationTokenSource LocalCts;
			lock (this.autosaveSync)
			{
				this.autosaveCts?.Cancel();
				this.autosaveCts = new CancellationTokenSource();
				LocalCts = this.autosaveCts;
				this.pendingAutosaveTask = this.InternalDebouncedSaveAsync(Reference, LocalCts.Token);
			}
			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public async Task FlushAutosaveAsync(KycReference Reference)
		{
			CancellationTokenSource? Local;
			Task? Pending;
			lock (this.autosaveSync)
			{
				Local = this.autosaveCts;
				Pending = this.pendingAutosaveTask;
				this.autosaveCts = null;
			}
			try { Local?.Cancel(); } catch { }
			if (Pending is not null && !Pending.IsCompleted)
			{
				try { await Pending; } catch { }
			}
			await this.SaveKycReferenceAsync(Reference);
		}

		private async Task InternalDebouncedSaveAsync(KycReference Reference, CancellationToken Ct)
		{
			try
			{
				await Task.Delay(AutosaveDelay, Ct);
				if (Ct.IsCancellationRequested) return;
				await this.SaveKycReferenceAsync(Reference);
			}
			catch (TaskCanceledException) { }
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
			}
		}

		#endregion

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
				byte[] Bytes;
				using (SKData Encoded = Bmp.Encode(SKEncodedImageFormat.Jpeg, 80)) Bytes = Encoded.ToArray();
				Bmp.Dispose();
				return Bytes;
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

		#endregion

	}
}
