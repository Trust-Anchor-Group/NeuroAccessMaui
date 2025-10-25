using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Cache.InternetCache;
using NeuroAccessMaui.UI.MVVM;
using NeuroAccessMaui.UI.MVVM.Building;
using NeuroAccessMaui.UI.MVVM.Policies;
using Microsoft.Maui;
using SkiaSharp;
using Svg.Skia;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// A control that displays an image from a URI (string) with built-in caching, loading indicator, and error fallback.
	/// Exposes Source (string), Aspect, IsLoading, ParentId, Permanent, CacheDuration, and ErrorPlaceholder for binding.
	/// </summary>
	public class UriImage : ContentView
	{
		// The source URI string of the image to load.
		public static readonly BindableProperty SourceProperty =
			BindableProperty.Create(
				nameof(Source),
				typeof(string),
				typeof(UriImage),
				default(string),
				propertyChanged: OnSourceChanged);

		// Placeholder to show if loading fails.
		public static readonly BindableProperty ErrorPlaceholderProperty =
			BindableProperty.Create(
				nameof(ErrorPlaceholder),
				typeof(ImageSource),
				typeof(UriImage),
				default(ImageSource),
				propertyChanged: OnErrorPlaceholderChanged);

		// Aspect property to control image aspect ratio.
		public static readonly BindableProperty AspectProperty =
			BindableProperty.Create(
				nameof(Aspect),
				typeof(Aspect),
				typeof(UriImage),
				Aspect.AspectFill,
				propertyChanged: OnAspectChanged);

		// Optional custom parent ID for cache entries (defaults to Source).
		public static readonly BindableProperty ParentIdProperty =
			BindableProperty.Create(
				nameof(ParentId),
				typeof(string),
				typeof(UriImage),
				default(string));

		// Whether to make this cache entry permanent or temporary.
		public static readonly BindableProperty PermanentProperty =
			BindableProperty.Create(
				nameof(Permanent),
				typeof(bool),
				typeof(UriImage),
				false);

		// Optional custom cache duration; null means use service default.
		public static readonly BindableProperty CacheDurationProperty =
			BindableProperty.Create(
				nameof(CacheDuration),
				typeof(TimeSpan?),
				typeof(UriImage),
				Constants.Cache.DefaultImageCache);

		// Read-only IsLoading property to indicate loading state.
		private static readonly BindablePropertyKey IsLoadingPropertyKey =
			BindableProperty.CreateReadOnly(
				nameof(IsLoading),
				typeof(bool),
				typeof(UriImage),
				false);

		public static readonly BindableProperty IsLoadingProperty = IsLoadingPropertyKey.BindableProperty;

		/// <summary>
		/// Source URI string of the image to load.
		/// </summary>
		public string Source
		{
			get => (string)this.GetValue(SourceProperty);
			set => this.SetValue(SourceProperty, value);
		}

		/// <summary>
		/// Image shown if loading fails.
		/// </summary>
		[TypeConverter(typeof(ImageSourceConverter))]
		public ImageSource ErrorPlaceholder
		{
			get => (ImageSource)this.GetValue(ErrorPlaceholderProperty);
			set => this.SetValue(ErrorPlaceholderProperty, value);
		}

		/// <summary>
		/// The Aspect for the inner Image.
		/// </summary>
		public Aspect Aspect
		{
			get => (Aspect)this.GetValue(AspectProperty);
			set => this.SetValue(AspectProperty, value);
		}

		/// <summary>
		/// Optional custom parent ID for cache entries.
		/// </summary>
		public string ParentId
		{
			get => (string)this.GetValue(ParentIdProperty);
			set => this.SetValue(ParentIdProperty, value);
		}

		/// <summary>
		/// Whether to make this cache entry permanent (true) or temporary (false).
		/// </summary>
		public bool Permanent
		{
			get => (bool)this.GetValue(PermanentProperty);
			set => this.SetValue(PermanentProperty, value);
		}

		/// <summary>
		/// Custom cache duration for this entry; null uses default expiry.
		/// </summary>
		public TimeSpan? CacheDuration
		{
			get => (TimeSpan?)this.GetValue(CacheDurationProperty);
			set => this.SetValue(CacheDurationProperty, value);
		}

		/// <summary>
		/// Indicates whether the image is currently loading.
		/// </summary>
		public bool IsLoading
		{
			get => (bool)this.GetValue(IsLoadingProperty);
			private set => this.SetValue(IsLoadingPropertyKey, value);
		}

		private readonly Image imageView;
		private readonly ActivityIndicator spinner;
		private readonly ObservableTask<int> loadImageTask;
		private bool isErrorDisplayed;
		private bool isDisposed;

		public UriImage()
		{
			this.spinner = new ActivityIndicator
			{
				IsVisible = false,
				IsRunning = false,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};

			this.imageView = new Image
			{
				Aspect = this.Aspect
			};

			this.Content = new Grid
			{
				Children = { this.imageView, this.spinner }
			};

			ObservableTaskBuilder Builder = new ObservableTaskBuilder();
			Builder.Named("UriImage.Load");
			Builder.AutoStart(false);
			Builder.WithPolicy(Policies.Retry(3, (int Attempt, Exception Error) => TimeSpan.FromMilliseconds(Math.Min(2000, 200 * Attempt))));
			Builder.Run(this.LoadImageCoreAsync);

			this.loadImageTask = Builder.Build();
			this.loadImageTask.StateChanged += this.OnLoadTaskStateChanged;
		}

		protected override void OnHandlerChanging(HandlerChangingEventArgs Args)
		{
			base.OnHandlerChanging(Args);

			if (Args.NewHandler is null && !this.isDisposed)
			{
				this.loadImageTask.StateChanged -= this.OnLoadTaskStateChanged;
				this.loadImageTask.Dispose();
				this.isDisposed = true;
			}
		}

		private static void OnAspectChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			UriImage Control = (UriImage)Bindable;
			Control.imageView.Aspect = (Aspect)NewValue;
		}

		private static void OnSourceChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			UriImage Control = (UriImage)Bindable;
			Control.isErrorDisplayed = false;
			if (!Control.isDisposed)
				Control.loadImageTask.Run();
		}

		private static void OnErrorPlaceholderChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			UriImage Control = (UriImage)Bindable;
			if (Control.isErrorDisplayed)
				Control.imageView.Source = (ImageSource)NewValue;
		}

		private void OnLoadTaskStateChanged(object? Sender, ObservableTaskStatus Status)
		{
			bool IsRunning = Status == ObservableTaskStatus.Running;
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.IsLoading = IsRunning;
				this.spinner.IsRunning = IsRunning;
				this.spinner.IsVisible = IsRunning;
			});

			if (Status == ObservableTaskStatus.Failed && !this.isErrorDisplayed)
				_ = this.ShowErrorAsync();
		}

		private async Task LoadImageCoreAsync(TaskContext<int> Context)
		{
			try
			{
				string? CurrentSource = this.Source;
				if (string.IsNullOrWhiteSpace(CurrentSource))
				{
					await this.ShowErrorAsync();
					return;
				}

				ImageSource? NewImage = await this.ResolveImageSourceAsync(CurrentSource, Context.CancellationToken);
				if (NewImage is null)
				{
					await this.ShowErrorAsync();
					return;
				}

				Context.CancellationToken.ThrowIfCancellationRequested();

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.isErrorDisplayed = false;
					this.imageView.Source = NewImage;
				});
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				throw;
			}
		}

		private async Task<ImageSource?> ResolveImageSourceAsync(string CurrentSource, CancellationToken CancellationToken)
		{
			bool IsSvgUri = CurrentSource.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);

			if (CurrentSource.StartsWith("resource://", StringComparison.Ordinal))
			{
				CancellationToken.ThrowIfCancellationRequested();
				return ImageSource.FromResource(CurrentSource[11..]);
			}

			if (CurrentSource.StartsWith("file://", StringComparison.Ordinal))
			{
				CancellationToken.ThrowIfCancellationRequested();
				if (IsSvgUri)
					return CurrentSource[7..^4];
				return CurrentSource[7..];
			}

			if (!Uri.TryCreate(CurrentSource, UriKind.Absolute, out Uri? UriValue))
				return null;

			string Key = string.IsNullOrEmpty(this.ParentId) ? CurrentSource : this.ParentId;
			IInternetCacheService CacheService = ServiceRef.InternetCacheService;
			(byte[]? ImageBytes, string _) = await CacheService.GetOrFetch(UriValue, Key, this.Permanent);

			if (ImageBytes is null || ImageBytes.Length == 0)
				throw new InvalidOperationException($"Unable to retrieve image from '{CurrentSource}'.");

			byte[] ProcessedBytes = ImageBytes;
			if (IsSvgUri)
				ProcessedBytes = this.TryConvertSvgToPng(ProcessedBytes, CancellationToken);

			CancellationToken.ThrowIfCancellationRequested();

			return this.CreateImageSourceFromBytes(ProcessedBytes);
		}

		private ImageSource CreateImageSourceFromBytes(byte[] ImageBytes)
		{
			byte[] Buffer = ImageBytes;
			return ImageSource.FromStream(() => new MemoryStream(Buffer));
		}

		private byte[] TryConvertSvgToPng(byte[] ImageBytes, CancellationToken CancellationToken)
		{
			try
			{
				CancellationToken.ThrowIfCancellationRequested();
				SKSvg Svg = new SKSvg();
				using (MemoryStream InputStream = new MemoryStream(ImageBytes))
				{
					Svg.Load(InputStream);
				}

				CancellationToken.ThrowIfCancellationRequested();

				if (Svg.Picture is null)
					return ImageBytes;

				using (MemoryStream OutputStream = new MemoryStream())
				{
					bool Converted = Svg.Picture.ToImage(OutputStream, SKColor.Parse("#00FFFFFF"), SKEncodedImageFormat.Png, 100, 1, 1, SKColorType.Rgba8888, SKAlphaType.Premul, SKColorSpace.CreateSrgb());
					if (!Converted)
						return ImageBytes;

					return OutputStream.ToArray();
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return ImageBytes;
			}
		}

		private Task ShowErrorAsync()
		{
			return MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.isErrorDisplayed = true;
				this.IsLoading = false;
				this.spinner.IsRunning = false;
				this.spinner.IsVisible = false;
				this.imageView.Source = this.ErrorPlaceholder;
			});
		}
	}
}
