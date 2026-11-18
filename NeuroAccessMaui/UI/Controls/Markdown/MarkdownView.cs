using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.Popups;
using NeuroAccessMaui.UI.Popups;
using Waher.Content.Markdown;

namespace NeuroAccessMaui.UI.Controls.Markdown
{
	/// <summary>
	/// Shows markdown text: contract texts, token descriptions, reports and chat messages.
	/// </summary>
	/// <remarks>
	/// <para>The text is parsed and built into blocks off the UI thread, and the blocks become views on the UI thread a few
	/// at a time, so the first lines show at once.</para>
	/// <para>In <see cref="MarkdownMode.Inline"/> mode, the text sits in the page's own scrolling. In
	/// <see cref="MarkdownMode.Document"/> mode, the text owns its scrolling, and a Contents button jumps to any heading.
	/// A page can show its own Contents button instead, with <see cref="HasContents"/> and
	/// <see cref="ShowContentsCommand"/>.</para>
	/// <para>Set one of <see cref="Markdown"/>, <see cref="Document"/> or <see cref="Model"/>. A model wins over a
	/// document, and a document over markdown.</para>
	/// </remarks>
	public class MarkdownView : ContentView
	{
		/// <summary>
		/// Blocks built before the text is first shown in Inline mode. The rest follow a few at a time.
		/// </summary>
		private const int firstBatch = 12;

		/// <summary>
		/// Blocks built between each pause in Inline mode.
		/// </summary>
		private const int laterBatch = 8;

		/// <summary>
		/// Minimum number of top-level headings for the Contents button to show.
		/// </summary>
		private const int minHeadingsForContents = 2;

		/// <summary>
		/// Room below the text in Document mode when the floating Contents button shows, so the last lines can scroll out
		/// from under it.
		/// </summary>
		private const double contentsButtonRoom = 72;

		#region Bindable properties

		/// <summary>See <see cref="Markdown"/>.</summary>
		public static readonly BindableProperty MarkdownProperty = BindableProperty.Create(
			nameof(Markdown), typeof(string), typeof(MarkdownView), null, propertyChanged: OnSourceChanged);

		/// <summary>See <see cref="Document"/>.</summary>
		public static readonly BindableProperty DocumentProperty = BindableProperty.Create(
			nameof(Document), typeof(MarkdownDocument), typeof(MarkdownView), null, propertyChanged: OnSourceChanged);

		/// <summary>See <see cref="Model"/>.</summary>
		public static readonly BindableProperty ModelProperty = BindableProperty.Create(
			nameof(Model), typeof(MarkdownModel), typeof(MarkdownView), null, propertyChanged: OnSourceChanged);

		/// <summary>See <see cref="InlineCodeIsPlaceholder"/>.</summary>
		public static readonly BindableProperty InlineCodeIsPlaceholderProperty = BindableProperty.Create(
			nameof(InlineCodeIsPlaceholder), typeof(bool), typeof(MarkdownView), false, propertyChanged: OnSourceChanged);

		/// <summary>See <see cref="Mode"/>.</summary>
		public static readonly BindableProperty ModeProperty = BindableProperty.Create(
			nameof(Mode), typeof(MarkdownMode), typeof(MarkdownView), MarkdownMode.Inline, propertyChanged: OnLayoutChanged);

		/// <summary>See <see cref="Density"/>.</summary>
		public static readonly BindableProperty DensityProperty = BindableProperty.Create(
			nameof(Density), typeof(MarkdownDensity), typeof(MarkdownView), MarkdownDensity.Comfortable, propertyChanged: OnLayoutChanged);

		/// <summary>See <see cref="CollapsedBlockCount"/>.</summary>
		public static readonly BindableProperty CollapsedBlockCountProperty = BindableProperty.Create(
			nameof(CollapsedBlockCount), typeof(int), typeof(MarkdownView), 0, propertyChanged: OnLayoutChanged);

