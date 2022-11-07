using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1.X509.Qualified;
using QuickJS;
using QuickJS.Native;


namespace Ccf.Ck.SysPlugins.QuickJS {
    public class JSHost : IDisposable {
        private bool disposedValue;
        private QuickJSRuntime _runtime = null;
        private QuickJSContext _context = null;

        public bool InitContext(string file) {
            try {
                _runtime = new QuickJSRuntime();
                _runtime.StdInitHandlers();
                _context = _runtime.CreateContext();
                _context.StdAddHelpers();

                (_context.EvalFile(file, Encoding.ASCII , JSEvalFlags.Module | JSEvalFlags.Strip) as IDisposable)?.Dispose();
                _runtime.RunStdLoop(_context);
                return true;
            } catch (Exception e) {
                _runtime = null;
                return false;
            }
        }

        private object CallGlobalLow(string fname, params JSValue[] args) {
            QuickJSValue glob = _context.GetGlobal();
            if (glob == null) return JSValue.Null;
            QuickJSValue func = (QuickJSValue)glob.GetProperty(fname) ;
            object result = func.Call(glob, args);
            return result;
            
            // QuickJSNativeApi.JS_Call()
            
        }
        public object CallGlobal(string fname, params object[] args) {
            var jargs = args.Select(arg => {
                if (arg == null) return JSValue.Null;
                return arg switch {
                    string => JSValue.Create(_context.NativeInstance, arg as string),
                    int => JSValue.Create((int)arg),
                    double => JSValue.Create((double)arg),
                    uint => JSValue.Create((uint)arg),
                    bool => JSValue.Create((bool)arg),
                    long => JSValue.Create((long)arg),
                    _ => JSValue.Null

                };
                
            }).ToArray();
            return CallGlobalLow(fname, jargs);
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~JSHost()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
