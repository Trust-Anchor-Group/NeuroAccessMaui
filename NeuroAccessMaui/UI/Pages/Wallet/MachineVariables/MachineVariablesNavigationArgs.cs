using NeuroAccessMaui.Services.Navigation;
using Waher.Script;

namespace NeuroAccessMaui.UI.Pages.Wallet.MachineVariables
{
	/// <summary>
	/// Holds navigation parameters specific to the current state of a state-machine.
	/// </summary>
	public class MachineVariablesNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Creates a new instance of the <see cref="MachineVariablesNavigationArgs"/> class.
		/// </summary>
		public MachineVariablesNavigationArgs()
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="MachineVariablesNavigationArgs"/> class.
		/// </summary>
		/// <param name="Running">If the State-machine is running</param>
		/// <param name="Ended">If the State-machine has ended</param>
		/// <param name="CurrentState">The Current State of the state-machine.</param>
		/// <param name="Variables">Current variables</param>
		public MachineVariablesNavigationArgs(bool Running, bool Ended, string CurrentState, Variables Variables)
		{
			this.Running = Running;
			this.Ended = Ended;
			this.CurrentState = CurrentState;
			this.Variables = Variables;
		}

		/// <summary>
		/// If the State-machine is running
		/// </summary>
		public bool Running { get; }

		/// <summary>
		/// If the State-machine has ended
		/// </summary>
		public bool Ended { get; }

		/// <summary>
		/// The Current State of the state-machine.
		/// </summary>
		public string? CurrentState { get; }

		/// <summary>
		/// Current variables
		/// </summary>
		public Variables? Variables { get; }
	}
}
