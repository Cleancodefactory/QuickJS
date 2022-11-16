﻿using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Globalization;
using static QuickJS.Native.QuickJSNativeApi;
using System.Runtime.CompilerServices;

namespace QuickJS.Native
{
	/// <summary>
	/// Holds a JavaScript value and provides type-testing and conversion functions.
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public struct JSValue
	{
		[StructLayout(LayoutKind.Sequential)]
		internal struct JSTagUnion
		{
			private unsafe void* _padding;
			public JSTag tag;
		}

		static JSValue()
		{
			if (IntPtr.Size == 4)
			{
				JS_FLOAT64_TAG_ADDEND_BITS = (unchecked((ulong)(0x7ff80000 - (int)JSTag.First + 1)) << 32); // quiet NaN encoding
				NaN.uint64 = unchecked(0x7ff8000000000000UL - JS_FLOAT64_TAG_ADDEND_BITS);
			}
			else
			{
				JS_FLOAT64_TAG_ADDEND_BITS = 0UL;
				NaN._tagdata.tag = JSTag.Float64;
				NaN.float64 = double.NaN;
			}
		}

		[FieldOffset(0)]
		internal ulong uint64;
		[FieldOffset(0)]
		internal int int32;
		[FieldOffset(0)]
		internal double float64;
		[FieldOffset(0)]
		internal unsafe void* ptr;
		[FieldOffset(0)]
		internal JSTagUnion _tagdata;

		private static readonly ulong JS_FLOAT64_TAG_ADDEND_BITS;

		/// <summary>
		/// Represents a value that is not a number.
		/// </summary>
		public static readonly JSValue NaN;

		/// <summary>
		/// Represents the intentional absence of any object value.
		/// It is one of JavaScript&apos;s primitive values and
		/// is treated as falsy for boolean operations.
		/// </summary>
		public static readonly JSValue Null = JS_MKVAL(JSTag.Null, 0);

		/// <summary>
		/// Represents the primitive value undefined.
		/// </summary>
		public static readonly JSValue Undefined = JS_MKVAL(JSTag.Undefined, 0);

		/// <summary>
		/// Represents a JavaScript false value.
		/// </summary>
		public static readonly JSValue False = JS_MKVAL(JSTag.Bool, 0);

		/// <summary>
		/// Represents a JavaScript true value.
		/// </summary>
		public static readonly JSValue True = JS_MKVAL(JSTag.Bool, 1);

		/// <summary>
		/// Represents an exception value.
		/// </summary>
		public static readonly JSValue Exception = JS_MKVAL(JSTag.Exception, 0);

		/// <summary>
		/// Represents an unitialized value. 
		/// </summary>
		public static readonly JSValue Uninitialized = JS_MKVAL(JSTag.Uninitialized, 0);

		/// <summary>
		/// Creates a value holding a 32-bit signed integer
		/// </summary>
		/// <param name="value">A 32-bit signed integer.</param>
		/// <returns>A <see cref="JSValue"/> holding a 32-bit signed integer.</returns>
		[MethodImpl(AggressiveInlining)]
		public static JSValue Create(int value)
		{
			return JS_NewInt32(default, value);
		}

		/// <summary>
		/// Creates a value holding a 32-bit unsigned integer
		/// </summary>
		/// <param name="value">A 32-bit unsigned integer.</param>
		/// <returns>A <see cref="JSValue"/> holding a 32-bit unsigned integer.</returns>
		[MethodImpl(AggressiveInlining)]
		public static JSValue Create(uint value)
		{
			return JS_NewUint32(default, value);
		}

		/// <summary>
		/// Creates a value holding a 64-bit signed integer.
		/// </summary>
		/// <param name="value">A 64-bit signed integer.</param>
		/// <returns>A <see cref="JSValue"/> holding a 64-bit signed integer.</returns>
		[MethodImpl(AggressiveInlining)]
		public static JSValue Create(long value)
		{
			return JS_NewInt64(default, value);
		}

		/// <summary>
		/// Creates a value holding a boolean value.
		/// </summary>
		/// <param name="value">A boolean value.</param>
		/// <returns>A <see cref="JSValue"/> holding a boolean value.</returns>
		[MethodImpl(AggressiveInlining)]
		public static JSValue Create(bool value)
		{
			return value ? JSValue.True : JSValue.False;
		}

		/// <summary>
		/// Creates a value holding a double-precision floating-point number.
		/// </summary>
		/// <param name="value">A double-precision floating-point number.</param>
		/// <returns>A <see cref="JSValue"/> holding a double-precision floating-point number.</returns>
		[MethodImpl(AggressiveInlining)]
		public static JSValue Create(double value)
		{
			return JS_NewFloat64(default, value);
		}

		/// <summary>
		/// Creates a new <see cref="JSValue"/> which is a string.
		/// </summary>
		/// <param name="context">
		/// A pointer to the native JSContext in which the value will be created.
		/// </param>
		/// <param name="value">The string from which to create the <see cref="JSValue"/>.</param>
		/// <returns>A <see cref="JSValue"/> created from the <paramref name="value"/>.</returns>
		public unsafe static JSValue Create(JSContext context, string value)
		{
			if (value is null)
				return JSValue.Null;
			fixed (byte* s = Utils.StringToManagedUTF8(value, out int len))
			{
				return JS_NewStringLen(context, s, len);
			}
		}

		/// <summary>
		/// Creates a new <see cref="JSValue"/> which is an Error object.
		/// </summary>
		/// <param name="context">
		/// A pointer to the native JSContext in which the value will be created.
		/// </param>
		/// <returns>A <see cref="JSValue"/> instance representing an error.</returns>
		[MethodImpl(AggressiveInlining)]
		public unsafe static JSValue CreateError(JSContext context)
		{
			return JS_NewError(context);
		}

		/// <summary>
		/// Creates a new JavaScript object.
		/// </summary>
		/// <param name="context">The context in which to create the new object.</param>
		/// <returns>A <see cref="JSValue"/> holding a new JavaScript object.</returns>
		/// <exception cref="QuickJSException">Cannot create a new object.</exception>
		[MethodImpl(AggressiveInlining)]
		public static JSValue CreateObject(JSContext context)
		{
			JSValue obj = JS_NewObject(context);
			if (obj.Tag == JSTag.Exception)
				context.ThrowPendingException();
			return obj;
		}

		/// <summary>
		/// Creates a new JavaScript object.
		/// </summary>
		/// <param name="context">The context in which to create the new object.</param>
		/// <param name="classId">The class ID of a new object.</param>
		/// <returns>A <see cref="JSValue"/> holding a new JavaScript object.</returns>
		/// <exception cref="QuickJSException">Cannot create a new object.</exception>
		[MethodImpl(AggressiveInlining)]
		public static JSValue CreateObject(JSContext context, JSClassID classId)
		{
			JSValue obj = JS_NewObjectClass(context, classId);
			if (obj.Tag == JSTag.Exception)
				context.ThrowPendingException();
			return obj;
		}

		/// <summary>
		/// Creates a new JavaScript object.
		/// </summary>
		/// <param name="context">The context in which to create a new object.</param>
		/// <param name="proto">The object to set as the prototype of a new object.</param>
		/// <param name="classId">The class ID of a new object.</param>
		/// <returns>A <see cref="JSValue"/> holding a new JavaScript object.</returns>
		/// <exception cref="QuickJSException">Cannot create a new object.</exception>
		[MethodImpl(AggressiveInlining)]
		public static JSValue CreateObject(JSContext context, JSValue proto, JSClassID classId)
		{
			JSTag tag = proto.Tag;
			if (tag != JSTag.Object && tag != JSTag.Null)
				throw new ArgumentOutOfRangeException(nameof(proto));

			JSValue obj = JS_NewObjectProtoClass(context, proto, classId);
			if (obj.Tag == JSTag.Exception)
				context.ThrowPendingException();
			return obj;
		}

		/// <summary>
		/// Creates a new JavaScript Array object.
		/// </summary>
		/// <param name="context">The context in which to create the new Array object.</param>
		/// <returns>A <see cref="JSValue"/> holding a new JavaScript Array object.</returns>
		/// <exception cref="QuickJSException">Cannot create a new array.</exception>
		[MethodImpl(AggressiveInlining)]
		public static JSValue CreateArray(JSContext context)
		{
			JSValue obj = JS_NewArray(context);
			if (obj.Tag == JSTag.Exception)
				context.ThrowPendingException();
			return obj;
		}

		/// <summary>
		/// Parses the specified JSON string, constructing the JavaScript value or object described by the string.
		/// </summary>
		/// <param name="context">A pointer to the context in which to create the new object.</param>
		/// <param name="json">The string to parse as JSON.</param>
		/// <param name="filename">The name of the JSON file.</param>
		/// <returns>The new <see cref="JSValue"/> corresponding to the given JSON text.</returns>
		/// <exception cref="QuickJSException">The string to parse is not valid JSON.</exception>
		public unsafe static JSValue FromJSON(JSContext context, string json, string filename)
		{
			if (json == null)
				return JSValue.Null;

			fixed (byte* buffer = Utils.StringToManagedUTF8(json, out int len))
			fixed (byte* file = Utils.StringToManagedUTF8(filename))
			{
				JSValue value = JS_ParseJSON(context, buffer, len, file);
				if (value == JSValue.Exception)
					context.ThrowPendingException();
				return value;
			}
		}

		[MethodImpl(AggressiveInlining)]
		internal static JSValue JS_MKVAL(JSTag tag, int value)
		{
			var v = new JSValue();
			v.uint64 = unchecked((uint)value);
			v._tagdata.tag = tag;
			return v;
		}

		[MethodImpl(AggressiveInlining)]
		internal static JSValue __JS_NewFloat64(JSContext ctx, double d)
		{
			JSValue v = new JSValue();
			v._tagdata.tag = JSTag.Float64;
			v.float64 = d;
			/* normalize NaN */
			if ((v.uint64 & 0x7fffffffffffffff) > 0x7ff0000000000000)
				return JSValue.NaN;
			else
				v.uint64 = v.uint64 - JS_FLOAT64_TAG_ADDEND_BITS;
			return v;
		}

		/// <summary>
		/// Returns a value that indicates whether the specified value is not a
		/// number (NaN).
		/// </summary>
		/// <param name="value">The value to be tested.</param>
		/// <returns>
		/// true if <paramref name="value"/> evaluates to NaN; otherwise, false.
		/// </returns>
		public static bool IsNaN(JSValue value)
		{
			if (IntPtr.Size == 4)
				return (value.uint64 >> 32) == (NaN.uint64 >> 32);
		
			if(value.Tag != JSTag.Float64)
				return false;

			return (value.uint64 & 0x7fffffffffffffff) > 0x7ff0000000000000;
		}

		/// <summary>
		/// Tests whether this JS value is a JS Error object.
		/// </summary>
		/// <param name="context">The pointer to the context that the <see cref="JSValue"/> belongs to.</param>
		/// <returns></returns>
		[MethodImpl(AggressiveInlining)]
		public bool IsError(JSContext context)
		{
			return JS_IsError(context, this);
		}

		/// <summary>
		/// Tests whether this JS value is a JS Array object.
		/// </summary>
		/// <param name="context">The pointer to the context that the <see cref="JSValue"/> belongs to.</param>
		/// <returns></returns>
		[MethodImpl(AggressiveInlining)]
		public bool IsArray(JSContext context)
		{
			return JS_IsArray(context, this) == 1;
		}

		/// <summary>
		/// Returns a <see cref="Int32"/> for the <see cref="JSValue"/>.
		/// </summary>
		/// <returns>A signed integer value.</returns>
		/// <exception cref="InvalidCastException">
		/// The <see cref="JSValue"/> is not a signed integer value.
		/// </exception>
		public int ToInt32()
		{
			if (TryGetInt32(out int value))
				return value;
			throw new InvalidCastException();
		}

		/// <summary>
		/// Tries to get the <see cref="Int32"/> value of <see cref="JSValue"/>.
		/// </summary>
		/// <param name="value">A signed integer value.</param>
		/// <returns>true if the operation is successful; otherwise, false.</returns>
		public bool TryGetInt32(out int value)
		{
			if (Tag == JSTag.Int)
			{
				value = this.int32;
				return true;
			}
			value = 0;
			return false;
		}

		/// <summary>
		/// Returns a <see cref="Double"/> for the <see cref="JSValue"/>.
		/// </summary>
		/// <returns>A floating-point value.</returns>
		/// <exception cref="InvalidCastException">
		/// The <see cref="JSValue"/> is not a floating-point value.
		/// </exception>
		public double ToDouble()
		{
			if (TryGetDouble(out double value))
				return value;
			throw new InvalidCastException();
		}

		/// <summary>
		/// Tries to get the <see cref="Double"/> value of <see cref="JSValue"/>.
		/// </summary>
		/// <param name="value">A floating-point value.</param>
		/// <returns>true if the operation is successful; otherwise, false.</returns>
		public bool TryGetDouble(out double value)
		{
			if (Tag == JSTag.Float64)
			{
				JSValue v = this;
				v.uint64 += JS_FLOAT64_TAG_ADDEND_BITS;
				value = v.float64;
				return true;
			}
			value = 0;
			return false;
		}

		/// <summary>
		/// Converts the JavaScript value to a native floating-point value.
		/// </summary>
		/// <returns>A floating-point value.</returns>
		/// <exception cref="InvalidCastException">
		/// The <see cref="JSValue"/> is not a number value.
		/// </exception>
		public double ToNumber()
		{
			if (TryGetNumber(out double value))
				return value;
			throw new InvalidCastException();
		}

		/// <summary>
		/// Tries to convert the JavaScript value to a native floating-point value.
		/// </summary>
		/// <param name="value">The result of the conversion operation.</param>
		/// <returns>true if the operation is successful; otherwise, false.</returns>
		public bool TryGetNumber(out double value)
		{
			switch (Tag)
			{
				case JSTag.Int:
					value = int32;
					return true;
				case JSTag.Float64:
					JSValue v = this;
					v.uint64 += JS_FLOAT64_TAG_ADDEND_BITS;
					value = v.float64;
					return true;
				default:
					value = 0;
					return false;
			}
		}

		/// <summary>
		/// Returns a <see cref="Boolean"/> for the <see cref="JSValue"/>.
		/// </summary>
		/// <returns>A <see cref="Boolean"/> value.</returns>
		/// <exception cref="InvalidCastException">
		/// The <see cref="JSValue"/> is not a <see cref="Boolean"/> value.
		/// </exception>
		public bool ToBoolean()
		{
			if (Tag == JSTag.Bool)
				return int32 != 0;
			throw new InvalidCastException();
		}

		/// <summary>
		/// Tries to get the <see cref="Boolean"/> value of <see cref="JSValue"/>.
		/// </summary>
		/// <param name="value">A <see cref="Boolean"/> value.</param>
		/// <returns>true if the operation is successful; otherwise, false.</returns>
		public bool TryGetBoolean(out bool value)
		{
			if (Tag == JSTag.Bool)
			{
				value = (int32 != 0);
				return true;
			}
			value = false;
			return false;
		}

		/// <summary>
		/// Converts the JavaScript value to a <see cref="Boolean"/> value.
		/// </summary>
		/// <param name="context">The context that <see cref="JSValue"/> belongs to.</param>
		/// <returns>A <see cref="Boolean"/> value.</returns>
		/// <exception cref="InvalidCastException">
		/// The value of the <see cref="JSValue"/> cannot be converted to a <see cref="Boolean"/> value.
		/// </exception>
		public bool ConvertToBoolean(JSContext context)
		{
			int rv = JS_ToBool(context, this);
			if (rv == -1)
				context.ThrowPendingException();
			return rv != 0;
		}

		/// <summary>
		/// Tries to convert the JavaScript value to a <see cref="Boolean"/> value.
		/// </summary>
		/// <param name="context">The context that <see cref="JSValue"/> belongs to.</param>
		/// <param name="value">A <see cref="Boolean"/> value.</param>
		/// <returns>true if the operation is successful; otherwise, false.</returns>
		public bool TryConvertToBoolean(JSContext context, out bool value)
		{
			int rv = JS_ToBool(context, this);
			value = rv != 0;
			if (rv == -1)
			{
				JS_FreeValue(context, JS_GetException(context));
				return false;
			}
			return true;
		}

		/// <summary>
		/// Gets a pointer to a native JSRefCountHeader.
		/// </summary>
		/// <returns>A pointer to memory containing a native JSRefCountHeader.</returns>
		public unsafe IntPtr ToPointer()
		{
			if (Tag <= JSTag.Object)
				return new IntPtr(ptr);
			throw new InvalidCastException();
		}

		/// <summary>
		/// Returns the value of the specified JavaScript property
		/// in this  <see cref="JSValue"/>.
		/// </summary>
		/// <param name="context">
		/// The context that <see cref="JSValue"/> belongs to.
		/// </param>
		/// <param name="name">The name of the property.</param>
		/// <returns>
		/// A string containing the value of the specified
		/// JavaScript property.
		/// </returns>
		public string GetStringProperty(JSContext context, string name)
		{
			JSValue val = JS_GetPropertyStr(context, this, name);
			try
			{
				return val.ToString(context, false);
			}
			finally
			{
				JS_FreeValue(context, val);
			}
		}

		/// <summary>
		/// Returns the value of the specified JavaScript property in this
		/// <see cref="JSValue"/> as <see cref="Int32"/> value.
		/// </summary>
		/// <param name="context">
		/// The context that <see cref="JSValue"/> belongs to.
		/// </param>
		/// <param name="name">The name of the property.</param>
		/// <returns>
		/// An <see cref="Int32"/> value that represents the value of
		/// the specified JavaScript property.
		/// </returns>
		/// <exception cref="InvalidCastException">
		/// The value of the specified JavaScript property is not
		/// an <see cref="Int32"/> value.
		/// </exception>
		public int GetInt32Property(JSContext context, string name)
		{
			JSValue val = JS_GetPropertyStr(context, this, name);
			try
			{
				if (val.Tag != JSTag.Int)
					throw new InvalidCastException();
				return val.int32;
			}
			finally
			{
				JS_FreeValue(context, val);
			}
		}

		/// <summary>
		/// Finds a specified property and retrieve its value.
		/// </summary>
		/// <param name="context">A pointer to the JavaScript context.</param>
		/// <param name="name">The name of the property to look up.</param>
		/// <returns>
		/// The current value of the property,
		/// or <see cref="JSValue.Undefined"/>/<see cref="JSValue.Exception"/>
		/// if no such property is found.
		/// </returns>
		public unsafe JSValue GetProperty(JSContext context, string name)
		{
			if (name is null)
				throw new ArgumentNullException(nameof(name));

			fixed (byte* str = Utils.StringToManagedUTF8(name))
			{
				return JS_GetPropertyStr(context, this, str);
			}
		}

		/// <summary>
		/// Finds a specified property and retrieve its value.
		/// </summary>
		/// <param name="context">A pointer to the JavaScript context.</param>
		/// <param name="index">The index of the property to look up.</param>
		/// <returns>
		/// The current value of the property,
		/// or <see cref="JSValue.Undefined"/>/<see cref="JSValue.Exception"/>
		/// if no such property is found.
		/// </returns>
		public JSValue GetProperty(JSContext context, int index)
		{
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			return JS_GetPropertyUint32(context, this, unchecked((uint)index));
		}

		/// <summary>
		/// Finds a specified property and retrieve its value.
		/// </summary>
		/// <param name="context">A pointer to the JavaScript context.</param>
		/// <param name="prop">The property to look up.</param>
		/// <returns>
		/// The current value of the property,
		/// or <see cref="JSValue.Undefined"/>/<see cref="JSValue.Exception"/>
		/// if no such property is found.
		/// </returns>
		public JSValue GetProperty(JSContext context, JSAtom prop)
		{
			return JS_GetProperty(context, this, prop);
		}

		/// <summary>
		/// Assigns a value to a property of an object.
		/// </summary>
		/// <param name="context">A pointer to the JavaScript context.</param>
		/// <param name="name">The name of the property to set.</param>
		/// <param name="value">The value to assign to the property.</param>
		/// <returns>true if the operation is successful; otherwise, false.</returns>
		/// <remarks>Warning: <paramref name="value"/> is freed by the function.</remarks>
		/// <exception cref="QuickJSException">An error has occurred.</exception>
		public unsafe bool SetProperty(JSContext context, string name, JSValue value)
		{
			if (name is null)
				throw new ArgumentNullException(nameof(name));

			int rv;
			fixed (byte* prop = Utils.StringToManagedUTF8(name))
			{
				rv = JS_SetPropertyStr(context, this, prop, value);
			}
			if (rv == -1)
				context.ThrowPendingException();
			return rv != 0;
		}

		/// <summary>
		/// Assigns a value to a property of an object.
		/// </summary>
		/// <param name="context">A pointer to the JavaScript context.</param>
		/// <param name="index">The index of the property to set.</param>
		/// <param name="value">The value to assign to the property.</param>
		/// <returns>true if the operation is successful; otherwise, false.</returns>
		/// <remarks>Warning: <paramref name="value"/> is freed by the function.</remarks>
		/// <exception cref="QuickJSException">An error has occurred.</exception>
		public bool SetProperty(JSContext context, int index, JSValue value)
		{
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			int rv = JS_SetPropertyUint32(context, this, (unchecked((uint)index)), value);
			if (rv == -1)
				context.ThrowPendingException();
			return rv != 0;
		}

		/// <summary>
		/// Assigns a value to a property of an object.
		/// </summary>
		/// <param name="context">A pointer to the JavaScript context.</param>
		/// <param name="prop">The property to set.</param>
		/// <param name="value">The value to assign to the property.</param>
		/// <returns>true if the operation is successful; otherwise, false.</returns>
		/// <remarks>Warning: <paramref name="value"/> is freed by the function.</remarks>
		/// <exception cref="QuickJSException">An error has occurred.</exception>
		public bool SetProperty(JSContext context, JSAtom prop, JSValue value)
		{
			int rv = JS_SetProperty(context, this, prop, value);
			if (rv == -1)
				context.ThrowPendingException();
			return rv != 0;
		}

		/// <summary>
		/// Assigns a value to a property of an object.
		/// </summary>
		/// <param name="context">A pointer to the JavaScript context.</param>
		/// <param name="name">The name of the property to set.</param>
		/// <param name="value">The value to assign to the property.</param>
		/// <returns>true if the operation is successful; otherwise, false.</returns>
		/// <remarks>Warning: <paramref name="value"/> is freed by the function.</remarks>
		/// <exception cref="QuickJSException">An error has occurred.</exception>
		[MethodImpl(AggressiveInlining)]
		public bool SetProperty(JSContext context, string name, int value)
		{
			return SetProperty(context, name, JS_NewInt32(context, value));
		}

		/// <summary>
		/// Assigns a value to a property of an object.
		/// </summary>
		/// <param name="context">A pointer to the JavaScript context.</param>
		/// <param name="name">The name of the property to set.</param>
		/// <param name="value">The value to assign to the property.</param>
		/// <returns>true if the operation is successful; otherwise, false.</returns>
		/// <remarks>Warning: <paramref name="value"/> is freed by the function.</remarks>
		/// <exception cref="QuickJSException">An error has occurred.</exception>
		[MethodImpl(AggressiveInlining)]
		public bool SetProperty(JSContext context, string name, double value)
		{
			return SetProperty(context, name, JS_NewFloat64(context, value));
		}

		/// <summary>
		/// Assigns a value to a property of an object.
		/// </summary>
		/// <param name="context">A pointer to the JavaScript context.</param>
		/// <param name="name">The name of the property to set.</param>
		/// <param name="value">The value to assign to the property.</param>
		/// <returns>true if the operation is successful; otherwise, false.</returns>
		/// <remarks>Warning: <paramref name="value"/> is freed by the function.</remarks>
		/// <exception cref="QuickJSException">An error has occurred.</exception>
		[MethodImpl(AggressiveInlining)]
		public bool SetProperty(JSContext context, string name, bool value)
		{
			return SetProperty(context, name, value ? True : False);
		}

		/// <summary>
		/// Assigns a value to a property of an object.
		/// </summary>
		/// <param name="context">A pointer to the JavaScript context.</param>
		/// <param name="name">The name of the property to set.</param>
		/// <param name="value">The value to assign to the property.</param>
		/// <returns>true if the operation is successful; otherwise, false.</returns>
		/// <remarks>Warning: <paramref name="value"/> is freed by the function.</remarks>
		/// <exception cref="QuickJSException">An error has occurred.</exception>
		[MethodImpl(AggressiveInlining)]
		public bool SetProperty(JSContext context, string name, string value)
		{
			return SetProperty(context, name, JSValue.Create(context, value));
		}


		/// <summary>
		/// Assigns a value to a property of an object.
		/// </summary>
		/// <param name="context">A pointer to the JavaScript context.</param>
		/// <param name="index">The index of the property to set.</param>
		/// <param name="value">The value to assign to the property.</param>
		/// <returns>true if the operation is successful; otherwise, false.</returns>
		/// <remarks>Warning: <paramref name="value"/> is freed by the function.</remarks>
		/// <exception cref="QuickJSException">An error has occurred.</exception>
		[MethodImpl(AggressiveInlining)]
		public bool SetProperty(JSContext context, int index, int value)
		{
			return SetProperty(context, index, JS_NewInt32(context, value));
		}

		/// <summary>
		/// Assigns a value to a property of an object.
		/// </summary>
		/// <param name="context">A pointer to the JavaScript context.</param>
		/// <param name="index">The index of the property to set.</param>
		/// <param name="value">The value to assign to the property.</param>
		/// <returns>true if the operation is successful; otherwise, false.</returns>
		/// <remarks>Warning: <paramref name="value"/> is freed by the function.</remarks>
		/// <exception cref="QuickJSException">An error has occurred.</exception>
		[MethodImpl(AggressiveInlining)]
		public bool SetProperty(JSContext context, int index, double value)
		{
			return SetProperty(context, index, JS_NewFloat64(context, value));
		}

		/// <summary>
		/// Assigns a value to a property of an object.
		/// </summary>
		/// <param name="context">A pointer to the JavaScript context.</param>
		/// <param name="index">The index of the property to set.</param>
		/// <param name="value">The value to assign to the property.</param>
		/// <returns>true if the operation is successful; otherwise, false.</returns>
		/// <remarks>Warning: <paramref name="value"/> is freed by the function.</remarks>
		/// <exception cref="QuickJSException">An error has occurred.</exception>
		[MethodImpl(AggressiveInlining)]
		public bool SetProperty(JSContext context, int index, bool value)
		{
			return SetProperty(context, index, value ? True : False);
		}

		/// <summary>
		/// Assigns a value to a property of an object.
		/// </summary>
		/// <param name="context">A pointer to the JavaScript context.</param>
		/// <param name="index">The index of the property to set.</param>
		/// <param name="value">The value to assign to the property.</param>
		/// <returns>true if the operation is successful; otherwise, false.</returns>
		/// <remarks>Warning: <paramref name="value"/> is freed by the function.</remarks>
		/// <exception cref="QuickJSException">An error has occurred.</exception>
		[MethodImpl(AggressiveInlining)]
		public bool SetProperty(JSContext context, int index, string value)
		{
			return SetProperty(context, index, JSValue.Create(context, value));
		}

		/// <summary>
		/// Tries to get the underlying string from a <see cref="JSValue"/>.
		/// </summary>
		/// <param name="ctx">The context that <see cref="JSValue"/> belongs to.</param>
		/// <param name="value">
		/// When the method returns, the string contained in the <see cref="JSValue"/>.
		/// </param>
		/// <returns>true if the operation is successful; otherwise, false.</returns>
		[MethodImpl(AggressiveInlining)]
		public bool TryGetString(JSContext ctx, out string value)
		{
			return TryGetString(ctx, true, out value);
		}

		/// <summary>
		/// Tries to get the underlying string from a <see cref="JSValue"/>.
		/// </summary>
		/// <param name="ctx">The context that <see cref="JSValue"/> belongs to.</param>
		/// <param name="cesu8">Determines if non-BMP1 codepoints are encoded as 1 or 2 utf-8 sequences.</param>
		/// <param name="value">
		/// When the method returns, the string contained in the <see cref="JSValue"/>.
		/// </param>
		/// <returns>true if the operation is successful; otherwise, false.</returns>
		public bool TryGetString(JSContext ctx, bool cesu8, out string value)
		{
			if (Tag == JSTag.String)
			{
				value = ToString(ctx, cesu8);
				return value != null;
			}
			value = null;
			return false;
		}

		/// <summary>
		/// Returns a string representation of the value of the current instance.
		/// </summary>
		/// <param name="ctx">The context that <see cref="JSValue"/> belongs to.</param>
		/// <param name="cesu8">Determines if non-BMP1 codepoints are encoded as 1 or 2 utf-8 sequences.</param>
		/// <returns>A string representation of the value of the current instance.</returns>
		public string ToString(JSContext ctx, bool cesu8)
		{
			SizeT len;
			IntPtr p = JS_ToCStringLen2(ctx, out len, this, cesu8);
			if (p == IntPtr.Zero)
				return null;

			try
			{
				return Utils.PtrToStringUTF8(p, len);
			}
			finally
			{
				JS_FreeCString(ctx, p);
			}
		}

		/// <summary>
		/// Returns a string representation of the value of the current instance.
		/// </summary>
		/// <param name="ctx">The context that <see cref="JSValue"/> belongs to.</param>
		/// <returns>A string representation of the value of the current instance.</returns>
		public string ToString(JSContext ctx)
		{
			return ToString(ctx, false);
		}

		/// <summary>
		/// Converts a JavaScript object or value to a JSON string, optionally replacing values if
		/// a <paramref name="replacer"/> function is specified or optionally including only
		/// the specified properties if a <paramref name="replacer"/> array is specified.
		/// </summary>
		/// <param name="ctx">A pointer to the JavaScript context.</param>
		/// <param name="replacer">
		/// A function that alters the behavior of the stringification process, or an array of String
		/// and Number that serve as a whitelist for selecting/filtering the properties of the value
		/// object to be included in the JSON string.<para/>
		/// If this value is null or not provided, all properties of the object are included in the
		/// resulting JSON string.
		/// </param>
		/// <param name="space">
		/// The string (or the first 10 characters of the string, if it&apos;s longer than that)
		/// is used as white space. If this parameter is null, no white space is used.
		/// </param>
		/// <param name="json">
		/// When the method returns, the JSON string representing this <see cref="JSValue"/>.
		/// </param>
		/// <returns>true if the operation is successful; otherwise, false.</returns>
		public bool TryGetJSON(JSContext ctx, JSValue replacer, string space, out string json)
		{
			JSValue value = JSValue.Undefined;
			JSValue spaceVal = JSValue.Create(ctx, space);
			try
			{
				value = JS_JSONStringify(ctx, this, replacer, spaceVal);
				if (value == JSValue.Exception)
				{
					json = null;
					JS_FreeValue(ctx, JS_GetException(ctx));
					return false;
				}
				return value.TryGetString(ctx, out json);
			}
			finally
			{
				JS_FreeValue(ctx, value);
				JS_FreeValue(ctx, spaceVal);
			}
		}

		/// <summary>
		/// Converts a JavaScript object or value to a JSON string, optionally replacing values if
		/// a <paramref name="replacer"/> function is specified or optionally including only
		/// the specified properties if a <paramref name="replacer"/> array is specified.
		/// </summary>
		/// <param name="ctx">A pointer to the JavaScript context.</param>
		/// <param name="replacer">
		/// A function that alters the behavior of the stringification process, or an array of String
		/// and Number that serve as a whitelist for selecting/filtering the properties of the value
		/// object to be included in the JSON string.<para/>
		/// If this value is null or not provided, all properties of the object are included in the
		/// resulting JSON string.
		/// </param>
		/// <param name="space">
		/// Indicates the number of space characters to use as white space;
		/// this number is capped at 10 (if it is greater, the value is just 10).
		/// Values less than 1 indicate that no space should be used.
		/// </param>
		/// <param name="json">
		/// When the method returns, the JSON string representing this <see cref="JSValue"/>.
		/// </param>
		/// <returns>true if the operation is successful; otherwise, false.</returns>
		public bool TryGetJSON(JSContext ctx, JSValue replacer, int space, out string json)
		{
			JSValue value = JSValue.Undefined;;
			try
			{
				value = JS_JSONStringify(ctx, this, replacer, JS_NewInt32(ctx, space));
				if (value == JSValue.Exception)
				{
					json = null;
					JS_FreeValue(ctx, JS_GetException(ctx));
					return false;
				}
				return value.TryGetString(ctx, out json);
			}
			finally
			{
				JS_FreeValue(ctx, value);
			}
		}

		/// <summary>
		/// Converts a JavaScript object or value to a JSON string.
		/// </summary>
		/// <param name="ctx">A pointer to the JavaScript context.</param>
		/// <param name="json">
		/// When the method returns, the JSON string representing this <see cref="JSValue"/>.
		/// </param>
		/// <returns>true if the operation is successful; otherwise, false.</returns>
		public bool TryGetJSON(JSContext ctx, out string json)
		{
			JSValue value = JSValue.Undefined; ;
			try
			{
				value = JS_JSONStringify(ctx, this, JSValue.Undefined, JSValue.Undefined);
				if (value == JSValue.Exception)
				{
					json = null;
					JS_FreeValue(ctx, JS_GetException(ctx));
					return false;
				}
				return value.TryGetString(ctx, out json);
			}
			finally
			{
				JS_FreeValue(ctx, value);
			}
		}

		/// <summary>
		/// Converts a JavaScript object or value to a JSON string, optionally replacing values if
		/// a <paramref name="replacer"/> function is specified or optionally including only
		/// the specified properties if a <paramref name="replacer"/> array is specified.
		/// </summary>
		/// <param name="ctx">A pointer to the JavaScript context.</param>
		/// <param name="replacer">
		/// A function that alters the behavior of the stringification process, or an array of String
		/// and Number that serve as a whitelist for selecting/filtering the properties of the value
		/// object to be included in the JSON string.<para/>
		/// If this value is null or not provided, all properties of the object are included in the
		/// resulting JSON string.
		/// </param>
		/// <param name="space">
		/// The string (or the first 10 characters of the string, if it&apos;s longer than that)
		/// is used as white space. If this parameter is null, no white space is used.
		/// </param>
		/// <returns>The JSON string representing this <see cref="JSValue"/>.</returns>
		public string ToJSON(JSContext ctx, JSValue replacer, string space)
		{
			JSValue value = JSValue.Undefined;
			JSValue spaceVal = JSValue.Create(ctx, space);
			try
			{
				value = JS_JSONStringify(ctx, this, replacer, spaceVal);
				if (value == JSValue.Exception)
					ctx.ThrowPendingException();
				return value.ToString(ctx);
			}
			finally
			{
				JS_FreeValue(ctx, value);
				JS_FreeValue(ctx, spaceVal);
			}
		}

		/// <summary>
		/// Converts a JavaScript object or value to a JSON string, optionally replacing values if
		/// a <paramref name="replacer"/> function is specified or optionally including only
		/// the specified properties if a <paramref name="replacer"/> array is specified.
		/// </summary>
		/// <param name="ctx">A pointer to the JavaScript context.</param>
		/// <param name="replacer">
		/// A function that alters the behavior of the stringification process, or an array of String
		/// and Number that serve as a whitelist for selecting/filtering the properties of the value
		/// object to be included in the JSON string.<para/>
		/// If this value is null or not provided, all properties of the object are included in the
		/// resulting JSON string.
		/// </param>
		/// <param name="space">
		/// Indicates the number of space characters to use as white space;
		/// this number is capped at 10 (if it is greater, the value is just 10).
		/// Values less than 1 indicate that no space should be used.
		/// </param>
		/// <returns>The JSON string representing this <see cref="JSValue"/>.</returns>
		public string ToJSON(JSContext ctx, JSValue replacer, int space)
		{
			JSValue value = JSValue.Undefined; ;
			try
			{
				value = JS_JSONStringify(ctx, this, replacer, JS_NewInt32(ctx, space));
				if (value == JSValue.Exception)
					ctx.ThrowPendingException();
				return value.ToString(ctx);
			}
			finally
			{
				JS_FreeValue(ctx, value);
			}
		}

		/// <summary>
		/// Converts a JavaScript object or value to a JSON string.
		/// </summary>
		/// <param name="ctx">A pointer to the JavaScript context.</param>
		/// <returns>The JSON string representing this <see cref="JSValue"/>.</returns>
		public string ToJSON(JSContext ctx)
		{
			JSValue value = JSValue.Undefined; ;
			try
			{
				value = JS_JSONStringify(ctx, this, JSValue.Undefined, JSValue.Undefined);
				if (value == JSValue.Exception)
					ctx.ThrowPendingException();
				return value.ToString(ctx);
			}
			finally
			{
				JS_FreeValue(ctx, value);
			}
		}

		/// <summary>
		/// Test whether this value is a Function.
		/// </summary>
		/// <param name="context">The pointer to the context that this value belongs to.</param>
		/// <returns>true if this value is a Function and false otherwise.</returns>
		[MethodImpl(AggressiveInlining)]
		public bool IsFunction(JSContext context)
		{
			return JS_IsFunction(context, this);
		}

		/// <summary>
		/// Gets the class ID of an object held by this value.
		/// </summary>
		/// <param name="classID">
		/// When the method returns, the class ID of an object held by this value.
		/// </param>
		/// <returns>true if this value is holding an object type; otherwise, false.</returns>
		public unsafe bool TryGetObjectClass(out JSClassID classID)
		{
			if (Tag != JSTag.Object)
			{
				classID = default;
				return false;
			}

			int cid = *(ushort*)((byte*)ToPointer() + 6);
			classID = *(JSClassID*)(&cid);
			return true;
		}

		/// <summary>
		/// Gets the tag of <see cref="JSValue"/>. 
		/// </summary>
		public JSTag Tag
		{
			get
			{
				if ((uint)((_tagdata.tag) - JSTag.First) >= (JSTag.Float64 - JSTag.First))
				{
					return JSTag.Float64;
				}
				return _tagdata.tag;
			}
		}

		/// <summary>
		/// Gets the value of the reference count.
		/// </summary>
		public unsafe int RefCount
		{
			get
			{
				if ((uint)_tagdata.tag >= unchecked((uint)JSTag.First))
				{
					JSRefCountHeader* p = (JSRefCountHeader*)ptr;
					return p->ref_count;
				}
				return 0;
			}
		}

		/// <summary>
		/// Returns a context-independed string representation of the value of
		/// the current instance or its type name.
		/// </summary>
		/// <returns>
		/// A string representation of the current instance.
		/// </returns>
		public override string ToString()
		{
			switch (Tag)
			{
				case JSTag.Bool:
					return ToBoolean().ToString();
				case JSTag.Int:
					return ToInt32().ToString();
				case JSTag.Float64:
					return ToDouble().ToString(NumberFormatInfo.InvariantInfo);
				case JSTag.Null:
					return null;
				case JSTag.Undefined:
					return "[undefined]";
				case JSTag.Uninitialized:
					return "[uninitialized]";
				case JSTag.Symbol:
					return "[symbol]";
				case JSTag.String:
					return "[string]";
				case JSTag.Object:
					return "[object]";
				case JSTag.First:
#pragma warning disable 219
					const uint assert = JSTag.First - JSTag.BigDecimal;
#pragma warning restore 219
					return "[BigDecimal]";
			}
			return "[" + Tag.ToString() + "]";
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			return obj is JSValue v && v == this;
		}

		/// <inheritdoc/>
		public unsafe override int GetHashCode()
		{
			if (sizeof(JSValue) == sizeof(ulong))
				return uint64.GetHashCode();
			return (uint64 & (ulong)((int)Tag << 17)).GetHashCode();
		}

		/// <summary>
		/// Compares two <see cref="JSValue"/> objects. The result specifies
		/// whether the values of the two <see cref="JSValue"/> objects are
		/// equal.
		/// </summary>
		/// <param name="left">A <see cref="JSValue"/> to compare.</param>
		/// <param name="right">A <see cref="JSValue"/> to compare.</param>
		/// <returns>
		/// true if <paramref name="left"/> and <paramref name="right"/> are
		/// equal; otherwise, false.
		/// </returns>
		public static unsafe bool operator ==(JSValue left, JSValue right)
		{
			if (sizeof(JSValue) == sizeof(ulong))
				return left.uint64 == right.uint64;
			return left.uint64 == right.uint64 && left._tagdata.tag == right._tagdata.tag;
		}

		/// <summary>
		/// Compares two <see cref="JSValue"/> objects. The result specifies
		/// whether the values of the two <see cref="JSValue"/> objects are
		/// unequal.
		/// </summary>
		/// <param name="left">A <see cref="JSValue"/> to compare.</param>
		/// <param name="right">A <see cref="JSValue"/> to compare.</param>
		/// <returns>
		/// true if <paramref name="left"/> and <paramref name="right"/> are
		/// unequal; otherwise, false.
		/// </returns>
		public static unsafe bool operator !=(JSValue left, JSValue right)
		{
			if (sizeof(JSValue) == sizeof(ulong))
				return left.uint64 != right.uint64;
			return left.uint64 != right.uint64 || left._tagdata.tag != right._tagdata.tag;
		}

		/// <summary>
		/// Converts the specified <see cref="JSValue"/> to a 64-bit unsigned integer.
		/// </summary>
		/// <param name="value">The <see cref="JSValue"/> to convert.</param>
		/// <exception cref="InvalidCastException">On a 64-bit platform.</exception>
		/// <remarks>This should only be used on 32 bit platforms to convert return values.</remarks>
		public static unsafe implicit operator ulong(JSValue value)
		{
			if (sizeof(JSValue) != sizeof(ulong))
				throw new InvalidCastException();
			return value.uint64;
		}

	}


}
