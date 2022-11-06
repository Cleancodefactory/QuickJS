using System;
using System.Collections.Generic;
using System.Text;
using QuickJS;
using QuickJS.Native;
using Ccf.Ck.SysPlugins.Interfaces;
using Ccf.Ck.SysPlugins.Data.Base;


namespace Ccf.Ck.SysPlugins.QuickJS {
    public class QuickJSImp : DataLoaderBase<QuickJSScopeContext> {
        protected override void ExecuteRead(IDataLoaderReadContext execContext) {
            var scope = execContext.OwnContextScoped as QuickJSScopeContext;
            if (scope != null) {
                JSHost host = scope.Host(); // This will invoke loading of the JS engine and loading the file(s) when called for the first time.
                if (host != null) {
                    object result = host.CallGlobal("main",2);
                    execContext.Results.Add(new Dictionary<string,object>() {{"result", result}});
                    return;
                }
            }
            throw new NullReferenceException("The scope context or host of QuickJS is not available. Check if it is loaded correctly.");
        }

        protected override void ExecuteWrite(IDataLoaderWriteContext execContext) {
            // TODO lets see if it works and then ...
            throw new NotImplementedException();

        }
    }
}