namespace NeuroAccessMaui.Extensions;

[ContentProperty(nameof(Member))]
public class NameOfExtensions : IMarkupExtension<string>
{
	public Type? Type { get; set; }
	public string? Member { get; set; }

	string IMarkupExtension<string>.ProvideValue(IServiceProvider serviceProvider)
	{
		throw new NotImplementedException();
	}

	object? IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
	{
		return ((IMarkupExtension<string?>)this).ProvideValue(serviceProvider);
	}

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
}


/*
 *
 * 	[ContentProperty(nameof(Text))]
	public class TranslateExtension : IMarkupExtension<BindingBase>
	{
		public string Text { get; set; } = string.Empty;

		public string? StringFormat { get; set; }

		object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);

		public BindingBase ProvideValue(IServiceProvider serviceProvider)
		{
#if NETSTANDARD1_0
			throw new NotSupportedException("Translate XAML MarkupExtension is not supported on .NET Standard 1.0");
#else
			#region Required work-around to prevent linker from removing the implementation
			if (DateTime.Now.Ticks < 0)
				_ = LocalizationResourceManager.Current[Text];
			#endregion

			var binding = new Binding
			{
				Mode = BindingMode.OneWay,
				Path = $"[{Text}]",
				Source = LocalizationResourceManager.Current,
				StringFormat = StringFormat
			};
			return binding;
#endif
		}
	}
*/
