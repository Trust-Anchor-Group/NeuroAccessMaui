using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Wallet.Tokens;
using NeuroAccessMaui.UI.Pages.Wallet.TokenDetails;

namespace NeuroAccessMaui.UI.Pages.Wallet.Flow
{
	/// <summary>
	/// A token's flow drawn as a map: each step is a card, laid out top to bottom from the first step, with arrows for the
	/// ways from one step to another. The map can be pinched and moved, and tapping a card selects its step.
	/// </summary>
	public class TokenFlowMap : ContentView
	{
		private const double nodeWidth = 150;
		private const double nodeHeight = 58;
		private const double columnGap = 24;
		private const double rowGap = 74;
		private const double margin = 28;
		private const double minScale = 0.3;
		private const double maxScale = 2.5;

		/// <summary>
		/// The steps of the flow, in order, with the first step first.
		/// </summary>
		public static readonly BindableProperty StepsProperty =
			BindableProperty.Create(nameof(Steps), typeof(IReadOnlyList<TokenFlowStepItem>), typeof(TokenFlowMap), null,
				propertyChanged: (Bindable, OldValue, NewValue) => ((TokenFlowMap)Bindable).Build());

		/// <summary>
		/// The step whose card is selected.
		/// </summary>
		public static readonly BindableProperty SelectedStepProperty =
			BindableProperty.Create(nameof(SelectedStep), typeof(TokenFlowStepItem), typeof(TokenFlowMap), null, BindingMode.TwoWay,
				propertyChanged: (Bindable, OldValue, NewValue) => ((TokenFlowMap)Bindable).UpdateSelection());

		/// <summary>
		/// The token's values now, by name, for dates and times on the arrows.
		/// </summary>
		public static readonly BindableProperty VariablesProperty =
			BindableProperty.Create(nameof(Variables), typeof(IReadOnlyDictionary<string, object>), typeof(TokenFlowMap), null,
				propertyChanged: (Bindable, OldValue, NewValue) => ((TokenFlowMap)Bindable).Build());

		private readonly Grid viewport;
		private readonly AbsoluteLayout content;
		private readonly GraphicsView edges;
		private readonly TokenFlowEdgeDrawable drawable = new();
		private readonly Dictionary<TokenFlowStepItem, Border> cards = [];
		private double contentWidth;
		private double contentHeight;
		private double lastPanX;
		private double lastPanY;
		private bool isPinching;
		private bool hasFitted;

		/// <summary>
		/// Creates the map.
		/// </summary>
		public TokenFlowMap()
		{
			this.edges = new GraphicsView
			{
				Drawable = this.drawable,
				InputTransparent = true
			};

			this.content = new AbsoluteLayout
			{
				AnchorX = 0,
				AnchorY = 0,
				HorizontalOptions = LayoutOptions.Start,
				VerticalOptions = LayoutOptions.Start
			};

			this.viewport = new Grid
			{
				IsClippedToBounds = true,
				Children = { this.content }
			};

			PinchGestureRecognizer Pinch = new();
			Pinch.PinchUpdated += this.Pinch_PinchUpdated;
			this.viewport.GestureRecognizers.Add(Pinch);

			PanGestureRecognizer Pan = new();
			Pan.PanUpdated += this.Pan_PanUpdated;
			this.viewport.GestureRecognizers.Add(Pan);

			this.viewport.SizeChanged += (Sender, Args) =>
			{
				if (!this.hasFitted)
					this.Fit();
			};

			this.Content = this.viewport;
		}

		/// <summary>
		/// The steps of the flow, in order, with the first step first.
		/// </summary>
		public IReadOnlyList<TokenFlowStepItem>? Steps
		{
			get => (IReadOnlyList<TokenFlowStepItem>?)this.GetValue(StepsProperty);
			set => this.SetValue(StepsProperty, value);
		}

		/// <summary>
		/// The step whose card is selected.
		/// </summary>
		public TokenFlowStepItem? SelectedStep
		{
			get => (TokenFlowStepItem?)this.GetValue(SelectedStepProperty);
			set => this.SetValue(SelectedStepProperty, value);
		}

