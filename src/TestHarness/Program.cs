namespace TestHarness
{
	using System;
	using System.Collections.Generic;
	using Accounting.Events;
	using Hydrospanner;

	internal static class Program
	{
		private static void Main()
		{
			var identifier = new TestStreamIdentifier();

			using (var bootstrapper = new Bootstrapper(identifier, ConnectionName, BuildHydratables))
			{
				bootstrapper.Start();
				Console.WriteLine("Press enter");
				Console.ReadLine();
			}
		}

		private static IHydratable[] BuildHydratables(Guid streamId)
		{
			return new IHydratable[] { new TestHydratable() };
		}

	    private const string ConnectionName = "Hydrospanner";
	}

	public class TestHydratable : IHydratable
	{
		public void Hydrate(object message, Dictionary<string, string> headers, bool replay)
		{
			var closed = message as AccountClosedEvent;
			this.accountId = closed.AccountId;
		}
		public IEnumerable<object> GatherMessages()
		{
			var @event = new AccountClosedEvent
			{
				AccountId = Guid.NewGuid(),
				Description = "Hello, World!",
				Dispatched = DateTime.UtcNow,
				MessageId = Guid.NewGuid(),
				Reason = CloseReason.Abuse,
				UserId = Guid.NewGuid(),
				Username = "test@test.com"
			};

			return new[] { @event };
		}

		private Guid accountId;
	}
}