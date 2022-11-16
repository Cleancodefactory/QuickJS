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
using Microsoft.Extensions.Hosting;

namespace Ccf.Ck.SysPlugins.QuickJS {
    public class QuickJSScopeContext : DataLoaderScopedContextBase, IPluginsSynchronizeContextScopedEx, ITransactionScope, IContextualBasketConsumer {
        public string PluginName { get; set; }
        public string ModuleName { get; set; }

        public const string CONF_FILE = "file";
        public const string CONF_MODE = "mode";
        public const string CONF_PROPNAME = "propertyname";
        public const string CONF_STACKSIZE = "stack";
        public const string CONF_MEMORYSIZE = "memory";
        public const string CONF_GCTHRESHOLD = "gcthreshold";
        public const string PROPNAME_DEFAULT = "data";
        public const string CONF_BASEPATH = "basepath";

        public KraftGlobalConfigurationSettings KraftGlobalConfigurationSettings => ProcessingContext.InputModel.KraftGlobalConfigurationSettings;
        private IProcessingContext ProcessingContext { get; set; }



        public JSHost Host() {
            if (CustomSettings.ContainsKey(CONF_FILE) && CustomSettings[CONF_FILE] != null) {
                var key = ModuleName + PluginName;
                string moduleRoot = System.IO.Path.Combine(KraftGlobalConfigurationSettings.GeneralSettings.ModulesRootFolder(ProcessingContext.InputModel.Module), ProcessingContext.InputModel.Module);
                var file = CustomSettings[CONF_FILE].Replace("@moduleroot@", moduleRoot);
#if STATIC_JS
                return QuickJSContainer.Instance.GetOrCreate(key, file,StackSize,MemorySize,GCThreshold);
#else
                JSHost host = new JSHost(StackSize, MemorySize, GCThreshold);
                host.InitContext(file);
                return host;
#endif
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
        public string BasePath { 
            get {
                string moduleRoot = System.IO.Path.Combine(KraftGlobalConfigurationSettings.GeneralSettings.ModulesRootFolder(ProcessingContext.InputModel.Module), ProcessingContext.InputModel.Module);
                if (CustomSettings.ContainsKey(CONF_BASEPATH)) {
                    return CustomSettings[CONF_BASEPATH].Replace("@moduleroot@", moduleRoot);
                } else {
                    return null;    
                }
            } 
        }
        private int? GetMemSetting(string setting_name, int mult = 1024) {
            if (CustomSettings.ContainsKey(setting_name)) {
                int.TryParse(CustomSettings[setting_name], out int parsed);
                return (parsed > 0)?parsed * mult:null;
            }
            return null;
        }
        /// <summary>
        /// Reads the stack size from the Configuration.json in K bytes
        /// Returns null if missing or incorrect, passing null to JSHost will use the default size
        /// </summary>
        public int? StackSize => GetMemSetting(CONF_STACKSIZE, 1024);
        /// <summary>
        /// Memory size in KBytes
        /// </summary>
        public int? MemorySize => GetMemSetting(CONF_MEMORYSIZE, 1024);
        /// <summary>
        /// GC Threshold in KBytes
        /// </summary>
        public int? GCThreshold => GetMemSetting(CONF_GCTHRESHOLD, 1024);
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
