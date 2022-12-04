using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ccf.Ck.Utilities.Generic;
using Org.BouncyCastle.Asn1.X509.Qualified;
using QuickJS;
using QuickJS.Native;
using SixLabors.ImageSharp.ColorSpaces;


namespace Ccf.Ck.SysPlugins.QuickJS {
    public class JSHost : IDisposable {
        private bool disposedValue;
        private QuickJSRuntime _runtime = null;
        private JSRuntime? _runtimeNative = null;
        private QuickJSContext _context = null;
        private JSContext? _contextNative = null;
        private object _locker = new object();

        private readonly int _stackSize = QuickJSRuntime.DefaultStackSize;
        private readonly int _memoryLimit = QuickJSRuntime.DefaultMemoryLimit;
        private readonly int _gcThreshold = QuickJSRuntime.DefaultGCThreshold;

        public string LastError { get; private set; }

        public JSHost(int? stackSize = null ,int? memoryLimit = null,int? gcThreshold = null) {
            if (stackSize != null) _stackSize = stackSize.Value;
            if (memoryLimit != null) _memoryLimit = memoryLimit.Value;
            if (gcThreshold != null) _gcThreshold = gcThreshold.Value;
        }

        public bool InitContext(string file) {
            if (_runtime!= null) throw new InvalidOperationException("The quickjs is alredy initialized in this JSHost.");
            try {
                lock (_locker) {
                    _runtime = new QuickJSRuntime(_memoryLimit, _gcThreshold, _stackSize);
                    _runtimeNative = _runtime.NativeInstance;
#if STATIC_JS
                    //_runtime.StdInitHandlers();
#else
                    _runtime.StdInitHandlers();
#endif
                    _context = _runtime.CreateContext();
                    _contextNative = _context.NativeInstance;
                    _context.StdAddHelpers();
                    //_context.InitModuleStd("std");
                    //_context.InitModuleOS("os");
                    //var o = _context.Eval("function main(n) { return n * n; }\nvar gyz='ako';", "<none>", JSEvalFlags.Global);
                    if (file != null) { // file is optional, but then the AppendCode or AppendFile must be used.
                        (_context.EvalFile(file, Encoding.UTF8, JSEvalFlags.Global) as IDisposable)?.Dispose();
                        _runtime.RunStdLoop(_context);
                    }
                }
                return true;
            } catch (Exception e) {
                lock (_locker) {
                    UnInitContext();
                }
                LastError = e.Message;
                throw;
                //return false;
            }
        }
#if !STATIC_JS
        public bool AppendCode(string script, string filename = null) {
            if (_runtime == null || _context == null) throw new InvalidOperationException("The quickjs is not initialized yet. Please follow the procedure - call InitContext first and then this method.");
            try {
                lock (_locker) {
                    (_context.Eval(script, filename == null ? "<root>":filename, JSValue.Null, JSEvalFlags.Global) as IDisposable)?.Dispose();
                }
                return true;
            } catch (Exception e) {
                lock (_locker) {
                    UnInitContext();
                }
                LastError = e.Message;
                return false;
            }
        }
        public bool AppendFile(string file) {
            if (_runtime == null || _context == null) throw new InvalidOperationException("The quickjs is not initialized yet. Please follow the procedure - call InitContext first and then this method.");
            try {
                lock (_locker) {
                    (_context.EvalFile(file, Encoding.UTF8, JSEvalFlags.Global) as IDisposable)?.Dispose();
                }
                return true;
            } catch (Exception e) {
                lock (_locker) {
                    UnInitContext();
                }
                LastError = e.Message;
                return false;
            }
        }
        public void RunInitLoop() {
            if (_context == null || _runtime == null) throw new InvalidOperationException("The quickjs is not initialized yet. Please follow the procedure - call InitContext first and then this method.");
            _runtime.RunStdLoop(_context);
        }
#endif
        private void UnInitContext()
        {
            if (_runtime != null)
            {
                if (_context != null)
                {
                    _context.Dispose();
                    _context = null;
                }
#if STATIC_JS
                //_runtime.StdFreeHandlers();
#else
                _runtime.StdFreeHandlers();
#endif
                _runtime.Dispose();
                _runtime = null;
                
            }
        }

        private object CallGlobalLow(string fname, params JSValue[] args) {
            LastError = null;
            object result = null;
            try
            {
                lock (_locker) {
                    QuickJSNativeApi.JS_UpdateStackTop(_runtimeNative.Value);
                    using QuickJSValue glob = _context.GetGlobal();
                    if (glob == null) return JSValue.Null;
                    using QuickJSValue func = (QuickJSValue)glob.GetProperty(fname);
                    result = func.Call(glob, args);
                }
                foreach (JSValue v in args) {
                    QuickJSNativeApi.JS_FreeValue(_contextNative.Value, v);
                }
                QuickJSNativeApi.JS_RunGC(_runtimeNative.Value);
                return result;
            } catch (Exception ex)
            {
                LastError = ex.Message;
                return null;
            }
        }
        
        public object CallGlobal(string fname, params object[] args) {
            // TODO: some disposing may be
            var jargs = args.Select(arg => {
                if (arg == null) return JSValue.Null;
                return arg switch {
                    string s => JSValue.Create(_context.NativeInstance, arg as string),
                    int i => JSValue.Create(i),
                    double d => JSValue.Create(d),
                    uint u => JSValue.Create(u),
                    bool b => JSValue.Create(b),
                    long l => JSValue.Create(l),
                    _ => throw new NotSupportedException("Unsupported argument type")
                };
            }).ToArray();
            var result = CallGlobalLow(fname, jargs);
            if (LastError != null)
            {
                throw new QuickJSException(LastError);
            }
            return result;
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                UnInitContext();
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
