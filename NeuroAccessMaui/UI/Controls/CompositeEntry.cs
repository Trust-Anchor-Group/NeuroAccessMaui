﻿using System.Windows.Input;
using CommunityToolkit.Maui.Markup;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Core;
using PathShape = Microsoft.Maui.Controls.Shapes.Path;

namespace NeuroAccessMaui.UI.Controls
{
	public class CompositeEntry : ContentView, IBorderDataElement, IStackElement, IPathDataElement, IEntryDataElement
	{
		private readonly Border innerBorder;
		private readonly Grid innerGrid;
		private readonly PathShape innerPath;
		private readonly Entry innerEntry;

		public static readonly BindableProperty FocusedCommandProperty = BindableProperty.Create(
			nameof(FocusedCommand),
			typeof(ICommand),
			typeof(CompositeEntry));

		public ICommand FocusedCommand
		{
			get => (ICommand)this.GetValue(FocusedCommandProperty);
			set => this.SetValue(FocusedCommandProperty, value);
		}

		public static readonly BindableProperty UnfocusedCommandProperty = BindableProperty.Create(
			nameof(UnfocusedCommand),
			typeof(ICommand),
			typeof(CompositeEntry));

		public ICommand UnfocusedCommand
		{
			get => (ICommand)this.GetValue(UnfocusedCommandProperty);
			set => this.SetValue(UnfocusedCommandProperty, value);
		}


		/// <summary>Bindable property for <see cref="Keyboard"/>.</summary>
		public static readonly BindableProperty KeyboardProperty = Entry.KeyboardProperty;

		/// <summary>Bindable property for <see cref="BorderStyle"/>.</summary>
		public static readonly BindableProperty BorderStyleProperty = BorderDataElement.BorderStyleProperty;

		/// <summary>Bindable property for <see cref="StackSpacing"/>.</summary>
		public static readonly BindableProperty StackSpacingProperty = StackElement.StackSpacingProperty;

		/// <summary>Bindable property for <see cref="PathData"/>.</summary>
		public static readonly BindableProperty PathDataProperty = PathDataElement.PathDataProperty;

		/// <summary>Bindable property for <see cref="PathStyle"/>.</summary>
		public static readonly BindableProperty PathStyleProperty = PathDataElement.PathStyleProperty;

		/// <summary>Bindable property for <see cref="EntryData"/>.</summary>
		public static readonly BindableProperty EntryDataProperty = EntryDataElement.EntryDataProperty;

		/// <summary>Bindable property for <see cref="EntryHint"/>.</summary>
		public static readonly BindableProperty EntryHintProperty = EntryDataElement.EntryHintProperty;

		/// <summary>Bindable property for <see cref="EntryStyle"/>.</summary>
		public static readonly BindableProperty EntryStyleProperty = EntryDataElement.EntryStyleProperty;

		/// <summary>Bindable property for <see cref="ReturnCommand"/>.</summary>
		public static readonly BindableProperty ReturnCommandProperty = EntryDataElement.ReturnCommandProperty;

		/// <summary>Bindable property for <see cref="IsPassword"/>.</summary>
		public static readonly BindableProperty IsPasswordProperty = EntryDataElement.IsPasswordProperty;

		/// <summary>Bindable property for <see cref="IsReadOnly"/>.</summary>
		public static readonly BindableProperty IsReadOnlyProperty = InputView.IsReadOnlyProperty;

		/// <summary>Bindable property for <see cref="Placeholder"/>.</summary>
		public static readonly BindableProperty PlaceholderProperty = InputView.PlaceholderProperty;

		public void OnBorderStylePropertyChanged(Style OldValue, Style NewValue)
		{
			this.innerBorder.Style = NewValue;
		}

		public void OnStackSpacingPropertyChanged(double OldValue, double NewValue)
		{
			this.innerGrid.ColumnSpacing = this.innerPath.IsVisible ? NewValue : 0;
		}

