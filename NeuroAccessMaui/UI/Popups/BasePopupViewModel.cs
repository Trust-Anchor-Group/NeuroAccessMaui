using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.UI.Popups
{
	public class BasePopupViewModel : BaseViewModel
	{
		/// <summary>
		/// Called when the popup is popped from the stack.
		/// This can be used to clean up resources, cancel tasks, etc.
		/// </summary>
		public virtual void OnPop()
		{
		}
	}
}
