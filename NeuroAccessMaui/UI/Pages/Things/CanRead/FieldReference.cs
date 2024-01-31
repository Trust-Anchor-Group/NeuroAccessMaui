using IdApp.Services;
using System;
using System.ComponentModel;

namespace NeuroAccessMaui.UI.Pages.Things.CanRead
{
	/// <summary>
	/// Class used to represent a field name, and if it is permitted or not.
	/// </summary>
	public class FieldReference : INotifyPropertyChanged
	{
		private readonly IServiceReferences references;
		private readonly string name;
		private bool permitted;

		/// <summary>
		/// Class used to represent a field name, and if it is permitted or not.
		/// </summary>
		/// <param name="References">Service References</param>
		/// <param name="FieldName">Field Name</param>
		/// <param name="Permitted">If the field is permitted or not.</param>
		public FieldReference(IServiceReferences References, string FieldName, bool Permitted)
		{
			this.references = References;
			this.name = FieldName;
			this.permitted = Permitted;
		}

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
				this.references.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Occurs when a property value changes.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;
	}
}
