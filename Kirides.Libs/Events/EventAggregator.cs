using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
namespace Kirides.Libs.Events
{
    public class EventAggregator : IEventAggregator
    {
        private readonly ConcurrentDictionary<Type, ICollection<Subscriber>> Subscribers = new ConcurrentDictionary<Type, ICollection<Subscriber>>();

        public ISubscriptionToken Subscribe<TPayload>(Action<TPayload> callback) => Subscribe(callback, ThreadOption.Default);
        public ISubscriptionToken Subscribe<TPayload>(Action<TPayload> callback, ThreadOption threadOption)
        {
            ICollection<Subscriber> eventSubs = Subscribers.GetOrAdd(typeof(TPayload), key => new List<Subscriber>());
            lock (eventSubs)
            {
                var sub = new Subscriber(callback) { ThreadOption = threadOption, Context = System.Threading.SynchronizationContext.Current };
                eventSubs.Add(sub);
                return new SubscriptionToken(this, sub);
            }
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
        private void InvokeEvent<TPayload>(Subscriber sub, TPayload payload, ThreadOption threadOption = ThreadOption.Default)
        {
            if (threadOption == ThreadOption.UIThread)
            {
                if (System.Windows.Application.Current.Dispatcher != null)
                    System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)(() => sub.Invoke(payload)), System.Windows.Threading.DispatcherPriority.Normal);
                else
                    InvokeEvent(sub, payload, ThreadOption.Inherited);
            }
            else
            {
                if (threadOption == ThreadOption.Inherited && sub.Context != null)
                    sub.Context.Post((sender) => sub.Invoke(payload), null);
                else
                    sub.Invoke(payload);
            }
        }
        public void Unsubscribe(ISubscriptionToken token) => token.Dispose();
        public void Unsubscribe<TPayload>(Action<TPayload> callback)
        {
            if (Subscribers.TryGetValue(typeof(TPayload), out ICollection<Subscriber> subs))
                lock (subs)
                    foreach (var sub in subs)
                        if (sub.Reference.Target == callback.Target)
                        {
                            subs.Remove(sub);
                            break;
                        }
        }
    }

    public enum ThreadOption
    {
        /// <summary>
        /// Runs the Event on the WPF-UiThread
        /// </summary>
        UIThread,
        /// <summary>
        /// Tries to run the Event on the Thread it was subscribed on, falls back to "Default" if on Threadpool Thread
        /// </summary>
        Inherited,
        /// <summary>
        /// Runs the Event on the current Thread
        /// </summary>
        Default
    }
    public interface IEventAggregator
    {
        ISubscriptionToken Subscribe<TPayload>(Action<TPayload> callback);
        ISubscriptionToken Subscribe<TPayload>(Action<TPayload> callback, ThreadOption threadOption);
        void Publish<TPayload>() where TPayload : new();
        void Publish<TPayload>(TPayload payload);
        void Unsubscribe<TPayload>(Action<TPayload> callback);
        void Unsubscribe(ISubscriptionToken token);
    }
    public interface ISubscriptionToken : IDisposable { }
    public class SubscriptionToken : ISubscriptionToken
    {
        private Subscriber subscriber;
        private IEventAggregator eventAggregator;
        public bool IsDisposed { get; private set; }

        public SubscriptionToken(IEventAggregator ea, Subscriber reference)
        {
            this.subscriber = reference;
            this.eventAggregator = ea;
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
            eventAggregator = null;
        }
    }
    public class Subscriber : IDisposable
    {
        public WeakReference Reference { get; private set; }
        private Delegate callback;
        public ThreadOption ThreadOption { get; set; }
        public System.Threading.SynchronizationContext Context { get; set; }
        public bool IsAlive => Reference == null || (!_disposed && Reference == null) ? true : !_disposed && Reference.IsAlive;
        private bool _disposed;

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
            if (!IsAlive || _disposed)
                return false;

            object target = null;
            if (Reference != null)
                target = Reference.Target;

            if (target == null)
                this.callback.DynamicInvoke(payload);
            else
                this.callback.DynamicInvoke(target, payload);

            return true;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            callback = null;
            Reference = null;
        }
    }
}