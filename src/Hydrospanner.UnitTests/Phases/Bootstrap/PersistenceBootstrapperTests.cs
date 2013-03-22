#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using Hydrospanner.Configuration;
	using Hydrospanner.Persistence;
	using Machine.Specifications;
	using NSubstitute;

	[Subject(typeof(PersistenceBootstrapper))]
	public class when_the_persistence_factory_provided_is_null
	{
		It should_throw_an_exception = () =>
			Catch.Exception(() => new PersistenceBootstrapper(null)).ShouldBeOfType<ArgumentNullException>();
	}

	public class when_restoring_from_persistence
	{
		Establish context = () =>
		{
			var store = Substitute.For<IBootstrapStore>();
			store.Load().Returns(stored);

			factory = Substitute.For<PersistenceFactory>();
			factory.CreateBootstrapStore().Returns(store);

			bootstrapper = new PersistenceBootstrapper(factory);
		};

		Because of = () =>
			loaded = bootstrapper.Restore();

		It should_return_the_bootstrap_info_from_the_underlying_bootstrap_storage = () =>
			loaded.ShouldEqual(stored);

		static PersistenceBootstrapper bootstrapper;
		static PersistenceFactory factory;
		static readonly BootstrapInfo stored = new BootstrapInfo();
		static BootstrapInfo loaded;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
