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
using System.Configuration;


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
            }
        }

        protected override void ExecuteWrite(IDataLoaderWriteContext execContext) {
            // TODO lets see if it works and then ...
            throw new NotImplementedException();

        }
        // Appends a raw source or include a file
        private static readonly Regex _regexAppendInclude = new Regex(@"\s*#(include|append)\s+(?:([a-zA-Z_][a-zA-Z0-9_]*)|(?:\'((?:\\'|[^\'])*)\'))\s*;", RegexOptions.Multiline);
        
        

        private static readonly Regex _regexFName = new Regex(@"\s*([a-zA-Z][a-zA-Z0-9_]+)\s*\(",RegexOptions.Multiline);
        private static readonly Regex _regexArguments = new Regex(@"\s*(?:(true|false|null)|([a-zA-Z_][a-zA-Z0-9_]*)|(?:\'((?:\\'|[^\'])*)\')|([\+\-]?\d+(?:\.\d*)?))?\s*(,|\)\s*(?=;|$))", RegexOptions.Multiline);


        #region Utilities

        protected object ExecuteQuery(IDataLoaderContext execContext) {
            var scope = execContext.OwnContextScoped as QuickJSScopeContext;
            if (scope != null) {
#if STATIC_JS
                JSHost host = scope.Host(); // This will invoke loading of the JS engine and loading the file(s) when called for the first time.
#else
                using JSHost host = scope.Host(); // This will invoke loading of the JS engine and loading the file(s) when called for the first time.
#endif
                if (host != null) {
                    string fname = null;
                    List<object> args = new List<object>();
                    // What to call
                    string query = GetQuery(execContext);
                    if (string.IsNullOrEmpty(query)) {
                        // TODO Maybe pass it through later?
                        throw new Exception("query or load query is required in nodes where quickjs plugin is used.");
                    }
                    ParseQuery(query, out fname, args, host, execContext);
                    host.RunInitLoop();
                    object result = host.CallGlobal(fname, args.ToArray());
                    return result;
                }

            }
            throw new NullReferenceException("The scope context or host of QuickJS is not available. Check if it is loaded correctly.");
        }

        private string ReadIncludeFile(string file, IDataLoaderContext execContext) {
            var scope = execContext.OwnContextScoped as QuickJSScopeContext;
            if (scope == null) throw new Exception("QuickJS data loaderCannot obtain the scope context");
            var basePath = scope.BasePath;
            if (basePath != null) {
                var filePath = Path.Combine(basePath, file);
                if (File.Exists(filePath)) {
                    var content = File.ReadAllText(filePath,Encoding.UTF8);
                    return content;
                } else {
                    throw new FileNotFoundException($"{filePath} cannot be found");
                }
            } else {
                throw new Exception("basepath is not configured. Cannot load file.");
            }
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
        /// Parses the query parameter, loads any scripts specified there and a single function call in the end 
        /// which it prepares, but does not execute - this has to be done outside. This enables running the 
        /// initialization loop in the middle
        /// </summary>
        /// <param name="fname"></param>
        /// <param name="args"></param>
        protected void ParseQuery(string query,out string fname, List<object> args, JSHost host, IDataLoaderContext ctx)
        {
            if (string.IsNullOrWhiteSpace(query)) throw new ArgumentNullException("one of query or loadquery is required");
            Match match;
            int Pos = 0;
            string content;
            match = _regexAppendInclude.Match(query,Pos);
            while (match.Success) {
                if (match.Groups[1].Success) {
                    switch (match.Groups[1].Value) {
                        case "include":
                            if (match.Groups[2].Success) { // From parameter
                                ParameterResolverValue file = ctx.Evaluate(match.Groups[2].Value);
                                if (file.Value is string filepath) {
                                    content = ReadIncludeFile(filepath, ctx);
                                    if (content != null) {
                                        if (!host.AppendCode(content, filepath)) {
                                            throw new QuickJSException($"Cannot add an include: {host.LastError}");
                                        }
                                    } else {
                                        throw new FileNotFoundException($"Cannot read the include file {filepath}");
                                    }
                                } else {
                                    throw new Exception($"The parameter {match.Groups[2].Value} did not resolve to string");
                                }
                            } else if (match.Groups[3].Success) { // From literal
                                content = ReadIncludeFile(match.Groups[3].Value, ctx);
                                if (content != null) {
                                    if (!host.AppendCode(content, match.Groups[3].Value)) {
                                        throw new QuickJSException($"Cannot add an include: {host.LastError}");
                                    }
                                } else {
                                    throw new FileNotFoundException($"Cannot read the include file {match.Groups[3].Value}");
                                }
                            }
                            break;
                        case "append":
                            if (match.Groups[2].Success) { // From parameter
                                ParameterResolverValue code = ctx.Evaluate(match.Groups[2].Value);
                                if (code.Value is string script) {
                                    if (script != null) {
                                        if (!host.AppendCode(script)) {
                                            throw new QuickJSException($"Cannot append script: {host.LastError}");
                                        }
                                    } else {
                                        throw new FileNotFoundException($"Append script failed.The parameter {match.Groups[2].Value} did not resolve to string.");
                                    }
                                }
                            } else if (match.Groups[3].Success) { // From literal
                                content = match.Groups[3].Value;
                                if (content != null) {
                                    if (!host.AppendCode(content)) {
                                        throw new QuickJSException($"Cannot append code: {host.LastError}");
                                    }
                                } else {
                                    throw new FileNotFoundException($"No scirpt code to append.");
                                }
                            }
                            break;
                            
                    }
                }
                Pos = match.Index + match.Length;
                match = match.NextMatch();
                
            }

            match = _regexFName.Match(query, Pos);
            if (match.Success && match.Groups[1].Success) {
                Pos = match.Index + match.Length;
                fname = match.Groups[1].Value;
                match = _regexArguments.Match(query, Pos);
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
                        if (match.Groups[(int)Term.delimiter].Value.IndexOf(")") == 0)
                        {
                            // complete
                            return;
                        }
                    }
                    match = match.NextMatch();
                }
                throw new ArgumentException("Syntax error in the query for QuickJS plugin. Missing closing bracket or ;.");
            } else {
                throw new ArgumentException("Syntax error in the query for QuickJS plugin. It must end with a function call with arguments from the node parameters or constants");
            }
        }
#endregion
    }
}