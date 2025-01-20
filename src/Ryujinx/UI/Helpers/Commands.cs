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
        
        public static RelayCommand<T> CreateWithArg<T>(Action<T?> action)
            => new(action);
        public static RelayCommand<T> CreateConditionalWithArg<T>(Action<T?> action, Predicate<T?> canExecute)
            => new(action, canExecute);

        public static AsyncRelayCommand Create(Func<Task> action)
            => new(action, AsyncRelayCommandOptions.None);
        public static AsyncRelayCommand CreateConcurrent(Func<Task> action)
            => new(action, AsyncRelayCommandOptions.AllowConcurrentExecutions);
        public static AsyncRelayCommand CreateSilentFail(Func<Task> action)
            => new(action, AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler);
        
        public static AsyncRelayCommand<T> CreateWithArg<T>(Func<T?, Task> action)
            => new(action, AsyncRelayCommandOptions.None);
        public static AsyncRelayCommand<T> CreateConcurrentWithArg<T>(Func<T?, Task> action)
            => new(action, AsyncRelayCommandOptions.AllowConcurrentExecutions);
        public static AsyncRelayCommand<T> CreateSilentFailWithArg<T>(Func<T?, Task> action)
            => new(action, AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler);

        public static AsyncRelayCommand CreateConditional(Func<Task> action, Func<bool> canExecute)
            => new(action, canExecute, AsyncRelayCommandOptions.None);
        public static AsyncRelayCommand CreateConcurrentConditional(Func<Task> action, Func<bool> canExecute)
            => new(action, canExecute, AsyncRelayCommandOptions.AllowConcurrentExecutions);
        public static AsyncRelayCommand CreateSilentFailConditional(Func<Task> action, Func<bool> canExecute)
            => new(action, canExecute, AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler);
        
        public static AsyncRelayCommand<T> CreateConditionalWithArg<T>(Func<T?, Task> action, Predicate<T?> canExecute)
            => new(action, canExecute, AsyncRelayCommandOptions.None);
        public static AsyncRelayCommand<T> CreateConcurrentConditionalWithArg<T>(Func<T?, Task> action, Predicate<T?> canExecute)
            => new(action, canExecute, AsyncRelayCommandOptions.AllowConcurrentExecutions);
        public static AsyncRelayCommand<T> CreateSilentFailConditionalWithArg<T>(Func<T?, Task> action, Predicate<T?> canExecute)
            => new(action, canExecute, AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler);
    }
}
