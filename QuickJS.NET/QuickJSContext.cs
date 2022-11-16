using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using QuickJS.Native;
using static QuickJS.Native.QuickJSNativeApi;

namespace QuickJS
{
	/// <summary>
	/// Represents a JavaScript context.
	/// </summary>
	/// <remarks>
	/// Each JSContext has its own global objects and system objects.
	/// There can be several JSContexts per JSRuntime and they can
	/// share objects, similar to frames of the same origin sharing
	/// JavaScript objects in a web browser.
	/// </remarks>
	public class QuickJSContext : IDisposable
	{
		private static readonly Dictionary<JSContext, QuickJSContext> _AllContexts = new Dictionary<JSContext, QuickJSContext>();

		private readonly JSContext _context;
		private readonly GCHandle _handle;
		private Exception _clrException;
#if NET20
		private readonly List<QuickJSRefcounted> _values = new List<QuickJSRefcounted>();
		private readonly List<QuickJSSafeDelegate> _functions = new List<QuickJSSafeDelegate>();
#else
		private readonly HashSet<QuickJSRefcounted> _values = new HashSet<QuickJSRefcounted>();
		private readonly HashSet<QuickJSSafeDelegate> _functions = new HashSet<QuickJSSafeDelegate>();
#endif
		private List<QuickJSRefcounted> _finalizedValues = new List<QuickJSRefcounted>();
		private EventHandler<HandledEventArgs> _onRuntimeInterrupt;

