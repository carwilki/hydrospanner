namespace Hydrospanner.Phases.Journal
{
	using System;

	[Flags]
	public enum JournalItemAction
	{
		None = 0,
		Journal = 1,
		Dispatch = 2,
		Acknowledge = 4,
	}
}