using System.Collections.ObjectModel;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages
{
	/// <summary>
	/// Grouped item interface.
	/// </summary>
	public interface IUniqueItem
	{
		/// <summary>
		/// Unique name used to compare items.
		/// </summary>
		public string UniqueName { get; }

		/// <summary>
		/// Get the groups's localised name
		/// </summary>
		public string LocalisedName => this.UniqueName;
	}

	/// <summary>
	/// Encapsulates a grouped item collection.
	/// </summary>
	/// <param name="Name">Group's unique name.</param>
	/// <param name="Items">Group's item collection.</param>
	public class ObservableItemGroup<T>(string Name, IEnumerable<T> Items) : ObservableCollection<T>(Items), IUniqueItem
		where T : IUniqueItem
	{
		/// <inheritdoc/>
		public string UniqueName { get; } = Name;

		/// <inheritdoc/>
		public string LocalisedName => ServiceRef.Localizer[this.UniqueName] ?? this.UniqueName;

		/// <summary>
		/// Get the groups's name (potentially used for display)
		/// </summary>
		public override string ToString()
		{
			return this.UniqueName;
		}

		/// <summary>
		/// Update the current collection items using a new collection
		/// </summary>
		public static void UpdateGroupsItems(ObservableItemGroup<IUniqueItem> OldCollection, ObservableItemGroup<IUniqueItem> NewCollection)
		{
			// First, remove items which are no longer in the new collection

			Dictionary<string, IUniqueItem> ToRemove = [];

			foreach (IUniqueItem Item in OldCollection)
				ToRemove[Item.UniqueName] = Item;

			foreach (IUniqueItem Item in NewCollection)
				ToRemove.Remove(Item.UniqueName);

			foreach (IUniqueItem Item in ToRemove.Values)
				OldCollection.Remove(Item);

			// Then recursivelly update every item.
			// An old item might move or a new item might be inserted in the middle or appended to the end.
			for (int i = 0; i < NewCollection.Count; i++)
			{
				IUniqueItem NewItem = NewCollection[i];

				if (i >= OldCollection.Count)
				{
					// appended to the end
					OldCollection.Add(NewItem);
				}
				else
				{
					IUniqueItem OldItem = OldCollection[i];

					if (OldItem.UniqueName.Equals(NewItem.UniqueName, StringComparison.Ordinal))
					{
						// The item is in its right place.
						// If it's a collection, do the update recursivelly

						if (OldItem is ObservableItemGroup<IUniqueItem> OldItems && NewItem is ObservableItemGroup<IUniqueItem> NewItems)
							UpdateGroupsItems(OldItems, NewItems);
					}
					else
					{
						// We removed the missing items, so this item is moved or has to be inserted
						int OldIndex = -1;

						for (int j = i + 1; j < OldCollection.Count; j++)
						{
							if (OldCollection[j].UniqueName.Equals(NewItem.UniqueName, StringComparison.Ordinal))
							{
								OldIndex = j;
								break;
							}
						}

						if (OldIndex == -1)
						{
							// The item isn't found in the old collection
							OldCollection.Insert(i, NewItem);
						}
						else
						{
							// Move the item to it's new position
							OldCollection.Move(OldIndex, i);

							// If it's a collection, do the update recursivelly
							if (OldItem is ObservableItemGroup<IUniqueItem> OldItems && NewItem is ObservableItemGroup<IUniqueItem> NewItems)
								UpdateGroupsItems(OldItems, NewItems);
						}
					}
				}
			}
		}
	}
}
