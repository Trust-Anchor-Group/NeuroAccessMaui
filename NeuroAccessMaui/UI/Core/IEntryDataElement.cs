﻿using System.Windows.Input;

namespace NeuroAccessMaui.UI.Core
{
	internal interface IEntryDataElement
	{
		//note to implementor: implement this property publicly
		string EntryData { get; }
		string EntryHint { get; }
		Style EntryStyle { get; }
		ICommand ReturnCommand { get; }
		bool IsPassword { get; }

		//note to implementor: but implement this method explicitly
		void OnEntryDataPropertyChanged(string OldValue, string NewValue);
		void OnEntryHintPropertyChanged(string OldValue, string NewValue);
		void OnEntryStylePropertyChanged(Style OldValue, Style NewValue);
		void OnReturnCommandPropertyChanged(ICommand OldValue, ICommand NewValue);
		void OnIsPasswordPropertyChanged(bool OldValue, bool NewValue);
	}
}
