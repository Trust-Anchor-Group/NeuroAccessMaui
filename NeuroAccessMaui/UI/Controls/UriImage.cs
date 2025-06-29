using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using NeuroAccessMaui.Services.Cache.InternetCache;
using NeuroAccessMaui.Services;
using System.ComponentModel;

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
				default(TimeSpan?));

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
		private bool isErrorDisplayed;

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
			_ = Control.LoadImageAsync();
		}

		private static void OnErrorPlaceholderChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			UriImage Control = (UriImage)Bindable;
			if (Control.isErrorDisplayed)
				Control.imageView.Source = (ImageSource)NewValue;
		}

		private async Task LoadImageAsync()
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.IsLoading = true;
				this.spinner.IsVisible = true;
				this.spinner.IsRunning = true;
			});

			if (string.IsNullOrWhiteSpace(this.Source)
				|| !Uri.TryCreate(this.Source, UriKind.Absolute, out Uri? UriVal))
			{
				this.ShowError();
				return;
			}

			try
			{
				string Key = !string.IsNullOrEmpty(this.ParentId) ? this.ParentId : this.Source;
				IInternetCacheService CacheService = ServiceRef.InternetCacheService;
				(byte[]? ImageBytes, string _) = await CacheService.GetOrFetch(UriVal, Key, this.Permanent);

				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.isErrorDisplayed = false;
					this.imageView.Source = ImageBytes is not null
						? ImageSource.FromStream(() => new MemoryStream(ImageBytes))
						: this.ErrorPlaceholder;
				});
			}
			catch
			{
				this.ShowError();
			}
			finally
			{
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.IsLoading = false;
					this.spinner.IsRunning = false;
					this.spinner.IsVisible = false;
				});
			}
		}

		private void ShowError()
		{
			MainThread.BeginInvokeOnMainThread(() =>
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
