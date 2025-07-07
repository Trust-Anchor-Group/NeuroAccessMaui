using Microsoft.Maui.Controls;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Test
{
    /// <summary>
    /// Default implementation of <see cref="INavigationService"/> that uses MAUI Routing for route-based page instantiation.
    /// </summary>
    [Singleton]
    public class NavigationService : INavigationService
    {
        private readonly Stack<ContentPage> navigationStack = new();
        private readonly Stack<ContentPage> modalStack = new();
        private readonly Stack<NavigationArgs?> navigationArgsStack = new();

        private readonly ConcurrentQueue<Func<Task>> taskQueue = new();
        private bool isExecutingTasks = false;
        private NavigationArgs? latestArguments = null;

        private Task<bool> Enqueue(Func<Task> Action)
        {
            TaskCompletionSource<bool> Tcs = new();
            this.taskQueue.Enqueue(async () =>
            {
                try
                {
                    await Action();
                    Tcs.TrySetResult(true);
                }
                catch (Exception Ex)
                {
                    Tcs.TrySetException(Ex);
                }
            });

            if (!this.isExecutingTasks)
            {
                this.isExecutingTasks = true;
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await this.ProcessQueue();
                });
            }

            return Tcs.Task;
        }

        private async Task ProcessQueue()
        {
            try
            {
                while (this.taskQueue.TryDequeue(out Func<Task>? action))
                {
                    await MainThread.InvokeOnMainThreadAsync(action);
                }
            }
            catch (Exception Ex)
            {
                ServiceRef.LogService.LogException(Ex);
            }
            finally
            {
                this.isExecutingTasks = false;
            }
        }

        /// <inheritdoc/>
        public ContentPage CurrentPage => this.navigationStack.Count > 0 ? this.navigationStack.Peek() : null;

        /// <inheritdoc/>
        public ContentPage CurrentModalPage => this.modalStack.Count > 0 ? this.modalStack.Peek() : null;

        /// <inheritdoc/>
        public Task GoToAsync(string Route)
        {
            return this.GoToAsync<NavigationArgs>(Route, null);
        }

        /// <inheritdoc/>
        public Task GoToAsync<TArgs>(string Route, TArgs? Args)
                where TArgs : NavigationArgs, new()
        {
            return this.Enqueue(async () =>
            {
				ContentPage? Page = Routing.GetOrCreateContent(Route, ServiceRef.Provider) as ContentPage ?? throw new InvalidOperationException($"No page registered for route '{Route}'.");
				NavigationArgs NavigationArgs = Args ?? new();
                NavigationArgs.SetBackArguments(this.navigationArgsStack.Count > 0 ? this.navigationArgsStack.Peek() : null);

                this.latestArguments = NavigationArgs;
                this.navigationArgsStack.Push(NavigationArgs);
                this.navigationStack.Push(Page);

                if (Application.Current.MainPage is CustomShell CustomShell)
                    await CustomShell.SetPageAsync(Page);
                else
                    Application.Current.MainPage = Page;
            });
        }

        /// <inheritdoc/>
        public Task GoToAsync(ContentPage Page)
        {
            return this.Enqueue(async () =>
            {
                this.latestArguments = null;
                this.navigationArgsStack.Push(null);
                this.navigationStack.Push(Page);

                if (Application.Current.MainPage is CustomShell CustomShell)
                    await CustomShell.SetPageAsync(Page);
                else
                    Application.Current.MainPage = Page;
            });
        }

        /// <inheritdoc/>
        public Task GoBackAsync()
        {
            return this.Enqueue(async () =>
            {
                if (this.navigationStack.Count > 1)
                {
                    this.navigationStack.Pop();
                    this.navigationArgsStack.Pop();
					ContentPage previous = this.navigationStack.Peek();

                    if (Application.Current.MainPage is CustomShell customShell)
                        await customShell.SetPageAsync(previous);
                    else
                        Application.Current.MainPage = previous;
                }
            });
        }

        /// <inheritdoc/>
        public Task PushModalAsync(string Route)
        {
            return this.Enqueue(async () =>
            {
				ContentPage? page = Routing.GetOrCreateContent(Route, ServiceRef.Provider) as ContentPage;
                if (page is null)
                    throw new InvalidOperationException($"No page registered for route '{Route}'.");

                this.modalStack.Push(page);

                if (Application.Current.MainPage is CustomShell customShell)
                    await customShell.PushModalAsync(page);
                else
                    await Application.Current.MainPage.Navigation.PushModalAsync(page);
            });
        }

        /// <inheritdoc/>
        public Task PopModalAsync()
        {
            return this.Enqueue(async () =>
            {
                if (this.modalStack.Count == 0)
                    return;

                this.modalStack.Pop();

                if (Application.Current.MainPage is CustomShell customShell)
                    await customShell.PopModalAsync();
                else
                    await Application.Current.MainPage.Navigation.PopModalAsync();
            });
        }

        /// <inheritdoc/>
        public TArgs? PopLatestArgs<TArgs>() where TArgs : NavigationArgs, new()
        {
            if (this.latestArguments is TArgs Args)
            {
                this.latestArguments = null;
                return Args;
            }

            return null;
        }
    }
}
