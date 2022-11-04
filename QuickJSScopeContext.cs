using Ccf.Ck.SysPlugins.Data.Base;
using Ccf.Ck.SysPlugins.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickJS;
using QuickJS.Native;
using Ccf.Ck.Models.ContextBasket;
using Ccf.Ck.Models.Settings;
using Ccf.Ck.SysPlugins.Interfaces.ContextualBasket;

namespace Ccf.Ck.SysPlugins.QuickJS {
    public class QuickJSScopeContext : DataLoaderScopedContextBase, IPluginsSynchronizeContextScopedEx, ITransactionScope, IContextualBasketConsumer {
        public string PluginName { get; set; }
        public string ModuleName { get; set; }

        public KraftGlobalConfigurationSettings KraftGlobalConfigurationSettings => ProcessingContext.InputModel.KraftGlobalConfigurationSettings;
        private IProcessingContext ProcessingContext { get;  set; }
        


        public JSHost Host() {
            if (CustomSettings.ContainsKey("file") && CustomSettings["file"] != null ) {
                var key = ModuleName + PluginName;
                string moduleRoot = System.IO.Path.Combine(KraftGlobalConfigurationSettings.GeneralSettings.ModulesRootFolder(ProcessingContext.InputModel.Module), ProcessingContext.InputModel.Module);
                var file = CustomSettings["file"].Replace("@moduleroot@", moduleRoot);
                return QuickJSContainer.Instance.GetOrCreate(key, file);
            } else {
                return null;
            }
        }


        public void CommitTransaction() {
            
        }

        public void RollbackTransaction() {
            
        }

        public object StartTransaction() {
            return null!;
        }

        public void InspectBasket(IContextualBasket basket) {
            var pc = basket.PickBasketItem<IProcessingContext>();
            ProcessingContext = pc;
        }
    }
}
