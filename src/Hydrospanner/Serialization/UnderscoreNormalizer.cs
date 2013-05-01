namespace Hydrospanner.Serialization
{
	using System.Text;

	internal class UnderscoreNormalizer
	{
		public string Normalize(string value)
		{
			if (string.IsNullOrEmpty(value))
				return value;

			this.builder.Clear();

			var hasUpper = false;
			var len = value.Length;
			for (var i = 0; i < len; i++)
			{
				var letter = value[i];
				if (char.IsUpper(letter))
				{
					if (hasUpper && (i + 1) < len && char.IsLower(value[i + 1]))
						this.builder.Append("_");

					letter = char.ToLower(letter);
					hasUpper = true;
				}

				this.builder.Append(letter);
			}

			return this.builder.ToString();
		}

		private readonly StringBuilder builder = new StringBuilder(1024);
	}
}