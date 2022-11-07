using QuickJS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ccf.Ck.SysPlugins.QuickJS {
    public class QuickJSContainer {
        public static QuickJSContainer Instance { get; private set; }
        private object lockInstance = new object();
        private QuickJSContainer() { }
        static QuickJSContainer() {
            Instance = new QuickJSContainer();
        }

        private Dictionary<string, JSHost> JSHosts = new Dictionary<string, JSHost>();
        public JSHost this[string key] {
            get {
                if (JSHosts.ContainsKey(key)) return JSHosts[key];
                return null;
            }
        }
        /// <summary>
        /// Creates a runtime and a context then loads the file as a module
        /// </summary>
        /// <param name="key"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public JSHost Create(string key, string file) {
            lock (lockInstance) {
                if (JSHosts.ContainsKey(key)) return null;
                JSHost host = new JSHost();
                if (host.InitContext(file)) {
                    JSHosts[key] = host; // Put it in the collection
                    return host;
                } else {
                    throw new QuickJSException(host.LastError);
                }
            }
        }
        public JSHost GetOrCreate(string key, string file) {
            if (JSHosts.ContainsKey(key)) return JSHosts[key];
            return Create(key, file);
        }
        
    }
}
