using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Tag;
using Waher.Content;

namespace NeuroAccessMaui.UI.Pages.Registration.Views;

public partial class ChooseProviderViewModel : BaseRegistrationViewModel
{
	public ChooseProviderViewModel() : base(RegistrationStep.ChooseProvider)
	{
	}

	/// <inheritdoc />
	protected override async Task OnInitialize()
	{
		await base.OnInitialize();
		await this.SetDomainName();

		ServiceRef.TagProfile.Changed += this.TagProfile_Changed;
	}

	/// <inheritdoc />
	protected override async Task OnDispose()
	{
		ServiceRef.TagProfile.Changed -= this.TagProfile_Changed;

		await base.OnDispose();
	}

	protected override void OnPropertyChanged(PropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);

		if (e.PropertyName == nameof(this.SelectedButton))
		{
			if ((this.SelectedButton is not null) && (this.SelectedButton.Button == ButtonType.Change))
			{
				MainThread.BeginInvokeOnMainThread(async () => {
					this.SelectedButton = null;
					await Services.UI.QR.QrCode.ScanQrCodeAndHandleResult(Constants.UriSchemes.Onboarding);
				});
			}
		}
	}

	/// <summary>
	/// The localized intro text to display to the user for explaining what 'choose account' is for.
	/// </summary>
	[ObservableProperty]
	private string domainName = string.Empty;

	/// <summary>
	/// The localized intro text to display to the user for explaining what 'choose account' is for.
	/// </summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasLocalizedName))]
	private string localizedName = string.Empty;

	/// <summary>
	/// The localized intro text to display to the user for explaining what 'choose account' is for.
	/// </summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasLocalizedDescription))]
	private string localizedDescription = string.Empty;

	/// <summary>
	/// The localized intro text to display to the user for explaining what 'choose account' is for.
	/// </summary>
	public bool HasLocalizedName => this.LocalizedName.Length > 0;

	/// <summary>
	/// The localized intro text to display to the user for explaining what 'choose account' is for.
	/// </summary>
	public bool HasLocalizedDescription => this.LocalizedDescription.Length > 0;


	/// <summary>
	/// Holds the list of buttons to display.
	/// </summary>
	public Collection<ButtonInfo> Buttons { get; } =
		[
			new(ButtonType.Approve),
			new(ButtonType.Change),
		];


	/// <summary>
	/// The selected Button
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
	private ButtonInfo? selectedButton;

	private async void TagProfile_Changed(object? Sender, PropertyChangedEventArgs e)
	{
		if (this.DomainName != ServiceRef.TagProfile.Domain)
		{
			await this.SetDomainName();
		}
	}

	private async Task SetDomainName()
	{
		if (string.IsNullOrEmpty(ServiceRef.TagProfile.Domain))
		{
			this.DomainName = string.Empty;
			this.LocalizedName = string.Empty;
			this.LocalizedDescription = string.Empty;
			return;
		}

		this.DomainName = ServiceRef.TagProfile.Domain;

		try
		{
			Uri DomainInfo = new("https://" + this.DomainName + "/Agent/Account/DomainInfo");
			string AcceptLanguage = App.SelectedLanguage.TwoLetterISOLanguageName;

			if (AcceptLanguage != "en")
			{
				AcceptLanguage += ";q=1,en;q=0.9";
			}

			object Result = await InternetContent.GetAsync(DomainInfo,
				new KeyValuePair<string, string>("Accept", "application/json"),
				new KeyValuePair<string, string>("Accept-Language", AcceptLanguage));

			if (Result is Dictionary<string, object> Response)
			{
				if (Response.TryGetValue("humanReadableName", out object? Obj) && Obj is string LocalizedName)
				{
					this.LocalizedName = LocalizedName;
				}

				if (Response.TryGetValue("humanReadableDescription", out Obj) && Obj is string LocalizedDescription)
				{
					this.LocalizedDescription = LocalizedDescription;
				}
			}
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
		}
	}

	public bool CanContinue => (this.SelectedButton is not null) && (this.SelectedButton.Button == ButtonType.Approve);

	[RelayCommand(CanExecute = nameof(CanContinue))]
	private void Continue()
	{

	}
}

public enum ButtonType
{
	Approve = 0,
	Change = 1,
}

public partial class ButtonInfo : ObservableObject
{
	public ButtonInfo(ButtonType Button)
	{
		this.Button = Button;

		LocalizationManager.CurrentCultureChanged += this.OnCurrentCultureChanged;
	}

	~ButtonInfo()
	{
		LocalizationManager.CurrentCultureChanged -= this.OnCurrentCultureChanged;
	}

	private void OnCurrentCultureChanged(object? Sender, CultureInfo Culture)
	{
		//this.OnPropertyChanged(nameof(this.LocalizedName));
		//this.OnPropertyChanged(nameof(this.LocalizedDescription));
	}

	public ButtonType Button { get; set; }

	public string LocalizedName
	{
		get
		{
			return this.Button switch
			{
				ButtonType.Approve => ServiceRef.Localizer[nameof(AppResources.ProviderSectionApproveOption)],
				ButtonType.Change => ServiceRef.Localizer[nameof(AppResources.ProviderSectionChangeOption)],
				_ => throw new NotImplementedException(),
			};
		}
	}

	public string ImageName
	{
		get
		{
			return this.Button switch
			{
				ButtonType.Approve => "approve_provider_button.png",
				ButtonType.Change => "change_provider_button.png",
				_ => throw new NotImplementedException(),
			};
		}
	}
}
