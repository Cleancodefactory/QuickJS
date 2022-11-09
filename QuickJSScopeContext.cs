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

        public const string CONF_FILE = "file";
        public const string CONF_MODE = "mode";
        public const string CONF_PROPNAME = "propertyname";
        public const string PROPNAME_DEFAULT = "data";

        public KraftGlobalConfigurationSettings KraftGlobalConfigurationSettings => ProcessingContext.InputModel.KraftGlobalConfigurationSettings;
        private IProcessingContext ProcessingContext { get; set; }



        public JSHost Host() {
            if (CustomSettings.ContainsKey(CONF_FILE) && CustomSettings[CONF_FILE] != null) {
                var key = ModuleName + PluginName;
                string moduleRoot = System.IO.Path.Combine(KraftGlobalConfigurationSettings.GeneralSettings.ModulesRootFolder(ProcessingContext.InputModel.Module), ProcessingContext.InputModel.Module);
                var file = CustomSettings[CONF_FILE].Replace("@moduleroot@", moduleRoot);
                return QuickJSContainer.Instance.GetOrCreate(key, file);
            } else {
                return null;
            }
        }

        #region Handy config access
        public ENodeMode Mode { 
            get {
                var mode = ENodeMode.SingleValue;
                if (CustomSettings.ContainsKey(CONF_MODE)) {
                    if (Enum.TryParse(CustomSettings[CONF_MODE], true,out ENodeMode parsedMode)) {
                        mode = parsedMode;
                    }
                }
                return mode;
            }
        }
        public string PropertyName {
            get {
                if (CustomSettings.ContainsKey(CONF_PROPNAME)) {
                    return CustomSettings[CONF_PROPNAME];
                }
                return PROPNAME_DEFAULT;
            }
        }
        #endregion

        #region Transaction (not used)
        public void CommitTransaction() {
            
        }

        public void RollbackTransaction() {
            
        }

        public object StartTransaction() {
            return null!;
        }
        #endregion

        public void InspectBasket(IContextualBasket basket) {
            var pc = basket.PickBasketItem<IProcessingContext>();
            ProcessingContext = pc;
        }
    }
}
