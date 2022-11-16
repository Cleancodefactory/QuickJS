using System;
using System.Collections.Generic;
using System.Text;

namespace QuickJS
{
	/// <summary>
	/// Object Writer flags.
	/// </summary>
	[Flags]
	public enum JSWriterFlags
	{
		/// <summary>
		/// Allow function/module
		/// </summary>
		ObjBytecode = (1 << 0),

		/// <summary>
		/// Byte swapped output
		/// </summary>
		ObjBSwap = (1 << 1),

		/// <summary>
		/// Allow SharedArrayBuffer
		/// </summary>
		ObjSab = (1 << 2),

		/// <summary>
		/// Allow object references to encode arbitrary object graph
		/// </summary>
		ObjReference = (1 << 3),

	}
}
