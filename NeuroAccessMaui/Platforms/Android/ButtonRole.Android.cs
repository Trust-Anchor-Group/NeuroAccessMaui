using AndroidX.Core.View;
using AndroidX.Core.View.Accessibility;
using AView = Android.Views.View;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Android part of <see cref="ButtonRole"/>: TalkBack says "button" for a view whose accessibility class is a button.
	/// </summary>
	public static partial class ButtonRole
	{
		/// <summary>
		/// The class name TalkBack reads as a button.
		/// </summary>
		private const string buttonClassName = "android.widget.Button";

		/// <summary>
		/// Wraps the view's accessibility delegate, so the view reports itself as a button. The delegate MAUI may have set
		/// is kept and still runs first.
		/// </summary>
		/// <param name="PlatformView">The platform view.</param>
		static partial void ApplyPlatform(object PlatformView)
		{
			if (PlatformView is not AView Native)
				return;

			AccessibilityDelegateCompat? Existing = ViewCompat.GetAccessibilityDelegate(Native);
			if (Existing is ButtonRoleDelegate)
				return;

			ViewCompat.SetAccessibilityDelegate(Native, new ButtonRoleDelegate(Existing));
		}

		/// <summary>
		/// An accessibility delegate that reports its view as a button, and passes everything else on to the delegate it
		/// wraps.
		/// </summary>
		/// <param name="Inner">The delegate the view had before, if any.</param>
		private sealed class ButtonRoleDelegate(AccessibilityDelegateCompat? Inner) : AccessibilityDelegateCompat
		{
			private readonly AccessibilityDelegateCompat? inner = Inner;

			/// <inheritdoc/>
			public override void OnInitializeAccessibilityNodeInfo(AView? Host, AccessibilityNodeInfoCompat? Info)
			{
				if (this.inner is not null)
					this.inner.OnInitializeAccessibilityNodeInfo(Host, Info);
				else
					base.OnInitializeAccessibilityNodeInfo(Host, Info);

				if (Info is null)
					return;

				// TalkBack reads the button and its text as one item, says "button", and offers to activate it. A double-tap
				// it can't pass on as a click is sent as a tap, which the button's tap gesture handles.
				Info.ClassName = buttonClassName;
				Info.ScreenReaderFocusable = true;
				Info.Clickable = true;
			}

			/// <inheritdoc/>
			public override bool PerformAccessibilityAction(AView? Host, int Action, Android.OS.Bundle? Args)
			{
				return this.inner is not null
					? this.inner.PerformAccessibilityAction(Host, Action, Args)
					: base.PerformAccessibilityAction(Host, Action, Args);
			}

			/// <inheritdoc/>
			public override void OnPopulateAccessibilityEvent(AView? Host, Android.Views.Accessibility.AccessibilityEvent? Event)
			{
				if (this.inner is not null)
					this.inner.OnPopulateAccessibilityEvent(Host, Event);
				else
					base.OnPopulateAccessibilityEvent(Host, Event);
			}

			/// <inheritdoc/>
			public override void OnInitializeAccessibilityEvent(AView? Host, Android.Views.Accessibility.AccessibilityEvent? Event)
			{
				if (this.inner is not null)
					this.inner.OnInitializeAccessibilityEvent(Host, Event);
				else
					base.OnInitializeAccessibilityEvent(Host, Event);
			}

			/// <inheritdoc/>
			public override bool DispatchPopulateAccessibilityEvent(AView? Host, Android.Views.Accessibility.AccessibilityEvent? Event)
			{
				return this.inner is not null
					? this.inner.DispatchPopulateAccessibilityEvent(Host, Event)
					: base.DispatchPopulateAccessibilityEvent(Host, Event);
			}
		}
	}
}
