using Microsoft.Maui.Controls;
using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Presents a titled, optionally summarized section whose content can be expanded or collapsed.
	/// </summary>
	[ContentProperty(nameof(SectionContent))]
	public partial class DisclosureSection : BaseContentView
	{
		/// <summary>
		/// Identifies the <see cref="Title"/> bindable property.
		/// </summary>
		public static readonly BindableProperty TitleProperty = BindableProperty.Create(
			nameof(Title),
			typeof(string),
			typeof(DisclosureSection),
			string.Empty);

		/// <summary>
		/// Identifies the <see cref="Summary"/> bindable property.
		/// </summary>
		public static readonly BindableProperty SummaryProperty = BindableProperty.Create(
			nameof(Summary),
			typeof(string),
			typeof(DisclosureSection),
			string.Empty);

		/// <summary>
		/// Identifies the <see cref="IconSource"/> bindable property.
		/// </summary>
		public static readonly BindableProperty IconSourceProperty = BindableProperty.Create(
			nameof(IconSource),
			typeof(string),
			typeof(DisclosureSection),
			default(string));

		/// <summary>
		/// Identifies the <see cref="IsExpanded"/> bindable property.
		/// </summary>
		public static readonly BindableProperty IsExpandedProperty = BindableProperty.Create(
			nameof(IsExpanded),
			typeof(bool),
			typeof(DisclosureSection),
			false,
			defaultBindingMode: BindingMode.TwoWay);

		/// <summary>
		/// Identifies the <see cref="SectionContent"/> bindable property.
		/// </summary>
		public static readonly BindableProperty SectionContentProperty = BindableProperty.Create(
			nameof(SectionContent),
			typeof(View),
			typeof(DisclosureSection),
			default(View));

		/// <summary>
		/// Initializes a new instance of the <see cref="DisclosureSection"/> class.
		/// </summary>
		public DisclosureSection()
		{
			this.InitializeComponent();
		}

		/// <summary>
		/// Gets or sets the section heading.
		/// </summary>
		public string Title
		{
			get => (string?)this.GetValue(TitleProperty) ?? string.Empty;
			set => this.SetValue(TitleProperty, value);
		}

		/// <summary>
		/// Gets or sets the optional supporting summary displayed beneath the heading.
		/// </summary>
		public string Summary
		{
			get => (string?)this.GetValue(SummaryProperty) ?? string.Empty;
			set => this.SetValue(SummaryProperty, value);
		}

		/// <summary>
		/// Gets or sets the optional SVG asset displayed beside the heading.
		/// </summary>
		public string? IconSource
		{
			get => (string?)this.GetValue(IconSourceProperty);
			set => this.SetValue(IconSourceProperty, value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether the section content is expanded.
		/// </summary>
		public bool IsExpanded
		{
			get => (bool)this.GetValue(IsExpandedProperty);
			set => this.SetValue(IsExpandedProperty, value);
		}

		/// <summary>
		/// Gets or sets the content displayed when the section is expanded.
		/// </summary>
		public View? SectionContent
		{
			get => (View?)this.GetValue(SectionContentProperty);
			set => this.SetValue(SectionContentProperty, value);
		}
	}
}