		/// <summary>See <see cref="TextPadding"/>.</summary>
		public static readonly BindableProperty TextPaddingProperty = BindableProperty.Create(
			nameof(TextPadding), typeof(Thickness), typeof(MarkdownView), new Thickness(0), propertyChanged: OnLayoutChanged);

		/// <summary>See <see cref="ShowsContentsButton"/>.</summary>
		public static readonly BindableProperty ShowsContentsButtonProperty = BindableProperty.Create(
			nameof(ShowsContentsButton), typeof(bool), typeof(MarkdownView), true, propertyChanged: OnLayoutChanged);

		/// <summary>See <see cref="KeepsPositionOnUpdate"/>.</summary>
		public static readonly BindableProperty KeepsPositionOnUpdateProperty = BindableProperty.Create(
			nameof(KeepsPositionOnUpdate), typeof(bool), typeof(MarkdownView), false);

		/// <summary>See <see cref="LinkCommand"/>.</summary>
		public static readonly BindableProperty LinkCommandProperty = BindableProperty.Create(
			nameof(LinkCommand), typeof(ICommand), typeof(MarkdownView), null);

		/// <summary>See <see cref="Tone"/>.</summary>
		public static readonly BindableProperty ToneProperty = BindableProperty.Create(
			nameof(Tone), typeof(MarkdownTone), typeof(MarkdownView), MarkdownTone.Default, propertyChanged: OnLayoutChanged);

		/// <summary>See <see cref="ImageSubtitle"/>.</summary>
		public static readonly BindableProperty ImageSubtitleProperty = BindableProperty.Create(
			nameof(ImageSubtitle), typeof(string), typeof(MarkdownView), null, propertyChanged: OnLayoutChanged);

		private static readonly BindablePropertyKey isBuildingPropertyKey = BindableProperty.CreateReadOnly(
			nameof(IsBuilding), typeof(bool), typeof(MarkdownView), false);

		/// <summary>See <see cref="IsBuilding"/>.</summary>
		public static readonly BindableProperty IsBuildingProperty = isBuildingPropertyKey.BindableProperty;

		private static readonly BindablePropertyKey hasTextPropertyKey = BindableProperty.CreateReadOnly(
			nameof(HasText), typeof(bool), typeof(MarkdownView), false);

		/// <summary>See <see cref="HasText"/>.</summary>
		public static readonly BindableProperty HasTextProperty = hasTextPropertyKey.BindableProperty;

		private static readonly BindablePropertyKey hasContentsPropertyKey = BindableProperty.CreateReadOnly(
			nameof(HasContents), typeof(bool), typeof(MarkdownView), false);

		/// <summary>See <see cref="HasContents"/>.</summary>
		public static readonly BindableProperty HasContentsProperty = hasContentsPropertyKey.BindableProperty;

		/// <summary>
		/// Markdown text to show.
		/// </summary>
		public string? Markdown
		{
			get => (string?)this.GetValue(MarkdownProperty);
			set => this.SetValue(MarkdownProperty, value);
		}

		/// <summary>
		/// Parsed markdown document to show, such as a contract text.
		/// </summary>
		public MarkdownDocument? Document
		{
			get => (MarkdownDocument?)this.GetValue(DocumentProperty);
			set => this.SetValue(DocumentProperty, value);
		}

		/// <summary>
		/// Text model to show, built with <see cref="MarkdownModel.FromMarkdownAsync"/> or
		/// <see cref="MarkdownModel.FromDocumentAsync"/>, for a view model that needs to look at the text first.
		/// </summary>
		public MarkdownModel? Model
		{
			get => (MarkdownModel?)this.GetValue(ModelProperty);
			set => this.SetValue(ModelProperty, value);
		}

		/// <summary>
		/// If inline code is a value that is still missing, as in contract texts, where a parameter without a value shows its
		/// guide text. Such values show on a warning tint. Not used when <see cref="Model"/> is set.
		/// </summary>
		public bool InlineCodeIsPlaceholder
		{
			get => (bool)this.GetValue(InlineCodeIsPlaceholderProperty);
			set => this.SetValue(InlineCodeIsPlaceholderProperty, value);
		}

