using Microsoft.Extensions.Localization;
using NeuroAccessMaui.Resources.Languages;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace NeuroAccessMaui.Services.Localization
{
	[ContentProperty(nameof(Path))]
	public class LocalizeExtension : IMarkupExtension<BindingBase>, INotifyPropertyChanged
	{
		private IStringLocalizer? localizer;

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
				ServiceRef.LogService.LogWarning($"No property found for '{this.Path}' in '{ResourcesType}'");
				return new Binding("Localizer[STRINGNOTDEFINED]", this.Mode, this.Converter, this.ConverterParameter, this.StringFormat, this);
			}

			return new Binding($"Localizer[{this.Path}]", this.Mode, this.Converter, this.ConverterParameter, this.StringFormat, this);
		}

		/*
			public string Member { get; set; }

			if (serviceProvider == null)
				throw new ArgumentNullException(nameof(serviceProvider));
			if (!(serviceProvider.GetService(typeof(IXamlTypeResolver)) is IXamlTypeResolver typeResolver))
				throw new ArgumentException("No IXamlTypeResolver in IServiceProvider");
			if (string.IsNullOrEmpty(Member) || Member.IndexOf(".", StringComparison.Ordinal) == -1)
				throw new XamlParseException("Syntax for x:Static is [Member=][prefix:]typeName.staticMemberName", serviceProvider);

			var dotIdx = Member.LastIndexOf('.');
			var typename = Member.Substring(0, dotIdx);
			var membername = Member.Substring(dotIdx + 1);

			var type = typeResolver.Resolve(typename, serviceProvider);

			var pinfo = type.GetRuntimeProperties().FirstOrDefault(pi => pi.Name == membername && pi.GetMethod.IsStatic);
			if (pinfo != null)
				return pinfo.GetMethod.Invoke(null, new object[] { });

			var finfo = type.GetRuntimeFields().FirstOrDefault(fi => fi.Name == membername && fi.IsStatic);
			if (finfo != null)
				return finfo.GetValue(null);

			throw new XamlParseException($"No static member found for {Member}", serviceProvider);
		*/

		/*
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (serviceProvider == null)
				throw new ArgumentNullException(nameof(serviceProvider));

			if (Type == null || string.IsNullOrEmpty(Member) || Member.Contains("."))
				throw new ArgumentException("Syntax for x:NameOf is Type={x:Type [className]} Member=[propertyName]");

			var pinfo = Type.GetRuntimeProperties().FirstOrDefault(pi => pi.Name == Member);
			var finfo = Type.GetRuntimeFields().FirstOrDefault(fi => fi.Name == Member);
			if (pinfo == null && finfo == null)
				throw new ArgumentException($"No property or field found for {Member} in {Type}");

			return Member;
		}
		*/

		public LocalizeExtension()
		{
			LocalizationManager.CurrentCultureChanged += this.OnCurrentCultureChanged;
		}

		~LocalizeExtension()
		{
			LocalizationManager.CurrentCultureChanged -= this.OnCurrentCultureChanged;
		}

		private void OnCurrentCultureChanged(object? sender, CultureInfo culture)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Localizer)));
		}

		public event PropertyChangedEventHandler? PropertyChanged;
	}
}
