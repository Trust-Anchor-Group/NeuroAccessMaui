using System.ComponentModel;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Tells screen reader users about changes they can't see. Set <see cref="AnnouncesChangesProperty"/> on a status label,
	/// such as "Loading more tokens…", so its text is read out whenever it changes or the label appears. Use
	/// <see cref="Announce"/> for changes that have no label of their own.
	/// </summary>
	public static class ScreenReader
	{
		/// <summary>
		/// Identifies the attached property that has a label's text read out when it changes or the label appears.
		/// </summary>
		public static readonly BindableProperty AnnouncesChangesProperty = BindableProperty.CreateAttached(
			"AnnouncesChanges",
			typeof(bool),
			typeof(ScreenReader),
			false,
			propertyChanged: OnAnnouncesChangesChanged);

		/// <summary>
		/// Gets if a label's text is read out when it changes or the label appears.
		/// </summary>
		/// <param name="View">The label.</param>
		/// <returns>If changes are read out.</returns>
		public static bool GetAnnouncesChanges(BindableObject View)
		{
			return (bool)View.GetValue(AnnouncesChangesProperty);
		}

		/// <summary>
		/// Sets if a label's text is read out when it changes or the label appears.
		/// </summary>
		/// <param name="View">The label.</param>
		/// <param name="Value">If changes are read out.</param>
		public static void SetAnnouncesChanges(BindableObject View, bool Value)
		{
			View.SetValue(AnnouncesChangesProperty, Value);
		}

		/// <summary>
		/// Reads a text out, if a screen reader is on. Errors are logged, not thrown.
		/// </summary>
		/// <param name="Text">The text to read.</param>
		public static void Announce(string? Text)
		{
			if (string.IsNullOrWhiteSpace(Text))
				return;

			MainThread.BeginInvokeOnMainThread(() =>
			{
				try
				{
					SemanticScreenReader.Default.Announce(Text);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			});
		}

		private static void OnAnnouncesChangesChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			if (Bindable is not Label Label)
				return;

			Label.PropertyChanged -= Label_PropertyChanged;

			if ((bool)NewValue)
				Label.PropertyChanged += Label_PropertyChanged;
		}

		/// <summary>
		/// Reads a label out when its text changes or it appears, while it is on screen.
		/// </summary>
		private static void Label_PropertyChanged(object? Sender, PropertyChangedEventArgs e)
		{
			if (Sender is not Label Label || e.PropertyName is not (nameof(Label.Text) or nameof(VisualElement.IsVisible)))
				return;

			if (Label.Handler is not null && Label.IsVisible)
				Announce(Label.Text);
		}
	}
}
