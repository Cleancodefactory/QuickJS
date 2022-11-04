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
        public string PluginName { get; set; }
        public string ModuleName { get; set; }



        public void CommitTransaction() {
            
        }

        public void RollbackTransaction() {
            
        }

        public object StartTransaction() {
            return null!;
        }
    }
}
