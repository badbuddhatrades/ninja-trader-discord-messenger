using System;

namespace NinjaTrader.Custom.AddOns.DiscordMessenger.Events
{
    public class EventManager
    {
        public event Action<string> OnPrintMessage;

        /// <summary>
        /// Safely invokes a parameterless event.
        /// </summary>
        public void SafelyInvoke(Action eventHandler)
        {
            TryInvoke(eventHandler);
        }

        /// <summary>
        /// Safely invokes an event with one parameter.
        /// </summary>
        public void SafelyInvoke<T>(Action<T> eventHandler, T arg)
        {
            TryInvoke(() => eventHandler?.Invoke(arg));
        }

        /// <summary>
        /// Safely invokes an event with two parameters.
        /// </summary>
        public void SafelyInvoke<T1, T2>(Action<T1, T2> eventHandler, T1 arg1, T2 arg2)
        {
            TryInvoke(() => eventHandler?.Invoke(arg1, arg2));
        }

        /// <summary>
        /// Triggers the OnPrintMessage event with the given message.
        /// </summary>
        public void PrintMessage(string message)
        {
            SafelyInvoke(OnPrintMessage, message);
        }

        /// <summary>
        /// Wraps the invocation of an action in a try-catch block.
        /// </summary>
        private void TryInvoke(Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                OnPrintMessage?.Invoke($"Error invoking event: {ex.Message}");
            }
        }
    }
}
