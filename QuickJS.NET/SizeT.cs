using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace QuickJS
{
	/// <summary>
	/// The unsigned integer type, equivalent of size_t.
	/// </summary>
	[DebuggerDisplay("{_value}")]
	[StructLayout(LayoutKind.Sequential)]
	unsafe public struct SizeT
	{
		private void* _value;

		/// <summary>
		/// Initializes a new instance of <see cref="SizeT"/> using the specified 32-bit value.
		/// </summary>
		/// <param name="value">A 32-bit signed integer.</param>
		public SizeT(int value)
		{
			_value = (void*)checked((uint)value);
		}

		/// <summary>
		/// Initializes a new instance of <see cref="SizeT"/> using the specified 32-bit value.
		/// </summary>
		/// <param name="value">A 32-bit unsigned integer.</param>
		public SizeT(uint value)
		{
			_value = (void*)value;
		}

		/// <summary>
		/// Initializes a new instance of <see cref="SizeT"/> using the specified 64-bit value.
		/// </summary>
		/// <param name="value">A 64-bit signed integer.</param>
		public SizeT(long value)
		{
			checked { _value = (void*)(ulong)value; }
		}

		/// <summary>
		/// Initializes a new instance of <see cref="SizeT"/> using the specified 64-bit value.
		/// </summary>
		/// <param name="value">A 64-bit unsigned integer.</param>
		public SizeT(ulong value)
		{
			_value = checked((void*)value);
		}

		/// <summary>
		/// Initializes a new instance of <see cref="SizeT"/> using the specified pointer.
		/// </summary>
		/// <param name="value">A pointer.</param>
		public SizeT(UIntPtr value)
		{
			_value = (void*)value;
		}

		/// <summary>
		/// Converts the value of a 32-bit signed integer to a <see cref="SizeT"/>.
		/// </summary>
		/// <param name="value">A 32-bit signed integer.</param>
		/// <returns>A new instance of <see cref="SizeT"/> initialized to value.</returns>
		public static implicit operator SizeT(int value)
		{
			return new SizeT(value);
		}

		/// <summary>
		/// Converts the value of a 32-bit unsigned integer to a <see cref="SizeT"/>.
		/// </summary>
		/// <param name="value">A 32-bit unsigned integer.</param>
		/// <returns>A new instance of <see cref="SizeT"/> initialized to value.</returns>
		public static implicit operator SizeT(uint value)
		{
			return new SizeT(value);
		}

		/// <summary>
		/// Converts the unsigned pointer to a <see cref="SizeT"/>.
		/// </summary>
		/// <param name="value">A pointer.</param>
		/// <returns>A new instance of <see cref="SizeT"/> initialized to value.</returns>
		public static implicit operator SizeT(UIntPtr value)
		{
			return new SizeT(value);
		}

		/// <summary>
		/// Converts the value of a 64-bit signed integer to a <see cref="SizeT"/>.
		/// </summary>
		/// <param name="value">A 64-bit signed integer.</param>
		/// <returns>A new instance of <see cref="SizeT"/> initialized to value.</returns>
		public static implicit operator SizeT(long value)
		{
			return new SizeT(value);
		}

		/// <summary>
		/// Converts the value of a 64-bit unsigned integer to a <see cref="SizeT"/>.
		/// </summary>
		/// <param name="value">A 64-bit unsigned integer.</param>
		/// <returns>A new instance of <see cref="SizeT"/> initialized to value.</returns>
		public static implicit operator SizeT(ulong value)
		{
			return new SizeT(value);
		}

		/// <summary>
		/// Converts the value of a <see cref="SizeT"/> to a 32-bit signed integer.
		/// </summary>
		/// <param name="value">The <see cref="SizeT"/> instance to convert.</param>
		/// <returns>The contents of <paramref name="value"/>.</returns>
		public static implicit operator int(SizeT value)
		{
			return checked((int)value._value);
		}

		/// <summary>
		/// Converts the value of a <see cref="SizeT"/> to a 32-bit unsigned integer.
		/// </summary>
		/// <param name="value">The <see cref="SizeT"/> instance to convert.</param>
		/// <returns>The contents of <paramref name="value"/>.</returns>
		public static implicit operator uint(SizeT value)
		{
			return checked((uint)value._value);
		}

		/// <summary>
		/// Converts the value of a <see cref="SizeT"/> to a pointer.
		/// </summary>
		/// <param name="value">The <see cref="SizeT"/> instance to convert.</param>
		/// <returns>The contents of <paramref name="value"/>.</returns>
		public static implicit operator UIntPtr(SizeT value)
		{
			return new UIntPtr(value._value);
		}

		/// <summary>
		/// Converts the value of a <see cref="SizeT"/> to a 64-bit signed integer.
		/// </summary>
		/// <param name="value">The <see cref="SizeT"/> instance to convert.</param>
		/// <returns>The contents of <paramref name="value"/>.</returns>
		public static implicit operator long(SizeT value)
		{
			return checked((long)value._value);
		}

		/// <summary>
		/// Converts the value of a <see cref="SizeT"/> to a 64-bit unsigned integer.
		/// </summary>
		/// <param name="value">The <see cref="SizeT"/> instance to convert.</param>
		/// <returns>The contents of <paramref name="value"/>.</returns>
		public static implicit operator ulong(SizeT value)
		{
			return checked((ulong)value._value);
		}

		/// <summary>
		/// Returns a value indicating whether this instance is equal to a specified object.
		/// </summary>
		/// <param name="obj">An object to compare with this instance or null.</param>
		/// <returns>
		/// true if <paramref name="obj"/> is an instance of <see cref="SizeT"/> and equals
		/// the value of this instance; otherwise, false.
		/// </returns>
		public override bool Equals(object obj)
		{
			return obj is SizeT a && a._value == _value;
		}

		/// <summary>
		/// Returns the hash code for this instance.
		/// </summary>
		/// <returns>A 32-bit signed integer hash code.</returns>
		public override int GetHashCode()
		{
			return new IntPtr(_value).GetHashCode();
		}

		/// <summary>
		/// Converts the numeric value of the current <see cref="SizeT"/> object to its
		/// equivalent string representation.
		/// </summary>
		/// <returns>The string representation of the value of this instance.</returns>
		public override string ToString()
		{
			return new IntPtr(_value).ToString();
		}
	}
}
