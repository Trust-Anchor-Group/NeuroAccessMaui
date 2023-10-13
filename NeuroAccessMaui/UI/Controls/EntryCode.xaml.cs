using System.Runtime.CompilerServices;

namespace NeuroAccessMaui.UI.Controls;

/// <summary>
/// public class used by IdApp.ExEntry
/// </summary>
public partial class EntryCode
{
	private List<Frame> frameContainer;
	private List<Label> codeLabels;

	/// <summary>
	/// Creates a new instance of the <see cref="EntryCode"/> class.
	/// </summary>
	public EntryCode()
	{
		this.InitializeComponent();
	}

	#region Properties

	/// <summary>
	///  The ReturnTypeProperty
	/// </summary>
	public static readonly BindableProperty ReturnTypeProperty =
		BindableProperty.Create(nameof(ReturnType), typeof(ReturnType), typeof(EntryCode), defaultValue: null);

	/// <summary>
	///  The TextProperty
	/// </summary>
	public static readonly BindableProperty TextProperty =
		BindableProperty.Create(nameof(Text), typeof(string), typeof(EntryCode), string.Empty, defaultBindingMode: BindingMode.TwoWay);

	/// <summary>
	///  The LengthProperty
	/// </summary>
	public static readonly BindableProperty LengthProperty =
		BindableProperty.Create(nameof(Length), typeof(int), typeof(EntryCode), default, propertyChanged: OnLengthChanged);

	/// <summary>
	///  The FieldHeightRequestProperty
	/// </summary>
	public static readonly BindableProperty FieldHeightRequestProperty =
		BindableProperty.Create(nameof(FieldHeightRequest), typeof(double), typeof(EntryCode), defaultValue: 40.0);

	/// <summary>
	///  The FieldWidthRequestProperty
	/// </summary>
	public static readonly BindableProperty FieldWidthRequestProperty =
		BindableProperty.Create(nameof(FieldWidthRequest), typeof(double), typeof(EntryCode), defaultValue: 40.0);

	/// <summary>
	/// Gets or sets ReturnType
	/// </summary>
	public ReturnType ReturnType
	{
		get { return (ReturnType)this.GetValue(ReturnTypeProperty); }
		set { this.SetValue(ReturnTypeProperty, value); }
	}

	/// <summary>
	/// Gets or sets the BackgroundColor
	/// </summary>
	public new Color BackgroundColor
	{
		get { return (Color)this.GetValue(BackgroundColorProperty); }
		set { this.SetValue(BackgroundColorProperty, value); }
	}

	/// <summary>
	/// Gets or sets the Text
	/// </summary>
	public string Text
	{
		get { return (string)this.GetValue(TextProperty); }
		set { this.SetValue(TextProperty, value); }
	}

	/// <summary>
	/// Gets or sets the Length
	/// </summary>
	public int Length
	{
		get { return (int)this.GetValue(LengthProperty); }
		set { this.SetValue(LengthProperty, value); }
	}

	/// <summary>
	/// Gets or sets the FieldHeightRequest
	/// </summary>
	public double FieldHeightRequest
	{
		get { return (double)this.GetValue(FieldHeightRequestProperty); }
		set { this.SetValue(FieldHeightRequestProperty, value); }
	}

	/// <summary>
	/// Gets or sets the FieldWidthRequest
	/// </summary>
	public double FieldWidthRequest
	{
		get { return (double)this.GetValue(FieldWidthRequestProperty); }
		set { this.SetValue(FieldWidthRequestProperty, value); }
	}
	#endregion Properties

	#region Events

	/// <summary>
	/// Evento TextChanged
	/// </summary>
	public event EventHandler TextChanged;

	/// <summary>
	/// Event sent when the entry is focused.
	/// </summary>
	public new event EventHandler<FocusEventArgs> Focused;

	/// <summary>
	/// Event sent when the entry is unfocused.
	/// </summary>
	public new event EventHandler<FocusEventArgs> Unfocused;

	#endregion Events

	#region Methods

	private static void OnLengthChanged(BindableObject bindable, object oldValue, object newValue)
	{
		int Length = (int)newValue;
		EntryCode control = (EntryCode)bindable;

		control.frameContainer = new List<Frame>();
		control.codeLabels = new List<Label>();

		control.ContainerGrid.ColumnDefinitions = new ColumnDefinitionCollection();


		for (int i = 0; i < Length; i++)
		{
			control.ContainerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			Frame Frame = new()
			{
				HasShadow = false,
				BackgroundColor = Colors.LightGray,
				Padding = new Thickness(0),
				Margin= new Thickness(0),
				HeightRequest = control.FieldHeightRequest,
				WidthRequest = control.FieldWidthRequest
			};

			Label Label = new()
			{
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center,
				HorizontalOptions = LayoutOptions.FillAndExpand,
				VerticalOptions = LayoutOptions.FillAndExpand,
				BackgroundColor = Colors.Transparent,
				TextColor = Colors.Black,
				Text = "_"
			};

			Frame.Content = Label;

			control.codeLabels.Add(Label);
			control.frameContainer.Add(Frame);
			//!!! control.ContainerGrid.Children.Add(Frame, i, 0);
		}
	}

	/// <summary>
	/// Change the value of PropossalMesage between StyleId Entry/ExEntry
	/// </summary>
	protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		switch (propertyName)
		{
			case nameof(this.Length):
				this.CodeEntry.MaxLength = this.Length;
				break;

			case nameof(this.FieldHeightRequest):
				if (this.frameContainer is not null)
				{
					foreach (Frame Frame in this.frameContainer)
					{
						Frame.HeightRequest = this.FieldHeightRequest;
					}
				}
				break;

			case nameof(this.FieldWidthRequest):
				if (this.frameContainer is not null)
				{
					foreach (Frame Frame in this.frameContainer)
					{
						Frame.WidthRequest = this.FieldWidthRequest;
					}
				}
				break;
		}
	}

	/// <summary>
	/// Method sent when the entry is focused..
	/// </summary>
	public new bool Focus()
	{
		Device.BeginInvokeOnMainThread(() =>
		{
			this.CodeEntry.Focus();
			this.CodeEntry.Text = string.Empty;
		});

		return true;
	}

	/// <summary>
	///  Method sent when the entry is unfocused..
	/// </summary>
	public new bool Unfocus()
	{
		Device.BeginInvokeOnMainThread(() =>
		{
			this.CodeEntry.Unfocus();
		});

		return true;
	}

	private void HandleFocusChange(object sender, FocusEventArgs e)
	{
		if (this.CodeEntry.IsFocused)
		{
			Focused?.Invoke(this, e);
		}
		else
		{
			Unfocused?.Invoke(this, e);
		}
	}

	private void HandleTextChanged(object sender, TextChangedEventArgs e)
	{
		string NewText = e.NewTextValue;
		int Length = NewText.Length;

		for (int i = 0; i < this.codeLabels.Count; i++)
		{
			if (i < Length)
			{
				string Text = NewText.Substring(i, 1);
				this.codeLabels[i].Text = Text;
			}
			else
			{
				this.codeLabels[i].Text = "_";
			}
		}

		this.Text = NewText;
		this.TextChanged?.Invoke(this, e);

		if (Length == this.Length)
		{
			this.Unfocus();
		}
	}

	private void TapGestureRecognizer_Tapped(object sender, EventArgs e)
	{
		this.Focus();
	}

	#endregion Methods
}
