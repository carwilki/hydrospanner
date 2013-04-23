﻿namespace SampleApplication
{
	using System;
	using System.Collections.Generic;
	using Hydrospanner;
	using Hydrospanner.Timeout;

	public class FizzBuzzAggregateHydrator : 
		IHydratable<CountCommand>,
		IHydratable<CountEvent>,
		IHydratable<FizzEvent>,
		IHydratable<BuzzEvent>,
		IHydratable<FizzBuzzEvent>,
		ITimeoutHydratable
	{
		public string Key { get { return KeyFactory(this.streamId); } }
		public bool IsComplete { get { return this.aggregate.IsComplete; } }
		public bool IsPublicSnapshot { get { return false; } }
		public ICollection<object> PendingMessages { get; private set; }
		public object Memento
		{
			get
			{
				return new FizzBuzzAggregateMemento
				{
					StreamId = this.streamId,
					Value = this.aggregate.Value,
				};
			}
		}

		public void Hydrate(Delivery<CountCommand> delivery)
		{
			this.aggregate.Increment(delivery.Message.Value);

			Console.WriteLine("Requesting Timeout");
			this.Timeouts.Add(DateTime.UtcNow.AddSeconds(2));
		}
		public void Hydrate(Delivery<CountEvent> delivery)
		{
			this.aggregate.Apply(delivery.Message);
		}
		public void Hydrate(Delivery<FizzEvent> delivery)
		{
			this.aggregate.Apply(delivery.Message);
		}
		public void Hydrate(Delivery<BuzzEvent> delivery)
		{
			this.aggregate.Apply(delivery.Message);
		}
		public void Hydrate(Delivery<FizzBuzzEvent> delivery)
		{
			this.aggregate.Apply(delivery.Message);
		}

		public ICollection<DateTime> Timeouts { get; private set; } 
		public void Hydrate(Delivery<TimeoutMessage> delivery)
		{
			Console.WriteLine("Timeout Received: " + delivery.Message.Instant);
		}

		public FizzBuzzAggregateHydrator(FizzBuzzAggregateMemento memento)
		{
			this.streamId = memento.StreamId;
			var pending = new List<object>();
			this.PendingMessages = pending;
			this.aggregate = new FizzBuzzAggregate(memento.StreamId, memento.Value, pending);
			this.Timeouts = new List<DateTime>();
		}
		public FizzBuzzAggregateHydrator(Guid streamId)
		{
			this.streamId = streamId;
			var pending = new List<object>();
			this.PendingMessages = pending;
			this.aggregate = new FizzBuzzAggregate(streamId, pending);
			this.Timeouts = new List<DateTime>();
		}

		public static FizzBuzzAggregateHydrator Restore(FizzBuzzAggregateMemento memento)
		{
			return new FizzBuzzAggregateHydrator(memento);
		}

		public static HydrationInfo Lookup(Delivery<CountCommand> delivery)
		{
			return new HydrationInfo(KeyFactory(delivery.Message.StreamId), () => new FizzBuzzAggregateHydrator(delivery.Message.StreamId));
		}
		public static HydrationInfo Lookup(Delivery<CountEvent> delivery)
		{
			return new HydrationInfo(KeyFactory(delivery.Message.StreamId), () => new FizzBuzzAggregateHydrator(delivery.Message.StreamId));
		}
		public static HydrationInfo Lookup(Delivery<FizzEvent> delivery)
		{
			return new HydrationInfo(KeyFactory(delivery.Message.StreamId), () => new FizzBuzzAggregateHydrator(delivery.Message.StreamId));
		}
		public static HydrationInfo Lookup(Delivery<BuzzEvent> delivery)
		{
			return new HydrationInfo(KeyFactory(delivery.Message.StreamId), () => new FizzBuzzAggregateHydrator(delivery.Message.StreamId));
		}
		public static HydrationInfo Lookup(Delivery<FizzBuzzEvent> delivery)
		{
			return new HydrationInfo(KeyFactory(delivery.Message.StreamId), () => new FizzBuzzAggregateHydrator(delivery.Message.StreamId));
		}

		public static string KeyFactory(Guid streamId)
		{
			return string.Format(HydratableKeys.AggregateKey, streamId);
		}

		private readonly FizzBuzzAggregate aggregate;
		private readonly Guid streamId;
	}

	public class FizzBuzzAggregateMemento
	{
		public Guid StreamId { get; set; }
		public int Value { get; set; }
	}
}