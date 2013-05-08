#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Persistence.SqlPersistence
{
	using System;
	using System.Data.Common;
	using Machine.Specifications;

	[Subject(typeof(SqlCheckpointStore))]
	public class when_initializing_the_checkpoint_store
	{
		It should_throw_if_the_session_factory_is_null = () =>
			Catch.Exception(() => new SqlCheckpointStore(null, ConnectionString)).ShouldBeOfType<ArgumentNullException>();

		It should_throw_if_the_connection_string_is_not_populated = () =>
			Catch.Exception(() => new SqlCheckpointStore(factory, " ")).ShouldBeOfType<ArgumentNullException>();

		static readonly DbProviderFactory factory = DbProviderFactories.GetFactory("System.Data.SqlClient");
		const string ConnectionString = "ConnectionString";
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414