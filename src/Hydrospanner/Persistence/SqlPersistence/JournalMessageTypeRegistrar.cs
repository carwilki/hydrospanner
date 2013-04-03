namespace Hydrospanner.Persistence.SqlPersistence
{
	using System.Collections.Generic;
	using System.Linq;

	public class JournalMessageTypeRegistrar
	{
		public virtual IEnumerable<string> AllTypes { get { return this.registeredTypes.OrderBy(x => x.Value).Select(x => x.Key); } }

		public virtual short GetIdentifier(string type)
		{
			return this.registeredTypes.ValueOrDefault(type);
		}

		public virtual bool IsRegistered(string type)
		{
			return this.GetIdentifier(type) > 0;
		}

		public virtual short Register(string type)
		{
			var toRegister = this.registeredTypes[type] = (short)(this.registeredTypes.Count + 1);
			this.typesPendingRegistration.Add(toRegister);
			return toRegister;
		}

		public virtual void MarkPendingAsRegistered()
		{
			this.registeredTypeCommittedIndex = this.registeredTypes.Count;
		}

		public virtual void DropPendingTypes()
		{
			this.typesPendingRegistration.Clear();

			if (this.registeredTypeCommittedIndex == this.registeredTypes.Count)
				return;

			var keys = this.registeredTypes.Where(x => x.Value > this.registeredTypeCommittedIndex).Select(x => x.Key).ToArray();
			foreach (var key in keys)
				this.registeredTypes.Remove(key);
		}

		public JournalMessageTypeRegistrar(IEnumerable<string> types)
		{
			// TODO: null checks

			foreach (var type in types)
				this.registeredTypeCommittedIndex = this.Register(type);
		}
		protected JournalMessageTypeRegistrar()
		{
		}

		private readonly Dictionary<string, short> registeredTypes = new Dictionary<string, short>(1024);
		private readonly HashSet<short> typesPendingRegistration = new HashSet<short>();
		private int registeredTypeCommittedIndex;
	}
}