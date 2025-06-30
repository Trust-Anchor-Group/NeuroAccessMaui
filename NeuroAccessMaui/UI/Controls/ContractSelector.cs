using System;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.UI.QR;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using CommunityToolkit.Mvvm.Input;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// A custom control for selecting a contract
	/// </summary>
	public partial class ContractSelector : VerticalStackLayout
	{
		#region Constructor

		/// <summary>
		/// Constructor for the ContractSelector.
		/// </summary>
		public ContractSelector()
		{
			Label TitleLabel = new();
			TitleLabel.SetBinding(Label.TextProperty, new Binding(nameof(this.TitleLabelText), source: this));
			TitleLabel.SetBinding(Label.StyleProperty, new Binding(nameof(this.TitleLabelStyle), source: this));

			// Add entry to middle of CompositeInputView
			CompositeEntry Entry = new();
			Entry.SetBinding(CompositeEntry.PlaceholderProperty, new Binding(nameof(this.EntryPlaceholderText), source: this));
			Entry.SetBinding(CompositeEntry.EntryDataProperty, new Binding(nameof(this.EntryData), source: this));
			Entry.SetBinding(CompositeEntry.IsValidProperty, new Binding(nameof(this.IsValid), source: this));
			Entry.SetBinding(CompositeEntry.StyleProperty, new Binding(nameof(this.EntryStyle), source: this));
			Entry.SetBinding(CompositeEntry.LabelTextProperty, new Binding(nameof(this.TopText), source: this));
			Entry.SetBinding(CompositeEntry.LabelStyleProperty, new Binding(nameof(this.TopTextStyle), source: this));

			ImageButton ScanQrButton = new()
			{
				PathData = Geometries.ScanQrIconPath,
				Style = AppStyles.SecondaryImageButton,
				Command = new AsyncRelayCommand(() => this.ScanQr(Entry))
			};

			ImageButton OpenButton = new()
			{
				PathData = Geometries.VisibilityOnPath,
				Style = AppStyles.SecondaryImageButton,
				Command = new AsyncRelayCommand(() => this.OpenContract(Entry))
			};

			TextButton ChooseContractButton = new()
			{
				Style = AppStyles.SecondaryButton
			};
			// Add in the ctor:
			ChooseContractButton.SetBinding(TextButton.LabelDataProperty, new Binding(nameof(this.ButtonText), source: this));
			ChooseContractButton.SetBinding(TextButton.CommandProperty, new Binding(nameof(this.ButtonCommand), source: this));

			Grid ButtonGrid = new Grid()
			{
				ColumnDefinitions = new ColumnDefinitionCollection
				{
					new ColumnDefinition { Width = GridLength.Auto },
					new ColumnDefinition { Width = GridLength.Star }
				},
				ColumnSpacing = AppStyles.SmallSpacing
			};
			ButtonGrid.Add(ScanQrButton, 0, 0);
			ButtonGrid.Add(ChooseContractButton, 1, 0);

			Entry.RightView = OpenButton;
			this.Add(TitleLabel);
			this.Add(Entry);
			this.Add(ButtonGrid);
			this.Spacing = AppStyles.SmallSpacing;
		}

		#endregion

		#region Bindable Properties

		/// <summary>
		/// Bindable property for the title label text.
		/// </summary>
		public static readonly BindableProperty TitleLabelTextProperty = BindableProperty.Create(nameof(TitleLabelText), typeof(string), typeof(ContractSelector), string.Empty);

		public string TitleLabelText
		{
			get => (string)this.GetValue(TitleLabelTextProperty);
			set => this.SetValue(TitleLabelTextProperty, value);
		}

		/// <summary>
		/// Bindable Property for the title label style.
		/// </summary>
		public static readonly BindableProperty TitleLabelStyleProperty = BindableProperty.Create(nameof(TitleLabelStyle), typeof(Style), typeof(ContractSelector), null);

		public Style TitleLabelStyle
		{
			get => (Style)this.GetValue(TitleLabelStyleProperty);
			set => this.SetValue(TitleLabelStyleProperty, value);
		}

		/// <summary>
		/// Bindable Property for the contract id entry style.
		/// </summary>
		public static readonly BindableProperty EntryStyleProperty = BindableProperty.Create(nameof(EntryStyle), typeof(Style), typeof(ContractSelector), null);
		public Style EntryStyle
		{
			get => (Style)this.GetValue(EntryStyleProperty);
			set => this.SetValue(EntryStyleProperty, value);
		}

		/// <summary>
		/// Bindable Property for the contract id entry top label text.
		/// </summary>
		public static readonly BindableProperty TopTextProperty = BindableProperty.Create(nameof(TopText), typeof(string), typeof(ContractSelector), string.Empty);

		public string TopText
		{
			get => (string)this.GetValue(TopTextProperty);
			set => this.SetValue(TopTextProperty, value);
		}

		/// <summary>
		/// Bindable Property for the contract id entry top label style.
		/// </summary>
		public static readonly BindableProperty TopTextStyleProperty = BindableProperty.Create(nameof(TopTextStyle), typeof(Style), typeof(ContractSelector), null);

		public Style TopTextStyle
		{
			get => (Style)this.GetValue(TopTextStyleProperty);
			set => this.SetValue(TopTextStyleProperty, value);
		}

		/// <summary>
		/// Bindable Property for the contract id entry data.
		/// </summary>
		public static readonly BindableProperty EntryDataProperty = BindableProperty.Create(nameof(EntryData), typeof(string), typeof(ContractSelector), string.Empty);

		public string EntryData
		{
			get => (string)this.GetValue(EntryDataProperty);
			set => this.SetValue(EntryDataProperty, value);
		}

		/// <summary>
		/// Bindable Property for the contract id entry placeholder text.
		/// </summary>
		public static readonly BindableProperty EntryPlaceholderTextProperty = BindableProperty.Create(nameof(EntryPlaceholderText), typeof(string), typeof(ContractSelector), string.Empty);

		public string EntryPlaceholderText
		{
			get => (string)this.GetValue(EntryPlaceholderTextProperty);
			set => this.SetValue(EntryPlaceholderTextProperty, value);
		}

		/// <summary>
		/// Bindable Property for the contract id entry isvalid.
		/// </summary>
		public static readonly BindableProperty IsValidProperty = BindableProperty.Create(nameof(IsValid), typeof(bool), typeof(ContractSelector), true);

		public bool IsValid
		{
			get => (bool)this.GetValue(IsValidProperty);
			set => this.SetValue(IsValidProperty, value);
		}

		/// <summary>
		/// Bindable Property for the select contract button text.
		/// </summary>
		public static readonly BindableProperty ButtonTextProperty = BindableProperty.Create(nameof(ButtonText), typeof(string), typeof(ContractSelector), string.Empty);

		public string ButtonText
		{
			get => (string)this.GetValue(ButtonTextProperty);
			set => this.SetValue(ButtonTextProperty, value);
		}

		/// <summary>
		/// Bindable Property for the select contract button style.
		/// </summary>
		public static readonly BindableProperty ButtonStyleProperty = BindableProperty.Create(nameof(ButtonStyle), typeof(Style), typeof(ContractSelector), null);

		public Style ButtonStyle
		{
			get => (Style)this.GetValue(ButtonStyleProperty);
			set => this.SetValue(ButtonStyleProperty, value);
		}

		/// <summary>
		/// Bindable Property for the select contract button command.
		/// </summary>
		public static readonly BindableProperty ButtonCommandProperty = BindableProperty.Create(nameof(ButtonCommand), typeof(ICommand), typeof(ContractSelector), null);

		public ICommand ButtonCommand
		{
			get => (ICommand)this.GetValue(ButtonCommandProperty);
			set => this.SetValue(ButtonCommandProperty, value);
		}

		#endregion

		#region Commands

		/// <summary>
		/// Command that opens the ScanQrPage. And gets the url from the QR code.
		/// </summary>
		[RelayCommand]
		public async Task ScanQr(CompositeEntry Entry)
		{
			string[] AllowedSchemas = [Constants.UriSchemes.IotSc];

			try
			{
				string Url = await QrCode.ScanQrCode(nameof(AppResources.QrScanCode), AllowedSchemas) ?? string.Empty;
				if (string.IsNullOrEmpty(Url))
					return;

				MainThread.BeginInvokeOnMainThread(() =>
				{
					Entry.EntryData = Url;
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}


		public async Task OpenContract(CompositeEntry Entry)
		{
			if (string.IsNullOrEmpty(Entry.EntryData))
			{
				return;
			}
			try
			{
				await ServiceRef.ContractOrchestratorService.OpenContract(Entry.EntryData, ServiceRef.Localizer[nameof(AppResources.RequestToAccessContract)], null);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}
		#endregion
	}
}
