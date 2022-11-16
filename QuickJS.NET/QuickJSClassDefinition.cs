using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using QuickJS.Native;
using static QuickJS.Native.QuickJSNativeApi;

namespace QuickJS
{
	/// <summary>
	/// Describes a class of JavaScript objects.
	/// </summary>
	public class QuickJSClassDefinition
	{
		private readonly Delegate _callImpl;
		private readonly JSClassGCMark _markImpl;
		private readonly JSClassFinalizer _finalizerImpl;

		/// <summary>
		/// Initializes a new instance of the <see cref="QuickJSClassDefinition"/> class.
		/// </summary>
		/// <param name="id">The class ID.</param>
		/// <param name="call">
		/// A function callback if the object of this class is a function. If objects of a class
		/// shouldn&apos;t be callable, use NULL. Most objects are not callable.
		/// </param>
		/// <param name="gcMark">
		/// The QuickJS JavaScript engine calls this callback during the mark phase of
		/// garbage collection.
		/// </param>
		/// <param name="finalizer">
		/// An object finalizer callback. This callback invoked when
		/// an object is finalized (prepared for garbage collection).
		/// </param>
		public unsafe QuickJSClassDefinition(JSClassID id, JSClassCall call, JSClassGCMark gcMark, JSClassFinalizer finalizer)
		{
			if ((id.ToInt32() & 0xFFFF0000) != 0) // JSObject.class_id is 16 bit unsigned integer.
				throw new ArgumentOutOfRangeException(nameof(id));

			JS_NewClassID(ref id);
			this.ID = id;
			if (call != null)
			{
				_callImpl = sizeof(JSValue) == 8 ? (Delegate)new JSClassCall32(CallImpl8) : new JSClassCall(CallImpl16);
				this.Call = call;
			}
			if (gcMark != null)
			{
				_markImpl = GCMarkImpl;
				this.Mark = gcMark;
			}
			if (finalizer != null)
			{
				_finalizerImpl = FinalizerImpl;
				this.Finalizer = finalizer;
			}
		}

		/// <summary>
		/// Gets the class ID.
		/// </summary>
		public JSClassID ID { get; }

		/// <summary>
		/// If it is not null, the object is a function. If has the
		/// <see cref="JSCallFlags.Constructor"/> flag, the function is called
		/// as a constructor. In this case, &apos;this_val&apos; is new.target.
		/// A constructor call only happens if the object constructor bit is set
		/// (see <see cref="JS_SetConstructorBit"/>).
		/// </summary>
		public JSClassCall Call { get; }

		/// <summary>
		/// Gets the object GC mark callback.
		/// </summary>
		public JSClassGCMark Mark { get; }

		/// <summary>
		/// Gets the object finalizer callback.
		/// </summary>
		public JSClassFinalizer Finalizer { get; }

		protected private virtual void CopyToClassDefImpl(ref JSClassDef classDef)
		{
			classDef.call = _callImpl is null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(_callImpl);
			classDef.gc_mark = _markImpl;
			classDef.finalizer = _finalizerImpl;
		}

		internal void CopyToClassDef(ref JSClassDef classDef)
		{
			CopyToClassDefImpl(ref classDef);
		}

		private unsafe JSValue CallImpl16(JSContext ctx, JSValue func_obj, JSValue this_val, int argc, JSValue[] argv, JSCallFlags flags)
		{
			try
			{
				return Call(ctx, func_obj, this_val, argc, argv, flags);
			}
			catch (OutOfMemoryException)
			{
				return JS_ThrowOutOfMemory(ctx);
			}
			catch (Exception ex)
			{
				return Utils.ReportException(ctx, ex);
			}
		}

		private unsafe ulong CallImpl8(JSContext ctx, JSValue func_obj, JSValue this_val, int argc, JSValue[] argv, JSCallFlags flags)
		{
			try
			{
				return Call(ctx, func_obj, this_val, argc, argv, flags).uint64;
			}
			catch (OutOfMemoryException)
			{
				return JS_ThrowOutOfMemory(ctx).uint64;
			}
			catch (Exception ex)
			{
				return Utils.ReportException(ctx, ex).uint64;
			}
		}

		private void GCMarkImpl(JSRuntime rt, JSValue val, JS_MarkFunc mark_func)
		{
			try { Mark(rt, val, mark_func); } catch { }
		}

		private void FinalizerImpl(JSRuntime rt, JSValue val)
		{
			try { Finalizer(rt, val); } catch { }
		}

		/// <summary>
		/// Creates a new class ID.
		/// </summary>
		/// <returns>A new class ID.</returns>
		[MethodImpl(AggressiveInlining)]
		public static JSClassID CreateClassID()
		{
			var cid = new JSClassID();
			return JS_NewClassID(ref cid);
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			return ID.ToInt32();
		}

	}
}
