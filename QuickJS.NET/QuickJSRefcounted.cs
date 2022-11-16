using System;
using QuickJS.Native;

namespace QuickJS
{
	sealed class QuickJSRefcounted
	{
		public readonly QuickJSContext Context;
		public readonly JSValue NativeValue;

		public QuickJSRefcounted(QuickJSContext context, JSValue value)
		{
			Context = context;
			this.NativeValue = value;
		}

		public override int GetHashCode()
		{
			return NativeValue.ToPointer().GetHashCode();
		}
	}
}
