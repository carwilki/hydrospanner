namespace Hydrospanner.Wireup
{
	using System;
	using System.Threading;
	using Disruptor;
	using log4net;

	public class LogAndTerminateProcessHandler : IExceptionHandler
	{
		public void HandleEventException(Exception ex, long sequence, object @event)
		{
			var message = string.Format("Exception processing sequence {0} for event {1}; process MUST terminate.", sequence, @event);
			LogAndShutdown(message, ex);
		}
		public void HandleOnStartException(Exception ex)
		{
			LogAndShutdown("Unhandled exception during startup; process MUST terminate.", ex);
		}
		public void HandleOnShutdownException(Exception ex)
		{
			LogAndShutdown("Unhandled exception during startup; process MUST terminate.", ex);
		}

		private static void LogAndShutdown(string message, Exception innerException)
		{
			Console.Write(message + " " + innerException);
			Log.Fatal(message, innerException);
			Thread.Sleep(1000);
			Environment.Exit(FatalError);
		}

		private const int FatalError = 1;
		private static readonly ILog Log = LogManager.GetLogger(typeof(LogAndTerminateProcessHandler));
	}
}