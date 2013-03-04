namespace Hydrospanner.Outbox
{
	using System.Configuration;
	using Disruptor;

	public class BookmarkHandler : IEventHandler<DispatchMessage>
	{
		public void OnNext(DispatchMessage data, long sequence, bool endOfBatch)
		{
			if (data.IncomingSequence <= this.largest)
				return;

			this.largest = data.IncomingSequence;

			if (endOfBatch)
				this.MoveBookmark();
		}
		private void MoveBookmark()
		{
			using (var connection = this.settings.OpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "UPDATE bookmarks SET sequence = {0} WHERE sequence < {0};".FormatWith(this.largest);
				command.ExecuteNonQuery(); // TODO: circuit breaker pattern
			}
		}

		public BookmarkHandler(string connectionName)
		{
			this.settings = ConfigurationManager.ConnectionStrings[connectionName];
		}

		private readonly ConnectionStringSettings settings;
		private long largest;
	}
}