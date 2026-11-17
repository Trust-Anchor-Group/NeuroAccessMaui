using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Wallet.Tokens;
using NeuroAccessMaui.UI.Pages.Wallet.TokenDetails;
using Path = Microsoft.Maui.Controls.Shapes.Path;

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

		/// <summary>
		/// The space between two cards in the same row with an arrow between them, wide enough for its label.
		/// </summary>
		private const double besideGap = 140;

		/// <summary>
		/// How far apart arrows both ways between two cards side by side run.
		/// </summary>
		private const double besideShift = 12;
		private const double rowGap = 74;
		private const double margin = 28;

		/// <summary>
		/// The room kept on each side of the cards for arrows that run back up the map.
		/// </summary>
		private const double laneSpace = 40;

		/// <summary>
		/// How far from the cards the first lane runs. Each further lane on the same side runs <see cref="laneStep"/> further out.
		/// </summary>
		private const double laneOffset = 34;

		/// <summary>
		/// The space between lanes on the same side.
		/// </summary>
		private const double laneStep = 14;
		private const double minScale = 0.3;
		private const double maxScale = 2.5;

		/// <summary>
		/// The space each arrow label sits in, centred on its arrow. A label is at most as wide as a card, and its tap area
		/// is at least 48 high.
		/// </summary>
		private const double labelBoxHeight = 64;

		/// <summary>
		/// The narrowest a label gets when labels sit side by side.
		/// </summary>
		private const double minLabelWidth = 72;

		/// <summary>
		/// The space kept between labels that sit side by side.
		/// </summary>
		private const double labelGap = 8;

		/// <summary>
		/// How close in height two labels are before they count as side by side.
		/// </summary>
		private const double labelBand = 20;

		/// <summary>
		/// The label's padding, icon and the space after the icon, taken from its width to give the text's width.
		/// </summary>
		private const double labelChrome = 37;

		/// <summary>
		/// The largest move between two pan reports that is taken as a real move. Android measures a pan from the first finger
		/// on the screen, so when that finger is lifted the total jumps to another finger.
		/// </summary>
		private const double maxPanStep = 80;

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
		/// The way on whose arrow label was tapped. It is marked on the map, and its step is selected.
		/// </summary>
		public static readonly BindableProperty MarkedExitProperty =
			BindableProperty.Create(nameof(MarkedExit), typeof(TokenFlowExit), typeof(TokenFlowMap), null, BindingMode.TwoWay,
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
		private readonly List<EdgeLabel> labels = [];
		private double contentWidth;
		private double contentHeight;
		private double lastPanX;
		private double lastPanY;
		private bool isPinching;
		private bool rebasePan;
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
		/// Raised when the user taps a step's card or an arrow's label, after the selection has changed.
		/// </summary>
		public event EventHandler? SelectionTapped;

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
		/// The way on whose arrow label was tapped. It is marked on the map, and its step is selected.
		/// </summary>
		public TokenFlowExit? MarkedExit
		{
			get => (TokenFlowExit?)this.GetValue(MarkedExitProperty);
			set => this.SetValue(MarkedExitProperty, value);
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
		/// Fits the whole flow again the next time the map changes size, such as when the phone turns.
		/// </summary>
		public void RefitOnResize()
		{
			this.hasFitted = false;
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
			this.labels.Clear();

			IReadOnlyList<TokenFlowStepItem> Steps = this.Steps ?? [];
			IReadOnlyDictionary<string, object> Variables = this.Variables ?? new Dictionary<string, object>();

			Dictionary<string, int> Depths = GetDepths(Steps);
			int RowCount = Depths.Count == 0 ? 0 : Depths.Values.Max() + 1;
			List<TokenFlowStepItem>[] Rows = [.. Enumerable.Range(0, RowCount).Select(i => new List<TokenFlowStepItem>())];

			foreach (TokenFlowStepItem Step in Steps)
				Rows[Depths[Step.Id]].Add(Step);

			// Which steps lead to which, leaving out loops. Cards side by side with an arrow between them get more space.
			HashSet<(string From, string To)> Links = [.. Steps.SelectMany(Step => Step.Exits
				.Where(Exit => Exit.Target != Step.Id)
				.Select(Exit => (Step.Id, Exit.Target)))];
			double[] RowWidths = [.. Rows.Select(Row => GetRowWidth(Row, Links))];

			this.contentWidth = Math.Max(nodeWidth, RowWidths.DefaultIfEmpty(0).Max()) + 2 * margin + 2 * laneSpace;
			this.contentHeight = RowCount * nodeHeight + Math.Max(0, RowCount - 1) * rowGap + 2 * margin;

			Dictionary<string, Rect> Bounds = [];

			for (int r = 0; r < Rows.Length; r++)
			{
				List<TokenFlowStepItem> Row = Rows[r];
				double X = (this.contentWidth - RowWidths[r]) / 2;
				double Y = margin + r * (nodeHeight + rowGap);

				for (int i = 0; i < Row.Count; i++)
				{
					Bounds[Row[i].Id] = new Rect(X, Y, nodeWidth, nodeHeight);

					if (i + 1 < Row.Count)
						X += nodeWidth + GetGap(Row[i], Row[i + 1], Links);
				}
			}

			this.drawable.Edges = [.. GetEdges(Steps, Rows, Bounds, Depths, Links, Variables)];
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

			// Labels go on top, so they can be tapped where they reach over a card.
			IReadOnlyList<TokenFlowEdge> Edges = this.drawable.Edges;
			Point[] Centers = [.. Edges.Select(TokenFlowEdgeDrawable.GetLabelCenter)];
			double[] Widths = GetLabelWidths(Centers, [.. Edges.Select(Edge => Edge.Route == TokenFlowRoute.Beside ? besideGap - 16 : nodeWidth)]);

			for (int i = 0; i < Edges.Count; i++)
			{
				EdgeLabel LabelView = this.CreateLabel(Edges[i], Widths[i]);
				this.labels.Add(LabelView);
				this.content.Add(LabelView.Holder);
				AbsoluteLayout.SetLayoutBounds(LabelView.Holder,
					new Rect(Centers[i].X - Widths[i] / 2, Centers[i].Y - labelBoxHeight / 2, Widths[i], labelBoxHeight));
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
		/// Gets the space between two cards side by side: wider when an arrow runs between them, so its label fits.
		/// </summary>
		/// <param name="Left">The step on the left.</param>
		/// <param name="Right">The step on the right.</param>
		/// <param name="Links">Which steps lead to which.</param>
		/// <returns>The space between the cards.</returns>
		private static double GetGap(TokenFlowStepItem Left, TokenFlowStepItem Right, HashSet<(string From, string To)> Links)
		{
			return Links.Contains((Left.Id, Right.Id)) || Links.Contains((Right.Id, Left.Id)) ? besideGap : columnGap;
		}

		/// <summary>
		/// Gets how wide a row of cards is, with the space between them.
		/// </summary>
		/// <param name="Row">The steps of the row, left to right.</param>
		/// <param name="Links">Which steps lead to which.</param>
		/// <returns>The row's width.</returns>
		private static double GetRowWidth(List<TokenFlowStepItem> Row, HashSet<(string From, string To)> Links)
		{
			double Width = Row.Count * nodeWidth;

			for (int i = 1; i < Row.Count; i++)
				Width += GetGap(Row[i - 1], Row[i], Links);

			return Width;
		}

		/// <summary>
		/// Gets the arrows between the steps: one for each step a step can move on to.
		/// </summary>
		private static IEnumerable<TokenFlowEdge> GetEdges(IReadOnlyList<TokenFlowStepItem> Steps, List<TokenFlowStepItem>[] Rows,
			Dictionary<string, Rect> Bounds, Dictionary<string, int> Depths, HashSet<(string From, string To)> Links,
			IReadOnlyDictionary<string, object> Variables)
		{
			int LeftLanes = 0;
			int RightLanes = 0;

			foreach (TokenFlowStepItem Step in Steps)
			{
				foreach (IGrouping<(string Target, bool IsFailure), TokenFlowExit> Group in Step.Exits.GroupBy(Exit => (Exit.Target, Exit.IsFailure)))
				{
					if (!Bounds.TryGetValue(Group.Key.Target, out Rect To))
						continue;

					// All ways to the same step share one arrow, so its label names each of them.
					List<TokenFlowExit> Exits = [.. Group.Distinct()];
					string Label = Group.Key.IsFailure
						? ServiceRef.Localizer[nameof(AppResources.FlowIncomingFailure)]
						: string.Join(", ", Exits.Select(Exit => TokenFlowStepItem.DescribeTrigger(Exit, Variables)).Distinct());
					string ShortLabel = string.Join(", ", Exits.Select(Exit => TokenFlowStepItem.ShortTrigger(Exit, Variables)).Distinct());

					(TokenFlowRoute Route, bool OnLeft, double LaneX) = GetRoute(Step.Id, Group.Key.Target, Rows, Bounds, Depths,
						ref LeftLanes, ref RightLanes);

					// Arrows both ways between cards side by side: the one to the right runs above the middle, the other below.
					double Shift = Route == TokenFlowRoute.Beside && Links.Contains((Group.Key.Target, Step.Id))
						? (To.Center.X > Bounds[Step.Id].Center.X ? -besideShift : besideShift)
						: 0;

					yield return new TokenFlowEdge(Bounds[Step.Id], To, Route, OnLeft, LaneX, Shift, Group.Key.IsFailure, Step.IsCurrent,
						Label, ShortLabel, TokenFlowStepItem.IconOf(Exits[0]), Step, Exits[0]);
				}
			}
		}

		/// <summary>
		/// Works out how an arrow runs, so it never passes behind another card. An arrow back up the map takes a lane on a
		/// side where neither card has a neighbour, outside every card of the rows it passes. Lanes on the same side are
		/// spaced apart.
		/// </summary>
		/// <param name="From">The step the arrow starts at.</param>
		/// <param name="To">The step the arrow points to.</param>
		/// <param name="Rows">The steps of each row, left to right.</param>
		/// <param name="Bounds">Where each step's card is.</param>
		/// <param name="Depths">The row of each step.</param>
		/// <param name="LeftLanes">How many lanes run on the left so far.</param>
		/// <param name="RightLanes">How many lanes run on the right so far.</param>
		/// <returns>How the arrow runs, if a loop or lane is on the left, and where a lane runs.</returns>
		private static (TokenFlowRoute Route, bool OnLeft, double LaneX) GetRoute(string From, string To, List<TokenFlowStepItem>[] Rows,
			Dictionary<string, Rect> Bounds, Dictionary<string, int> Depths, ref int LeftLanes, ref int RightLanes)
		{
			int FromRow = Depths[From];
			int ToRow = Depths[To];
			bool FromHasLeft = Rows[FromRow][0].Id != From;
			bool FromHasRight = Rows[FromRow][^1].Id != From;

			if (From == To)
				return (TokenFlowRoute.Loop, FromHasRight && !FromHasLeft, 0);

			if (ToRow > FromRow)
				return (TokenFlowRoute.Down, false, 0);

			if (ToRow == FromRow)
			{
				int FromColumn = Rows[FromRow].FindIndex(Step => Step.Id == From);
				int ToColumn = Rows[ToRow].FindIndex(Step => Step.Id == To);

				return (Math.Abs(FromColumn - ToColumn) == 1 ? TokenFlowRoute.Beside : TokenFlowRoute.UnderRow, false, 0);
			}

			bool ToHasLeft = Rows[ToRow][0].Id != To;
			bool ToHasRight = Rows[ToRow][^1].Id != To;
			List<Rect> Passed = [.. Enumerable.Range(ToRow, FromRow - ToRow + 1).SelectMany(Row => Rows[Row]).Select(Step => Bounds[Step.Id])];

			if (!FromHasRight && !ToHasRight)
				return (TokenFlowRoute.Lane, false, Passed.Max(Box => Box.Right) + laneOffset + laneStep * RightLanes++);

			if (!FromHasLeft && !ToHasLeft)
				return (TokenFlowRoute.Lane, true, Passed.Min(Box => Box.Left) - laneOffset - laneStep * LeftLanes++);

			return (TokenFlowRoute.Up, false, 0);
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
				? Step.BadgeText
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
			Tap.Tapped += (Sender, Args) =>
			{
				this.MarkedExit = null;
				this.SelectedStep = Step;
				this.SelectionTapped?.Invoke(this, EventArgs.Empty);
			};
			Card.GestureRecognizers.Add(Tap);

			SemanticProperties.SetDescription(Card, string.IsNullOrEmpty(Detail) ? Step.Name : Step.Name + ", " + Detail);

			return Card;
		}

		/// <summary>
		/// Gets how wide each arrow's label may be: at most its widest, but narrower where labels sit side by side, so they
		/// don't cover each other.
		/// </summary>
		/// <param name="Centers">Where the labels go.</param>
		/// <param name="MaxWidths">The widest each label may be.</param>
		/// <returns>The width of each label's space.</returns>
		private static double[] GetLabelWidths(Point[] Centers, double[] MaxWidths)
		{
			double[] Widths = new double[Centers.Length];

			for (int i = 0; i < Centers.Length; i++)
			{
				double Width = MaxWidths[i];

				for (int j = 0; j < Centers.Length; j++)
				{
					if (i != j && Math.Abs(Centers[i].Y - Centers[j].Y) < labelBand)
						Width = Math.Min(Width, Math.Abs(Centers[i].X - Centers[j].X) - labelGap);
				}

				Widths[i] = Math.Max(minLabelWidth, Width);
			}

			return Widths;
		}

		/// <summary>
		/// Creates the label of an arrow: the trigger's icon and a few words, on up to two lines. A tap selects the step the
		/// arrow starts at, with this way marked.
		/// </summary>
		/// <param name="Edge">The arrow.</param>
		/// <param name="Width">How wide the label may be.</param>
		/// <returns>The label.</returns>
		private EdgeLabel CreateLabel(TokenFlowEdge Edge, double Width)
		{
			Path Icon = new()
			{
				Data = Edge.Icon,
				Aspect = Stretch.Uniform,
				WidthRequest = 12,
				HeightRequest = 12,
				VerticalOptions = LayoutOptions.Center
			};

			Label Text = new()
			{
				Text = Edge.ShortLabel,
				FontFamily = "HaasGroteskMedium",
				FontSize = 11,
				MaxLines = 2,
				LineBreakMode = LineBreakMode.TailTruncation,
				MaximumWidthRequest = Width - labelChrome,
				VerticalOptions = LayoutOptions.Center
			};

			Border Pill = new()
			{
				StrokeShape = new RoundRectangle { CornerRadius = 8 },
				Padding = new Thickness(8, 4),
				InputTransparent = true,
				Content = new HorizontalStackLayout
				{
					Spacing = 5,
					Children = { Icon, Text }
				}
			};

			// The tap area reaches above and below the label, so it is at least 48 high.
			ContentView Target = new()
			{
				Content = Pill,
				Padding = new Thickness(0, 12),
				BackgroundColor = Colors.Transparent,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};

			TapGestureRecognizer Tap = new();
			Tap.Tapped += (Sender, Args) =>
			{
				this.MarkedExit = Edge.Exit;
				this.SelectedStep = Edge.FromStep;
				this.SelectionTapped?.Invoke(this, EventArgs.Empty);
			};
			Target.GestureRecognizers.Add(Tap);

			SemanticProperties.SetDescription(Target, Edge.Label);

			// Only the label takes taps. The rest of its space lets them through to the map.
			Grid Holder = new()
			{
				InputTransparent = true,
				CascadeInputTransparent = false,
				Children = { Target }
			};

			EdgeLabel Result = new(Edge, Holder, Pill, Text, Icon);
			StyleLabel(Result, false);

			return Result;
		}

		/// <summary>
		/// Colours a label, marked when its way was tapped.
		/// </summary>
		/// <param name="LabelView">The label.</param>
		/// <param name="IsMarked">If the label is marked.</param>
		private static void StyleLabel(EdgeLabel LabelView, bool IsMarked)
		{
			LabelView.Pill.StrokeThickness = IsMarked ? 1.5 : 1;
			LabelView.Pill.SetDynamicResource(Border.StrokeProperty, IsMarked ? "ContentAccessWL" : "SurfaceDividerWL");
			LabelView.Pill.SetDynamicResource(VisualElement.BackgroundColorProperty, "SurfaceBackgroundWL");
			LabelView.Text.SetDynamicResource(Label.TextColorProperty, IsMarked ? "ContentAccessWL" : "ContentPrimaryWL");
			LabelView.Icon.SetDynamicResource(Shape.FillProperty, IsMarked ? "ContentAccessWL" : "ContentSecondaryWL");
		}

		/// <summary>
		/// Marks the card of the step the token is in now, the selected card, and the label of the tapped way.
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

			TokenFlowExit? Marked = this.MarkedExit;

			foreach (EdgeLabel LabelView in this.labels)
				StyleLabel(LabelView, Marked is not null && LabelView.Edge.FromStep == this.SelectedStep && LabelView.Edge.Exit == Marked);
		}

		private void Pinch_PinchUpdated(object? Sender, PinchGestureUpdatedEventArgs e)
		{
			// While two fingers pinch, the pan gesture also reports moves; those are left out, so the map doesn't jump.
			switch (e.Status)
			{
				case GestureStatus.Started:
					this.isPinching = true;
					this.rebasePan = true;
					return;

				case GestureStatus.Completed:
				case GestureStatus.Canceled:
					this.isPinching = false;
					this.rebasePan = true;
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
					this.rebasePan = false;
					break;

				case GestureStatus.Running:
					// The map moves by how far the finger moved since the last report, so a pinch in between can't make it
					// jump.
					double Dx = e.TotalX - this.lastPanX;
					double Dy = e.TotalY - this.lastPanY;
					this.lastPanX = e.TotalX;
					this.lastPanY = e.TotalY;

					// The first report after a pinch, and a jump to another finger, only set where the next move starts from.
					if (this.rebasePan || Math.Abs(Dx) > maxPanStep || Math.Abs(Dy) > maxPanStep)
					{
						this.rebasePan = false;
						break;
					}

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

		/// <summary>
		/// The label of an arrow on the map, with the parts that change when it is marked.
		/// </summary>
		/// <param name="Edge">The arrow.</param>
		/// <param name="Holder">The space the label sits in, placed on the map.</param>
		/// <param name="Pill">The label's outline and fill.</param>
		/// <param name="Text">The label's words.</param>
		/// <param name="Icon">The trigger's icon.</param>
		private sealed record EdgeLabel(TokenFlowEdge Edge, View Holder, Border Pill, Label Text, Path Icon);
	}
}
