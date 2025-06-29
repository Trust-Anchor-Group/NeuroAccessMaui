using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.Controls
{

	/// <summary>
	/// 
	/// </summary>
	public class WrappedDatePicker : Microsoft.Maui.Controls.DatePicker, IDatePicker
	{
		DateTime IDatePicker.Date
		{
			get => this.Date;
			set
			{
				if (value.Equals(DateTime.Today.Date))
					this.Date = value.AddDays(-1);
				this.Date = value;
				this.OnPropertyChanged(nameof(this.Date));
			}
		}
	}
}
