namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using Snapshot;

	public sealed class Transformer : ITransformer
	{
		public IEnumerable<object> Handle(object message, Dictionary<string, string> headers, long sequence, bool live)
		{
			var type = message.GetType();
			var callback = this.callbacks.Add(type, () => this.RegisterCallback(type));
			return callback(message, headers, sequence, live);
		}
		private IEnumerable<object> Handle<T>(Delivery<T> delivery)
		{
			this.gathered.Clear();

			// determine if this is the first live message

			foreach (var hydratable in this.repository.Load(delivery))
			{
				hydratable.Hydrate(delivery);
				this.GatherState(delivery.Live, delivery.Sequence, hydratable as IHydratable);
			}

			return this.gathered;
		}
		private void GatherState(bool live, long messageSequence, IHydratable hydratable)
		{
			if (live)
			{
				this.gathered.AddRange(hydratable.PendingMessages);
				hydratable.PendingMessages.Clear();
			}
				
			if (hydratable.IsPublicSnapshot || hydratable.IsComplete)
				this.TakeSnapshot(hydratable, messageSequence);

			if (hydratable.IsComplete)
				this.repository.Delete(hydratable);

			// if this is the first live message
			// enumerate over every single hydratable in the repository...
			// for each IAlarmHydratable, get the set of datetime timeouts requested
			// and add those the timeout hydratable (which is referenced right here)
		}
		private void TakeSnapshot(IHydratable hydratable, long messageSequence)
		{
			var memento = hydratable.GetMemento();
			var cloner = memento as ICloneable;
			memento = (cloner == null ? memento : cloner.Clone()) ?? memento;

			var next = this.snapshotRing.Next();
			var claimed = this.snapshotRing[next];
			claimed.AsPublicSnapshot(hydratable.Key, memento, messageSequence);
			this.snapshotRing.Publish(next);
		}

		private HandleDelegate RegisterCallback(Type messageType)
		{
			var method = this.callbackDelegateMethod.MakeGenericMethod(messageType);
			var callback = Delegate.CreateDelegate(typeof(HandleDelegate), this, method);
			return (HandleDelegate)callback;
		}
		// ReSharper disable UnusedMember.Local
		// ReSharper disable SuspiciousTypeConversion.Global
		private IEnumerable<object> RegisterCallbackDelegate<T>(object message, Dictionary<string, string> headers, long sequence, bool live)
		{
			var delivery = new Delivery<T>((T)message, headers, sequence, live);
			return this.Handle(delivery);
		}
		// ReSharper restore SuspiciousTypeConversion.Global
		// ReSharper restore UnusedMember.Local

		public Transformer(IRepository repository, IRingBuffer<SnapshotItem> snapshotRing) : this()
		{
			if (repository == null)
				throw new ArgumentNullException("repository");

			if (snapshotRing == null)
				throw new ArgumentNullException("snapshotRing");

			this.repository = repository;
			this.snapshotRing = snapshotRing;
		}
		public Transformer()
		{
			this.callbackDelegateMethod = this.GetType().GetMethod("RegisterCallbackDelegate", BindingFlags.Instance | BindingFlags.NonPublic);
		}

		private readonly Dictionary<Type, HandleDelegate> callbacks = new Dictionary<Type, HandleDelegate>(); 
		private readonly List<object> gathered = new List<object>();
		private readonly MethodInfo callbackDelegateMethod;
		private readonly IRingBuffer<SnapshotItem> snapshotRing;
		private readonly IRepository repository;
	}

	internal delegate IEnumerable<object> HandleDelegate(object message, Dictionary<string, string> headers, long sequence, bool live);
}