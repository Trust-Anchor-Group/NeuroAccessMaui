using System;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Provides data for the <see cref="ViewSwitcher.SelectionChanging"/> event.
	/// </summary>
	public sealed class ViewSwitcherSelectionChangingEventArgs : EventArgs
	{
		public ViewSwitcherSelectionChangingEventArgs(
			int oldIndex,
			object? oldItem,
			string? oldStateKey,
			int newIndex,
			object? newItem,
			string? newStateKey)
		{
			this.OldIndex = oldIndex;
			this.OldItem = oldItem;
			this.OldStateKey = oldStateKey;
			this.NewIndex = newIndex;
			this.NewItem = newItem;
			this.NewStateKey = newStateKey;
		}

		public int OldIndex { get; }

		public object? OldItem { get; }

		public string? OldStateKey { get; }

		public int NewIndex { get; }

		public object? NewItem { get; }

		public string? NewStateKey { get; }

		/// <summary>
		/// Gets or sets a value indicating whether the selection change should be canceled.
		/// </summary>
		public bool Cancel { get; set; }
	}
}
