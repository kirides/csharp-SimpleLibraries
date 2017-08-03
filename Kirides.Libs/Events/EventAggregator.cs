using Kirides.Libs.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Kirides.Libs.Events
{
    public class EventAggregator : IEventAggregator
    {
        private readonly ConcurrentDictionary<Type, ICollection<Subscriber>> Subscribers = new ConcurrentDictionary<Type, ICollection<Subscriber>>();

        protected virtual IDisposable Subscribe<TPayload>(Action<TPayload> callback, SynchronizationContext synchronizationContext, ThreadOption threadOption)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            ICollection<Subscriber> eventSubs = Subscribers.GetOrAddSafe(typeof(TPayload), key => new List<Subscriber>());
            lock (eventSubs)
            {
                var sub = new Subscriber(callback) { ThreadOption = threadOption, Context = synchronizationContext };
                eventSubs.Add(sub);
                return new SubscriptionToken(sub);
            }
        }
        public IDisposable Subscribe<TPayload>(Action<TPayload> callback) => Subscribe(callback, ThreadOption.Default);
        public IDisposable Subscribe<TPayload>(Action<TPayload> callback, ThreadOption threadOption) => Subscribe(callback, SynchronizationContext.Current, threadOption);
        public IDisposable Subscribe<TPayload>(Action<TPayload> callback, SynchronizationContext synchronizationContext)
        {
            if (synchronizationContext == null) throw new ArgumentNullException(nameof(synchronizationContext));
            return Subscribe(callback, synchronizationContext, ThreadOption.Inherited);
        }

        public void Publish<TPayload>() where TPayload : new() => Publish(new TPayload());
        public void Publish<TPayload>(TPayload payload)
        {
            if (Subscribers.TryGetValue(typeof(TPayload), out ICollection<Subscriber> eventSubs))
            {
                ICollection<Subscriber> referencesToRemove = new List<Subscriber>();
                lock (eventSubs)
                {
                    Subscriber[] momSubs = new Subscriber[eventSubs.Count];
                    eventSubs.CopyTo(momSubs, 0);
                    foreach (var sub in momSubs)
                    {
                        if (!sub.IsAlive)
                        {
                            referencesToRemove.Add(sub);
                            continue;
                        }
                        InvokeEvent(sub, payload, sub.ThreadOption);
                    }
                }
                if (referencesToRemove.Count == 0)
                    return;

                lock (eventSubs)
                    foreach (var item in referencesToRemove)
                        eventSubs.Remove(item);
            }
        }
        private void InvokeEvent<TPayload>(Subscriber sub, TPayload payload, ThreadOption threadOption = ThreadOption.Inherited)
        {
            if (threadOption == ThreadOption.Inherited && sub.Context != null)
                sub.Context.Post(x => sub.Invoke((TPayload)x), payload);
            else
                sub.Invoke(payload);
        }
        public void Unsubscribe(IDisposable token) => token.Dispose();
        public void Unsubscribe<TPayload>(Action<TPayload> callback)
        {
            if (Subscribers.TryGetValue(typeof(TPayload), out ICollection<Subscriber> subs))
                lock (subs)
                    foreach (var sub in subs)
                        if (sub.Reference.Target == callback.Target)
                        {
                            sub.Dispose();
                            subs.Remove(sub);
                            break;
                        }
        }
    }

    public enum ThreadOption
    {
        /// <summary>
        /// Tries to run the Event on the Thread it was subscribed on
        /// <para />Or the supplied one (for <see cref="IEventAggregator.Subscribe{TPayload}(Action{TPayload}, SynchronizationContext)"/>)
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
        IDisposable Subscribe<TPayload>(Action<TPayload> callback);
        IDisposable Subscribe<TPayload>(Action<TPayload> callback, ThreadOption threadOption);
        IDisposable Subscribe<TPayload>(Action<TPayload> callback, SynchronizationContext synchronizationContext);
        void Publish<TPayload>() where TPayload : new();
        void Publish<TPayload>(TPayload payload);
        void Unsubscribe<TPayload>(Action<TPayload> callback);
        void Unsubscribe(IDisposable token);
    }
    public class SubscriptionToken : IDisposable
    {
        private Subscriber subscriber;
        public bool IsDisposed { get; private set; }

        public SubscriptionToken(Subscriber reference)
        {
            this.subscriber = reference;
        }

        /// <summary>
        /// Unsubscribes from the associated Event and removes references
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
            subscriber.Dispose();
            subscriber = null;
        }
    }
    public class Subscriber : IDisposable
    {
        public WeakReference Reference { get; private set; }
        private Delegate callback;
        public ThreadOption ThreadOption { get; set; }
        public SynchronizationContext Context { get; set; }
        public bool IsAlive => Reference == null || (!_disposed && Reference == null) ? true : !_disposed && Reference.IsAlive;
        private bool _disposed { get; set; }

        public Subscriber(Delegate callback)
        {
            var target = callback.Target;
            if (target == null)
            {
                // Static method
                this.Reference = null;
                this.callback = callback;
            }
            else
            {
                // Instance method
                this.Reference = new WeakReference(callback.Target);
                var paramType = callback.Method.GetParameters()[0].ParameterType;
                var callbackType = typeof(Action<,>).MakeGenericType(target.GetType(), paramType);
                // Creates a delegate without target.
                this.callback = Delegate.CreateDelegate(callbackType, callback.Method);
            }
        }

        public bool Invoke<TPayload>(TPayload payload)
        {
            if (!IsAlive || _disposed) return false;

            object target = null;
            if (Reference != null) target = Reference.Target;

            if (target == null) this.callback.DynamicInvoke(payload);
            else this.callback.DynamicInvoke(target, payload);

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
            callback = null;
            Reference = null;
            Context = null;
        }
    }
}