
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Tag;

namespace NeuroAccessMaui.UI.Pages.Main.Apps
{
    public partial class AppsViewModel : BaseViewModel
	{
		private bool hasBetaFeatures;

		public AppsViewModel() : base()
		{
			this.hasBetaFeatures = ServiceRef.TagProfile.HasBetaFeatures;
		}

		public bool HasBetaFeatures
		{
			get => this.hasBetaFeatures;
			set => this.hasBetaFeatures = value;
		}


		// Binding for selecting between NeuroIconButton and NeuroIconButtonDisabled style depending on if has beta features enabled
		public Style BetaButtonStyle => this.HasBetaFeatures ? AppStyles.NeuroIconButton : AppStyles.NeuroIconButtonDisabled;

	}
}
