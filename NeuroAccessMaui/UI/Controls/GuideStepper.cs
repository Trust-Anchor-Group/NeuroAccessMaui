using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Animations;
using NeuroAccessMaui.Services;
using ShapePath = Microsoft.Maui.Controls.Shapes.Path;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Shows a compact horizontal overview of a multi-step flow with completed, active, and upcoming steps.
	/// </summary>
	/// <remarks>
	/// Steps before <see cref="CurrentStep"/> are complete, the step at <see cref="CurrentStep"/> is active, and
	/// later steps are upcoming. Setting <see cref="CurrentStep"/> to the number of steps marks every step as complete.
	/// </remarks>
	[ContentProperty(nameof(Steps))]
	public class GuideStepper : ContentView
	{
		private const double indicatorSize = 24;
		private const double connectorGap = 6;

		private readonly Grid layout;
		private readonly ObservableCollection<GuideStep> steps = [];
		private readonly List<StepVisual> visuals = [];

		/// <summary>
		/// Identifies the <see cref="CurrentStep"/> bindable property.
		/// </summary>
		public static readonly BindableProperty CurrentStepProperty = BindableProperty.Create(
			nameof(CurrentStep),
			typeof(int),
			typeof(GuideStepper),
			0,
			propertyChanged: (Bindable, OldValue, NewValue) => ((GuideStepper)Bindable).ApplyState((int)OldValue, true));

		/// <summary>
		/// Initializes a new instance of the <see cref="GuideStepper"/> class.
		/// </summary>
		public GuideStepper()
		{
			this.layout = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				},
				RowSpacing = 6
			};

			this.Content = this.layout;
			this.steps.CollectionChanged += this.OnStepsChanged;
		}

		/// <summary>
		/// Identifies the display status of a single step.
		/// </summary>
		private enum GuideStepStatus
		{
			/// <summary>
			/// The step has not been reached yet.
			/// </summary>
			Upcoming,

			/// <summary>
			/// The step is the one the user is currently on.
			/// </summary>
			Active,

			/// <summary>
			/// The step has been completed.
			/// </summary>
			Complete
		}

		/// <summary>
		/// Gets the steps to display, in order.
		/// </summary>
		public IList<GuideStep> Steps => this.steps;

		/// <summary>
		/// Gets or sets the zero-based index of the active step.
		/// </summary>
		public int CurrentStep
		{
			get => (int)this.GetValue(CurrentStepProperty);
			set => this.SetValue(CurrentStepProperty, value);
		}

		private static bool IsReducedMotion
		{
			get
			{
				try
				{
					IServiceProvider? Provider = ServiceRef.Provider;
					return Provider?.GetService<IMotionSettings>()?.ReduceMotion == true;
				}
				catch (Exception)
				{
					return false;
				}
			}
		}

		private void OnStepsChanged(object? Sender, NotifyCollectionChangedEventArgs E)
		{
			this.Rebuild();
		}

		private void Rebuild()
		{
			this.layout.Children.Clear();
			this.layout.ColumnDefinitions.Clear();
			this.visuals.Clear();

			int Count = this.steps.Count;
			for (int i = 0; i < Count; i++)
			{
				this.layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

				StepVisual Visual = new StepVisual(i, Count, this.steps[i]);
				this.layout.Add(Visual.Track, i, 0);
				this.layout.Add(Visual.Indicator, i, 0);
				this.layout.Add(Visual.Caption, i, 1);
				this.visuals.Add(Visual);
			}

			this.ApplyState(this.CurrentStep, false);
		}

		private void ApplyState(int PreviousStep, bool Animate)
		{
			int Current = this.CurrentStep;
			bool CanAnimate = Animate && !IsReducedMotion;

			foreach (StepVisual Visual in this.visuals)
			{
				GuideStepStatus Status = ResolveStatus(Visual.Index, Current);
				Visual.Apply(Status, Visual.Index <= Current, Visual.Index < Current);

				if (CanAnimate && Status != GuideStepStatus.Upcoming && Status != ResolveStatus(Visual.Index, PreviousStep))
					_ = PopAsync(Visual.Indicator);
			}
		}

		private static GuideStepStatus ResolveStatus(int Index, int Current)
		{
			if (Index < Current)
				return GuideStepStatus.Complete;

			return Index == Current ? GuideStepStatus.Active : GuideStepStatus.Upcoming;
		}

		private static async Task PopAsync(VisualElement Element)
		{
			try
			{
				Element.CancelAnimations();
				await Element.ScaleToAsync(1.18, 120, Easing.CubicOut);
				await Element.ScaleToAsync(1, 180, Easing.CubicIn);
			}
			catch (Exception)
			{
				Element.Scale = 1;
			}
		}

		/// <summary>
		/// Holds the views that make up one step column.
		/// </summary>
		private sealed class StepVisual
		{
			private readonly BoxView leftConnector;
			private readonly BoxView rightConnector;
			private readonly Border circle;
			private readonly Label number;
			private readonly ShapePath check;

			/// <summary>
			/// Initializes a new instance of the <see cref="StepVisual"/> class.
			/// </summary>
			/// <param name="Index">Zero-based step index.</param>
			/// <param name="Count">Total number of steps.</param>
			/// <param name="Step">The step description providing the caption.</param>
			public StepVisual(int Index, int Count, GuideStep Step)
			{
				this.Index = Index;

				// Each column draws half of the connector on either side, stopping short of its indicator.
				double Inset = indicatorSize / 2 + connectorGap;
				this.leftConnector = CreateConnector(Index > 0, new Thickness(0, 0, Inset, 0));
				this.rightConnector = CreateConnector(Index < Count - 1, new Thickness(Inset, 0, 0, 0));
				this.Track = new Grid
				{
					ColumnDefinitions =
					{
						new ColumnDefinition(GridLength.Star),
						new ColumnDefinition(GridLength.Star)
					},
					HeightRequest = indicatorSize,
					InputTransparent = true
				};
				this.Track.Add(this.leftConnector, 0, 0);
				this.Track.Add(this.rightConnector, 1, 0);

				this.number = new Label
				{
					Text = (Index + 1).ToString(CultureInfo.CurrentCulture),
					FontFamily = "SpaceGroteskBold",
					FontSize = 12,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center,
					HorizontalTextAlignment = TextAlignment.Center,
					VerticalTextAlignment = TextAlignment.Center
				};

				this.circle = new Border
				{
					WidthRequest = indicatorSize,
					HeightRequest = indicatorSize,
					StrokeShape = new Ellipse(),
					Padding = 0,
					Content = this.number
				};

				this.check = new ShapePath
				{
					Data = Geometries.SuccessCheckmarkPath,
					Aspect = Stretch.Uniform,
					WidthRequest = indicatorSize,
					HeightRequest = indicatorSize,
					IsVisible = false
				};
				this.check.SetDynamicResource(ShapePath.FillProperty, "ContentAccessWL");

				this.Indicator = new Grid
				{
					WidthRequest = indicatorSize,
					HeightRequest = indicatorSize,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center,
					Children = { this.circle, this.check }
				};
				this.Indicator.SetValue(AutomationProperties.IsInAccessibleTreeProperty, false);

				this.Caption = new Label
				{
					FontSize = 12,
					HorizontalOptions = LayoutOptions.Fill,
					HorizontalTextAlignment = TextAlignment.Center,
					LineBreakMode = LineBreakMode.WordWrap,
					MaxLines = 2,
					Margin = new Thickness(2, 0)
				};
				this.Caption.SetBinding(Label.TextProperty, new Binding(nameof(GuideStep.Text), source: Step));
			}

			/// <summary>
			/// Gets the zero-based step index.
			/// </summary>
			public int Index { get; }

			/// <summary>
			/// Gets the connector track drawn behind the indicator.
			/// </summary>
			public Grid Track { get; }

			/// <summary>
			/// Gets the numbered or checked indicator.
			/// </summary>
			public Grid Indicator { get; }

			/// <summary>
			/// Gets the caption label under the indicator.
			/// </summary>
			public Label Caption { get; }

			/// <summary>
			/// Applies colors and visibility for the given status.
			/// </summary>
			/// <param name="Status">The step status.</param>
			/// <param name="IsLeftComplete">Whether the connector from the previous step is complete.</param>
			/// <param name="IsRightComplete">Whether the connector to the next step is complete.</param>
			public void Apply(GuideStepStatus Status, bool IsLeftComplete, bool IsRightComplete)
			{
				SetConnectorColor(this.leftConnector, IsLeftComplete);
				SetConnectorColor(this.rightConnector, IsRightComplete);

				bool IsComplete = Status == GuideStepStatus.Complete;
				this.check.IsVisible = IsComplete;
				this.circle.IsVisible = !IsComplete;

				if (Status == GuideStepStatus.Active)
				{
					this.circle.StrokeThickness = 2;
					this.circle.SetDynamicResource(Border.StrokeProperty, "ContentAccessWL");
					this.circle.SetDynamicResource(VisualElement.BackgroundColorProperty, "TnPSuccessbgWL");
					this.number.SetDynamicResource(Label.TextColorProperty, "ContentAccessWL");
					this.Caption.SetDynamicResource(Label.TextColorProperty, "ContentPrimaryWL");
					this.Caption.FontFamily = "SpaceGroteskBold";
				}
				else
				{
					this.circle.StrokeThickness = 1.5;
					this.circle.SetDynamicResource(Border.StrokeProperty, "ContentSecondaryWL");
					this.circle.SetDynamicResource(VisualElement.BackgroundColorProperty, "SurfaceBackgroundWL");
					this.number.SetDynamicResource(Label.TextColorProperty, "ContentSecondaryWL");
					this.Caption.SetDynamicResource(Label.TextColorProperty, IsComplete ? "ContentPrimaryWL" : "ContentSecondaryWL");
					this.Caption.ClearValue(Label.FontFamilyProperty);
				}
			}

			private static BoxView CreateConnector(bool IsVisible, Thickness Margin)
			{
				return new BoxView
				{
					HeightRequest = 2,
					CornerRadius = 1,
					VerticalOptions = LayoutOptions.Center,
					Margin = Margin,
					IsVisible = IsVisible
				};
			}

			private static void SetConnectorColor(BoxView Connector, bool IsComplete)
			{
				Connector.SetDynamicResource(BoxView.ColorProperty, IsComplete ? "ContentAccessWL" : "TnPNeutralv300bgWL");
			}
		}
	}
}
