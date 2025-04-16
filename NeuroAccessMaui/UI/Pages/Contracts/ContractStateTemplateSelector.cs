

using NeuroAccessMaui.UI.Pages.Contracts.ObjectModel;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts
{
	public class ContractStateTemplateSelector : DataTemplateSelector
	{
		public DataTemplate? ProposedTemplate { get; set; }
		public DataTemplate? RejectedTemplate { get; set; }
		public DataTemplate? ApprovedTemplate { get; set; }
		public DataTemplate? BeingSignedTemplate { get; set; }
		public DataTemplate? SignedTemplate { get; set; }
		public DataTemplate? FailedTemplate { get; set; }
		public DataTemplate? ObsoletedTemplate { get; set; }
		public DataTemplate? DeletedTemplate { get; set; }

		protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
		{
			if (item is ObservableContract Contract)
			{
				return Contract.ContractState switch
				{
					ContractState.Proposed => this.ProposedTemplate ?? this.SignedTemplate!,
					ContractState.Rejected => this.RejectedTemplate ?? this.SignedTemplate!,
					ContractState.Approved => this.ApprovedTemplate ?? this.SignedTemplate!,
					ContractState.BeingSigned => this.BeingSignedTemplate ?? this.SignedTemplate!,
					ContractState.Signed => this.SignedTemplate ?? this.SignedTemplate!,
					ContractState.Failed => this.FailedTemplate ?? this.SignedTemplate!,
					ContractState.Obsoleted => this.ObsoletedTemplate ?? this.SignedTemplate!,
					ContractState.Deleted => this.DeletedTemplate ?? this.SignedTemplate!,
					_ => this.SignedTemplate!
				};
			}

			// Fallback if item is not a Contract or state unknown.
			return this.SignedTemplate!;
		}
	}
}
