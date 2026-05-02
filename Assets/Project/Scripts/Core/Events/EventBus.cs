using System;
using System.Collections.Generic;

namespace BlackJackFFLite.Core.Events
{
    public sealed class EventBus
    {
        private readonly Dictionary<Type, Delegate> _events = new Dictionary<Type, Delegate>();

        public void Subscribe<T>(Action<T> listener)
        {
            Type eventType = typeof(T);

            if (_events.TryGetValue(eventType, out Delegate existingDelegate))
            {
                _events[eventType] = Delegate.Combine(existingDelegate, listener);
            }
            else
            {
                _events[eventType] = listener;
            }
        }

        public void Unsubscribe<T>(Action<T> listener)
        {
            Type eventType = typeof(T);

            if (!_events.TryGetValue(eventType, out Delegate existingDelegate))
                return;

            Delegate currentDelegate = Delegate.Remove(existingDelegate, listener);

            if (currentDelegate == null)
            {
                _events.Remove(eventType);
            }
            else
            {
                _events[eventType] = currentDelegate;
            }
        }

        public void Publish<T>(T eventData)
        {
            Type eventType = typeof(T);

            if (!_events.TryGetValue(eventType, out Delegate existingDelegate))
                return;

            if (existingDelegate is Action<T> callback)
            {
                callback.Invoke(eventData);
            }
        }

        public void Clear()
        {
            _events.Clear();
        }
    }
}