		/// <summary>
		/// If the text sits in the page's scrolling (<see cref="MarkdownMode.Inline"/>), or owns its scrolling
		/// (<see cref="MarkdownMode.Document"/>).
		/// </summary>
		public MarkdownMode Mode
		{
			get => (MarkdownMode)this.GetValue(ModeProperty);
			set => this.SetValue(ModeProperty, value);
		}

		/// <summary>
		/// How much room the text takes.
		/// </summary>
		public MarkdownDensity Density
		{
			get => (MarkdownDensity)this.GetValue(DensityProperty);
			set => this.SetValue(DensityProperty, value);
		}

		/// <summary>
		/// In Inline mode, how many blocks show before Show more. Zero shows all. The text is never cut right after a
		/// heading.
		/// </summary>
		public int CollapsedBlockCount
		{
			get => (int)this.GetValue(CollapsedBlockCountProperty);
			set => this.SetValue(CollapsedBlockCountProperty, value);
		}

		/// <summary>
		/// Room around the text. In Document mode, the text scrolls under it at the top and bottom.
		/// </summary>
		public Thickness TextPadding
		{
			get => (Thickness)this.GetValue(TextPaddingProperty);
			set => this.SetValue(TextPaddingProperty, value);
		}

		/// <summary>
		/// In Document mode, if a Contents button shows when the text has headings.
		/// </summary>
		public bool ShowsContentsButton
		{
			get => (bool)this.GetValue(ShowsContentsButtonProperty);
			set => this.SetValue(ShowsContentsButtonProperty, value);
		}

		/// <summary>
		/// In Document mode, if the text stays where it was scrolled to when it is updated, as a report that refreshes does.
		/// </summary>
		public bool KeepsPositionOnUpdate
		{
			get => (bool)this.GetValue(KeepsPositionOnUpdateProperty);
			set => this.SetValue(KeepsPositionOnUpdateProperty, value);
		}

		/// <summary>
		/// Runs when a link or a card is tapped, with the link as parameter. Without it, links open as the app opens links:
		/// app links in the app, and web links after asking.
		/// </summary>
		public ICommand? LinkCommand
		{
			get => (ICommand?)this.GetValue(LinkCommandProperty);
			set => this.SetValue(LinkCommandProperty, value);
		}

		/// <summary>
		/// What the text is shown on: the page's own surfaces, or a surface in the accent color, such as a sent chat
		/// message, where the text is white. Cards and images keep their own look.
		/// </summary>
		public MarkdownTone Tone
		{
			get => (MarkdownTone)this.GetValue(ToneProperty);
			set => this.SetValue(ToneProperty, value);
		}

		/// <summary>
		/// The line under the title when an image is opened in the viewer, such as who sent it and when. Null shows where
		/// the image is from, as for a chart drawn by the server, and an empty text shows no line.
		/// </summary>
		public string? ImageSubtitle
		{
			get => (string?)this.GetValue(ImageSubtitleProperty);
			set => this.SetValue(ImageSubtitleProperty, value);
		}

		/// <summary>
		/// If the text is being parsed and built.
		/// </summary>
		public bool IsBuilding
		{
			get => (bool)this.GetValue(IsBuildingProperty);
			private set => this.SetValue(isBuildingPropertyKey, value);
		}

		/// <summary>
		/// If there is text to show.
		/// </summary>
		public bool HasText
		{
			get => (bool)this.GetValue(HasTextProperty);
			private set => this.SetValue(hasTextPropertyKey, value);
		}

		/// <summary>
		/// If the text has enough headings for Contents to be useful.
		/// </summary>
		public bool HasContents
		{
			get => (bool)this.GetValue(HasContentsProperty);
			private set => this.SetValue(hasContentsPropertyKey, value);
		}

		/// <summary>
		/// Shows the Contents sheet, for a page that shows its own Contents button. Set
		/// <see cref="ShowsContentsButton"/> to false on such a page.
		/// </summary>
		public ICommand ShowContentsCommand { get; }

