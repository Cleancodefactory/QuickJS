using System;

namespace QuickJS
{
	/// <summary>
	/// Object Writer/Reader flags.
	/// </summary>
	[Flags]
	public enum JSReaderFlags
	{
		/// <summary>
		/// Allow function/module
		/// </summary>
		ObjBytecode = (1 << 0),

		/// <summary>
		/// Avoid duplicating &apos;buf&apos; data
		/// </summary>
		ObjRomData = (1 << 1),

		/// <summary>
		/// Allow SharedArrayBuffer
		/// </summary>
		ObjSab = (1 << 2),

		/// <summary>
		/// Allow object references
		/// </summary>
		ObjReference = (1 << 3),

	}
}
