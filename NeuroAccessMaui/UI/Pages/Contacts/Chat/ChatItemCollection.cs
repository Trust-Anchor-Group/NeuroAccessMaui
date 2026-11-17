using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat
{
	/// <summary>
	/// The rows of a chat. Older messages are added in one change, so the list lays out once.
	/// </summary>
	public sealed class ChatItemCollection : ObservableCollection<ChatItem>
	{
		/// <summary>
		/// Inserts rows at <paramref name="Index"/>, and raises one change for all of them.
		/// </summary>
		/// <param name="Index">Where the rows go.</param>
		/// <param name="Items">Rows, in order.</param>
		public void InsertRange(int Index, IList<ChatItem> Items)
		{
			if (Items.Count == 0)
				return;

			this.CheckReentrancy();

			int i = Index;
			foreach (ChatItem Item in Items)
				this.Items.Insert(i++, Item);

			this.OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(this.Count)));
			this.OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
			this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (System.Collections.IList)Items, Index));
		}

		/// <summary>
		/// Replaces all rows, and raises one change.
		/// </summary>
		/// <param name="Items">Rows, in order.</param>
		public void Reset(IEnumerable<ChatItem> Items)
		{
			this.CheckReentrancy();

			this.Items.Clear();
			foreach (ChatItem Item in Items)
				this.Items.Add(Item);

			this.OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(this.Count)));
			this.OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
			this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}
	}
}
