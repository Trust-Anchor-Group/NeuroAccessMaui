using System.Windows.Input;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Provides the shared layout of identity flow pages: a header with a leading action and a title, a body, and a sticky
	/// footer separated from the body by a hairline.
	/// </summary>
	/// <remarks>
	/// The shell does not apply safe-area or keyboard insets. The hosting page keeps doing that through the
	/// <c>SafeArea</c> and <c>KeyboardInsets</c> attached properties, so the footer always sits above the insets.
	/// </remarks>
	public partial class IdentityFlowShell : ContentView
	{
		/// <summary>
		/// Identifies the <see cref="Title"/> bindable property.
		/// </summary>
		public static readonly BindableProperty TitleProperty = BindableProperty.Create(
			nameof(Title),
			typeof(string),
			typeof(IdentityFlowShell),
			string.Empty,
			propertyChanged: OnHeaderChanged);

		/// <summary>
		/// Identifies the <see cref="HeaderContent"/> bindable property.
		/// </summary>
		public static readonly BindableProperty HeaderContentProperty = BindableProperty.Create(
			nameof(HeaderContent),
			typeof(View),
			typeof(IdentityFlowShell),
			propertyChanged: OnHeaderChanged);

		/// <summary>
		/// Identifies the <see cref="TrailingContent"/> bindable property.
		/// </summary>
		public static readonly BindableProperty TrailingContentProperty = BindableProperty.Create(
			nameof(TrailingContent),
			typeof(View),
			typeof(IdentityFlowShell));

		/// <summary>
		/// Identifies the <see cref="ShowLeadingAction"/> bindable property.
		/// </summary>
		public static readonly BindableProperty ShowLeadingActionProperty = BindableProperty.Create(
			nameof(ShowLeadingAction),
			typeof(bool),
			typeof(IdentityFlowShell),
			false);

		/// <summary>
		/// Identifies the <see cref="LeadingActionCommand"/> bindable property.
		/// </summary>
		public static readonly BindableProperty LeadingActionCommandProperty = BindableProperty.Create(
			nameof(LeadingActionCommand),
			typeof(ICommand),
			typeof(IdentityFlowShell));

		/// <summary>
		/// Identifies the <see cref="LeadingActionIcon"/> bindable property.
		/// </summary>
		public static readonly BindableProperty LeadingActionIconProperty = BindableProperty.Create(
			nameof(LeadingActionIcon),
			typeof(string),
			typeof(IdentityFlowShell),
			"close.svg");

		/// <summary>
		/// Identifies the <see cref="LeadingActionDescription"/> bindable property.
		/// </summary>
		public static readonly BindableProperty LeadingActionDescriptionProperty = BindableProperty.Create(
			nameof(LeadingActionDescription),
			typeof(string),
			typeof(IdentityFlowShell),
			string.Empty);

		/// <summary>
		/// Identifies the <see cref="BodyContent"/> bindable property.
		/// </summary>
		public static readonly BindableProperty BodyContentProperty = BindableProperty.Create(
			nameof(BodyContent),
			typeof(View),
			typeof(IdentityFlowShell));

		/// <summary>
		/// Identifies the <see cref="StickyFooterContent"/> bindable property.
		/// </summary>
		public static readonly BindableProperty StickyFooterContentProperty = BindableProperty.Create(
			nameof(StickyFooterContent),
			typeof(View),
			typeof(IdentityFlowShell));

		/// <summary>
		/// Identifies the <see cref="ShowStickyFooter"/> bindable property.
		/// </summary>
		public static readonly BindableProperty ShowStickyFooterProperty = BindableProperty.Create(
			nameof(ShowStickyFooter),
			typeof(bool),
			typeof(IdentityFlowShell),
			false);

		/// <summary>
		/// Initializes a new instance of the <see cref="IdentityFlowShell"/> class.
		/// </summary>
		public IdentityFlowShell()
		{
			this.InitializeComponent();
		}

		/// <summary>
		/// Gets or sets the title shown in the header. It is hidden when <see cref="HeaderContent"/> is set.
		/// </summary>
		public string Title
		{
			get => (string)this.GetValue(TitleProperty);
			set => this.SetValue(TitleProperty, value);
		}

		/// <summary>
		/// Gets or sets optional content shown in the middle of the header instead of <see cref="Title"/>, such as a
		/// provider banner.
		/// </summary>
		public View? HeaderContent
		{
			get => (View?)this.GetValue(HeaderContentProperty);
			set => this.SetValue(HeaderContentProperty, value);
		}

		/// <summary>
		/// Gets or sets optional content shown at the trailing edge of the header.
		/// </summary>
		public View? TrailingContent
		{
			get => (View?)this.GetValue(TrailingContentProperty);
			set => this.SetValue(TrailingContentProperty, value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether the leading header action is visible.
		/// </summary>
		public bool ShowLeadingAction
		{
			get => (bool)this.GetValue(ShowLeadingActionProperty);
			set => this.SetValue(ShowLeadingActionProperty, value);
		}

		/// <summary>
		/// Gets or sets the command run by the leading header action.
		/// </summary>
		public ICommand? LeadingActionCommand
		{
			get => (ICommand?)this.GetValue(LeadingActionCommandProperty);
			set => this.SetValue(LeadingActionCommandProperty, value);
		}

		/// <summary>
		/// Gets or sets the SVG asset of the leading header action, such as <c>close.svg</c>.
		/// </summary>
		public string LeadingActionIcon
		{
			get => (string)this.GetValue(LeadingActionIconProperty);
			set => this.SetValue(LeadingActionIconProperty, value);
		}

		/// <summary>
		/// Gets or sets the accessible description of the leading header action.
		/// </summary>
		public string LeadingActionDescription
		{
			get => (string)this.GetValue(LeadingActionDescriptionProperty);
			set => this.SetValue(LeadingActionDescriptionProperty, value);
		}

		/// <summary>
		/// Gets or sets the body content hosted by the shell.
		/// </summary>
		public View? BodyContent
		{
			get => (View?)this.GetValue(BodyContentProperty);
			set => this.SetValue(BodyContentProperty, value);
		}

		/// <summary>
		/// Gets or sets the content of the sticky footer, typically the primary actions of the step.
		/// </summary>
		public View? StickyFooterContent
		{
			get => (View?)this.GetValue(StickyFooterContentProperty);
			set => this.SetValue(StickyFooterContentProperty, value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether the sticky footer is visible.
		/// </summary>
		public bool ShowStickyFooter
		{
			get => (bool)this.GetValue(ShowStickyFooterProperty);
			set => this.SetValue(ShowStickyFooterProperty, value);
		}

		/// <summary>
		/// Gets a value indicating whether the header title is shown, which is when a title is set and no custom header
		/// content replaces it.
		/// </summary>
		public bool ShowTitle => this.HeaderContent is null && !string.IsNullOrWhiteSpace(this.Title);

		/// <summary>
		/// Gets a value indicating whether custom header content is shown.
		/// </summary>
		public bool HasHeaderContent => this.HeaderContent is not null;

		private static void OnHeaderChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			IdentityFlowShell Shell = (IdentityFlowShell)Bindable;
			Shell.OnPropertyChanged(nameof(ShowTitle));
			Shell.OnPropertyChanged(nameof(HasHeaderContent));
		}
	}
}
