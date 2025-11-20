using System.Collections.Generic;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Controls
{
	internal sealed class ViewSwitcherViewCache
	{
		private readonly Dictionary<int, View> indexCache = new Dictionary<int, View>();
		private readonly Dictionary<string, View> stateCache = new Dictionary<string, View>();

		public bool IsEnabled { get; set; }

		public bool TryGetByIndex(int index, out View? view)
		{
			if (!this.IsEnabled)
			{
				view = null;
				return false;
			}

			return this.indexCache.TryGetValue(index, out view);
		}

		public bool TryGetByStateKey(string stateKey, out View? view)
		{
			if (!this.IsEnabled)
			{
				view = null;
				return false;
			}

			if (string.IsNullOrWhiteSpace(stateKey))
			{
				view = null;
				return false;
			}

			return this.stateCache.TryGetValue(stateKey, out view);
		}

		public void StoreByIndex(int index, View view)
		{
			if (!this.IsEnabled)
				return;

			this.indexCache[index] = view;
		}

		public void StoreByStateKey(string stateKey, View view)
		{
			if (!this.IsEnabled)
				return;

			if (string.IsNullOrWhiteSpace(stateKey))
				return;

			this.stateCache[stateKey] = view;
		}

		public void Remove(View view)
		{
			if (!this.IsEnabled)
				return;

			List<int> indexesToRemove = new List<int>();
			foreach (KeyValuePair<int, View> pair in this.indexCache)
			{
				if (ReferenceEquals(pair.Value, view))
					indexesToRemove.Add(pair.Key);
			}

			foreach (int index in indexesToRemove)
			{
				this.indexCache.Remove(index);
			}

			List<string> statesToRemove = new List<string>();
			foreach (KeyValuePair<string, View> pair in this.stateCache)
			{
				if (ReferenceEquals(pair.Value, view))
					statesToRemove.Add(pair.Key);
			}

			foreach (string key in statesToRemove)
			{
				this.stateCache.Remove(key);
			}
		}

		public void Clear()
		{
			this.indexCache.Clear();
			this.stateCache.Clear();
		}
	}
}
