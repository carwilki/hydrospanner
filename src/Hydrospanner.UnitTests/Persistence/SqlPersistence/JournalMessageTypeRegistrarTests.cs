#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using Machine.Specifications;

	[Subject(typeof(JournalMessageTypeRegistrar))]
	public class when_intantiating_the_registrar
	{
		It should_throw_if_the_types_are_null = () =>
			Catch.Exception(() => new JournalMessageTypeRegistrar(null)).ShouldBeOfType<ArgumentNullException>();

		It should_NOT_throw_if_the_types_are_NOT_null = () =>
			Catch.Exception(() => new JournalMessageTypeRegistrar(new List<string>())).ShouldBeNull();
	}

	[Subject(typeof(JournalMessageTypeRegistrar))]
	public class when_registering_types
	{
		Establish context = () =>
			registrar = new JournalMessageTypeRegistrar(new string[0]);

		It should_register_types_appropriately = () =>
		{
			it_should_register_types();
			it_should_NOT_remember_types_after_droping_pending_types();
			it_should_register_types();
			it_should_remember_types_after_marking_them_as_registered();
		};

		static void it_should_register_types()
		{
			registrar.AllTypes.ShouldBeEmpty();

			registrar.IsRegistered(StringType).ShouldBeFalse();
			registrar.GetIdentifier(StringType).ShouldEqual(default(short));
			registrar.Register(StringType).ShouldEqual((short)1);
			registrar.IsRegistered(StringType).ShouldBeTrue();
			registrar.GetIdentifier(StringType).ShouldEqual((short)1);

			registrar.IsRegistered(Int32Type).ShouldBeFalse();
			registrar.GetIdentifier(Int32Type).ShouldEqual(default(short));
			registrar.Register(Int32Type).ShouldEqual((short)2);
			registrar.IsRegistered(Int32Type).ShouldBeTrue();
			registrar.GetIdentifier(Int32Type).ShouldEqual((short)2);

			registrar.AllTypes.ShouldBeLike(new[] { StringType, Int32Type });
		}

		static void it_should_NOT_remember_types_after_droping_pending_types()
		{
			registrar.DropPendingTypes();
			registrar.AllTypes.ShouldBeEmpty();
		}

		static void it_should_remember_types_after_marking_them_as_registered()
		{
			registrar.MarkPendingAsRegistered();
			registrar.AllTypes.ShouldBeLike(new[] { StringType, Int32Type });
		}

		static JournalMessageTypeRegistrar registrar;
		static readonly string StringType = string.Empty.ResolvableTypeName();
		static readonly string Int32Type = default(int).ResolvableTypeName();
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169
