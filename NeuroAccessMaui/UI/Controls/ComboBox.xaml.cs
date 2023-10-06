using System.ComponentModel;
using System.Collections;

namespace NeuroAccessMaui.UI.Controls;

public partial class ComboBox
{
	private bool suppressFiltering;
	private bool suppressSelectedItemFiltering;

	public ComboBox()
	{
		this.InitializeComponent();

		this.InnerEntry.Keyboard = Keyboard.Create(KeyboardFlags.None);

		Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.ListView.SetSeparatorStyle(this.InnerListView,
			Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.SeparatorStyle.FullWidth);
	}

	public static readonly BindableProperty ListViewHeightRequestProperty = BindableProperty.Create(nameof(ListViewHeightRequest), typeof(double), typeof(ComboBox), defaultValue: null,
		propertyChanged: (Bindable, OldVal, NewVal) =>
		{
			ComboBox ComboBox = (ComboBox)Bindable;
			ComboBox.InnerListView.HeightRequest = (double)NewVal;
		});

	public double ListViewHeightRequest
	{
		get => (double)this.GetValue(ListViewHeightRequestProperty);
		set => this.SetValue(ListViewHeightRequestProperty, value);
	}

	public static readonly BindableProperty EntryBackgroundColorProperty = BindableProperty.Create(nameof(EntryBackgroundColor), typeof(Color), typeof(ComboBox), defaultValue: null,
		propertyChanged: (Bindable, OldVal, NewVal) =>
		{
			ComboBox ComboBox = (ComboBox)Bindable;
			ComboBox.InnerEntry.BackgroundColor = (Color)NewVal;
		});

	public Color EntryBackgroundColor
	{
		get => (Color)this.GetValue(EntryBackgroundColorProperty);
		set => this.SetValue(EntryBackgroundColorProperty, value);
	}

	public static readonly BindableProperty EntryFontSizeProperty = BindableProperty.Create(nameof(EntryFontSize), typeof(double), typeof(ComboBox), defaultValue: null,
		propertyChanged: (Bindable, OldVal, NewVal) =>
		{
			ComboBox ComboBox = (ComboBox)Bindable;
			ComboBox.InnerEntry.FontSize = (double)NewVal;
		});

	[TypeConverter(typeof(FontSizeConverter))]
	public double EntryFontSize
	{
		get => (double)this.GetValue(EntryFontSizeProperty);
		set => this.SetValue(EntryFontSizeProperty, value);
	}

