using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeuroAccessMaui.Resources.Languages;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Converters
{
	internal class ContractStateToString : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is ContractState contractState)
			{
				// Get localized string from resources
				return contractState switch
				{
					ContractState.Proposed => AppResources.Proposed,
					ContractState.Rejected => AppResources.Rejected,
					ContractState.Approved => AppResources.BeingSigned,
					ContractState.BeingSigned => AppResources.BeingSigned,
					ContractState.Signed => AppResources.Signed,
					ContractState.Failed => AppResources.Failed,
					ContractState.Obsoleted => AppResources.Obsoleted,
					ContractState.Deleted => AppResources.Deleted,
					_ => string.Empty
				};
			}
			return string.Empty;
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			// Converting back is not typically necessary in this case
			return Binding.DoNothing;
		}
	}
}