		/// <summary>
		/// The token's values now, by name, for dates and times on the arrows.
		/// </summary>
		public IReadOnlyDictionary<string, object>? Variables
		{
			get => (IReadOnlyDictionary<string, object>?)this.GetValue(VariablesProperty);
			set => this.SetValue(VariablesProperty, value);
		}

		/// <summary>
		/// Zooms and moves the map so the whole flow fits.
		/// </summary>
		public void Fit()
		{
			double Width = this.viewport.Width;
			double Height = this.viewport.Height;

			if (Width <= 0 || Height <= 0 || this.contentWidth <= 0 || this.contentHeight <= 0)
				return;

			double Scale = Math.Clamp(Math.Min(Width / this.contentWidth, Height / this.contentHeight), minScale, 1.2);

			this.content.Scale = Scale;
			this.content.TranslationX = (Width - this.contentWidth * Scale) / 2;
			this.content.TranslationY = Math.Max(0, (Height - this.contentHeight * Scale) / 2);
			this.hasFitted = true;
		}

		/// <summary>
		/// Lays out the cards and arrows for the steps.
		/// </summary>
		private void Build()
		{
			this.content.Clear();
			this.cards.Clear();

			IReadOnlyList<TokenFlowStepItem> Steps = this.Steps ?? [];
			IReadOnlyDictionary<string, object> Variables = this.Variables ?? new Dictionary<string, object>();

			Dictionary<string, int> Depths = GetDepths(Steps);
			int RowCount = Depths.Count == 0 ? 0 : Depths.Values.Max() + 1;
			List<TokenFlowStepItem>[] Rows = [.. Enumerable.Range(0, RowCount).Select(i => new List<TokenFlowStepItem>())];

			foreach (TokenFlowStepItem Step in Steps)
				Rows[Depths[Step.Id]].Add(Step);

			int MaxColumns = Rows.Length == 0 ? 1 : Math.Max(1, Rows.Max(Row => Row.Count));
			this.contentWidth = MaxColumns * nodeWidth + (MaxColumns - 1) * columnGap + 2 * margin + 40;
			this.contentHeight = RowCount * nodeHeight + Math.Max(0, RowCount - 1) * rowGap + 2 * margin;

			Dictionary<string, Rect> Bounds = [];

			for (int r = 0; r < Rows.Length; r++)
			{
				List<TokenFlowStepItem> Row = Rows[r];
				double RowWidth = Row.Count * nodeWidth + (Row.Count - 1) * columnGap;
				double X = (this.contentWidth - 40 - RowWidth) / 2;
				double Y = margin + r * (nodeHeight + rowGap);

				foreach (TokenFlowStepItem Step in Row)
				{
					Rect Box = new(X, Y, nodeWidth, nodeHeight);
					Bounds[Step.Id] = Box;
					X += nodeWidth + columnGap;
				}
			}

			this.drawable.Edges = [.. GetEdges(Steps, Bounds, Depths, Variables)];
			this.edges.WidthRequest = this.contentWidth;
			this.edges.HeightRequest = this.contentHeight;
			this.content.Add(this.edges);
			AbsoluteLayout.SetLayoutBounds(this.edges, new Rect(0, 0, this.contentWidth, this.contentHeight));

			foreach (TokenFlowStepItem Step in Steps)
			{
				Border Card = this.CreateCard(Step);
				this.cards[Step] = Card;
				this.content.Add(Card);
				AbsoluteLayout.SetLayoutBounds(Card, Bounds[Step.Id]);
			}

			this.content.WidthRequest = this.contentWidth;
			this.content.HeightRequest = this.contentHeight;
			this.edges.Invalidate();
			this.UpdateSelection();

			this.hasFitted = false;
			this.Fit();
		}

