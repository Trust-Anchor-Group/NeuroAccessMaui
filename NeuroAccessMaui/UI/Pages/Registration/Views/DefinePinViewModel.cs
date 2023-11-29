using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Tag;

namespace NeuroAccessMaui.UI.Pages.Registration.Views;

public partial class DefinePinViewModel : BaseRegistrationViewModel
{
	public DefinePinViewModel() : base(RegistrationStep.DefinePin)
	{
	}

	public string LocalizedValidationError1
	{
		get
		{
			return string.Empty;
		}
	}

	public string LocalizedValidationError2
	{
		get
		{
			return string.Empty;
		}
	}
}
