using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;

namespace Kirides.Libs.Events
{
    public class EventAggregator : IEventAggregator
    {
        Dictionary<Type, object> events = new Dictionary<Type, object>();
        public IPubSubEvent<TPayload> GetEvent<TPayload>()
        {
            var type = typeof(TPayload);
            lock (events)
            {
                if (!events.TryGetValue(typeof(TPayload), out var value))
                {
                    value = new PubSubEvent<TPayload>();
                    events[type] = value;
                }
                return (PubSubEvent<TPayload>)value;
            }
        }
    }

    public class PubSubEvent<TPayload> : IPubSubEvent<TPayload>
    {
        public int SubscriberCount => Subscribers.Count;
        private List<EventSub> Subscribers = new List<EventSub>();

        public IDisposable Subscribe(Action<TPayload> callback)
            => Subscribe(callback, null, ThreadOption.Default);

        public IDisposable Subscribe(Action<TPayload> callback, SynchronizationContext synchronizationContext)
            => Subscribe(callback, synchronizationContext, ThreadOption.Inherited);

        public IDisposable Subscribe(Action<TPayload> callback, ThreadOption threadOption)
            => Subscribe(callback, threadOption == ThreadOption.Inherited ? SynchronizationContext.Current : null, threadOption);

        public IDisposable Subscribe(Action<TPayload> callback, SynchronizationContext synchronizationContext, ThreadOption threadOption)
            => Subscribe(callback, synchronizationContext, threadOption, false);

        public IDisposable Subscribe(Action<TPayload> callback, SynchronizationContext synchronizationContext = null, ThreadOption threadOption = ThreadOption.Default, bool keepReferenceAlive = false)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            lock (Subscribers)
            {
                var sub = new EventSub(callback, synchronizationContext, this, keepReferenceAlive);
                Subscribers.Add(sub);
                return sub;
            }
        }

        public void Publish(TPayload payload)
        {
            List<EventSub> eventSubs;
            lock (Subscribers)
            {
                eventSubs = new List<EventSub>(Subscribers);
            }

            List<EventSub> toRemove = null;
            foreach (var sub in eventSubs)
            {
                if (!sub.Invoke(payload))
                {
                    if (toRemove == null)
                        toRemove = new List<EventSub>();
                    toRemove.Add(sub);
                    continue;
                }
            }
            if (toRemove == null || toRemove.Count == 0)
                return;

            foreach (var item in toRemove)
                item.Dispose();
        }

        public void Unsubscribe(IDisposable token)
        {
            var sub = token as EventSub;
            if (sub == null)
                throw new InvalidOperationException("Invalid Subscriptiontoken!");

            lock (Subscribers)
            {
                Subscribers.Remove(sub);
            }

            sub.Dispose();
        }


        private class EventSub : IDisposable
        {
            public WeakReference Reference { get; private set; }
            private Action<object, TPayload> _callback;
            private object _keepAliveReference;
            private readonly IPubSubEvent<TPayload> _pubSubEvent;

            public ThreadOption ThreadOption { get; set; }
            public SynchronizationContext Context { get; private set; }
            public bool IsAlive => Reference == null || (!_disposed && Reference == null) ? true : !_disposed && Reference.IsAlive;
            private bool _disposed { get; set; }

            public EventSub(Action<TPayload> callback, SynchronizationContext context, IPubSubEvent<TPayload> pubSubEvent, bool keepAlive = false)
            {
                var target = callback.Target;
                if (target == null)
                {
                    Reference = null;
                }
                else
                {
                    Reference = new WeakReference(target);
                    if (keepAlive)
                    {
                        _keepAliveReference = target;
                    }
                }

                // Create the "(instance, value) => instance.callback(value)" Lambda
                var instanceParam = Expression.Parameter(typeof(object), "instance");
                var valueParam = Expression.Parameter(typeof(TPayload), "value");
                var functionCallExpression = Expression.Call(target == null ? null : Expression.Convert(instanceParam, target.GetType()), callback.Method, valueParam);
                var lambdaExpression = Expression.Lambda<Action<object, TPayload>>(functionCallExpression, instanceParam, valueParam);
                var compiledLambda = lambdaExpression.Compile();

                _callback = compiledLambda;
                Context = context;
                _pubSubEvent = pubSubEvent;
            }

            public bool Invoke(TPayload payload)
            {
                if (!IsAlive || _disposed) return false;
                if (ThreadOption == ThreadOption.Inherited && Context != null)
                {
                    Context.Send(state => _callback(Reference?.Target, (TPayload)state), payload);
                }
                else
                {
                    _callback(Reference?.Target, payload);
                }
                return true;
            }

            public void Dispose()
            {
                Dispose(true);
            }

            protected void Dispose(bool disposing)
            {
                if (!disposing) return;
                if (_disposed) return;
                _disposed = true;
                _pubSubEvent.Unsubscribe(this);
                _callback = null;
                _keepAliveReference = null;
                Context = null;
                Reference = null;
            }
        }
    }

    public interface IPubSubEvent<TPayload>
    {
        IDisposable Subscribe(Action<TPayload> callback);
        IDisposable Subscribe(Action<TPayload> callback, SynchronizationContext synchronizationContext);
        IDisposable Subscribe(Action<TPayload> callback, ThreadOption threadOption);
        IDisposable Subscribe(Action<TPayload> callback, SynchronizationContext synchronizationContext, ThreadOption threadOption);
        IDisposable Subscribe(Action<TPayload> callback, SynchronizationContext synchronizationContext = null, ThreadOption threadOption = ThreadOption.Default, bool keepReferenceAlive = false);
        void Publish(TPayload payload);
        void Unsubscribe(IDisposable token);
    }

    public enum ThreadOption
    {
        /// <summary>
        /// Tries to run the Event on the Thread it was subscribed on
        /// <para />Or the supplied one (for <see cref="PubSubEvent{TPayload}.Subscribe(Action{TPayload}, SynchronizationContext, ThreadOption)"/>)
        /// <para />Falls back to "Default" if there is no <see cref="SynchronizationContext"/>
        /// </summary>
        Inherited,
        /// <summary>
        /// Runs the Event on the current Thread
        /// </summary>
        Default
    }
    public interface IEventAggregator
    {
        IPubSubEvent<TPayload> GetEvent<TPayload>();
    }
}
