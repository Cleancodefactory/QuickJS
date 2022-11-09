using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ccf.Ck.SysPlugins.QuickJS {
    public enum ENodeMode {
        Default = 0,
        /// <summary>
        /// Converts result to NET type and
        /// values: are set to [propertyname] in the result
        /// object: add/set single result (read/write)
        /// array: Multiple results (read)
        /// </summary>
        Node = 0,
        /// <summary>
        ///  Parses result as JSON and then threats it as in Node mode
        /// </summary>
        Jsonparse = 1,
        /// <summary>
        ///  Assign the result to [propertyname] value types only, objects and other complex ones will cause exception
        /// </summary>
        SingleValue = 2,
        /// <summary>
        ///  Result is forcibly converted to JSON (using JS context's means) and assigned to the [propertyname]
        /// </summary>
        Asjson = 3
    }
}
