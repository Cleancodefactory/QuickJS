﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using QuickJS.Native;
using static QuickJS.Native.QuickJSNativeApi;

namespace QuickJS
{
	/// <summary>
	/// Represents a JavaScript runtime corresponding to an object heap.
	/// Several runtimes can exist at the same time but they cannot exchange
	/// objects. Inside a given runtime, no multi-threading is supported.
	/// </summary>
	public class QuickJSRuntime : IDisposable
	{
		public const int DefaultStackSize = (256 * 1024);
		public const int DefaultGCThreshold = (256 * 1024);
		public const int DefaultMemoryLimit = -1;

		private static readonly JSInterruptHandler _HandleInterrupt = HandleInterrupt;

		private readonly JSRuntime _runtime;
		private readonly GCHandle _handle;
		private readonly Thread _thread;
		private readonly List<QuickJSClassDefinition> _classes;

		/// <summary>
		/// Occurs periodically while JavaScript code runs.
		/// </summary>
		public event EventHandler<HandledEventArgs> Interrupt;

		/// <summary>
		/// Initializes a new instance of the <see cref="QuickJSRuntime"/>.
		/// </summary>
		public QuickJSRuntime()
			: this(DefaultMemoryLimit, DefaultGCThreshold, DefaultStackSize)
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QuickJSRuntime"/>.
		/// </summary>
		/// <param name="stackSize">The maximum system stack size.</param>
		public QuickJSRuntime(int stackSize)
			: this(DefaultMemoryLimit, DefaultGCThreshold, stackSize)
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QuickJSRuntime"/>.
		/// </summary>
		/// <param name="memoryLimit">The memory allocation limit of the JavaScript runtime (in bytes).</param>
		/// <param name="gcThreshold">The GC threshold (in bytes). Use -1 to disable automatic GC.</param>
		/// <param name="stackSize">The maximum system stack size.</param>
		public QuickJSRuntime(int memoryLimit, int gcThreshold, int stackSize)
		{
			_thread = Thread.CurrentThread;
			_runtime = JS_NewRuntime();
			_handle = GCHandle.Alloc(this, GCHandleType.Normal);
			_classes = new List<QuickJSClassDefinition>();

			IntPtr opaque = GCHandle.ToIntPtr(_handle);
			JS_SetRuntimeOpaque(_runtime, opaque);
			JS_SetInterruptHandler(_runtime, _HandleInterrupt, opaque);

			if (memoryLimit > 0)
				JS_SetMemoryLimit(_runtime, memoryLimit);
			if (gcThreshold != DefaultGCThreshold)
				JS_SetGCThreshold(_runtime, gcThreshold);
			if (stackSize != DefaultStackSize)
				JS_SetMaxStackSize(_runtime, stackSize);
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
			JS_FreeRuntime(_runtime);
			_handle.Free();
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Gets a pointer to a native JSRuntime.
		/// </summary>
		public JSRuntime NativeInstance
		{
			get
			{
				if (!_handle.IsAllocated)
					throw new ObjectDisposedException(this.GetType().Name);
				VerifyAccess();
				return _runtime;
			}
		}

		/// <summary>
		/// Determines whether the calling thread is the thread associated with
		/// this runtime.
		/// </summary>
		/// <returns>
		/// true if the calling thread is the thread associated with this
		/// runtime; otherwise, false.
		/// </returns>
		public bool CheckAccess()
		{
			return Thread.CurrentThread == _thread;
		}

		/// <summary>
		/// Determines whether the calling thread has access to this runtime.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// The calling thread does not have access to this Dispatcher.
		/// </exception>
		public void VerifyAccess()
		{
			if (!CheckAccess())
				throw new InvalidOperationException("Cross-thread operation not valid.");
		}

		/// <summary>
		/// Register a new JS class.
		/// </summary>
		/// <param name="className">The name of the class.</param>
		/// <param name="classDefinition">
		/// The descriptor for the JS class being registered.
		/// </param>
		/// <returns>The class ID of the registered class.</returns>
		public unsafe JSClassID RegisterNewClass(string className, QuickJSClassDefinition classDefinition)
		{
			if (className is null)
				throw new ArgumentNullException(nameof(className));
			if (classDefinition is null)
				throw new ArgumentNullException(nameof(classDefinition));
			if (_classes.Contains(classDefinition))
				throw new ArgumentOutOfRangeException(nameof(classDefinition), "Already registered class.");

			var classDef = new JSClassDef();
			classDefinition.CopyToClassDef(ref classDef);
			fixed (byte* name = Utils.StringToManagedUTF8(className))
			{
				classDef.class_name = new IntPtr(name);
				if (0 != JS_NewClass(_runtime, classDefinition.ID, classDef))
					throw new QuickJSRuntimeException("Cannot create a new object internal class.");
			}
			_classes.Add(classDefinition);
			return classDefinition.ID;
		}

		/// <summary>
		/// Creates a new <see cref="QuickJSContext"/> associated with this runtime.
		/// </summary>
		/// <returns>A new <see cref="QuickJSContext"/>.</returns>
		public virtual QuickJSContext CreateContext()
		{
			return new QuickJSContext(this, false);
		}

		/// <summary>
		/// Creates a new raw <see cref="QuickJSContext"/> associated with this runtime.
		/// </summary>
		/// <returns>A new <see cref="QuickJSContext"/>.</returns>
		public virtual QuickJSContext CreateRawContext()
		{
			return new QuickJSContext(this, true);
		}

		/// <summary>
		/// Set default loader for ES6 modules.
		/// </summary>
		public void SetDefaultModuleLoader()
		{
			IntPtr moduleHandle, fn_js_module_loader;
			const string fnModuleLoaderName = "js_module_loader";
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				moduleHandle = NativeMethods.GetModuleHandle("quickjs");
				if (moduleHandle == IntPtr.Zero)
					throw new InvalidOperationException();
				fn_js_module_loader = NativeMethods.GetProcAddress(moduleHandle, fnModuleLoaderName);
			}
			else
			{
				const int RTLD_NOW = 2;
				moduleHandle = NativeMethods.dlopen("libquickjs.so", RTLD_NOW);
				if (moduleHandle == IntPtr.Zero)
					throw new InvalidOperationException();
				fn_js_module_loader = NativeMethods.GetProcAddress(moduleHandle, fnModuleLoaderName);
				NativeMethods.dlclose(moduleHandle);
			}

			if (fn_js_module_loader == IntPtr.Zero)
				throw new EntryPointNotFoundException("Unable to find an entry point name '" + fnModuleLoaderName + "'.");
			JS_SetModuleLoaderFunc(this.NativeInstance, IntPtr.Zero, fn_js_module_loader, IntPtr.Zero);
		}

