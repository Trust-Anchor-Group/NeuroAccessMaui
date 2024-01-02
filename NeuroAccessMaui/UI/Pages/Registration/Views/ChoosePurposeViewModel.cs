using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Tag;

namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class ChoosePurposeViewModel : BaseRegistrationViewModel
	{
		public ChoosePurposeViewModel()
			: base(RegistrationStep.RequestPurpose)
		{
		}

		/// <summary>
		/// Holds the list of purposes to display.
		/// </summary>
		public Collection<PurposeInfo> Purposes { get; } =
			[
				new(PurposeUse.Personal),
				new(PurposeUse.Work),
				new(PurposeUse.Educational),
				new(PurposeUse.Experimental)
			];

		/// <summary>
		/// The selected Purpose
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
		private PurposeInfo? selectedPurpose;

		public bool CanContinue => this.SelectedPurpose is not null;

		[RelayCommand(CanExecute = nameof(CanContinue))]
		private void Continue()
		{
			//!!! The old code for existing accounts. Should be implemented somehow else
			// bool IsTest = this.PurposeRequired ? this.IsEducationalPurpose || this.IsExperimentalPurpose : this.TagProfile.IsTest;
			// PurposeUse Purpose = this.PurposeRequired ? (PurposeUse)this.PurposeNr : this.TagProfile.Purpose;

			PurposeUse Purpose = this.SelectedPurpose!.Purpose;
			bool IsTest = Purpose == PurposeUse.Educational || Purpose == PurposeUse.Experimental;

			ServiceRef.TagProfile.SetPurpose(IsTest, Purpose);

			GoToRegistrationStep(RegistrationStep.ValidatePhone);
		}
	}

	public partial class PurposeInfo : ObservableObject
	{
		public PurposeInfo(PurposeUse Purpose)
		{
			this.Purpose = Purpose;

			LocalizationManager.CurrentCultureChanged += this.OnCurrentCultureChanged;
		}

		~PurposeInfo()
		{
			LocalizationManager.CurrentCultureChanged -= this.OnCurrentCultureChanged;
		}

		private void OnCurrentCultureChanged(object? Sender, CultureInfo Culture)
		{
			this.OnPropertyChanged(nameof(this.LocalizedName));
			this.OnPropertyChanged(nameof(this.LocalizedDescription));
		}


		public PurposeUse Purpose { get; set; }

		public string LocalizedName
		{
			get
			{
				return this.Purpose switch
				{
					PurposeUse.Personal => ServiceRef.Localizer[nameof(AppResources.PurposePersonal)],
					PurposeUse.Work => ServiceRef.Localizer[nameof(AppResources.PurposeWork)],
					PurposeUse.Educational => ServiceRef.Localizer[nameof(AppResources.PurposeEducational)],
					PurposeUse.Experimental => ServiceRef.Localizer[nameof(AppResources.PurposeExperimental)],
					_ => throw new NotImplementedException(),
				};
			}
		}

		public string LocalizedDescription
		{
			get
			{
				return this.Purpose switch
				{
					PurposeUse.Personal => ServiceRef.Localizer[nameof(AppResources.PurposePersonalDescription)],
					PurposeUse.Work => ServiceRef.Localizer[nameof(AppResources.PurposeWorkDescription)],
					PurposeUse.Educational => ServiceRef.Localizer[nameof(AppResources.PurposeEducationalDescription)],
					PurposeUse.Experimental => ServiceRef.Localizer[nameof(AppResources.PurposeExperimentalDescription)],
					_ => throw new NotImplementedException(),
				};
			}
		}
	}
}
