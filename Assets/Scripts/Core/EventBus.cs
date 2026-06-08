using System;
using System.Collections.Generic;

namespace SoftFluidPuzzle.Core
{
    public static class EventBus
    {
        private static readonly Dictionary<string, Delegate> _eventHandlers = new Dictionary<string, Delegate>();

        public static void Subscribe<T>(string eventName, Action<T> handler)
        {
            if (!_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName] = handler;
            }
            else
            {
                _eventHandlers[eventName] = Delegate.Combine(_eventHandlers[eventName], handler);
            }
        }

        public static void Subscribe(string eventName, Action handler)
        {
            if (!_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName] = handler;
            }
            else
            {
                _eventHandlers[eventName] = Delegate.Combine(_eventHandlers[eventName], handler);
            }
        }

        public static void Unsubscribe<T>(string eventName, Action<T> handler)
        {
            if (_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName] = Delegate.Remove(_eventHandlers[eventName], handler);
                if (_eventHandlers[eventName] == null)
                {
                    _eventHandlers.Remove(eventName);
                }
            }
        }

        public static void Unsubscribe(string eventName, Action handler)
        {
            if (_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName] = Delegate.Remove(_eventHandlers[eventName], handler);
                if (_eventHandlers[eventName] == null)
                {
                    _eventHandlers.Remove(eventName);
                }
            }
        }

        public static void Publish<T>(string eventName, T eventArgs)
        {
            if (_eventHandlers.TryGetValue(eventName, out Delegate handlers))
            {
                (handlers as Action<T>)?.Invoke(eventArgs);
            }
        }

        public static void Publish(string eventName)
        {
            if (_eventHandlers.TryGetValue(eventName, out Delegate handlers))
            {
                (handlers as Action)?.Invoke();
            }
        }

        public static void Clear()
        {
            _eventHandlers.Clear();
        }
    }
}