		/// <summary>
		/// Executes the pending jobs.
		/// </summary>
		/// <param name="onlyOnce">Only one job should be executed.</param>
		/// <returns>true</returns>
		/// <exception cref="QuickJSException">
		/// An unhandled JavaScript exception was thrown.
		/// </exception>
		public bool ExecutePendingJob(bool onlyOnce)
		{
			do
			{
				int error = JS_ExecutePendingJob(this.NativeInstance, out JSContext ctx);
				if (error > 0)
					continue;

				if (error == 0)
					break;

				ctx.ThrowPendingException();
			} while (!onlyOnce);

			return JS_IsJobPending(this.NativeInstance);
		}

		/// <summary>
		/// Initializes handlers of the &apos;std&apos; module.
		/// </summary>
		public void StdInitHandlers()
		{
			js_std_init_handlers(this.NativeInstance);
		}

		/// <summary>
		/// Frees handlers of the &apos;std&apos; module.
		/// </summary>
		/// <remarks>Should be called just before freeing the <see cref="QuickJSRuntime"/>.</remarks>
		public void StdFreeHandlers()
		{
			js_std_free_handlers(this.NativeInstance);
		}

		/// <summary>
		/// Begins running a standard main loop which calls the user JS callbacks.
		/// </summary>
		/// <param name="context">The main context.</param>
		public void RunStdLoop(QuickJSContext context)
		{
			if (context is null)
				throw new ArgumentNullException(nameof(context));
			JSRuntime runtime = JS_GetRuntime(context.NativeInstance);
			if (runtime != _runtime)
				throw new ArgumentOutOfRangeException(nameof(context));
			js_std_loop(context.NativeInstance);
		}

		/// <summary>
		/// Determines whether a class with the specified ID is available in the given JavaScript runtime.
		/// </summary>
		/// <param name="id">The class ID.</param>
		/// <returns>true if a class with the specified ID is registered; otherwise, false.</returns>
		public bool IsRegisteredClass(JSClassID id)
		{
			return JS_IsRegisteredClass(this.NativeInstance, id);
		}

		/// <summary>
		/// Forces an immediate garbage collection.
		/// </summary>
		public void Collect()
		{
			JS_RunGC(this.NativeInstance);
		}

		/// <summary>
		/// Raises the <see cref="Interrupt"/> event.
		/// </summary>
		/// <param name="e">A <see cref="HandledEventArgs"/> that contains the event data.</param>
		protected virtual void OnInterrupt(HandledEventArgs e)
		{
			Interrupt?.Invoke(this, e);
		}

		private static int HandleInterrupt(JSRuntime rt, IntPtr opaque)
		{
			var e = new HandledEventArgs();
			((QuickJSRuntime)GCHandle.FromIntPtr(opaque).Target).OnInterrupt(e);
			return e.Handled ? 1 : 0;
		}

	}
}
