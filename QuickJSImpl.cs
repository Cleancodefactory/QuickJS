using System;
using System.Text;
using QuickJS;
using QuickJS.Native;
using Ccf.Ck.SysPlugins.Interfaces;
using Ccf.Ck.SysPlugins.Data.Base;


namespace Ccf.Ck.SysPlugins.QuickJS {
    public class QuickJSImpl : DataLoaderBase<QuickJSScopeContext> {
        protected override void ExecuteRead(IDataLoaderReadContext execContext) {
            throw new NotImplementedException();
        }

        protected override void ExecuteWrite(IDataLoaderWriteContext execContext) {
            throw new NotImplementedException();

        }
    }
}