		/// <summary>
		/// Gets how far each step is from the first step, counted in steps. Steps that can't be reached come last.
		/// </summary>
		/// <param name="Steps">The steps, with the first step first.</param>
		/// <returns>The row of each step, by code name.</returns>
		private static Dictionary<string, int> GetDepths(IReadOnlyList<TokenFlowStepItem> Steps)
		{
			Dictionary<string, int> Depths = [];
			Dictionary<string, TokenFlowStepItem> ById = Steps.ToDictionary(Step => Step.Id);

			if (Steps.Count == 0)
				return Depths;

			Queue<TokenFlowStepItem> Queue = new();
			Depths[Steps[0].Id] = 0;
			Queue.Enqueue(Steps[0]);

			while (Queue.Count > 0)
			{
				TokenFlowStepItem Step = Queue.Dequeue();

				foreach (TokenFlowExit Exit in Step.Exits)
				{
					if (!Depths.ContainsKey(Exit.Target) && ById.TryGetValue(Exit.Target, out TokenFlowStepItem? Next))
					{
						Depths[Exit.Target] = Depths[Step.Id] + 1;
						Queue.Enqueue(Next);
					}
				}
			}

			int Last = Depths.Count == 0 ? 0 : Depths.Values.Max() + 1;
			foreach (TokenFlowStepItem Step in Steps)
				Depths.TryAdd(Step.Id, Last);

			return Depths;
		}

		/// <summary>
		/// Gets the arrows between the steps: one for each step a step can move on to.
		/// </summary>
		private static IEnumerable<TokenFlowEdge> GetEdges(IReadOnlyList<TokenFlowStepItem> Steps, Dictionary<string, Rect> Bounds,
			Dictionary<string, int> Depths, IReadOnlyDictionary<string, object> Variables)
		{
			foreach (TokenFlowStepItem Step in Steps)
			{
				foreach (IGrouping<(string Target, bool IsFailure), TokenFlowExit> Group in Step.Exits.GroupBy(Exit => (Exit.Target, Exit.IsFailure)))
				{
					if (!Bounds.TryGetValue(Group.Key.Target, out Rect To))
						continue;

					TokenFlowExit First = Group.First();
					string Label = Group.Key.IsFailure
						? ServiceRef.Localizer[nameof(AppResources.FlowIncomingFailure)]
						: TokenFlowStepItem.DescribeTrigger(First, Variables);

					yield return new TokenFlowEdge(Bounds[Step.Id], To, Group.Key.Target == Step.Id,
						Depths[Group.Key.Target] > Depths[Step.Id], Group.Key.IsFailure, Step.IsCurrent, Label);
				}
			}
		}

		/// <summary>
		/// Creates the card of a step.
		/// </summary>
		/// <param name="Step">The step.</param>
		/// <returns>The card.</returns>
		private Border CreateCard(TokenFlowStepItem Step)
		{
			Label Name = new()
			{
				Text = Step.Name,
				FontFamily = "SpaceGroteskSemiBold",
				FontSize = 14,
				MaxLines = 2,
				LineBreakMode = LineBreakMode.TailTruncation
			};
			Name.SetDynamicResource(Label.TextColorProperty, Step.IsFailureStep ? "ContentSecondaryWL" : "ContentPrimaryWL");

			VerticalStackLayout Text = new()
			{
				Spacing = 2,
				VerticalOptions = LayoutOptions.Center,
				Children = { Name }
			};

			string Detail = Step.IsCurrent
				? ServiceRef.Localizer[nameof(AppResources.FlowNow)]
				: Step.IsEnd ? ServiceRef.Localizer[nameof(AppResources.FlowEnd)] : string.Empty;

			if (!string.IsNullOrEmpty(Detail))
			{
				Label DetailLabel = new()
				{
					Text = Detail,
					FontFamily = Step.IsCurrent ? "HaasGroteskBold" : "HaasGroteskRegular",
					FontSize = 11
				};
				DetailLabel.SetDynamicResource(Label.TextColorProperty, Step.IsCurrent ? "ContentAccessWL" : "ContentSecondaryWL");
				Text.Add(DetailLabel);
			}

			Border Card = new()
			{
				StrokeShape = new RoundRectangle { CornerRadius = 14 },
				Padding = new Thickness(12, 6),
				Content = Text
			};

			if (Step.IsFailureStep)
				Card.StrokeDashArray = [4, 3];

			TapGestureRecognizer Tap = new();
			Tap.Tapped += (Sender, Args) => this.SelectedStep = Step;
			Card.GestureRecognizers.Add(Tap);

			SemanticProperties.SetDescription(Card, string.IsNullOrEmpty(Detail) ? Step.Name : Step.Name + ", " + Detail);

			return Card;
		}

