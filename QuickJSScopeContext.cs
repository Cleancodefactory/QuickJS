using Ccf.Ck.SysPlugins.Data.Base;
using Ccf.Ck.SysPlugins.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickJS;
using QuickJS.Native;

namespace Ccf.Ck.SysPlugins.QuickJS {
    public class QuickJSScopeContext : DataLoaderScopedContextBase, IPluginsSynchronizeContextScopedEx, ITransactionScope {
        public void CommitTransaction() {
            throw new NotImplementedException();
        }

        public void RollbackTransaction() {
            throw new NotImplementedException();
        }

        public object StartTransaction() {
            throw new NotImplementedException();
        }
    }
}