		public void OnPathDataPropertyChanged(Geometry OldValue, Geometry NewValue)
		{
			this.innerPath.Data = NewValue;

			if (NewValue is null)
			{
				this.innerPath.IsVisible = false;
				this.innerGrid.ColumnSpacing = 0;
			}
			else
			{
				this.innerPath.IsVisible = true;
				this.innerGrid.ColumnSpacing = this.StackSpacing;
			}
		}

		public void OnPathStylePropertyChanged(Style OldValue, Style NewValue)
		{
			this.innerPath.Style = NewValue;
		}

		public void OnEntryDataPropertyChanged(string OldValue, string NewValue)
		{
			//!!! The Text is already the NewValue.
			//!!! this.innerEntry.Text = NewValue;
		}

		public void OnEntryHintPropertyChanged(string OldValue, string NewValue)
		{
			this.innerEntry.Placeholder = NewValue;
		}

		public void OnEntryStylePropertyChanged(Style OldValue, Style NewValue)
		{
			this.innerEntry.Style = NewValue;
		}

		public void OnReturnCommandPropertyChanged(ICommand OldValue, ICommand NewValue)
		{
			this.innerEntry.ReturnCommand = NewValue;
		}

		public void OnIsPasswordPropertyChanged(bool _, bool NewValue)
		{
			this.innerEntry.IsPassword = NewValue;
		}

		public void OnIsReadOnlyPropertyChanged(bool _, bool NewValue)
		{
			this.innerEntry.IsReadOnly = NewValue;
		}

		public void OnPlaceholderPropertyChanged(string _, string NewValue)
		{
			this.innerEntry.Placeholder = NewValue;
		}

		public void OnBackgroundColorPropertyChanged(Color _, Color NewValue)
		{
			this.innerEntry.BackgroundColor = NewValue;
			this.innerBorder.BackgroundColor = NewValue;
		}


		public Keyboard Keyboard
		{
			get => (Keyboard)this.GetValue(KeyboardProperty);
			set => this.SetValue(KeyboardProperty, value);
		}

		public Style BorderStyle
		{
			get => (Style)this.GetValue(BorderDataElement.BorderStyleProperty);
			set => this.SetValue(BorderDataElement.BorderStyleProperty, value);
		}

		public double StackSpacing
		{
			get => (double)this.GetValue(StackElement.StackSpacingProperty);
			set => this.SetValue(StackElement.StackSpacingProperty, value);
		}

		public Geometry PathData
		{
			get => (Geometry)this.GetValue(PathDataElement.PathDataProperty);
			set => this.SetValue(PathDataElement.PathDataProperty, value);
		}

		public Style PathStyle
		{
			get => (Style)this.GetValue(PathDataElement.PathStyleProperty);
			set => this.SetValue(PathDataElement.PathStyleProperty, value);
		}

		public string EntryData
		{
			get => (string)this.GetValue(EntryDataElement.EntryDataProperty);
			set
			{
				if (!this.IsReadOnly)
					this.SetValue(EntryDataElement.EntryDataProperty, value);
				else
				{
					string OldValue = this.EntryData;

					if (OldValue != value)
					{
						this.OnPropertyChanged(nameof(this.EntryData));
						this.innerEntry.IsReadOnly = false;
					}
				}
			}
		}

		public string EntryHint
		{
			get => (string)this.GetValue(EntryDataElement.EntryHintProperty);
			set => this.SetValue(EntryDataElement.EntryHintProperty, value);
		}

		public Style EntryStyle
		{
			get => (Style)this.GetValue(EntryDataElement.EntryStyleProperty);
			set => this.SetValue(EntryDataElement.EntryStyleProperty, value);
		}

		public ICommand ReturnCommand
		{
			get => (ICommand)this.GetValue(EntryDataElement.ReturnCommandProperty);
			set => this.SetValue(EntryDataElement.ReturnCommandProperty, value);
		}

		public bool IsPassword
		{
			get => (bool)this.GetValue(EntryDataElement.IsPasswordProperty);
			set
			{
				this.SetValue(EntryDataElement.IsPasswordProperty, value);

				if (this.innerEntry is not null)
					this.innerEntry.IsPassword = value;
			}
		}

