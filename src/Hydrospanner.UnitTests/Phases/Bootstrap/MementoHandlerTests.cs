﻿#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using Machine.Specifications;
	using NSubstitute;

	[Subject(typeof(MementoHandler))]
	public class when_a_null_repository_is_provided
	{
		It should_throw_an_exception = () =>
			Catch.Exception(() => new MementoHandler(null)).ShouldBeOfType<ArgumentNullException>();
	}

	public class when_a_bootstrap_item_is_handled
	{
		Establish context = () =>
		{
			item = new BootstrapItem
			{
				Key = "key",
				Memento = 42,
				MementoType = typeof(int)
			};
			repository = Substitute.For<IRepository>();
			handler = new MementoHandler(repository);
		};

		Because of = () =>
			handler.OnNext(item, 0, false);

		It should_pass_the_memento_to_the_repository = () =>
			repository.Received(1).Restore("key", item.Memento);

		static BootstrapItem item;
		static IRepository repository;
		static MementoHandler handler;
	}

	public class when_a_null_memento_is_encountered
	{
		Establish context = () =>
		{
			item = new BootstrapItem
			{
				Key = "key",
				MementoType = typeof(string)
			};

			repository = Substitute.For<IRepository>();
			handler = new MementoHandler(repository);
		};

		Because of = () =>
			handler.OnNext(item, 0, false);

		It should_pass_the_memento_to_the_repository = () =>
			repository.Received(1).Restore<string>("key", null);

		static BootstrapItem item;
		static IRepository repository;
		static MementoHandler handler;
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414
