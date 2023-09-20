namespace NeuroAccessMaui.Helpers;

/// <summary>
/// PasswordMask is a class which defines <c>IsEnabled</c> attached <see cref="BindableProperty"/> used to store the state
/// of <see cref="Entry"/> password masking.
/// </summary>
public class PasswordMask : BindableObject
{
	/// <summary>
	/// Implements the attached property that indicates if the <see cref="Entry"/> password must be masked.
	/// </summary>
	public static readonly BindableProperty IsEnabledProperty = BindableProperty.CreateAttached("IsEnabled", typeof(bool), typeof(PasswordMask),
		defaultValue: true,
		propertyChanged: (bindable, oldValue, newValue) =>
		{
			if (bindable is Entry Entry)
			{
				string TogglerEffectName = $"{Constants.Effects.ResolutionGroupName}.{Constants.Effects.PasswordMaskTogglerEffect}";
				if (!Entry.Effects.Any(Effect => Effect.ResolveId == TogglerEffectName))
				{
					Effect Effect = Effect.Resolve(TogglerEffectName);
					Entry.Effects.Add(Effect);
				}
			}
		});

	/// <summary>
	/// Gets the value indicating if the password associated with <paramref name="bindable"/> must be masked.
	/// </summary>
	public static bool GetIsEnabled(BindableObject bindable)
	{
		return (bool)bindable.GetValue(IsEnabledProperty);
	}

	/// <summary>
	/// Sets the value indicating if the password associated with <paramref name="bindable"/> must be masked.
	/// </summary>
	public static void SetIsEnabled(BindableObject bindable, bool value)
	{
		bindable.SetValue(IsEnabledProperty, value);
	}
}