		public bool IsReadOnly
		{
			get => (bool)this.GetValue(InputView.IsReadOnlyProperty);
			set
			{
				this.SetValue(InputView.IsReadOnlyProperty, value);

				if (this.innerEntry is not null)
					this.innerEntry.IsReadOnly = value;
			}
		}

		public string Placeholder
		{
			get => (string)this.GetValue(InputView.PlaceholderProperty);
			set
			{
				this.SetValue(InputView.PlaceholderProperty, value);

				if (this.innerEntry is not null)
					this.innerEntry.Placeholder = value;
			}
		}

		/// <summary>
		/// Embedded Entry control
		/// </summary>
		public Entry Entry => this.innerEntry;

		/// <summary>
		/// Embedded Border encapsulating the Entry
		/// </summary>
		public Border Border => this.innerBorder;

		/// <inheritdoc/>
		protected override void OnPropertyChanged(string PropertyName)
		{
			base.OnPropertyChanged(PropertyName);

			switch (PropertyName)
			{
				case nameof(this.BackgroundColor):
					if (this.innerEntry is not null)
						this.innerEntry.BackgroundColor = this.BackgroundColor;

					if (this.innerBorder is not null)
						this.innerBorder.BackgroundColor = this.BackgroundColor;
					break;
			}
		}

		/// <summary>
		/// Occurs when the user finalizes the text in an entry with the return key.
		/// </summary>
		public event EventHandler? Completed;

		public override string? ToString()
		{
			string? Result = this.innerEntry.Text;
			return Result;
		}

		public CompositeEntry()
			: base()
		{
			this.innerPath = new()
			{
				VerticalOptions = LayoutOptions.Center,
				IsVisible = this.PathData is not null,
				HeightRequest = 24,
				WidthRequest = 24,
				Aspect = Stretch.Uniform
			};

			this.innerEntry = new()
			{
				VerticalOptions = LayoutOptions.Center,
				HorizontalOptions = LayoutOptions.Fill,
				IsReadOnly = this.IsReadOnly,
				IsPassword = this.IsPassword,
				Placeholder = this.Placeholder,
				BackgroundColor = this.BackgroundColor,
				Keyboard = this.Keyboard
			};

			this.innerEntry.Completed += this.InnerEntry_Completed;

			this.innerGrid = new()
			{
				HorizontalOptions = LayoutOptions.Fill,
				ColumnDefinitions =
				[
					new() { Width = GridLength.Auto },
					new() { Width = GridLength.Star },
				],
				RowDefinitions =
				[
					new() { Height = GridLength.Auto },
				]
			};

			this.innerGrid.Add(this.innerPath, 0, 0);

			if (this.PathData is not null)
				this.innerGrid.Add(this.innerEntry, 1, 0);

			this.innerBorder = new()
			{
				StrokeThickness = 2,
				Content = this.PathData is null ? this.innerEntry : this.innerGrid,
				BackgroundColor = this.BackgroundColor
			};

			this.Content = this.innerBorder;

			this.innerEntry.SetBinding(Entry.TextProperty, new Binding(EntryDataProperty.PropertyName, source: this, mode: BindingMode.TwoWay));
			this.innerEntry.SetBinding(Entry.KeyboardProperty, new Binding(KeyboardProperty.PropertyName, source: this));
			this.innerEntry.SetBinding(Entry.AutomationIdProperty, new Binding(AutomationIdProperty.PropertyName, source: this));

			this.innerEntry.Focused += this.OnEntryFocused;
			this.innerEntry.Unfocused += this.OnEntryUnfocused;
		}		

		private void OnEntryFocused(object? sender, FocusEventArgs e)
		{
			if (this.FocusedCommand?.CanExecute(e) ?? false)
			{
				this.FocusedCommand.Execute(e);
			}
		}

		private void OnEntryUnfocused(object? sender, FocusEventArgs e)
		{
			if (this.UnfocusedCommand?.CanExecute(e) ?? false)
			{
				this.UnfocusedCommand.Execute(e);
			}
		}

		private void InnerEntry_Completed(object? sender, EventArgs e)
		{
			try
			{
				this.Completed?.Invoke(sender, e);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}
	}
}
