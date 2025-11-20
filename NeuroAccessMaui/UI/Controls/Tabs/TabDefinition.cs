using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Represents a data descriptor for a tab entry displayed by <see cref="TabNavigationHost"/>.
	/// </summary>
	public class TabDefinition : ObservableObject
	{
		private string key = string.Empty;
		private string title = string.Empty;
		private string? icon;
		private int? badgeCount;
		private bool isEnabled = true;
		private bool isProminent;
		private bool isSelected;
		private ICommand? command;
		private object? commandParameter;
		private object? tag;

		/// <summary>
		/// Gets or sets an optional unique key associated with the tab (e.g., for routing).
		/// </summary>
		public string Key
		{
			get => this.key;
			set => this.SetProperty(ref this.key, value);
		}

		/// <summary>
		/// Gets or sets the title displayed for the tab.
		/// </summary>
		public string Title
		{
			get => this.title;
			set => this.SetProperty(ref this.title, value);
		}

		/// <summary>
		/// Gets or sets the icon image displayed alongside the title.
		/// </summary>
		public string? Icon
		{
			get => this.icon;
			set => this.SetProperty(ref this.icon, value);
		}

		/// <summary>
		/// Gets or sets the badge count displayed for the tab, if any.
		/// </summary>
		public int? BadgeCount
		{
			get => this.badgeCount;
			set => this.SetProperty(ref this.badgeCount, value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether the tab can be interacted with.
		/// </summary>
		public bool IsEnabled
		{
			get => this.isEnabled;
			set => this.SetProperty(ref this.isEnabled, value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether the tab should receive prominent styling.
		/// </summary>
		/// <remarks>
		/// Templates can use this value to differentiate specific items (e.g., a larger centered button).
		/// </remarks>
		public bool IsProminent
		{
			get => this.isProminent;
			set => this.SetProperty(ref this.isProminent, value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether the tab is currently selected.
		/// </summary>
		public bool IsSelected
		{
			get => this.isSelected;
			set => this.SetProperty(ref this.isSelected, value);
		}

		/// <summary>
		/// Gets or sets a command that is invoked when the tab is selected.
		/// </summary>
		public ICommand? Command
		{
			get => this.command;
			set => this.SetProperty(ref this.command, value);
		}

		/// <summary>
		/// Gets or sets the command parameter supplied when <see cref="Command"/> executes.
		/// </summary>
		public object? CommandParameter
		{
			get => this.commandParameter;
			set => this.SetProperty(ref this.commandParameter, value);
		}

		/// <summary>
		/// Gets or sets an optional tag that can store arbitrary data associated with the tab.
		/// </summary>
		public object? Tag
		{
			get => this.tag;
			set => this.SetProperty(ref this.tag, value);
		}
	}
}
