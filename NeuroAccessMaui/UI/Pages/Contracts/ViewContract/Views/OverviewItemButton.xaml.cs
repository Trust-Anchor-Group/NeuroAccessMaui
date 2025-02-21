using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace NeuroAccessMaui.UI.Pages.Contracts.ViewContract.Views
{

	public partial class OverviewItemButton : ContentView
	{
		// Command Property
		public static readonly BindableProperty CommandProperty =
			 BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(OverviewItemButton));

		public ICommand Command
		{
			get => (ICommand)this.GetValue(CommandProperty);
			set => this.SetValue(CommandProperty, value);
		}

		// IconData Property
		public static readonly BindableProperty IconDataProperty =
			 BindableProperty.Create(nameof(IconData), typeof(Geometry), typeof(OverviewItemButton));

		public Geometry IconData
		{
			get => (Geometry)this.GetValue(IconDataProperty);
			set => this.SetValue(IconDataProperty, value);
		}

		// IsOk Property
		public static readonly BindableProperty IsOkProperty =
			 BindableProperty.Create(nameof(IsOk), typeof(bool), typeof(OverviewItemButton));

		public bool IsOk
		{
			get => (bool)this.GetValue(IsOkProperty);
			set
			{
				Console.WriteLine($"Setting IsOk to {value}");
				this.SetValue(IsOkProperty, value);
			}
		}

		// TopLabelText Property
		public static readonly BindableProperty TopLabelTextProperty =
			 BindableProperty.Create(nameof(TopLabelText), typeof(string), typeof(OverviewItemButton));

		public string TopLabelText
		{
			get => (string)this.GetValue(TopLabelTextProperty);
			set => this.SetValue(TopLabelTextProperty, value);
		}

		// BottomLabelText Property
		public static readonly BindableProperty BottomLabelTextProperty =
			 BindableProperty.Create(nameof(BottomLabelText), typeof(string), typeof(OverviewItemButton));

		public string BottomLabelText
		{
			get => (string)this.GetValue(BottomLabelTextProperty);
			set => this.SetValue(BottomLabelTextProperty, value);
		}

		public OverviewItemButton()
		{
			this.InitializeComponent();
		}
	}
}
