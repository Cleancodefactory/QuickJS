using QuickJS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ccf.Ck.SysPlugins.QuickJS {
    public static class QuickJSValueExtensions {
        public static T GetProperty<T>(this QuickJSValue tval, string propname) {
            object oval = tval.GetProperty(propname);
            if (oval == null) { return default(T); }
            if (oval.GetType() == typeof(T)) { return (T)oval; }
            if (oval is IConvertible) {
                T t = (T)((IConvertible)oval).ToType(typeof(T), null);
                return t;
            }
            return default(T);
        }
    }
}