		/// <summary>
		/// Marks the card of the step the token is in now, and the selected card.
		/// </summary>
		private void UpdateSelection()
		{
			foreach (KeyValuePair<TokenFlowStepItem, Border> Pair in this.cards)
			{
				bool IsSelected = Pair.Key == this.SelectedStep;
				bool IsCurrent = Pair.Key.IsCurrent;

				Pair.Value.StrokeThickness = IsSelected || IsCurrent ? 2 : 1;
				Pair.Value.SetDynamicResource(Border.StrokeProperty, IsSelected || IsCurrent ? "ContentAccessWL" : "SurfaceDividerWL");
				Pair.Value.SetDynamicResource(VisualElement.BackgroundColorProperty, IsCurrent ? "TnPSuccessbgWL" : "SurfaceElevation1WL");
			}
		}

		private void Pinch_PinchUpdated(object? Sender, PinchGestureUpdatedEventArgs e)
		{
			// While two fingers pinch, the pan gesture also reports moves; those are left out, so the map doesn't jump.
			switch (e.Status)
			{
				case GestureStatus.Started:
					this.isPinching = true;
					return;

				case GestureStatus.Completed:
				case GestureStatus.Canceled:
					this.isPinching = false;
					return;
			}

			// Android can report a very large or small step when a finger is lifted. Such a step is not a real pinch.
			if (e.Scale < 0.5 || e.Scale > 2)
				return;

			double Scale = Math.Clamp(this.content.Scale * e.Scale, minScale, maxScale);
			double Factor = Scale / this.content.Scale;

			// Zoom around the point between the fingers, so it stays under them.
			double OriginX = e.ScaleOrigin.X * this.viewport.Width;
			double OriginY = e.ScaleOrigin.Y * this.viewport.Height;

			this.content.Scale = Scale;
			this.MoveTo(OriginX - (OriginX - this.content.TranslationX) * Factor, OriginY - (OriginY - this.content.TranslationY) * Factor);
		}

		private void Pan_PanUpdated(object? Sender, PanUpdatedEventArgs e)
		{
			switch (e.StatusType)
			{
				case GestureStatus.Started:
					this.lastPanX = 0;
					this.lastPanY = 0;
					break;

				case GestureStatus.Running:
					// The map moves by how far the finger moved since the last report, so a pinch in between can't make it
					// jump.
					double Dx = e.TotalX - this.lastPanX;
					double Dy = e.TotalY - this.lastPanY;
					this.lastPanX = e.TotalX;
					this.lastPanY = e.TotalY;

					if (!this.isPinching)
						this.MoveTo(this.content.TranslationX + Dx, this.content.TranslationY + Dy);
					break;
			}
		}

		/// <summary>
		/// Moves the map, keeping at least part of it in view.
		/// </summary>
		/// <param name="X">The map's left edge, in view coordinates.</param>
		/// <param name="Y">The map's top edge, in view coordinates.</param>
		private void MoveTo(double X, double Y)
		{
			double Width = this.contentWidth * this.content.Scale;
			double Height = this.contentHeight * this.content.Scale;
			double Keep = 80;

			this.content.TranslationX = Math.Clamp(X, Keep - Width, this.viewport.Width - Keep);
			this.content.TranslationY = Math.Clamp(Y, Keep - Height, this.viewport.Height - Keep);
		}
	}
}
