using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.Animations
{
	/// <summary>
	/// Helper responsible for registering default animation descriptors.
	/// </summary>
	public static class AnimationCatalog
	{
		/// <summary>
		/// Registers default animation descriptors with the supplied registry.
		/// </summary>
		/// <param name="Registry">Animation registry instance.</param>
		public static void RegisterDefaults(IAnimationRegistry Registry)
		{
			ArgumentNullException.ThrowIfNull(Registry);

			RegisterShellAnimations(Registry);
			RegisterViewSwitcherAnimations(Registry);
		}

		private static void RegisterShellAnimations(IAnimationRegistry Registry)
		{
			TimeSpan PageDuration = TimeSpan.FromMilliseconds(300);
			Easing PageEasing = Easing.Linear;

			Registry.Register(new TransitionAnimationDescriptor(
				AnimationKeys.Shell.PageCrossFade,
				PageDuration,
				PageEasing,
				Context => new TransitionAnimation(
					new FadeAnimation(PageDuration, PageEasing, 0, 1),
					new FadeAnimation(PageDuration, PageEasing, null, 0))));

			Registry.Register(new TransitionAnimationDescriptor(
				AnimationKeys.Shell.PageSlideLeft,
				TimeSpan.FromMilliseconds(250),
				Easing.CubicOut,
				Context => CreateHorizontalSlideTransition(Context, SlideDirection.Left)));

			Registry.Register(new TransitionAnimationDescriptor(
				AnimationKeys.Shell.PageSlideRight,
				TimeSpan.FromMilliseconds(250),
				Easing.CubicOut,
				Context => CreateHorizontalSlideTransition(Context, SlideDirection.Right)));

			RegisterPopupAnimations(Registry);
			RegisterToastAnimations(Registry);
		}

		private static void RegisterViewSwitcherAnimations(IAnimationRegistry Registry)
		{
			TimeSpan Duration = TimeSpan.FromMilliseconds(250);
			Easing Easing = Easing.Linear;
			Registry.Register(new TransitionAnimationDescriptor
				(
					AnimationKeys.ViewSwitcher.CrossFade,
					Duration,
					Easing,
					_ => new TransitionAnimation(
						new FadeAnimation(Duration, Easing, 0, 1),
						new FadeAnimation(Duration, Easing, null, 0))
					)
				);
		}

		private static void RegisterPopupAnimations(IAnimationRegistry Registry)
		{
			TimeSpan FadeDuration = TimeSpan.FromMilliseconds(150);
			TimeSpan ScaleDuration = TimeSpan.FromMilliseconds(200);

			Registry.Register(new ViewAnimationDescriptor(
				AnimationKeys.Shell.PopupShowFade,
				FadeDuration,
				Easing.CubicOut,
				_ => new FadeAnimation(FadeDuration, Easing.CubicOut, 0, 1)));

			Registry.Register(new ViewAnimationDescriptor(
				AnimationKeys.Shell.PopupHideFade,
				FadeDuration,
				Easing.CubicIn,
				_ => new FadeAnimation(FadeDuration, Easing.CubicIn, null, 0)));

			Registry.Register(new ViewAnimationDescriptor(
				AnimationKeys.Shell.PopupShowScale,
				ScaleDuration,
				Easing.CubicOut,
				_ => new CompositeViewAnimation(
					AnimationCompositionMode.Parallel,
					new List<IViewAnimation>
					{
						new FadeAnimation(ScaleDuration, Easing.CubicOut, 0, 1),
						new ScaleAnimation(ScaleDuration, Easing.CubicOut, 0.9, 1)
					}))); 

			Registry.Register(new ViewAnimationDescriptor(
				AnimationKeys.Shell.PopupHideScale,
				TimeSpan.FromMilliseconds(150),
				Easing.CubicIn,
				_ => new CompositeViewAnimation(
					AnimationCompositionMode.Parallel,
					new List<IViewAnimation>
					{
						new FadeAnimation(TimeSpan.FromMilliseconds(150), Easing.CubicIn, null, 0),
						new ScaleAnimation(TimeSpan.FromMilliseconds(150), Easing.CubicIn, null, 0.9)
					}))); 

			Registry.Register(new ViewAnimationDescriptor(
				AnimationKeys.Shell.PopupShowSlideUp,
				ScaleDuration,
				Easing.CubicOut,
				Context =>
				{
					double StartY = Context.ViewportHeight > 0 ? Context.ViewportHeight * 0.5 : 200;
					return new CompositeViewAnimation(
						AnimationCompositionMode.Parallel,
						new List<IViewAnimation>
						{
							new FadeAnimation(ScaleDuration, Easing.CubicOut, 0, 1),
							new TranslateAnimation(ScaleDuration, Easing.CubicOut, null, StartY, 0, 0)
						});
				})); 

			Registry.Register(new ViewAnimationDescriptor(
				AnimationKeys.Shell.PopupHideSlideUp,
				TimeSpan.FromMilliseconds(150),
				Easing.CubicIn,
				Context => new CompositeViewAnimation(
					AnimationCompositionMode.Parallel,
					new List<IViewAnimation>
					{
						new FadeAnimation(TimeSpan.FromMilliseconds(150), Easing.CubicIn, null, 0),
						new TranslateAnimation(TimeSpan.FromMilliseconds(150), Easing.CubicIn, null, null, 0, 50)
					}))); 
		}

		private static void RegisterToastAnimations(IAnimationRegistry Registry)
		{
			TimeSpan FadeDuration = TimeSpan.FromMilliseconds(150);

			Registry.Register(new ViewAnimationDescriptor(
				AnimationKeys.Shell.ToastShowFade,
				FadeDuration,
				Easing.CubicOut,
				_ => new FadeAnimation(FadeDuration, Easing.CubicOut, 0, 1)));

			Registry.Register(new ViewAnimationDescriptor(
				AnimationKeys.Shell.ToastHideFade,
				FadeDuration,
				Easing.CubicIn,
				_ => new FadeAnimation(FadeDuration, Easing.CubicIn, null, 0)));

			Registry.Register(new ViewAnimationDescriptor(
				AnimationKeys.Shell.ToastShowSlideTop,
				FadeDuration,
				Easing.CubicOut,
				_ => new CompositeViewAnimation(
					AnimationCompositionMode.Parallel,
					new List<IViewAnimation>
					{
						new FadeAnimation(FadeDuration, Easing.CubicOut, 0, 1),
						new TranslateAnimation(FadeDuration, Easing.CubicOut, null, -40, 0, 0)
					}))); 

			Registry.Register(new ViewAnimationDescriptor(
				AnimationKeys.Shell.ToastHideSlideTop,
				FadeDuration,
				Easing.CubicIn,
				_ => new CompositeViewAnimation(
					AnimationCompositionMode.Parallel,
					new List<IViewAnimation>
					{
						new FadeAnimation(FadeDuration, Easing.CubicIn, null, 0),
						new TranslateAnimation(FadeDuration, Easing.CubicIn, null, null, 0, -40)
					}))); 

			Registry.Register(new ViewAnimationDescriptor(
				AnimationKeys.Shell.ToastShowSlideBottom,
				FadeDuration,
				Easing.CubicOut,
				_ => new CompositeViewAnimation(
					AnimationCompositionMode.Parallel,
					new List<IViewAnimation>
					{
						new FadeAnimation(FadeDuration, Easing.CubicOut, 0, 1),
						new TranslateAnimation(FadeDuration, Easing.CubicOut, null, 40, 0, 0)
					}))); 

			Registry.Register(new ViewAnimationDescriptor(
				AnimationKeys.Shell.ToastHideSlideBottom,
				FadeDuration,
				Easing.CubicIn,
				_ => new CompositeViewAnimation(
					AnimationCompositionMode.Parallel,
					new List<IViewAnimation>
					{
						new FadeAnimation(FadeDuration, Easing.CubicIn, null, 0),
						new TranslateAnimation(FadeDuration, Easing.CubicIn, null, null, 0, 40)
					}))); 
		}

		private enum SlideDirection
		{
			Left,
			Right
		}

		private static TransitionAnimation CreateHorizontalSlideTransition(IAnimationContext Context, SlideDirection Direction)
		{
			double Width = Context.ViewportWidth > 0 ? Context.ViewportWidth : 400;
			double EnterFrom = Direction == SlideDirection.Left ? Width : -Width;
			double ExitTo = Direction == SlideDirection.Left ? -Width : Width;
			TimeSpan Duration = TimeSpan.FromMilliseconds(250);
			Easing SlideEasing = Easing.CubicOut;
			IViewAnimation EnterAnimation = new TranslateAnimation(Duration, SlideEasing, EnterFrom, 0, 0, 0);
			IViewAnimation ExitAnimation = new TranslateAnimation(Duration, SlideEasing, 0, 0, ExitTo, 0);
			return new TransitionAnimation(EnterAnimation, ExitAnimation);
		}
	}
}
