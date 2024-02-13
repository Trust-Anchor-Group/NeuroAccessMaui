using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Localization;
using NeuroAccessMaui.Resources.Languages;

namespace NeuroAccessMaui.Services.Localization
{
	/// <summary>
	/// Access to localized strings.
	/// </summary>
	[ContentProperty(nameof(Path))]
	public class LocalizeExtension : IMarkupExtension<BindingBase>, INotifyPropertyChanged, IDisposable
	{
		private static readonly Dictionary<Type, Dictionary<string, bool>> missingStrings = [];
		private static Timer? timer = null;

		private IStringLocalizer? localizer;
		private bool isDisposed;

		/// <summary>
		/// Localizer instance reference.
		/// </summary>
		public IStringLocalizer Localizer
		{
			get
			{
				this.localizer ??= LocalizationManager.GetStringLocalizer(this.StringResource);
				return this.localizer;
			}
		}

		public string Path { get; set; } = ".";
		public BindingMode Mode { get; set; } = BindingMode.OneWay;
		public IValueConverter? Converter { get; set; } = null;
		public string? ConverterParameter { get; set; } = null;
		public string? StringFormat { get; set; } = null;
		public Type? StringResource { get; set; } = null;

		public object ProvideValue(IServiceProvider ServiceProvider)
		{
			return (this as IMarkupExtension<BindingBase>).ProvideValue(ServiceProvider);
		}

		BindingBase IMarkupExtension<BindingBase>.ProvideValue(IServiceProvider ServiceProvider)
		{
			Type ResourcesType = this.StringResource ?? typeof(AppResources);

			if (ResourcesType.GetRuntimeProperties().FirstOrDefault(pi => pi.Name == this.Path) is null)
			{
				lock (missingStrings)
				{
					timer?.Dispose();
					timer = null;

					if (!missingStrings.TryGetValue(ResourcesType, out Dictionary<string, bool>? PerId))
					{
						PerId = [];
						missingStrings[ResourcesType] = PerId;
					}

					PerId[this.Path] = true;

					timer = new Timer(LogWarning, null, 1000, Timeout.Infinite);
				}

				return new Binding("Localizer[STRINGNOTDEFINED]", this.Mode, this.Converter, this.ConverterParameter, this.StringFormat, this);
			}

			return new Binding($"Localizer[{this.Path}]", this.Mode, this.Converter, this.ConverterParameter, this.StringFormat, this);
		}

		private static void LogWarning(object? _)
		{
			StringBuilder sb = new();

			sb.AppendLine("Missing localized strings:");
			sb.AppendLine();

			lock (missingStrings)
			{
				foreach (KeyValuePair<Type, Dictionary<string, bool>> P in missingStrings)
				{
					sb.AppendLine();
					sb.AppendLine(P.Key.FullName);
					sb.AppendLine(new string('-', (P.Key.FullName?.Length ?? 0) + 3));
					sb.AppendLine();

					foreach (string Key in P.Value.Keys)
					{
						sb.Append("* ");
						sb.AppendLine(Key);
					}
				}
			}

			ServiceRef.LogService.LogWarning(sb.ToString());
		}

		public LocalizeExtension()
		{
			LocalizationManager.CurrentCultureChanged += this.OnCurrentCultureChanged;
		}

		~LocalizeExtension()
		{
			this.Dispose(false);
		}

		private void OnCurrentCultureChanged(object? sender, CultureInfo culture)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Localizer)));
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			if (this.isDisposed)
				return;

			LocalizationManager.CurrentCultureChanged -= this.OnCurrentCultureChanged;

			this.isDisposed = true;
		}

		public event PropertyChangedEventHandler? PropertyChanged;
	}
}