	public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(ComboBox), defaultValue: null,
		propertyChanged: (Bindable, OldVal, NewVal) =>
		{
			ComboBox ComboBox = (ComboBox)Bindable;
			ComboBox.InnerListView.ItemsSource = (IEnumerable)NewVal;
		});

	public IEnumerable ItemsSource
	{
		get => (IEnumerable)this.GetValue(ItemsSourceProperty);
		set => this.SetValue(ItemsSourceProperty, value);
	}

	public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(ComboBox), defaultValue: null,
		propertyChanged: (Bindable, OldVal, NewVal) =>
		{
			ComboBox ComboBox = (ComboBox)Bindable;
			ComboBox.InnerListView.SelectedItem = NewVal;
		});

	public object SelectedItem
	{
		get => (object)this.GetValue(SelectedItemProperty);
		set => this.SetValue(SelectedItemProperty, value);
	}

	public static new readonly BindableProperty VisualProperty = BindableProperty.Create(nameof(Visual), typeof(IVisual), typeof(ComboBox), defaultValue: new VisualMarker.DefaultVisual(),
		propertyChanged: (Bindable, OldVal, NewVal) =>
		{
			ComboBox ComboBox = (ComboBox)Bindable;
			ComboBox.InnerListView.Visual = (IVisual)NewVal;
			ComboBox.InnerEntry.Visual = (IVisual)NewVal;
		});

	public new IVisual Visual
	{
		get => (IVisual)this.GetValue(VisualProperty);
		set => this.SetValue(VisualProperty, value);
	}

	public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(ComboBox), defaultValue: "",
		propertyChanged: (Bindable, OldVal, NewVal) =>
		{
			ComboBox ComboBox = (ComboBox)Bindable;
			ComboBox.InnerEntry.Placeholder = (string)NewVal;
		});

	public string Placeholder
	{
		get => (string)this.GetValue(PlaceholderProperty);
		set => this.SetValue(PlaceholderProperty, value);
	}

	public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(ComboBox), defaultValue: "",
		propertyChanged: (Bindable, OldVal, NewVal) =>
		{
			ComboBox ComboBox = (ComboBox)Bindable;
			ComboBox.InnerEntry.Text = (string)NewVal;
		});

	public string Text
	{
		get => (string)this.GetValue(TextProperty);
		set => this.SetValue(TextProperty, value);
	}

	public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(ComboBox), defaultValue: null,
		propertyChanged: (Bindable, OldVal, NewVal) =>
		{
			ComboBox ComboBox = (ComboBox)Bindable;
			ComboBox.InnerListView.ItemTemplate = (DataTemplate)NewVal;
		});

	public DataTemplate ItemTemplate
	{
		get => (DataTemplate)this.GetValue(ItemTemplateProperty);
		set => this.SetValue(ItemTemplateProperty, value);
	}

	public static readonly BindableProperty EntryDisplayPathProperty = BindableProperty.Create(nameof(EntryDisplayPath), typeof(string), typeof(ComboBox), defaultValue: "");

	public string EntryDisplayPath
	{
		get => (string)this.GetValue(EntryDisplayPathProperty);
		set => this.SetValue(EntryDisplayPathProperty, value);
	}

	public event EventHandler<SelectedItemChangedEventArgs>? SelectedItemChanged;

	protected virtual void OnSelectedItemChanged(SelectedItemChangedEventArgs e)
	{
		EventHandler<SelectedItemChangedEventArgs>? Handler = SelectedItemChanged;
		Handler?.Invoke(this, e);
	}

	public event EventHandler<TextChangedEventArgs>? TextChanged;

	protected virtual void OnTextChanged(TextChangedEventArgs e)
	{
		EventHandler<TextChangedEventArgs>? Handler = TextChanged;
		Handler?.Invoke(this, e);
	}

	public new bool Focus()
	{
		return this.InnerEntry.Focus();
	}

	public new void Unfocus()
	{
		this.InnerEntry.Unfocus();
	}

	private void InnerEntry_Focused(object sender, FocusEventArgs e)
	{
		//this.InnerListView.IsVisible = true;
	}

	private void InnerEntry_Unfocused(object sender, FocusEventArgs e)
	{
		//this.InnerListView.IsVisible = false;
	}

	private void InnerEntry_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (this.suppressFiltering)
		{
			return;
		}

	if (string.IsNullOrEmpty(e.NewTextValue))
		{
			this.suppressSelectedItemFiltering = true;
			this.InnerListView.SelectedItem = null;
			this.suppressSelectedItemFiltering = false;
		}

		//this.InnerListView.IsVisible = true;

		this.OnTextChanged(e);
	}

	private void InnerListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
	{
		if (this.suppressSelectedItemFiltering)
		{
			return;
		}

		this.suppressFiltering = true;
		object? SelectedItem = e.SelectedItem;

		if (!string.IsNullOrEmpty(this.EntryDisplayPath) && (SelectedItem is not null))
		{
			System.Reflection.PropertyInfo? Property = SelectedItem.GetType().GetProperty(this.EntryDisplayPath);
			object? Value = Property?.GetValue(SelectedItem, null);
			this.InnerEntry.Text = Value?.ToString();
		}
		else
		{
			this.InnerEntry.Text = SelectedItem?.ToString();
		}

		this.suppressFiltering = false;
		//this.InnerListView.IsVisible = false;

		this.OnSelectedItemChanged(e);

		this.InnerEntry.Unfocus();
	}
}
