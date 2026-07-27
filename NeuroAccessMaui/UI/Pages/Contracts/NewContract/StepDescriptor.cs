using System.ComponentModel;
using System.Globalization;

namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract
{
	/// <summary>
	/// Describes one navigable step in the guided contract-creation flow.
	/// </summary>
	public class StepDescriptor : INotifyPropertyChanged
	{
		private bool isCurrent;
		private bool isVisited;
		private bool isComplete;
		private bool isEnabled = true;

		/// <summary>
		/// Gets the stable state-machine key for the step.
		/// </summary>
		public required string Key { get; init; }

		/// <summary>
		/// Gets the localized, user-facing step title.
		/// </summary>
		public required string Title { get; init; }

		/// <summary>
		/// Gets the zero-based position of the step.
		/// </summary>
		public int Index { get; init; }

		/// <summary>
		/// Gets or sets whether this is the currently displayed step.
		/// </summary>
		public bool IsCurrent
		{
			get => this.isCurrent;
			set
			{
				if (this.isCurrent != value)
				{
					this.isCurrent = value;
					this.OnPropertyChanged(nameof(this.IsCurrent));
				}
			}
		}

		/// <summary>
		/// Gets or sets whether the user has visited this step.
		/// </summary>
		public bool IsVisited
		{
			get => this.isVisited;
			set
			{
				if (this.isVisited != value)
				{
					this.isVisited = value;
					this.OnPropertyChanged(nameof(this.IsVisited));
				}
			}
		}

		/// <summary>
		/// Gets or sets whether the step currently satisfies its validation rules.
		/// </summary>
		public bool IsComplete
		{
			get => this.isComplete;
			set
			{
				if (this.isComplete != value)
				{
					this.isComplete = value;
					this.OnPropertyChanged(nameof(this.IsComplete));
				}
			}
		}

		/// <summary>
		/// Gets or sets whether the step can be selected.
		/// </summary>
		public bool IsEnabled
		{
			get => this.isEnabled;
			set
			{
				if (this.isEnabled != value)
				{
					this.isEnabled = value;
					this.OnPropertyChanged(nameof(this.IsEnabled));
				}
			}
		}

		/// <summary>
		/// Gets the one-based step number for display.
		/// </summary>
		public string DisplayNumber => (this.Index + 1).ToString(CultureInfo.InvariantCulture);

		/// <summary>
		/// Gets the asynchronous validation callback for the step.
		/// </summary>
		public Func<Task<bool>>? ValidateAsync { get; init; }

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.Key;
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;

		private void OnPropertyChanged(string PropertyName)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
		}
	}
}
