using IdApp.Services;
using NeuroAccessMaui.Services;
using System;
using System.ComponentModel;

namespace NeuroAccessMaui.UI.Pages.Things.CanRead
{
	/// <summary>
	/// Class used to represent a field name, and if it is permitted or not.
	/// </summary>
	/// <param name="FieldName">Field Name</param>
	/// <param name="Permitted">If the field is permitted or not.</param>
	public class FieldReference(string FieldName, bool Permitted) : INotifyPropertyChanged
	{
		private readonly string name = FieldName;
		private bool permitted = Permitted;

		/// <summary>
		/// Tag name.
		/// </summary>
		public string Name => this.name;

		/// <summary>
		/// If the field is permitted or not.
		/// </summary>
		public bool Permitted
		{
			get => this.permitted;
			set
			{
				this.permitted = value;
				this.OnPropertyChanged(nameof(this.Permitted));
			}
		}

		/// <summary>
		/// Called when a property has changed.
		/// </summary>
		/// <param name="PropertyName">Name of property</param>
		public void OnPropertyChanged(string PropertyName)
		{
			try
			{
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Occurs when a property value changes.
		/// </summary>
		public event PropertyChangedEventHandler? PropertyChanged;
	}
}
