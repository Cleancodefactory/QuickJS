using System;

namespace QuickJS
{
	/// <summary>
	/// Provides the options for the user to define custom behavior when parsing JSON.
	/// </summary>
	[Flags]
	public enum JSParseJsonFlags
	{
		/// <summary>
		/// No flags.
		/// </summary>
		None = 0,

		/// <summary>
		/// Allow extended JSON.
		/// </summary>
		Extended = 1 << 0,

	}
}
