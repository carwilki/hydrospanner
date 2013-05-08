#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Persistence.SqlPersistence
{
	using System;
	using System.Data.Common;
	using Machine.Specifications;

	[Subject(typeof(SqlMessageStoreReader))]
	public class when_initializing_the_reader
	{
		It should_throw_if_the_session_factory_is_null = () =>
			Catch.Exception(() => new SqlMessageStoreReader(null, ConnectionString, types, StartingSequence)).ShouldBeOfType<ArgumentNullException>();

		It should_throw_if_the_connectionstring_is_not_populated = () =>
			Catch.Exception(() => new SqlMessageStoreReader(factory, " ", types, StartingSequence)).ShouldBeOfType<ArgumentNullException>();

		It should_throw_if_the_set_of_type_is_null = () =>
			Catch.Exception(() => new SqlMessageStoreReader(factory, ConnectionString, null, StartingSequence)).ShouldBeOfType<ArgumentNullException>();

		It should_throw_if_the_starting_sequence_out_of_range = () =>
			Catch.Exception(() => new SqlMessageStoreReader(factory, ConnectionString, types, -1)).ShouldBeOfType<ArgumentOutOfRangeException>();

		static readonly DbProviderFactory factory = DbProviderFactories.GetFactory("System.Data.SqlClient");
		const string ConnectionString = "ConnectionString";
		const long StartingSequence = 0;
		static readonly string[] types = new string[0];
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414