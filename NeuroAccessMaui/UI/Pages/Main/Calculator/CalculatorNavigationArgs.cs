using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Controls;

namespace NeuroAccessMaui.UI.Pages.Main.Calculator
{
	/// <summary>
	/// Holds navigation parameters for the calculator.
	/// </summary>
	public class CalculatorNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Holds navigation parameters for the calculator.
		/// </summary>
		public CalculatorNavigationArgs()
			: this(null)
		{
		}

		/// <summary>
		/// Holds navigation parameters for the calculator.
		/// </summary>
		/// <param name="Entry">Entry whose value is being calculated.</param>
		public CalculatorNavigationArgs(CompositeEntry? Entry)
		{
			this.Entry = Entry;
			this.ViewModel = null;
			this.Property = null;
		}

		/// <summary>
		/// Holds navigation parameters for the calculator.
		/// </summary>
		/// <param name="ViewModel">View model containing a bindable property with the value to calculate.</param>
		/// <param name="Property">Property containing the value to calculate.</param>
		public CalculatorNavigationArgs(BaseViewModel ViewModel, string Property)
		{
			this.Entry = null;
			this.ViewModel = ViewModel;
			this.Property = Property;
		}

		/// <summary>
		/// Entry whose value is being calculated.
		/// </summary>
		public CompositeEntry? Entry { get; }

		/// <summary>
		/// View model containing a bindable property with the value to calculate.
		/// </summary>
		public BaseViewModel? ViewModel { get; }

		/// <summary>
		/// Property containing the value to calculate.
		/// </summary>
		public string? Property { get; }
	}
}
