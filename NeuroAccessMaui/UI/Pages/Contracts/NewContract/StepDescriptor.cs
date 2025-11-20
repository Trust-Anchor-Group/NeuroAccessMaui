using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract
{
    /// <summary>
    /// Describes a wizard step in the new contract flow.
    /// </summary>
    public class StepDescriptor : INotifyPropertyChanged
    {
        public required string Key { get; init; }
        public required string Title { get; init; }
        public int Index { get; init; }
        private bool isCurrent;
        private bool isVisited;
        private bool isComplete;
        private bool isEnabled = true;

        public bool IsCurrent { get => this.isCurrent; set { if (this.isCurrent != value) { this.isCurrent = value; this.OnPropertyChanged(nameof(this.IsCurrent)); } } }
        public bool IsVisited { get => this.isVisited; set { if (this.isVisited != value) { this.isVisited = value; this.OnPropertyChanged(nameof(this.IsVisited)); } } }
        public bool IsComplete { get => this.isComplete; set { if (this.isComplete != value) { this.isComplete = value; this.OnPropertyChanged(nameof(this.IsComplete)); } } }
        public bool IsEnabled { get => this.isEnabled; set { if (this.isEnabled != value) { this.isEnabled = value; this.OnPropertyChanged(nameof(this.IsEnabled)); } } }

        public string DisplayNumber => (this.Index + 1).ToString(CultureInfo.InvariantCulture);
        public Func<Task<bool>>? ValidateAsync { get; init; }
        public Func<bool>? CanNavigateTo { get; init; }

        public override string ToString() => this.Key;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
