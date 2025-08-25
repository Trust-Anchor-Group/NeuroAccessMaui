using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeuroAccessMaui.UI.MVVM.Policies;

namespace NeuroAccessMaui.UI.MVVM.Factory
{
	public interface IObservableTaskFactory
	{
		ObservableTask<TProgress> Create<TProgress>(
			string name,
			Func<TaskContext<TProgress>, Task> op,
			bool autoStart = true,
			bool useTaskRun = false,
			params IAsyncPolicy[] policies);
	}
}
