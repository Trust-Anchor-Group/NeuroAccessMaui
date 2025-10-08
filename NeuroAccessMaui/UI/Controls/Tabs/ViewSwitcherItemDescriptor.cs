using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Controls
{
	internal sealed class ViewSwitcherItemDescriptor
	{
		public int Index { get; set; }

		public object? Item { get; set; }

		public ViewSwitcherStateView? StateView { get; set; }

		public string? StateKey { get; set; }

		public View? InlineView { get; set; }
	}
}
