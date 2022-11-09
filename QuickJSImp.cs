using System;
using System.Collections.Generic;
using System.Text;
using QuickJS;
using QuickJS.Native;
using Ccf.Ck.SysPlugins.Interfaces;
using Ccf.Ck.SysPlugins.Data.Base;
using System.Text.RegularExpressions;
using Ccf.Ck.Models.Resolvers;
using System.Globalization;
using Ccf.Ck.Utilities.Json;


namespace Ccf.Ck.SysPlugins.QuickJS {
    public class QuickJSImp : DataLoaderBase<QuickJSScopeContext> {
        protected override void ExecuteRead(IDataLoaderReadContext execContext) {
            var r = ExecuteQuery(execContext);
            if (execContext is INodePluginContextWithResults res && execContext.OwnContextScoped is QuickJSScopeContext scope) {
                switch (scope.Mode) {
                    case ENodeMode.Node:
                        throw new NotImplementedException("Not implemented yet!");
                    case ENodeMode.SingleValue:
                        res.Results.Add(new Dictionary<string, object>() { { scope.PropertyName,
                                r switch {
                                    int i => i,
                                    double d => d,
                                    string s => s,
                                    bool b => b,
                                    null => null,
                                    _ => throw new Exception("Type cannot be converted to simple value, try AsJson mode")

                                }
                         } });
                        break;
                    case ENodeMode.Jsonparse:
                        if (r == null) {
                            res.Results.Add(new Dictionary<string, object>() { { scope.PluginName, null } });
                        } else if (r is string s) {
                            object result = DictionaryStringObjectJson.Deserialize(s);
                            if (result is Dictionary<string, object> dso) {
                                res.Results.Add(dso);
                            } else if (result is List<object> list) {
                                foreach (var item in list) {
                                    if (item is Dictionary<string, object> dict) {
                                        res.Results.Add(dict);
                                    }
                                }
                            } else {
                                res.Results.Add(new Dictionary<string, object>() { { scope.PropertyName,
                                        result switch {
                                            int i => i,
                                            double d => d,
                                            bool b => b,
                                            _ => throw new Exception($"Unsupported type returned by the javascript {GetQuery(execContext)}")
                                        }
                                    } }); 
                            }
                        }
                    break;
                    case ENodeMode.Asjson:
                        if (r is QuickJSValue jsval) {
                            res.Results.Add(new Dictionary<string, object>() { { scope.PropertyName, jsval.ToJSON() } });
                        } else {
                            throw new Exception("In AsJson mode the returned data must be object or array of objects.");
                        }
                    break;
                }
                res.Results.Add(new Dictionary<string, object>() { { "result", r } });
            }
        }

        protected override void ExecuteWrite(IDataLoaderWriteContext execContext) {
            // TODO lets see if it works and then ...
            throw new NotImplementedException();

        }
        private static readonly Regex _regexFName = new Regex(@"^\s*([a-zA-Z][a-zA-Z0-9_]+)\s*\(",RegexOptions.Singleline);
        private static readonly Regex _regexArguments = new Regex(@"\s*(?:(true|false|null)|([a-zA-Z_][a-zA-Z0-9_]*)|(?:\'((?:\\'|[^\'])*)\')|([\+\-]?\d+(?:\.\d*)?))?\s*(,|\))", RegexOptions.Singleline);


        #region Utilities

        protected object ExecuteQuery(IDataLoaderContext execContext) {
            var scope = execContext.OwnContextScoped as QuickJSScopeContext;
            if (scope != null) {
                JSHost host = scope.Host(); // This will invoke loading of the JS engine and loading the file(s) when called for the first time.
                if (host != null) {
                    string fname = null;
                    List<object> args = new List<object>();
                    // What to call
                    string query = GetQuery(execContext);
                    if (string.IsNullOrEmpty(query)) {
                        // TODO Maybe pass it through later?
                        throw new Exception("query or load query is required in nodes where quickjs plugin is used.");
                    }
                    ParseQuery(query, out fname, args, execContext);
                    object result = host.CallGlobal(fname, args.ToArray());
                    return result;
                }
            }
            throw new NullReferenceException("The scope context or host of QuickJS is not available. Check if it is loaded correctly.");
        }

        private enum Term
        {
            literal = 1,
            identifier = 2,
            text = 3,
            number = 4,
            delimiter = 5
        }
        /// <summary>
        /// Parses the query parameter as a single function call
        /// </summary>
        /// <param name="fname"></param>
        /// <param name="args"></param>
        protected void ParseQuery(string query,out string fname, List<object> args, IDataLoaderContext ctx)
        {
            if (string.IsNullOrWhiteSpace(query)) throw new ArgumentNullException("one of query or loadquery is required");
            Match match = _regexFName.Match(query);
            if (match.Success && match.Groups[1].Success) {
                fname = match.Groups[1].Value;
                match = _regexArguments.Match(query, match.Length);
                while (match.Success)
                {
                    for (int i = 1; i < match.Groups.Count - 1; i++) {
                        if (match.Groups[i].Success) {
                            string current = match.Groups[i].Value;
                            switch ((Term)i) {
                                case Term.literal:
                                    if (current == "true") {
                                        args.Add(true);
                                    } else if (current == "false") {
                                        args.Add(false);
                                    } else if (current == "null") {
                                        args.Add(null);
                                    } else {
                                        throw new Exception("Syntax error");
                                    }
                                goto nextArg;
                                case Term.identifier:
                                    ParameterResolverValue val = ctx.Evaluate(current);
                                    args.Add(val.Value);
                                goto nextArg;
                                case Term.text:
                                    args.Add(current.Replace("\\'", "'"));
                                goto nextArg;
                                case Term.number:
                                    if (current.IndexOf(".") >= 0) {
                                        if (double.TryParse(current, NumberStyles.Any, CultureInfo.InvariantCulture, out double d)) {
                                            args.Add(d);
                                        }
                                    } else {
                                        if (int.TryParse(current, NumberStyles.Any, CultureInfo.InvariantCulture, out int l)) {
                                            args.Add(l);
                                        }
                                    }
                                goto nextArg;

                            }
                        }
                    }
                    nextArg:
                    if (match.Groups[(int)Term.delimiter].Success)
                    {
                        if (match.Groups[(int)Term.delimiter].Value == ")")
                        {
                            // complete
                            return;
                        }
                    }
                    match = match.NextMatch();
                }
                throw new ArgumentException("Syntax error in the query for QuickJS plugin. Missing closing bracket.");
            } else {
                throw new ArgumentException("Syntax error in the query for QuickJS plugin. It must be a function call with arguments from the node parameters or constants");
            }
        }
        #endregion
    }
}