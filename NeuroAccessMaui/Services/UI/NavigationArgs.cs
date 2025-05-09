﻿namespace NeuroAccessMaui.Services.UI
{
	/// <summary>
	/// An base class holding page specific navigation parameters.
	/// </summary>
	public class NavigationArgs
	{
		private NavigationArgs? parentArgs = null;
		private BackMethod backMethod = BackMethod.Inherited;
		private string? uniqueId = null;

		/// <summary>
		/// The completion source for the navigation task.
		/// Will return true when the navigation and transitions are completed.
		/// </summary>
		public readonly TaskCompletionSource<bool> NavigationCompletionSource = new();

		/// <summary>
		/// Returns the task for <see cref="NavigationCompletionSource"/>
		/// </summary>
		public Task GetNavigationCompletionTask()
		{
			return this.NavigationCompletionSource.Task;
		}

		/// <summary>
		/// Sets the reference to the main parent's <see cref="NavigationArgs"/>.
		/// </summary>
		public void SetBackArguments(NavigationArgs? ParentArgs, BackMethod BackMethod = BackMethod.Inherited, string? UniqueId = null)
		{
			this.backMethod = BackMethod;
			this.parentArgs = ParentArgs;
			this.uniqueId = UniqueId;
		}

		/// <summary>
		/// Get the route used for the <see cref="IUiService.GoBackAsync"/> method.
		/// </summary>
		public string GetBackRoute()
		{
			BackMethod BackMethod = this.backMethod;
			string BackRoute = "..";
			NavigationArgs? ParentArgs = this.parentArgs;

			if (BackMethod == BackMethod.Inherited)
			{
				while (ParentArgs is not null && ParentArgs.backMethod == BackMethod.Inherited)
				{
					ParentArgs = ParentArgs.parentArgs;
					BackRoute += "/..";
				}

				if (ParentArgs is null)
					return ".."; // Pop is inherited by default

				BackMethod = ParentArgs.backMethod;
			}

			switch (BackMethod)
			{
				case BackMethod.Pop:
				default:
					return "..";

				case BackMethod.Pop2:
					return "../..";

				case BackMethod.Pop3:
					return "../../..";

				case BackMethod.CurrentPage:
					if (BackMethod == BackMethod.Inherited)
						return BackRoute + "/..";
					else
						return "..";
			}
		}

		/// <summary>
		/// An unique view identifier used to search the args of similar view types.
		/// </summary>
		public string? UniqueId => this.uniqueId;

		/// <summary>
		/// Is the navigation animated
		/// </summary>
		public bool Animated { get; set; } = true;
	}
}
