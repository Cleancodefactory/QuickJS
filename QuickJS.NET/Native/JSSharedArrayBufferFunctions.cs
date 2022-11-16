using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace QuickJS.Native
{
	/// <summary>
	/// Encapsulates a method that allocates memory from the unmanaged memory of the process.
	/// </summary>
	/// <param name="opaque">An opaque pointer.</param>
	/// <param name="size">The required number of bytes in memory.</param>
	/// <returns>
	/// A pointer to the newly allocated memory. If the function failed to allocate the requested
	/// block of memory, a null pointer is returned.
	/// </returns>
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate IntPtr SharedArrayBufferAllocDelegate(IntPtr opaque, SizeT size);

	/// <summary>
	/// Encapsulates a method that frees memory previously allocated from the unmanaged memory of the process.
	/// </summary>
	/// <param name="opaque">An opaque pointer.</param>
	/// <param name="ptr">The pointer to the previously allocated memory.</param>
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void SharedArrayBufferFreeDelegate(IntPtr opaque, IntPtr ptr);

	/// <summary>
	/// Encapsulates a method that clones a buffer pointed to by <paramref name="ptr"/>.
	/// </summary>
	/// <param name="opaque">An opaque pointer.</param>
	/// <param name="ptr">An pointer to the buffer.</param>
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void SharedArrayBufferCloneDelegate(IntPtr opaque, IntPtr ptr);

	/// <summary>
	/// Contains data and callbacks that are used to allocate, free and clone SharedArrayBuffers
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct JSSharedArrayBufferFunctions
	{
		/// <summary>
		/// A delegate to an application-defined memory allocation method.
		/// </summary>
		public SharedArrayBufferAllocDelegate sab_alloc;
		/// <summary>
		/// A delegate to an application-defined memory free method.
		/// </summary>
		public SharedArrayBufferFreeDelegate sab_free;
		/// <summary>
		/// A delegate to an application-defined clone method.
		/// </summary>
		public SharedArrayBufferCloneDelegate sab_dup;
		/// <summary>
		/// An opaque pointer.
		/// </summary>
		public IntPtr sab_opaque;
	}
}
