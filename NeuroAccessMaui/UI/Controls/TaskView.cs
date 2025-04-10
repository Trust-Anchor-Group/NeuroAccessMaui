using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeuroAccessMaui.UI.MVVM;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// A ContentView control that displays the state of an asynchronous operation defined by an <see cref="ObservableTask"/>.
	/// </summary>
	/// <typeparam name="TProgress">
	/// The type of the progress value reported by the ObservableTask.
	/// </typeparam>
	/// <remarks>
	/// <para>
	/// <b>Overview:</b> TaskView is designed to simplify the creation of state-specific UIs for asynchronous operations.
	/// It supports different visual representations (for example, Pending, Running, Refreshing, Succeeded, and Error states)
	/// using one of three approaches:
	/// </para>
	/// <list type="bullet">
	///     <item>
	///         <description>
	///             <b>Inline Views</b> – Directly defined content in XAML using properties such as <see cref="RunningView"/>.
	///         </description>
	///     </item>
	///     <item>
	///         <description>
	///             <b>DataTemplates</b> – Defined in resources and assigned via properties like <see cref="RunningTemplate"/>.
	///         </description>
	///     </item>
	///     <item>
	///         <description>
	///             <b>ControlTemplates</b> – Defined for advanced scenarios via properties like <see cref="RunningControlTemplate"/>.
	///         </description>
	///     </item>
	/// </list>
	/// <para>
	/// <b>Content Selection Order:</b> For a given state, the control checks for content in the following order:
	/// 1. Inline view (e.g. <see cref="RunningView"/>)
	/// 2. DataTemplate (e.g. <see cref="RunningTemplate"/>)
	/// 3. ControlTemplate (e.g. <see cref="RunningControlTemplate"/>)
	/// 4. Default content (<see cref="DefaultView"/>, <see cref="DefaultTemplate"/>, or <see cref="DefaultControlTemplate"/>)
	/// </para>
	/// <para>
	/// <b>State Grouping:</b> If specific content for a state (for example, Pending) is not provided, TaskView will fallback to content defined for a related state (such as Running).
	/// </para>
	/// <para>
	/// <b>Transitions:</b> The control uses fade animations to transition smoothly between state changes.
	/// </para>
	/// <para>
	/// <b>Usage:</b> Bind an instance of <see cref="ObservableTask"/> to the <see cref="Task"/> property and provide the desired state-specific content.
	/// </para>
	/// </remarks>
	/// <example>
	/// The following XAML example shows how to define inline content for the Running state along with DataTemplates for other states:
	/// <code language="xml">
	/// &lt;ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
	///              xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
	///              xmlns:controls="clr-namespace:NeuroAccessMaui.UI.Controls"
	///              x:Class="NeuroAccessMaui.Views.SamplePage"&gt;
	///     &lt;ContentPage.Resources&gt;
	///         &lt;DataTemplate x:Key="PendingTemplate"&gt;
	///             &lt;Label Text="Please wait..."
	///                    HorizontalOptions="Center"
	///                    VerticalOptions="Center" /&gt;
	///         &lt;/DataTemplate&gt;
	///         &lt;DataTemplate x:Key="SucceededTemplate"&gt;
	///             &lt;Label Text="Operation completed!"
	///                    HorizontalOptions="Center"
	///                    VerticalOptions="Center" /&gt;
	///         &lt;/DataTemplate&gt;
	///         &lt;DataTemplate x:Key="ErrorTemplate"&gt;
	///             &lt;Label Text="Something went wrong."
	///                    HorizontalOptions="Center"
	///                    VerticalOptions="Center" /&gt;
	///         &lt;/DataTemplate&gt;
	///     &lt;/ContentPage.Resources&gt;
	///
	///     &lt;controls:TaskView Task="{Binding MyObservableTask}"
	///                        PendingTemplate="{StaticResource PendingTemplate}"
	///                        SucceededTemplate="{StaticResource SucceededTemplate}"
	///                        ErrorTemplate="{StaticResource ErrorTemplate}"&gt;
	///         &lt;controls:TaskView.RunningView&gt;
	///             &lt;StackLayout HorizontalOptions="Center" VerticalOptions="Center"&gt;
	///                 &lt;ActivityIndicator IsRunning="True" /&gt;
	///                 &lt;Label Text="Loading data..." /&gt;
	///             &lt;/StackLayout&gt;
	///         &lt;/controls:TaskView.RunningView&gt;
	///     &lt;/controls:TaskView&gt;
	/// &lt;/ContentPage&gt;
	/// </code>
	/// </example>
	public class TaskView<TProgress> : ContentView
	{
		#region Bindable Properties

		// The bound ObservableTask.
		public static readonly BindableProperty TaskProperty =
			 BindableProperty.Create(nameof(Task), typeof(ObservableTask), typeof(TaskView<TProgress>),
				  default(ObservableTask), propertyChanged: OnTaskChanged);

		public ObservableTask Task
		{
			get => (ObservableTask)this.GetValue(TaskProperty);
			set => this.SetValue(TaskProperty, value);
		}

		// --- DataTemplate properties ---
		public static readonly BindableProperty PendingTemplateProperty =
			 BindableProperty.Create(nameof(PendingTemplate), typeof(DataTemplate), typeof(TaskView<TProgress>), default(DataTemplate));
		public DataTemplate PendingTemplate
		{
			get => (DataTemplate)this.GetValue(PendingTemplateProperty);
			set => this.SetValue(PendingTemplateProperty, value);
		}

		public static readonly BindableProperty RunningTemplateProperty =
			 BindableProperty.Create(nameof(RunningTemplate), typeof(DataTemplate), typeof(TaskView<TProgress>), default(DataTemplate));
		public DataTemplate RunningTemplate
		{
			get => (DataTemplate)this.GetValue(RunningTemplateProperty);
			set => this.SetValue(RunningTemplateProperty, value);
		}

		public static readonly BindableProperty RefreshingTemplateProperty =
			 BindableProperty.Create(nameof(RefreshingTemplate), typeof(DataTemplate), typeof(TaskView<TProgress>), default(DataTemplate));
		public DataTemplate RefreshingTemplate
		{
			get => (DataTemplate)this.GetValue(RefreshingTemplateProperty);
			set => this.SetValue(RefreshingTemplateProperty, value);
		}

		public static readonly BindableProperty SucceededTemplateProperty =
			 BindableProperty.Create(nameof(SucceededTemplate), typeof(DataTemplate), typeof(TaskView<TProgress>), default(DataTemplate));
		public DataTemplate SucceededTemplate
		{
			get => (DataTemplate)this.GetValue(SucceededTemplateProperty);
			set => this.SetValue(SucceededTemplateProperty, value);
		}

		public static readonly BindableProperty ErrorTemplateProperty =
			 BindableProperty.Create(nameof(ErrorTemplate), typeof(DataTemplate), typeof(TaskView<TProgress>), default(DataTemplate));
		public DataTemplate ErrorTemplate
		{
			get => (DataTemplate)this.GetValue(ErrorTemplateProperty);
			set => this.SetValue(ErrorTemplateProperty, value);
		}

		public static readonly BindableProperty DefaultTemplateProperty =
			 BindableProperty.Create(nameof(DefaultTemplate), typeof(DataTemplate), typeof(TaskView<TProgress>), default(DataTemplate));
		public DataTemplate DefaultTemplate
		{
			get => (DataTemplate)this.GetValue(DefaultTemplateProperty);
			set => this.SetValue(DefaultTemplateProperty, value);
		}

		// --- Inline View properties ---
		public static readonly BindableProperty PendingViewProperty =
			 BindableProperty.Create(nameof(PendingView), typeof(View), typeof(TaskView<TProgress>), default(View));
		public View PendingView
		{
			get => (View)this.GetValue(PendingViewProperty);
			set => this.SetValue(PendingViewProperty, value);
		}

		public static readonly BindableProperty RunningViewProperty =
			 BindableProperty.Create(nameof(RunningView), typeof(View), typeof(TaskView<TProgress>), default(View));
		public View RunningView
		{
			get => (View)this.GetValue(RunningViewProperty);
			set => this.SetValue(RunningViewProperty, value);
		}

		public static readonly BindableProperty RefreshingViewProperty =
			 BindableProperty.Create(nameof(RefreshingView), typeof(View), typeof(TaskView<TProgress>), default(View));
		public View RefreshingView
		{
			get => (View)this.GetValue(RefreshingViewProperty);
			set => this.SetValue(RefreshingViewProperty, value);
		}

		public static readonly BindableProperty SucceededViewProperty =
			 BindableProperty.Create(nameof(SucceededView), typeof(View), typeof(TaskView<TProgress>), default(View));
		public View SucceededView
		{
			get => (View)this.GetValue(SucceededViewProperty);
			set => this.SetValue(SucceededViewProperty, value);
		}

		public static readonly BindableProperty ErrorViewProperty =
			 BindableProperty.Create(nameof(ErrorView), typeof(View), typeof(TaskView<TProgress>), default(View));
		public View ErrorView
		{
			get => (View)this.GetValue(ErrorViewProperty);
			set => this.SetValue(ErrorViewProperty, value);
		}

		public static readonly BindableProperty DefaultViewProperty =
			 BindableProperty.Create(nameof(DefaultView), typeof(View), typeof(TaskView<TProgress>), default(View));
		public View DefaultView
		{
			get => (View)this.GetValue(DefaultViewProperty);
			set => this.SetValue(DefaultViewProperty, value);
		}

		// --- ControlTemplate properties ---
		public static readonly BindableProperty PendingControlTemplateProperty =
			 BindableProperty.Create(nameof(PendingControlTemplate), typeof(ControlTemplate), typeof(TaskView<TProgress>), default(ControlTemplate));
		public ControlTemplate PendingControlTemplate
		{
			get => (ControlTemplate)this.GetValue(PendingControlTemplateProperty);
			set => this.SetValue(PendingControlTemplateProperty, value);
		}

		public static readonly BindableProperty RunningControlTemplateProperty =
			 BindableProperty.Create(nameof(RunningControlTemplate), typeof(ControlTemplate), typeof(TaskView<TProgress>), default(ControlTemplate));
		public ControlTemplate RunningControlTemplate
		{
			get => (ControlTemplate)this.GetValue(RunningControlTemplateProperty);
			set => this.SetValue(RunningControlTemplateProperty, value);
		}

		public static readonly BindableProperty RefreshingControlTemplateProperty =
			 BindableProperty.Create(nameof(RefreshingControlTemplate), typeof(ControlTemplate), typeof(TaskView<TProgress>), default(ControlTemplate));
		public ControlTemplate RefreshingControlTemplate
		{
			get => (ControlTemplate)this.GetValue(RefreshingControlTemplateProperty);
			set => this.SetValue(RefreshingControlTemplateProperty, value);
		}

		public static readonly BindableProperty SucceededControlTemplateProperty =
			 BindableProperty.Create(nameof(SucceededControlTemplate), typeof(ControlTemplate), typeof(TaskView<TProgress>), default(ControlTemplate));
		public ControlTemplate SucceededControlTemplate
		{
			get => (ControlTemplate)this.GetValue(SucceededControlTemplateProperty);
			set => this.SetValue(SucceededControlTemplateProperty, value);
		}

		public static readonly BindableProperty ErrorControlTemplateProperty =
			 BindableProperty.Create(nameof(ErrorControlTemplate), typeof(ControlTemplate), typeof(TaskView<TProgress>), default(ControlTemplate));
		public ControlTemplate ErrorControlTemplate
		{
			get => (ControlTemplate)this.GetValue(ErrorControlTemplateProperty);
			set => this.SetValue(ErrorControlTemplateProperty, value);
		}

		public static readonly BindableProperty DefaultControlTemplateProperty =
			 BindableProperty.Create(nameof(DefaultControlTemplate), typeof(ControlTemplate), typeof(TaskView<TProgress>), default(ControlTemplate));
		public ControlTemplate DefaultControlTemplate
		{
			get => (ControlTemplate)this.GetValue(DefaultControlTemplateProperty);
			set => this.SetValue(DefaultControlTemplateProperty, value);
		}

		#endregion

		#region Task Change Handling

		private static void OnTaskChanged(BindableObject bindable, object oldValue, object newValue)
		{
			TaskView<TProgress> Control = (TaskView<TProgress>)bindable;
			if (oldValue is ObservableTask OldTask)
			{
				OldTask.PropertyChanged -= Control.Task_PropertyChanged;
			}
			if (newValue is ObservableTask NewTask)
			{
				NewTask.PropertyChanged += Control.Task_PropertyChanged;
			}
			Control.UpdateContent();
		}

		private void Task_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ObservableTask.State) ||
				 e.PropertyName == nameof(ObservableTask.IsRefreshing))
			{
				MainThread.BeginInvokeOnMainThread(this.UpdateContent);
			}
		}

		#endregion

		#region Content Selection & Creation

		/// <summary>
		/// Helper method that returns a View based on the following order of precedence:
		/// 1. Inline view (if set)
		/// 2. DataTemplate (if set)
		/// 3. ControlTemplate (if set, wrapped in a ContentView)
		/// </summary>
		private View? GetContent(View inlineView, DataTemplate? template, ControlTemplate? controlTemplate)
		{
			if (inlineView != null)
				return inlineView;

			if (template != null)
			{
				object Content = template.CreateContent();
				if (Content is View View)
					return View;
				if (Content is ViewCell Cell)
					return Cell.View;
			}

			if (controlTemplate is not null)
			{
				ContentView View = new ContentView { ControlTemplate = controlTemplate };
				return View;
			}

			return null;
		}

		/// <summary>
		/// Returns the default content if nothing is set for the active state.
		/// </summary>
		private View? GetDefaultContent() => this.GetContent(this.DefaultView, this.DefaultTemplate, this.DefaultControlTemplate);

		/// <summary>
		/// Based on the current Task state, returns the appropriate content view.
		/// </summary>
		private View? SelectContent()
		{
			if (this.Task is null)
				return this.GetDefaultContent();

			// For Pending state: first try Pending-specific content, then fallback to Running.
			if (this.Task.IsPending)
			{
				View? Content = this.GetContent(this.PendingView, this.PendingTemplate, this.PendingControlTemplate);
				if (Content is not null)
					return Content;

				Content = this.GetContent(this.RunningView, this.RunningTemplate, this.RunningControlTemplate);
				if (Content is not null)
					return Content;
			}

			// For Running state: if refreshing try the refresh-specific content.
			if (this.Task.IsRunning)
			{
				if (this.Task.IsRefreshing)
				{
					View? Content = this.GetContent(this.RefreshingView, this.RefreshingTemplate, this.RefreshingControlTemplate);
					if (Content is not null)
						return Content;
				}

				View? RunningContent = this.GetContent(this.RunningView, this.RunningTemplate, this.RunningControlTemplate);
				if (RunningContent is not null)
					return RunningContent;
			}

			// For Succeeded state.
			if (this.Task.IsSucceeded)
			{
				View? Content = this.GetContent(this.SucceededView, this.SucceededTemplate, this.SucceededControlTemplate);
				if (Content is not null)
					return Content;
			}

			// For error conditions (Failed or Canceled).
			if (this.Task.IsFailed || this.Task.IsCanceled)
			{
				View? Content = this.GetContent(this.ErrorView, this.ErrorTemplate, this.ErrorControlTemplate);
				if (Content is not null)
					return Content;
			}

			return this.GetDefaultContent();
		}

		#endregion

		#region Animation & Content Update

		/// <summary>
		/// Instantiates the correct content for the current task state and animates the transition.
		/// </summary>
		private async void UpdateContent()
		{
			View? NewContent = this.SelectContent();
			if (NewContent is null)
				return;

			NewContent.BindingContext = this.BindingContext;

			try
			{
				if (this.Content is not null)
				{
					View OldContent = this.Content;
					await OldContent.FadeTo(0, 250);
					this.Content = NewContent;
					NewContent.Opacity = 0;
					await NewContent.FadeTo(1, 250);
				}
				else
				{
					this.Content = NewContent;
					NewContent.Opacity = 0;
					await NewContent.FadeTo(1, 250);
				}
			}
			catch (Exception)
			{
				this.Content.Opacity = 1;
			}
		}

		protected override void OnBindingContextChanged()
		{
			base.OnBindingContextChanged();

			if (this.Content is not null)
				this.Content.BindingContext = this.BindingContext;
		}
		#endregion
	}

	/// <inheritdoc />
	public class TaskView : TaskView<int>
	{
		// You may customize the non-generic version further if needed.
	}
}
