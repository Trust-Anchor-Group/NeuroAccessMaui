using System;

namespace NeuroAccessMaui.UI.Controls
{
	internal sealed class ViewSwitcherSelectionState
	{
		private bool updatingFromIndex;
		private bool updatingFromItem;
		private bool updatingFromStateKey;

		public ViewSwitcherSelectionState()
		{
			this.SelectedIndex = -1;
			this.SelectedItem = null;
			this.SelectedStateKey = null;
			this.IndexBehavior = ViewSwitcherSelectedIndexBehavior.Clamp;
		}

		public int SelectedIndex { get; private set; }

		public object? SelectedItem { get; private set; }

		public string? SelectedStateKey { get; private set; }

		public ViewSwitcherSelectedIndexBehavior IndexBehavior { get; set; }

		public bool ShouldIgnore(ViewSwitcherSelectionChangeSource source)
		{
			return source switch
			{
				ViewSwitcherSelectionChangeSource.Index => this.updatingFromIndex,
				ViewSwitcherSelectionChangeSource.Item => this.updatingFromItem,
				ViewSwitcherSelectionChangeSource.StateKey => this.updatingFromStateKey,
				_ => false
			};
		}

		public SelectionStateUpdateScope BeginUpdate(ViewSwitcherSelectionChangeSource source)
		{
			this.SetUpdating(source, true);
			return new SelectionStateUpdateScope(this, source);
		}

		public int NormalizeIndex(int requestedIndex, int totalCount)
		{
			if (requestedIndex < 0)
				return -1;

			if (totalCount <= 0)
			{
				switch (this.IndexBehavior)
				{
					case ViewSwitcherSelectedIndexBehavior.Throw:
						throw new ArgumentOutOfRangeException(nameof(requestedIndex), requestedIndex, "No items are available.");
					case ViewSwitcherSelectedIndexBehavior.Ignore:
						return this.SelectedIndex;
					default:
						return -1;
				}
			}

			if (requestedIndex < totalCount)
				return requestedIndex;

			switch (this.IndexBehavior)
			{
				case ViewSwitcherSelectedIndexBehavior.Throw:
					throw new ArgumentOutOfRangeException(nameof(requestedIndex), requestedIndex, "Requested index is outside the available range.");
				case ViewSwitcherSelectedIndexBehavior.Ignore:
					return this.SelectedIndex;
				case ViewSwitcherSelectedIndexBehavior.Wrap:
				{
					int remainder = requestedIndex % totalCount;
					if (remainder < 0)
						remainder += totalCount;
					return remainder;
				}
				case ViewSwitcherSelectedIndexBehavior.Clamp:
				default:
					return totalCount - 1;
			}
		}

		public void UpdateSnapshot(int index, object? item, string? stateKey)
		{
			this.SelectedIndex = index;
			this.SelectedItem = item;
			this.SelectedStateKey = stateKey;
		}

		public void Reset()
		{
			this.SelectedIndex = -1;
			this.SelectedItem = null;
			this.SelectedStateKey = null;
		}

		private void SetUpdating(ViewSwitcherSelectionChangeSource source, bool isUpdating)
		{
			switch (source)
			{
				case ViewSwitcherSelectionChangeSource.Index:
					this.updatingFromIndex = isUpdating;
					break;
				case ViewSwitcherSelectionChangeSource.Item:
					this.updatingFromItem = isUpdating;
					break;
				case ViewSwitcherSelectionChangeSource.StateKey:
					this.updatingFromStateKey = isUpdating;
					break;
			}
		}

		public readonly struct SelectionStateUpdateScope : IDisposable
		{
			private readonly ViewSwitcherSelectionState owner;
			private readonly ViewSwitcherSelectionChangeSource source;

			public SelectionStateUpdateScope(ViewSwitcherSelectionState owner, ViewSwitcherSelectionChangeSource source)
			{
				this.owner = owner;
				this.source = source;
			}

			public void Dispose()
			{
				this.owner.SetUpdating(this.source, false);
			}
		}
	}
}
