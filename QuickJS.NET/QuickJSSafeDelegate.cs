using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using QuickJS.Native;
using static QuickJS.Native.QuickJSNativeApi;

namespace QuickJS
{
	internal sealed class QuickJSSafeDelegate
	{
		private readonly Delegate _callback;
		private readonly Delegate _handler;
		private readonly int _data_len;

		public unsafe QuickJSSafeDelegate(JSCFunction function)
		{
			_callback = function;
			_handler = sizeof(JSValue) == sizeof(ulong) ? (Delegate)new JSCFunction32(Impl8) : new JSCFunction(Impl16);
		}

		public unsafe QuickJSSafeDelegate(JSCFunctionData function)
		{
			_callback = function;
			_handler = sizeof(JSValue) == sizeof(ulong) ? (Delegate)new JSCFunctionData32(CFnDataImpl8) : new JSCFunctionData(CFnDataImpl16);
		}

		public unsafe QuickJSSafeDelegate(JSCFunctionDataDelegate function, int dataLength)
		{
			_data_len = dataLength;
			_callback = function;
			_handler = sizeof(JSValue) == sizeof(ulong) ? (Delegate)new JSCFunctionData32(CFnData2Impl8) : new JSCFunctionData(CFnData2Impl16);
		}

		private ulong Impl8(JSContext cx, JSValue thisArg, int argc, JSValue[] argv)
		{
			try
			{
				return ((JSCFunction)_callback)(cx, thisArg, argc, argv).uint64;
			}
			catch (OutOfMemoryException)
			{
				return JS_ThrowOutOfMemory(cx).uint64;
			}
			catch (Exception ex)
			{
				return Utils.ReportException(cx, ex).uint64;
			}
		}

		private JSValue Impl16(JSContext cx, JSValue thisArg, int argc, JSValue[] argv)
		{
			try
			{
				return ((JSCFunction)_callback)(cx, thisArg, argc, argv);
			}
			catch (OutOfMemoryException)
			{
				return JS_ThrowOutOfMemory(cx);
			}
			catch (Exception ex)
			{
				return Utils.ReportException(cx, ex);
			}
		}

		private unsafe ulong CFnDataImpl8(JSContext cx, JSValue thisArg, int argc, JSValue[] argv, int magic, JSValue* data)
		{
			try
			{
				return ((JSCFunctionData)_callback)(cx, thisArg, argc, argv, magic, data).uint64;
			}
			catch (OutOfMemoryException)
			{
				return JS_ThrowOutOfMemory(cx).uint64;
			}
			catch (Exception ex)
			{
				return Utils.ReportException(cx, ex).uint64;
			}
		}

		private unsafe JSValue CFnDataImpl16(JSContext cx, JSValue thisArg, int argc, JSValue[] argv, int magic, JSValue* data)
		{
			try
			{
				return ((JSCFunctionData)_callback)(cx, thisArg, argc, argv, magic, data);
			}
			catch (OutOfMemoryException)
			{
				return JS_ThrowOutOfMemory(cx);
			}
			catch (Exception ex)
			{
				return Utils.ReportException(cx, ex);
			}
		}

		private unsafe ulong CFnData2Impl8(JSContext cx, JSValue thisArg, int argc, JSValue[] argv, int magic, JSValue* data)
		{
			try
			{
				var fnData = new JSValue[_data_len];
				for (int i = 0; i < fnData.Length; i++)
					fnData[i] = data[i];
				return ((JSCFunctionDataDelegate)_callback)(cx, thisArg, argv, magic, fnData).uint64;
			}
			catch (OutOfMemoryException)
			{
				return JS_ThrowOutOfMemory(cx).uint64;
			}
			catch (Exception ex)
			{
				return Utils.ReportException(cx, ex).uint64;
			}
		}

		private unsafe JSValue CFnData2Impl16(JSContext cx, JSValue thisArg, int argc, JSValue[] argv, int magic, JSValue* data)
		{
			try
			{
				var fnData = new JSValue[_data_len];
				for (int i = 0; i < fnData.Length; i++)
					fnData[i] = data[i];
				return ((JSCFunctionDataDelegate)_callback)(cx, thisArg, argv, magic, fnData);
			}
			catch (OutOfMemoryException)
			{
				return JS_ThrowOutOfMemory(cx);
			}
			catch (Exception ex)
			{
				return Utils.ReportException(cx, ex);
			}
		}

		[MethodImpl(AggressiveInlining)]
		public IntPtr GetPointer()
		{
			return Marshal.GetFunctionPointerForDelegate(_handler);
		}

	}
}