		/// <summary>
		/// Gets the <see cref="QuickJSContext"/> associated with the specified <see cref="JSContext"/>.
		/// </summary>
		/// <param name="context">The pointer to the native JSContext.</param>
		/// <param name="wrapper">The <see cref="QuickJSContext"/> associated with the <paramref name="context"/> or null if not found.</param>
		/// <returns>true if wrapper is found; otherwise, false.</returns>
		public static bool TryWrap(JSContext context, out QuickJSContext wrapper)
		{
			lock (_AllContexts)
			{
				return _AllContexts.TryGetValue(context, out wrapper);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QuickJSContext"/>.
		/// </summary>
		/// <param name="runtime">A JavaScript runtime.</param>
		public QuickJSContext(QuickJSRuntime runtime)
			: this(runtime, false)
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QuickJSContext"/>.
		/// </summary>
		/// <param name="runtime">A JavaScript runtime.</param>
		/// <param name="raw">
		/// The value that determines that the context should be created without the default intrinsic objects.
		/// </param>
		public unsafe QuickJSContext(QuickJSRuntime runtime, bool raw)
		{
			if (runtime is null)
				throw new ArgumentNullException(nameof(runtime));

			_onRuntimeInterrupt = OnRuntimeInterrupt;

			this.Runtime = runtime;
			_context = raw ? JS_NewContextRaw(runtime.NativeInstance) : JS_NewContext(runtime.NativeInstance);
			if (_context == JSContext.Null)
				throw new InvalidOperationException();

			lock (_AllContexts)
			{
				_AllContexts.Add(_context, this);
			}

			_handle = GCHandle.Alloc(this, GCHandleType.Normal);
			JS_SetContextOpaque(_context, GCHandle.ToIntPtr(_handle));
			runtime.Interrupt += _onRuntimeInterrupt;
		}


		/// <summary>
		/// Releases the unmanaged resources used by the <see cref="QuickJSRuntime"/>. 
		/// </summary>
		/// <param name="disposing">
		/// true to release both managed and unmanaged resources; false to release
		/// only unmanaged resources.
		/// </param>
		protected virtual void Dispose(bool disposing)
		{
			if (!_handle.IsAllocated)
				return;

			Runtime.CheckAccess();
			Runtime.Interrupt -= _onRuntimeInterrupt;

			lock (_AllContexts)
			{
				_AllContexts.Remove(_context);
			}

			foreach (QuickJSRefcounted refcounted in _values)
			{
				JS_FreeValue(_context, refcounted.NativeValue);
			}

			JS_FreeContext(_context);
			_handle.Free();
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			Dispose(true);
		}

		private void OnRuntimeInterrupt(object sender, HandledEventArgs e)
		{
			if (_finalizedValues.Count == 0)
				return;

			QuickJSRefcounted[] refcounted;
			lock (_finalizedValues)
			{
				refcounted = _finalizedValues.ToArray();
				_finalizedValues.Clear();
			}
			foreach (QuickJSRefcounted item in refcounted)
			{
				JS_FreeValue(_context, item.NativeValue);
				_values.Remove(item);
			}
		}

		internal void ReleaseInContextThread(QuickJSRefcounted value)
		{
			if (value is null)
				return;

			lock (_finalizedValues)
			{
				_finalizedValues.Add(value);
			}
		}

		/// <summary>
		/// Gets a pointer to a native JSContext.
		/// </summary>
		public JSContext NativeInstance
		{
			get
			{
				if (!_handle.IsAllocated)
					throw new ObjectDisposedException(this.GetType().Name);
				return _context;
			}
		}

		/// <summary>
		/// Gets the <see cref="QuickJSRuntime"/> that the context belongs to.
		/// </summary>
		public QuickJSRuntime Runtime { get; }

		/// <summary>
		/// Determines that the specified <paramref name="context"/> and this context
		/// belong to the same runtime.
		/// </summary>
		/// <returns>true if contexts are compatible; otherwise, false.</returns>
		/// <remarks>
		/// There can be several JSContexts per JSRuntime and they can share objects,
		/// similar to frames of the same origin sharing JavaScript objects in a web
		/// browser.
		/// </remarks>
		public bool IsCompatibleWith(QuickJSContext context)
		{
			if (ReferenceEquals(context, this))
				return true;
			return !(context is null) && context.Runtime.NativeInstance == this.Runtime.NativeInstance;
		}

		internal void AddValue(QuickJSRefcounted value)
		{
#if NET20
			if (_values.Contains(value))
				return;
#endif
			_values.Add(value);
		}

		internal void RemoveValue(QuickJSRefcounted value)
		{
			_values.Remove(value);
		}

		/// <summary>
		/// Adds base object classes.
		/// </summary>
		public void AddIntrinsicBaseObjects()
		{
			JS_AddIntrinsicBaseObjects(this.NativeInstance);
		}

		/// <summary>
		/// Adds the BigDecimal class.
		/// </summary>
		public void AddIntrinsicBigDecimal()
		{
			JS_AddIntrinsicBigDecimal(this.NativeInstance);
		}

		/// <summary>
		/// Adds the BigFloat class.
		/// </summary>
		public void AddIntrinsicBigFloat()
		{
			JS_AddIntrinsicBigFloat(this.NativeInstance);
		}

		/// <summary>
		/// Adds the BigInt class.
		/// </summary>
		public void AddIntrinsicBigInt()
		{
			JS_AddIntrinsicBigInt(this.NativeInstance);
		}

		/// <summary>
		/// Adds the Date class.
		/// </summary>
		public void AddIntrinsicDate()
		{
			JS_AddIntrinsicDate(this.NativeInstance);
		}

		/// <summary>
		/// Adds the eval() function.
		/// </summary>
		public void AddIntrinsicEval()
		{
			JS_AddIntrinsicEval(this.NativeInstance);
		}

		/// <summary>
		/// Adds the JSON class.
		/// </summary>
		public void AddIntrinsicJSON()
		{
			JS_AddIntrinsicJSON(this.NativeInstance);
		}

		/// <summary>
		/// Adds maps and sets.
		/// </summary>
		public void AddIntrinsicMapSet()
		{
			JS_AddIntrinsicMapSet(this.NativeInstance);
		}

		/// <summary>
		/// Enable operator overloading. Must be called after all overloadable base types are initialized.
		/// </summary>
		public void AddIntrinsicOperators()
		{
			JS_AddIntrinsicOperators(this.NativeInstance);
		}

		/// <summary>
		/// Adds the Promise class.
		/// </summary>
		public void AddIntrinsicPromise()
		{
			JS_AddIntrinsicPromise(this.NativeInstance);
		}

		/// <summary>
		/// Adds the Proxy class.
		/// </summary>
		public void AddIntrinsicProxy()
		{
			JS_AddIntrinsicProxy(this.NativeInstance);
		}

		/// <summary>
		/// Adds the RegExp class.
		/// </summary>
		public void AddIntrinsicRegExp()
		{
			JS_AddIntrinsicRegExp(this.NativeInstance);
		}

		/// <summary>
		/// Adds the String.prototype.normalize() function.
		/// </summary>
		public void AddIntrinsicStringNormalize()
		{
			JS_AddIntrinsicStringNormalize(this.NativeInstance);
		}

		/// <summary>
		/// Adds the typed arrays, buffers and the DataView class.
		/// </summary>
		public void AddIntrinsicTypedArrays()
		{
			JS_AddIntrinsicTypedArrays(this.NativeInstance);
		}

		/// <summary>
		/// Adds <see href="https://bellard.org/quickjs/quickjs.html#Global-objects">global objects</see>:
		/// <list type="definition">
		///   <item>
		///     <term>scriptArgs</term>
		///     <description>provides the command line arguments. The first argument is the script name.</description>
		///   </item>
		///   <item>
		///     <term>print(...args)</term>
		///     <description>print the arguments separated by spaces and a trailing newline.</description>
		///   </item>
		///   <item>
		///     <term>console.log(...args)</term>
		///     <description>same as print().</description>
		///   </item>
		/// </list>	
		/// </summary>
		public unsafe void StdAddHelpers()
		{
			string[] args = Environment.GetCommandLineArgs();
			IntPtr argv = Utils.CreateArgv(Encoding.UTF8, args);
			try
			{
				js_std_add_helpers(this.NativeInstance, args.Length - 1, new IntPtr(((void**)argv) + 1));
			}
			finally
			{
				Utils.ReleaseArgv(argv);
			}
		}

		/// <summary>
		/// Initializes the <see href="https://bellard.org/quickjs/quickjs.html#std-module">&apos;std&apos; module</see>.
		/// </summary>
		/// <param name="moduleName">The module name.</param>
		/// <returns></returns>
		public JSModuleDef InitModuleStd(string moduleName)
		{
			return js_init_module_std(this.NativeInstance, moduleName);
		}

		/// <summary>
		/// Initializes the <see href="https://bellard.org/quickjs/quickjs.html#os-module">&apos;os&apos; module</see>.
		/// </summary>
		/// <param name="moduleName">The module name.</param>
		/// <returns></returns>
		public JSModuleDef InitModuleOS(string moduleName)
		{
			return js_init_module_os(this.NativeInstance, moduleName);
		}

		public void StdDumpError()
		{
			js_std_dump_error(this.NativeInstance);
		}

		/// <summary>
		/// Raises a user-defined exception.
		/// </summary>
		/// <param name="expression">
		/// The expression to throw. WARNING: <paramref name="expression"/> is freed.
		/// </param>
		public void Throw(JSValue expression)
		{
			JS_Throw(this.NativeInstance, expression);
		}

		/// <summary>
		/// Raises an Error exception with the specified message.
		/// </summary>
		/// <param name="message">An error message.</param>
		public unsafe void ThrowError(string message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			byte[] strbuf = Utils.StringToManagedUTF8(message);
			fixed (byte* s = strbuf)
			{
				JSContext cx = this.NativeInstance;
				JS_Throw(cx, JS_NewStringLen(cx, s, strbuf.Length - 1));
			}
		}

		/// <summary>
		/// Raises a SyntaxError exception.
		/// </summary>
		/// <param name="message">An error message.</param>
		public void ThrowSyntaxError(string message)
		{
			JS_ThrowSyntaxError(this.NativeInstance, message);
		}

		/// <summary>
		/// Raises a SyntaxError exception using the specified format information.
		/// </summary>
		/// <param name="format">A composite format string.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public void ThrowSyntaxError(string format, params object[] args)
		{
			JS_ThrowSyntaxError(this.NativeInstance, format, args);
		}

		/// <summary>
		/// Raises a TypeError exception.
		/// </summary>
		/// <param name="message">An error message.</param>
		public void ThrowTypeError(string message)
		{
			JS_ThrowTypeError(this.NativeInstance, message);
		}

		/// <summary>
		/// Raises a TypeError exception using the specified format information.
		/// </summary>
		/// <param name="format">A composite format string.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public void ThrowTypeError(string format, params object[] args)
		{
			JS_ThrowTypeError(this.NativeInstance, format, args);
		}

		/// <summary>
		/// Raises a ReferenceError exception.
		/// </summary>
		/// <param name="message">An error message.</param>
		public void ThrowReferenceError(string message)
		{
			JS_ThrowReferenceError(this.NativeInstance, message);
		}

		/// <summary>
		/// Raises a ReferenceError exception using the specified format information.
		/// </summary>
		/// <param name="format">A composite format string.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public void ThrowReferenceError(string format, params object[] args)
		{
			JS_ThrowReferenceError(this.NativeInstance, format, args);
		}

		/// <summary>
		/// Raises a RangeError exception.
		/// </summary>
		/// <param name="message">An error message.</param>
		public void ThrowRangeError(string message)
		{
			JS_ThrowRangeError(this.NativeInstance, message);
		}

		/// <summary>
		/// Raises a RangeError exception using the specified format information.
		/// </summary>
		/// <param name="format">A composite format string.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public void ThrowRangeError(string format, params object[] args)
		{
			JS_ThrowRangeError(this.NativeInstance, format, args);
		}

		/// <summary>
		/// Raises an InternalError exception.
		/// </summary>
		/// <param name="message">An error message.</param>
		public void ThrowInternalError(string message)
		{
			JS_ThrowInternalError(this.NativeInstance, message);
		}

		/// <summary>
		/// Raises an InternalError exception using the specified format information.
		/// </summary>
		/// <param name="format">A composite format string.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public void ThrowInternalError(string format, params object[] args)
		{
			JS_ThrowInternalError(this.NativeInstance, format, args);
		}

		/// <summary>
		/// Raises an OutOfMemory exception.
		/// </summary>
		public void ThrowOutOfMemory()
		{
			JS_ThrowOutOfMemory(this.NativeInstance);
		}

		/// <summary>
		/// Raises a StackOverflow exception.
		/// </summary>
		public void ThrowStackOverflow()
		{
			JS_ThrowInternalError(this.NativeInstance, "stack overflow");
		}

		/// <summary>
		/// Retrieves a context&apos;s global object.
		/// </summary>
		/// <returns>A context&apos;s global object.</returns>
		public QuickJSValue GetGlobal()
		{
			return QuickJSValue.Wrap(this, JS_GetGlobalObject(this.NativeInstance));
		}

		/// <summary>
		/// Creates a new JavaScript function in this context.
		/// </summary>
		/// <param name="name">The name property of the new function object.</param>
		/// <param name="function">A delegate to the function that is to be exposed to JavaScript.</param>
		/// <param name="argCount">The number of arguments the function expects to receive.</param>
		/// <returns>A value containing the new function.</returns>
		[MethodImpl(AggressiveInlining)]
		public unsafe JSValue CreateFunctionRaw(string name, JSCFunction function, int argCount)
		{
			fixed (byte* fnName = Utils.StringToManagedUTF8(name))
			{
				return CreateFunctionRawInternal(fnName, function, argCount);
			}
		}

		[MethodImpl(AggressiveInlining)]
		internal unsafe JSValue CreateFunctionRawInternal(byte* name, JSCFunction function, int argCount)
		{
			var fn = new QuickJSSafeDelegate(function);
			JSValue fnValue = JS_NewCFunction2(this.NativeInstance, fn.GetPointer(), name, argCount, JSCFunctionEnum.Generic, 0);
			if (JS_IsException(fnValue))
				_context.ThrowPendingException();
			else
				_functions.Add(fn);
			return fnValue;
		}

		/// <summary>
		/// Creates a new JavaScript function in this context.
		/// </summary>
		/// <param name="function">A delegate to the function that is to be exposed to JavaScript.</param>
		/// <param name="argCount">The number of arguments the function expects to receive.</param>
		/// <param name="magic">A magic value.</param>
		/// <param name="data">An array containing data to be used by the method.</param>
		/// <returns>A value containing the new function.</returns>
		public unsafe JSValue CreateFunctionRaw(JSCFunctionData function, int argCount, int magic, JSValue[] data)
		{
			var fn = new QuickJSSafeDelegate(function);
			JSValue fnValue = JS_NewCFunctionData(this.NativeInstance, fn.GetPointer(), argCount, magic, data is null ? 0 : data.Length, data);
			if (JS_IsException(fnValue))
				_context.ThrowPendingException();
			else
				_functions.Add(fn);
			return fnValue;
		}

		/// <summary>
		/// Creates a new JavaScript function in this context.
		/// </summary>
		/// <param name="function">A delegate to the function that is to be exposed to JavaScript.</param>
		/// <param name="argCount">The number of arguments the function expects to receive.</param>
		/// <param name="magic">A magic value.</param>
		/// <param name="data">An array containing data to be used by the method.</param>
		/// <returns>A value containing the new function.</returns>
		public JSValue CreateFunctionRaw(JSCFunctionDataDelegate function, int argCount, int magic, JSValue[] data)
		{
			int dataLength = data is null ? 0 : data.Length;
			var fn = new QuickJSSafeDelegate(function, dataLength);
			JSValue fnValue = JS_NewCFunctionData(this.NativeInstance, fn.GetPointer(), argCount, magic, dataLength, data);
			if (JS_IsException(fnValue))
				_context.ThrowPendingException();
			else
				_functions.Add(fn);
			return fnValue;
		}

		/// <summary>
		/// Creates a new JavaScript constructor function in this context.
		/// </summary>
		/// <param name="name">The name property of the new function object.</param>
		/// <param name="function">A delegate to the function that is to be exposed to JavaScript.</param>
		/// <param name="argCount">The number of arguments the function expects to receive.</param>
		/// <returns>A value containing the new function.</returns>
		public JSValue CreateConstructorRaw(string name, JSCFunction function, int argCount)
		{
			JSValue ctor = CreateFunctionRaw(name, function, argCount);
			JS_SetConstructorBit(this.NativeInstance, ctor, true);
			return ctor;
		}

		/// <summary>
		/// Creates a new JavaScript constructor function in this context.
		/// </summary>
		/// <param name="function">A delegate to the function that is to be exposed to JavaScript.</param>
		/// <param name="argCount">The number of arguments the function expects to receive.</param>
		/// <param name="magic">A magic value.</param>
		/// <param name="data">An array containing data to be used by the method.</param>
		/// <returns>A value containing the new function.</returns>
		public JSValue CreateConstructorRaw(JSCFunctionData function, int argCount, int magic, JSValue[] data)
		{
			JSValue ctor = CreateFunctionRaw(function, argCount, magic, data);
			JS_SetConstructorBit(this.NativeInstance, ctor, true);
			return ctor;
		}

		/// <summary>
		/// Creates a new JavaScript function in this context.
		/// </summary>
		/// <param name="function">A delegate to the function that is to be exposed to JavaScript.</param>
		/// <param name="argCount">The number of arguments the function expects to receive.</param>
		/// <param name="magic">A magic value.</param>
		/// <param name="data">An array containing data to be used by the method.</param>
		/// <returns>A value containing the new function.</returns>
		public JSValue CreateConstructorRaw(JSCFunctionDataDelegate function, int argCount, int magic, JSValue[] data)
		{
			JSValue ctor = CreateFunctionRaw(function, argCount, magic, data);
			JS_SetConstructorBit(this.NativeInstance, ctor, true);
			return ctor;
		}

		/// <summary>
		/// Creates a native constructor function and assigns it as a property to the global object.
		/// </summary>
		/// <param name="name">The name property of the new function object.</param>
		/// <param name="function">A delegate to the function that is to be exposed to JavaScript.</param>
		/// <param name="argCount">The number of arguments the function expects to receive.</param>
		/// <param name="flags">A bitwise combination of the <see cref="JSPropertyFlags"/>.</param>
		/// <returns>true if the property has been defined or redefined; otherwise false.</returns>
		public unsafe bool DefineConstructor(string name, JSCFunction function, int argCount, JSPropertyFlags flags)
		{
			fixed (byte* fnName = Utils.StringToManagedUTF8(name))
			{
				int rv = -1;
				JSValue globObj = JS_GetGlobalObject(this.NativeInstance);
				try
				{
					JSValue ctor = CreateFunctionRawInternal(fnName, function, argCount);
					JS_SetConstructorBit(this.NativeInstance, ctor, true);
					rv = JS_DefinePropertyValueStr(this.NativeInstance, globObj, fnName, ctor, flags & JSPropertyFlags.CWE);
					if (rv == -1)
						this.NativeInstance.ThrowPendingException();
				}
				finally
				{
					JS_FreeValue(this.NativeInstance, globObj);
				}
				return rv == 1;
			}
		}

		/// <summary>
		/// Creates a native constructor function and assigns it as a property to the global object.
		/// </summary>
		/// <param name="name">The name of the property to be defined or modified.</param>
		/// <param name="func">The function associated with the property.</param>
		/// <param name="argCount">The number of arguments the function expects to receive.</param>
		/// <param name="magic">A magic value.</param>
		/// <param name="data">An array containing data to be used by the method.</param>
		/// <param name="flags">A bitwise combination of the <see cref="JSPropertyFlags"/>.</param>
		/// <returns>true if the property has been defined or redefined; otherwise false.</returns>
		public unsafe bool DefineConstructor(string name, JSCFunctionData func, int argCount, int magic, JSValue[] data, JSPropertyFlags flags)
		{
			fixed (byte* aName = Utils.StringToManagedUTF8(name))
			{
				int rv = -1;
				JSValue globObj = JS_GetGlobalObject(this.NativeInstance);
				try
				{
					JSValue ctor = CreateFunctionRaw(func, argCount, magic, data);
					JS_SetConstructorBit(this.NativeInstance, ctor, true);
					rv = JS_DefinePropertyValueStr(this.NativeInstance, globObj, aName, ctor, flags & JSPropertyFlags.CWE);
					if (rv == -1)
						this.NativeInstance.ThrowPendingException();
				}
				finally
				{
					JS_FreeValue(this.NativeInstance, globObj);
				}
				return rv == 1;
			}
		}

		/// <summary>
		/// Creates a native constructor function and assigns it as a property tothe global object.
		/// </summary>
		/// <param name="name">The name of the property to be defined or modified.</param>
		/// <param name="func">The function associated with the property.</param>
		/// <param name="argCount">The number of arguments the function expects to receive.</param>
		/// <param name="magic">A magic value.</param>
		/// <param name="data">An array containing data to be used by the method.</param>
		/// <param name="flags">A bitwise combination of the <see cref="JSPropertyFlags"/>.</param>
		/// <returns>true if the property has been defined or redefined; otherwise false.</returns>
		public unsafe bool DefineConstructor(string name, JSCFunctionDataDelegate func, int argCount, int magic, JSValue[] data, JSPropertyFlags flags)
		{
			fixed (byte* aName = Utils.StringToManagedUTF8(name))
			{
				int rv = -1;
				JSValue globObj = JS_GetGlobalObject(this.NativeInstance);
				try
				{
					JSValue ctor = CreateFunctionRaw(func, argCount, magic, data);
					JS_SetConstructorBit(this.NativeInstance, ctor, true);
					rv = JS_DefinePropertyValueStr(this.NativeInstance, globObj, aName, ctor, flags & JSPropertyFlags.CWE);
					if (rv == -1)
						this.NativeInstance.ThrowPendingException();
				}
				finally
				{
					JS_FreeValue(this.NativeInstance, globObj);
				}
				return rv == 1;
			}
		}

		/// <summary>
		/// Evaluates a script or module source code from the specified file.
		/// </summary>
		/// <param name="path">The path to the file that contains source code.</param>
		/// <param name="encoding">The encoding applied to the contents of the file.</param>
		/// <param name="flags">A bitwise combination of the <see cref="JSEvalFlags"/>.</param>
		/// <returns>A value of the final expression as a CLR type.</returns>
		public object EvalFile(string path, Encoding encoding, JSEvalFlags flags)
		{
			if (encoding is UTF8Encoding || encoding is ASCIIEncoding)
			{
				byte[] buffer;
				using (FileStream fs = File.OpenRead(path))
				{
					buffer = new byte[fs.Length + 1];
					fs.Read(buffer, 0, (int)fs.Length);
					buffer[buffer.Length - 1] = 0;
				}
				return EvalInternal(buffer, path, flags);
			}
			else
			{
				return ConvertJSValueToClrObject(EvalInternal(Utils.StringToManagedUTF8(File.ReadAllText(path, encoding)), path, flags), true);
			}
		}

		/// <summary>
		/// Evaluates a script or module source code from the specified file.
		/// </summary>
		/// <param name="path">The path to the file that contains source code.</param>
		/// <param name="encoding">The encoding applied to the contents of the file.</param>
		/// <param name="flags">A bitwise combination of the <see cref="JSEvalFlags"/>.</param>
		/// <returns>A value of the final expression as a CLR type.</returns>
		public object EvalFile(string path, Encoding encoding, QuickJSValue thisObj, JSEvalFlags flags)
		{
			if (thisObj is null)
				throw new ArgumentNullException(nameof(thisObj));

			try
			{
				if (encoding is UTF8Encoding || encoding is ASCIIEncoding)
				{
					byte[] buffer;
					using (FileStream fs = File.OpenRead(path))
					{
						buffer = new byte[fs.Length + 1];
						fs.Read(buffer, 0, (int)fs.Length);
						buffer[buffer.Length - 1] = 0;
					}
					return EvalThisInternal(buffer, path, thisObj.NativeInstance, flags);
				}
				else
				{
					return ConvertJSValueToClrObject(EvalThisInternal(Utils.StringToManagedUTF8(File.ReadAllText(path, encoding)), path, thisObj.NativeInstance, flags), true);
				}
			}
			finally
			{
				GC.KeepAlive(thisObj);
			}
		}

		/// <summary>
		/// Evaluates JavaScript code represented as a string.
		/// </summary>
		/// <param name="code">The JavaScript code.</param>
		/// <param name="filename">The path or URI to file where the script in question can be found, if any.</param>
		/// <param name="flags">A bitwise combination of the <see cref="JSEvalFlags"/>.</param>
		/// <returns>A value of the final expression as a CLR type.</returns>
		public object Eval(string code, string filename, JSEvalFlags flags)
		{
			if (code == null)
				throw new ArgumentNullException(nameof(code));

			return ConvertJSValueToClrObject(EvalInternal(Utils.StringToManagedUTF8(code), filename, flags), true);
		}

		/// <summary>
		/// Evaluates JavaScript code represented as a string.
		/// </summary>
		/// <param name="code">The JavaScript code.</param>
		/// <param name="filename">The path or URI to file where the script in question can be found, if any.</param>
		/// <param name="thisArg">The object to use as &apos;this&apos;.</param>
		/// <param name="flags">A bitwise combination of the <see cref="JSEvalFlags"/>.</param>
		/// <returns>A value of the final expression as a CLR type.</returns>
		public object Eval(string code, string filename, QuickJSValue thisArg, JSEvalFlags flags)
		{
			if (code == null)
				throw new ArgumentNullException(nameof(code));

			if (thisArg is null)
				throw new ArgumentNullException(nameof(thisArg));

			if (thisArg.Context != this)
				throw new ArgumentOutOfRangeException(nameof(thisArg));

			JSValue rv = EvalThisInternal(Utils.StringToManagedUTF8(code), filename, thisArg.NativeInstance, flags);
			GC.KeepAlive(thisArg);
			return ConvertJSValueToClrObject(rv, true);
		}

		/// <summary>
		/// Evaluates JavaScript code represented as a string.
		/// </summary>
		/// <param name="code">The JavaScript code.</param>
		/// <param name="filename">The path or URI to file where the script in question can be found, if any.</param>
		/// <param name="thisVal">The value to use as &apos;this&apos;.</param>
		/// <param name="flags">A bitwise combination of the <see cref="JSEvalFlags"/>.</param>
		/// <returns>A value of the final expression as a CLR type.</returns>
		public object Eval(string code, string filename, JSValue thisVal, JSEvalFlags flags)
		{
			if (code == null)
				throw new ArgumentNullException(nameof(code));

			JSValue thisArg = JS_DupValue(this.NativeInstance, thisVal);
			try
			{
				return ConvertJSValueToClrObject(EvalThisInternal(Utils.StringToManagedUTF8(code), filename, thisArg, flags), true);
			}
			finally
			{
				JS_FreeValue(this.NativeInstance, thisArg);
			}
			
		}


		internal unsafe JSValue EvalInternal(byte[] input, string filename, JSEvalFlags flags)
		{
			_clrException = null;

			JSEvalFlags evalType = flags & JSEvalFlags.TypeMask;
			if (evalType != JSEvalFlags.Global && evalType != JSEvalFlags.Module)
				throw new ArgumentOutOfRangeException(nameof(flags));

			if (input.Length < 1 || input[input.Length - 1] != 0)
				throw new ArgumentOutOfRangeException(nameof(input));

			if (filename is null)
				filename = "<anonymous>";

			JSValue val;
			byte[] file = Utils.StringToManagedUTF8(filename);
			fixed (byte* buffer = input)
			fixed (byte* pfile = file)
			{
				val = JS_Eval(this.NativeInstance, buffer, input.Length - 1, pfile, flags);
			}

			if (JS_IsException(val))
			{
				ThrowPendingException();
			}
			return val;
		}

		internal unsafe JSValue EvalThisInternal(byte[] input, string filename, JSValue thisVal, JSEvalFlags flags)
		{
			_clrException = null;

			JSEvalFlags evalType = flags & JSEvalFlags.TypeMask;
			if (evalType != JSEvalFlags.Global && evalType != JSEvalFlags.Module)
				throw new ArgumentOutOfRangeException(nameof(flags));

			if (input.Length < 1 || input[input.Length - 1] != 0)
				throw new ArgumentOutOfRangeException(nameof(input));

			if (filename is null)
				filename = "<anonymous>";

			JSValue val;
			byte[] file = Utils.StringToManagedUTF8(filename);
			fixed (byte* buffer = input)
			fixed (byte* pfile = file)
			{
				val = JS_EvalThis(this.NativeInstance, thisVal, buffer, input.Length - 1, pfile, flags);
			}

			if (JS_IsException(val))
			{
				ThrowPendingException();
			}
			return val;
		}

		/// <summary>
		/// Converts the value of the specified <see cref="JSValue"/> to a value of a .NET type.
		/// </summary>
		/// <param name="value">The <see cref="JSValue"/> to convert.</param>
		/// <param name="freeValue">
		/// A value indicating that the <see cref="JSValue"/> should be freed.
		/// </param>
		/// <returns>
		/// An <see cref="Object"/> whose value is equivalent to <paramref name="value"/>.
		/// </returns>
		/// <exception cref="InvalidCastException">
		/// This conversion is not supported.
		/// </exception>
		protected internal virtual object ConvertJSValueToClrObject(JSValue value, bool freeValue)
		{
			switch (value.Tag)
			{
				case JSTag.Bool:
					return value.ToBoolean();
				case JSTag.Null:
					return null;
				case JSTag.String:
					string s = value.ToString(this.NativeInstance);
					if (freeValue)
						JS_FreeValue(this.NativeInstance, value);
					return s;
				case JSTag.Int:
					return value.ToInt32();
				case JSTag.Float64:
					return value.ToDouble();
				case JSTag.Undefined:
					return QuickJSValue.Undefined;
				default:
					if (value.Tag <= JSTag.Object)
						return QuickJSValue.Wrap(this, value);
					if (freeValue)
						JS_FreeValue(this.NativeInstance, value);
					throw new InvalidCastException();
			}
		}

		/// <summary>
		/// Converts the specified <paramref name="value"/> to a <see cref="JSValue"/>.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>
		/// A new <see cref="JSValue"/> whose value is equivalent to <paramref name="value"/>.
		/// </returns>
		/// <exception cref="InvalidCastException">
		/// This conversion is not supported.
		/// </exception>
		protected internal virtual JSValue ConvertClrObjectToJSValue(object value)
		{
			if (value is null)
				return JSValue.Null;
			if (QuickJSValue.IsUndefined(value))
				return JSValue.Undefined;
			if (value.GetType().IsPrimitive)
			{
				if (value is bool bVal)
					return JSValue.Create(bVal);
				if (value is double f64)
					return JSValue.Create(f64);
				if (value is float f32)
					return JSValue.Create(f32);
				if (value is int i32)
					return JSValue.Create(i32);
				if (value is long i64)
					return JSValue.Create(i64);
				if (value is byte i8)
					return JSValue.Create(i8);
				if (value is short i16)
					return JSValue.Create(i16);
				if (value is string s)
					return JSValue.Create(this.NativeInstance, s);
				if (value is uint u32)
					return JSValue.Create(u32);
				if (value is ulong u64)
					return JSValue.Create(u64);
				if (value is uint u16)
					return JSValue.Create(u16);
				if (value is uint u8)
					return JSValue.Create(u8);
			}
			throw new NotImplementedException();
		}

		internal void SetClrException(Exception exception)
		{
			_clrException = exception;
		}

		/// <summary>
		/// Throws the actual exception that is stored in the <see cref="QuickJSContext"/>.
		/// </summary>
		/// <remarks>
		/// If there is no pending exception in the context, the method returns
		/// without creating or throwing an exception.
		/// </remarks>
		public void ThrowPendingException()
		{
			this.NativeInstance.ThrowPendingException(Interlocked.Exchange(ref _clrException, null));
		}

		/// <summary>
		/// Sets the class prototype object.
		/// </summary>
		/// <param name="id">A JavaScript class identifier.</param>
		/// <param name="proto">A prototype object or <see cref="JSValue.Null"/>.</param>
		public void SetClassPrototype(JSClassID id, JSValue proto)
		{
			JSTag tag = proto.Tag;
			if (tag != JSTag.Object && tag != JSTag.Null)
				throw new ArgumentOutOfRangeException(nameof(proto));
			if (!Runtime.IsRegisteredClass(id))
				throw new ArgumentOutOfRangeException(nameof(id));
			JS_SetClassProto(this.NativeInstance, id, proto);
		}

		/// <summary>
		/// Sets the class prototype object.
		/// </summary>
		/// <param name="id">A JavaScript class identifier.</param>
		/// <param name="proto">A prototype object or null.</param>
		/// <param name="disposeProto">true if <paramref name="proto"/> should be disposed of.</param>
		public void SetClassPrototype(JSClassID id, QuickJSValue proto, bool disposeProto)
		{
			if (!Runtime.IsRegisteredClass(id))
				throw new ArgumentOutOfRangeException(nameof(id));
			if (proto is null)
			{
				JS_SetClassProto(this.NativeInstance, id, JSValue.Null);
			}
			else
			{
				JS_SetClassProto(this.NativeInstance, id, proto.GetNativeInstance());
				if (disposeProto)
					proto.Dispose();
			}
		}

		/// <summary>
		/// Get the builtin class prototype object.
		/// </summary>
		/// <param name="id">A JavaScript class identifier.</param>
		public QuickJSValue GetClassPrototype(JSClassID id)
		{
			if (!Runtime.IsRegisteredClass(id))
				throw new ArgumentOutOfRangeException(nameof(id));
			JSValue proto = JS_GetClassProto(this.NativeInstance, id);
			return proto.Tag == JSTag.Object ? QuickJSValue.Wrap(this, proto) : null;
		}

	}
}
