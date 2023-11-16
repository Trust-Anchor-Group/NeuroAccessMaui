using System.Windows.Input;

namespace NeuroAccessMaui.UI.Core;

internal static class ButtonElement
{
	/// <summary>
	/// The backing store for the <see cref="ICommandElement.Command" /> bindable property.
	/// </summary>
	public static readonly BindableProperty CommandProperty = BindableProperty.Create(
		nameof(IButtonElement.Command), typeof(ICommand), typeof(IButtonElement), null,
		propertyChanging: CommandElement.OnCommandChanging, propertyChanged: CommandElement.OnCommandChanged);

	/// <summary>
	/// The backing store for the <see cref="ICommandElement.CommandParameter" /> bindable property.
	/// </summary>
	public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
		nameof(IButtonElement.CommandParameter), typeof(object), typeof(IButtonElement), null,
		propertyChanged: CommandElement.OnCommandParameterChanged);

	/// <summary>
	/// The string identifier for the pressed visual state of this control.
	/// </summary>
	public const string PressedVisualState = "Pressed";

	/// <summary>
	/// A method to signal that this element was clicked/tapped.
	/// By calling this, the <see cref="ICommandElement.Command"/> and clicked events are triggered.
	/// </summary>
	/// <param name="VisualElement">The element that was interacted with.</param>
	/// <param name="ButtonElementManager">The button element implementation to trigger the commands and events on.</param>
	public static void ElementClicked(VisualElement VisualElement, IButtonElement ButtonElementManager)
	{
		if (VisualElement.IsEnabled == true)
		{
			ButtonElementManager.Command?.Execute(ButtonElementManager.CommandParameter);
			ButtonElementManager.PropagateUpClicked();
		}
	}

	//!!!
	/*
	/// <summary>
	/// A method to signal that this element was pressed.
	/// By calling this, <see cref="IButtonElement.IsPressed"/> is set to <see langword="true"/>, the visual state is changed and events are triggered.
	/// </summary>
	/// <param name="visualElement">The element that was interacted with.</param>
	/// <param name="ButtonElementManager">The button element implementation to trigger the commands and events on.</param>
	public static void ElementPressed(VisualElement visualElement, IButtonElement ButtonElementManager)
	{
		if (visualElement.IsEnabled == true)
		{
			ButtonElementManager.SetIsPressed(true);
			visualElement.ChangeVisualStateInternal();
			ButtonElementManager.PropagateUpPressed();
		}
	}

	/// <summary>
	/// A method to signal that this element was released.
	/// By calling this, <see cref="IButtonElement.IsPressed"/> is set to <see langword="false"/>, the visual state is changed and events are triggered.
	/// </summary>
	/// <param name="visualElement">The element that was interacted with.</param>
	/// <param name="ButtonElementManager">The button element implementation to trigger the commands and events on.</param>
	public static void ElementReleased(VisualElement visualElement, IButtonElement ButtonElementManager)
	{
		// Even if the button is disabled, we still want to remove the Pressed state;
		// the button may have been disabled by the the pressing action
		ButtonElementManager.SetIsPressed(false);
		visualElement.ChangeVisualStateInternal();
		ButtonElementManager.PropagateUpReleased();
	}
	*/
}