		#endregion

		private MarkdownModel shownModel = MarkdownModel.Empty;
		private MarkdownViewFactory? factory;
		private VerticalStackLayout? stack;
		private ScrollView? scroll;
		private Grid? documentLayout;
		private View? contentsButton;
		private readonly List<View> collapsedViews = [];
		private int sourceVersion;
		private int renderVersion;
		private bool expanded;

		/// <summary>
		/// Shows markdown text.
		/// </summary>
		public MarkdownView()
		{
			this.ShowContentsCommand = new Command(async () => await this.ShowContentsAsync());
		}

		/// <summary>
		/// The text model shown.
		/// </summary>
		public MarkdownModel ShownModel => this.shownModel;

		/// <summary>
		/// Raised on the UI thread when a new text model is shown.
		/// </summary>
		public event EventHandler? ModelShown;

		#region Building

		private static void OnSourceChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			if (Bindable is MarkdownView View)
				View.StartBuild();
		}

		private static void OnLayoutChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			if (Bindable is MarkdownView View)
			{
				View.factory = null;
				View.Render(false);
			}
		}

		private void StartBuild()
		{
			int Version = ++this.sourceVersion;

			MarkdownModel? Model = this.Model;
			if (Model is not null)
			{
				this.Show(Model);
				return;
			}

			MarkdownDocument? Doc = this.Document;
			string? Markdown = this.Markdown;

			if (Doc is null && string.IsNullOrWhiteSpace(Markdown))
			{
				this.Show(MarkdownModel.Empty);
				return;
			}

			MarkdownBuildOptions Options = this.InlineCodeIsPlaceholder ? MarkdownBuildOptions.Contract : MarkdownBuildOptions.Default;

			this.IsBuilding = true;
			_ = this.BuildAsync(Version, Doc, Markdown, Options);
		}

		private async Task BuildAsync(int Version, MarkdownDocument? Doc, string? Markdown, MarkdownBuildOptions Options)
		{
			MarkdownModel Model;

			try
			{
				Model = Doc is not null ?
					await MarkdownModel.FromDocumentAsync(Doc, Options) :
					await MarkdownModel.FromMarkdownAsync(Markdown, Options);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				Model = new MarkdownModel([new MdNotice { Text = Ex.Message }]);
			}

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (Version == this.sourceVersion)
					this.Show(Model);
			});
		}

		private void Show(MarkdownModel Model)
		{
			bool IsUpdate = !this.shownModel.IsEmpty && !Model.IsEmpty;

			this.shownModel = Model;
			this.expanded = false;
			this.IsBuilding = false;
			this.HasText = !Model.IsEmpty;
			this.HasContents = Model.Headings.Count >= minHeadingsForContents;

			this.Render(IsUpdate && this.KeepsPositionOnUpdate);
			this.ModelShown?.Invoke(this, EventArgs.Empty);
		}

		#endregion

		#region Rendering

		private MarkdownViewFactory Factory
		{
			get
			{
				this.factory ??= new MarkdownViewFactory(MarkdownMetrics.For(this.Density), this.OnLinkTapped, this.Tone, this.ImageSubtitle);
				return this.factory;
			}
		}

		private void Render(bool KeepPosition)
		{
			int Version = ++this.renderVersion;

			if (this.Mode == MarkdownMode.Document)
				_ = this.RenderDocumentAsync(Version, KeepPosition);
			else
				_ = this.RenderInlineAsync(Version);
		}

		/// <summary>
		/// Inline mode: all blocks in a stack, the first ones at once and the rest a few at a time.
		/// </summary>
		private async Task RenderInlineAsync(int Version)
		{
			this.scroll = null;
			this.documentLayout = null;
			this.contentsButton = null;
			this.collapsedViews.Clear();

			MarkdownViewFactory Factory = this.Factory;
			IReadOnlyList<MdBlock> Blocks = this.shownModel.Blocks;
			int Shown = this.CollapsedIndex(Blocks);

			VerticalStackLayout Stack = new()
			{
				Spacing = Factory.Metrics.BlockSpacing,
				Padding = this.TextPadding
			};

			this.stack = Stack;

			try
			{
				bool Done = await this.AddBlocksAsync(Stack, Shown, Version, () =>
				{
					if (this.Content != Stack)
						this.Content = Stack;
				});

				if (Done && Shown < Blocks.Count)
					Stack.Add(this.CreateShowMoreButton());
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Adds the views of the first <paramref name="Count"/> blocks to a stack: the first ones at once, and the rest a
		/// few at a time, so the first lines show at once.
		/// </summary>
		/// <param name="Stack">The stack to add the views to.</param>
		/// <param name="Count">Number of blocks to add.</param>
		/// <param name="Version">The render the views are for.</param>
		/// <param name="ShowStack">Puts the stack on screen. Called when the first batch is added, or at the end.</param>
		/// <returns>If all blocks were added, or false if a newer render started first.</returns>
		private async Task<bool> AddBlocksAsync(VerticalStackLayout Stack, int Count, int Version, Action ShowStack)
		{
			MarkdownViewFactory Factory = this.Factory;
			IReadOnlyList<MdBlock> Blocks = this.shownModel.Blocks;
			bool Shown = false;

			for (int i = 0; i < Count; i++)
			{
				Stack.Add(Factory.Create(Blocks[i], i == 0));

				if (i + 1 == firstBatch)
				{
					ShowStack();
					Shown = true;
				}
				else if (i + 1 > firstBatch && (i + 1 - firstBatch) % laterBatch == 0)
				{
					await Task.Delay(1);
					if (Version != this.renderVersion)
						return false;
				}
			}

			if (!Shown)
				ShowStack();

			return true;
		}

		/// <summary>
		/// Where Show more cuts the text, or the number of blocks if it is not cut. Cutting is not worth it for one block,
		/// and the text is not cut right after a heading.
		/// </summary>
		private int CollapsedIndex(IReadOnlyList<MdBlock> Blocks)
		{
			int Max = this.CollapsedBlockCount;

			if (Max <= 0 || this.expanded || Blocks.Count <= Max + 1)
				return Blocks.Count;

			int Cut = Max;
			while (Cut > 1 && Blocks[Cut - 1] is MdHeading)
				Cut--;

			return Cut;
		}

		private View CreateShowMoreButton()
		{
			string ShowMore = ServiceRef.Localizer[nameof(AppResources.ShowMore)];
			string ShowLess = ServiceRef.Localizer[nameof(AppResources.ShowLess)];

			Label Caption = new()
			{
				Text = ShowMore,
				FontFamily = MarkdownFonts.Medium,
				FontSize = this.Factory.Metrics.BodySize - 1,
				VerticalOptions = LayoutOptions.Center
			};
			Caption.SetDynamicResource(Label.TextColorProperty, MarkdownColors.Accent);

			Microsoft.Maui.Controls.Shapes.Path Chevron = new()
			{
				Data = Geometries.DropDownArrowPath,
				Aspect = Stretch.Uniform,
				WidthRequest = 12,
				HeightRequest = 12,
				VerticalOptions = LayoutOptions.Center
			};
			Chevron.SetDynamicResource(Microsoft.Maui.Controls.Shapes.Path.FillProperty, MarkdownColors.Accent);

			HorizontalStackLayout Button = new()
			{
				Spacing = 6,
				MinimumHeightRequest = 48,
				MinimumWidthRequest = 48,
				HorizontalOptions = LayoutOptions.Start,
				Children = { Caption, Chevron }
			};
			SemanticProperties.SetDescription(Button, ShowMore);

			TapGestureRecognizer Tap = new();
			Tap.Tapped += async (Sender, e) =>
			{
				try
				{
					this.expanded = !this.expanded;
					Caption.Text = this.expanded ? ShowLess : ShowMore;
					Chevron.Rotation = this.expanded ? 180 : 0;
					SemanticProperties.SetDescription(Button, Caption.Text);

					if (this.expanded)
						await this.ExpandAsync(Button);
					else
					{
						foreach (View View in this.collapsedViews)
							View.IsVisible = false;
					}
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			};
			Button.GestureRecognizers.Add(Tap);

			return Button;
		}

		private async Task ExpandAsync(View Button)
		{
			VerticalStackLayout? Stack = this.stack;
			if (Stack is null)
				return;

			if (this.collapsedViews.Count > 0)
			{
				foreach (View View in this.collapsedViews)
					View.IsVisible = true;

				return;
			}

			int Version = this.renderVersion;
			IReadOnlyList<MdBlock> Blocks = this.shownModel.Blocks;
			int Start = Stack.IndexOf(Button);
			MarkdownViewFactory Factory = this.Factory;

			for (int i = Start; i < Blocks.Count; i++)
			{
				View View = Factory.Create(Blocks[i], false);
				this.collapsedViews.Add(View);
				Stack.Insert(Stack.IndexOf(Button), View);

				if ((i - Start + 1) % laterBatch == 0)
				{
					await Task.Delay(1);
					if (Version != this.renderVersion)
						return;
				}
			}
		}

		/// <summary>
		/// Document mode: all blocks in a stack in the view's own scrolling, the first ones at once and the rest a few at a
		/// time.
		/// </summary>
		/// <remarks>
		/// A list with one item per block would only build the blocks on screen, but on Android it gives blocks of
		/// different heights the wrong height, which cuts their text off. A stack measures every block.
		/// </remarks>
		private async Task RenderDocumentAsync(int Version, bool KeepPosition)
		{
			double KeepY = KeepPosition && this.scroll is not null ? this.scroll.ScrollY : 0;

			this.collapsedViews.Clear();

			Thickness Padding = this.TextPadding;
			bool ShowContents = this.ShowsContentsButton && this.HasContents;

			VerticalStackLayout Stack = new()
			{
				Spacing = this.Factory.Metrics.BlockSpacing,
				Padding = new Thickness(Padding.Left, Padding.Top, Padding.Right, Padding.Bottom + (ShowContents ? contentsButtonRoom : 0))
			};

			this.stack = Stack;

			ScrollView Scroll = this.ShowDocumentLayout(ShowContents);

			try
			{
				// A text that is updated in place is built off screen, and then shown where the reader was.
				bool Done = await this.AddBlocksAsync(Stack, this.shownModel.Blocks.Count, Version, () =>
				{
					if (KeepY <= 0 && Scroll.Content != Stack)
					{
						Scroll.Content = Stack;

						if (Scroll.ScrollY > 0)
							_ = Scroll.ScrollToAsync(0, 0, false);
					}
				});

				if (Done && KeepY > 0)
				{
					Scroll.Content = Stack;
					RestoreScrollPosition(Scroll, Stack, KeepY);
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Shows the Document mode layout: the scrolling text, with the floating Contents button over it if it shows.
		/// </summary>
		/// <param name="ShowContents">If the floating Contents button shows.</param>
		/// <returns>The scroll view that holds the text.</returns>
		private ScrollView ShowDocumentLayout(bool ShowContents)
		{
			if (this.scroll is null || this.documentLayout is null)
			{
				this.scroll = new ScrollView();
				this.documentLayout = new Grid { Children = { this.scroll } };
			}

			if (this.contentsButton is not null)
			{
				this.documentLayout.Remove(this.contentsButton);
				this.contentsButton = null;
			}

			if (ShowContents)
			{
				this.contentsButton = this.CreateContentsButton();
				this.documentLayout.Add(this.contentsButton);
			}

			if (this.Content != this.documentLayout)
				this.Content = this.documentLayout;

			return this.scroll;
		}

		/// <summary>
		/// Scrolls to where the reader was once the new text has been laid out.
		/// </summary>
		/// <param name="Scroll">The scroll view that holds the text.</param>
		/// <param name="Stack">The new text.</param>
		/// <param name="ScrollY">Where the reader was.</param>
		private static void RestoreScrollPosition(ScrollView Scroll, VerticalStackLayout Stack, double ScrollY)
		{
			void OnSizeChanged(object? Sender, EventArgs e)
			{
				Stack.SizeChanged -= OnSizeChanged;
				_ = Scroll.ScrollToAsync(0, ScrollY, false);
			}

			Stack.SizeChanged += OnSizeChanged;
		}

		/// <summary>
		/// In Document mode, the index of the block at the top of the screen.
		/// </summary>
		private int FirstVisibleBlockIndex()
		{
			VerticalStackLayout? Stack = this.stack;
			ScrollView? Scroll = this.scroll;

			if (Stack is null || Scroll is null)
				return 0;

			int Index = 0;

			for (int i = 0; i < Stack.Count; i++)
			{
				if (Stack[i] is not View Block || Block.Y > Scroll.ScrollY + 1)
					break;

				Index = i;
			}

			return Index;
		}

		/// <summary>
		/// The Contents button floats at the bottom of the text. It keeps a tap area of at least 48 by 48.
		/// </summary>
		private View CreateContentsButton()
		{
			string Text = ServiceRef.Localizer[nameof(AppResources.MarkdownContents)];

			Microsoft.Maui.Controls.Shapes.Path Icon = new()
			{
				Data = Geometries.HamburgerIconPath,
				Aspect = Stretch.Uniform,
				WidthRequest = 16,
				HeightRequest = 16,
				VerticalOptions = LayoutOptions.Center
			};
			Icon.SetDynamicResource(Microsoft.Maui.Controls.Shapes.Path.FillProperty, MarkdownColors.Text);

			Label Caption = new()
			{
				Text = Text,
				FontFamily = MarkdownFonts.Medium,
				FontSize = 14,
				VerticalOptions = LayoutOptions.Center,
				LineBreakMode = LineBreakMode.NoWrap
			};
			Caption.SetDynamicResource(Label.TextColorProperty, MarkdownColors.Text);
			AutomationProperties.SetIsInAccessibleTree(Caption, false);

			Border Button = new()
			{
				StrokeThickness = 1,
				StrokeShape = new RoundRectangle { CornerRadius = 24 },
				Padding = new Thickness(16, 0),
				MinimumHeightRequest = 48,
				MinimumWidthRequest = 48,
				HorizontalOptions = LayoutOptions.End,
				VerticalOptions = LayoutOptions.End,
				Margin = new Thickness(16),
				Shadow = new Shadow { Opacity = 0.12f, Radius = 8, Offset = new Point(0, 2) },
				Content = new HorizontalStackLayout
				{
					Spacing = 8,
					VerticalOptions = LayoutOptions.Center,
					Children = { Icon, Caption }
				}
			};
			Button.SetDynamicResource(VisualElement.BackgroundColorProperty, MarkdownColors.Card);
			Button.SetDynamicResource(Border.StrokeProperty, MarkdownColors.Line);
			SemanticProperties.SetDescription(Button, Text);

			TapGestureRecognizer Tap = new();
			Tap.Tapped += async (Sender, e) => await this.ShowContentsAsync();
			Button.GestureRecognizers.Add(Tap);

			return Button;
		}

		#endregion

		#region Links and Contents

		private void OnLinkTapped(string Url)
		{
			if (Url.StartsWith('#'))
			{
				_ = this.ScrollToAnchorAsync(Url[1..]);
				return;
			}

			ICommand? Command = this.LinkCommand;
			if (Command is not null)
			{
				if (Command.CanExecute(Url))
					Command.Execute(Url);

				return;
			}

			_ = OpenLinkAsync(Url);
		}

		private static async Task OpenLinkAsync(string Url)
		{
			try
			{
				await App.OpenUrlAsync(Url);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private Task ScrollToAnchorAsync(string Anchor)
		{
			IReadOnlyList<MdBlock> Blocks = this.shownModel.Blocks;

			for (int i = 0; i < Blocks.Count; i++)
			{
				MdBlock Block = Blocks[i];

				if ((Block is MdHeading Heading && string.Equals(Heading.Anchor, Anchor, StringComparison.OrdinalIgnoreCase)) ||
					(Block is MdFootnotes && Anchor.StartsWith("fn:", StringComparison.Ordinal)))
				{
					return this.ScrollToBlockAsync(i);
				}
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Scrolls so that a heading is at the top.
		/// </summary>
		/// <param name="Heading">A top-level heading of the text shown.</param>
		public Task ScrollToHeadingAsync(MdHeading Heading)
		{
			int Index = IndexOf(this.shownModel.Blocks, Heading);
			return Index < 0 ? Task.CompletedTask : this.ScrollToBlockAsync(Index);
		}

		private static int IndexOf(IReadOnlyList<MdBlock> Blocks, MdBlock Block)
		{
			for (int i = 0; i < Blocks.Count; i++)
			{
				if (ReferenceEquals(Blocks[i], Block))
					return i;
			}

			return -1;
		}

		private async Task ScrollToBlockAsync(int Index)
		{
			try
			{
				if (this.Mode == MarkdownMode.Document)
				{
					await this.ScrollDocumentToBlockAsync(Index);
					return;
				}

				VerticalStackLayout? Stack = this.stack;
				if (Stack is null)
					return;

				if (Index >= this.CollapsedIndex(this.shownModel.Blocks) && !this.expanded && Stack.Count > 0 && Stack[^1] is View Button)
				{
					this.expanded = true;
					await this.ExpandAsync(Button);
				}

				View? Target = null;
				int i = 0;

				foreach (IView Child in Stack)
				{
					if (Child is View View && View.IsVisible && i++ == Index)
					{
						Target = View;
						break;
					}
				}

				Element? Parent = this.Parent;
				while (Parent is not null && Parent is not ScrollView)
					Parent = Parent.Parent;

				if (Target is not null && Parent is ScrollView Scroll)
				{
					await Scroll.ScrollToAsync(Target, ScrollToPosition.Start, true);
					Target.SetSemanticFocus();
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// In Document mode, scrolls so that a block is at the top, and moves the screen reader to it. A block that is not
		/// built yet is skipped.
		/// </summary>
		/// <param name="Index">Index of the top-level block.</param>
		private async Task ScrollDocumentToBlockAsync(int Index)
		{
			VerticalStackLayout? Stack = this.stack;
			ScrollView? Scroll = this.scroll;

			if (Stack is null || Scroll is null || Index < 0 || Index >= Stack.Count || Stack[Index] is not View Target)
				return;

			await Scroll.ScrollToAsync(Target, ScrollToPosition.Start, true);
			Target.SetSemanticFocus();
		}

		/// <summary>
		/// Shows the Contents sheet, built from the text's headings, with the section on screen marked.
		/// </summary>
		public async Task ShowContentsAsync()
		{
			IReadOnlyList<MdHeading> Headings = this.shownModel.Headings;
			if (Headings.Count == 0)
				return;

			MdHeading? Current = null;
			if (this.Mode == MarkdownMode.Document)
			{
				IReadOnlyList<MdBlock> Blocks = this.shownModel.Blocks;
				int FirstVisible = this.FirstVisibleBlockIndex();

				for (int i = 0; i < Blocks.Count && i <= FirstVisible; i++)
				{
					if (Blocks[i] is MdHeading Heading)
						Current = Heading;
				}
			}

			try
			{
				MarkdownContentsPopup Popup = new(Headings, Current, this.ScrollToHeadingAsync);
				PopupOptions Options = new()
				{
					Placement = PopupPlacement.Bottom,
					ShowTransition = PopupTransition.SlideUp
				};

				await ServiceRef.PopupService.PushAsync(Popup, Options);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		#endregion
	}
}
