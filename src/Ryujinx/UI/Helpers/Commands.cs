using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Helpers
{
#nullable enable
    public static class Commands
    {
        public static RelayCommand Create(Action action)
            => new(action);
        public static RelayCommand CreateConditional(Action action, Func<bool> canExecute)
            => new(action, canExecute);
        
        public static RelayCommand<T> Create<T>(Action<T?> action)
            => new(action);
        public static RelayCommand<T> CreateConditional<T>(Action<T?> action, Predicate<T?> canExecute)
            => new(action, canExecute);

        public static AsyncRelayCommand Create(Func<Task> action)
            => new(action, AsyncRelayCommandOptions.None);
        public static AsyncRelayCommand CreateConcurrent(Func<Task> action)
            => new(action, AsyncRelayCommandOptions.AllowConcurrentExecutions);
        public static AsyncRelayCommand CreateSilentFail(Func<Task> action)
            => new(action, AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler);
        
        public static AsyncRelayCommand<T> Create<T>(Func<T?, Task> action)
            => new(action, AsyncRelayCommandOptions.None);
        public static AsyncRelayCommand<T> CreateConcurrent<T>(Func<T?, Task> action)
            => new(action, AsyncRelayCommandOptions.AllowConcurrentExecutions);
        public static AsyncRelayCommand<T> CreateSilentFail<T>(Func<T?, Task> action)
            => new(action, AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler);

        public static AsyncRelayCommand CreateConditional(Func<Task> action, Func<bool> canExecute)
            => new(action, canExecute, AsyncRelayCommandOptions.None);
        public static AsyncRelayCommand CreateConcurrentConditional(Func<Task> action, Func<bool> canExecute)
            => new(action, canExecute, AsyncRelayCommandOptions.AllowConcurrentExecutions);
        public static AsyncRelayCommand CreateSilentFailConditional(Func<Task> action, Func<bool> canExecute)
            => new(action, canExecute, AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler);
        
        public static AsyncRelayCommand<T> CreateConditional<T>(Func<T?, Task> action, Predicate<T?> canExecute)
            => new(action, canExecute, AsyncRelayCommandOptions.None);
        public static AsyncRelayCommand<T> CreateConcurrentConditional<T>(Func<T?, Task> action, Predicate<T?> canExecute)
            => new(action, canExecute, AsyncRelayCommandOptions.AllowConcurrentExecutions);
        public static AsyncRelayCommand<T> CreateSilentFailConditional<T>(Func<T?, Task> action, Predicate<T?> canExecute)
            => new(action, canExecute, AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler);
    }
}
