# QuickJS CoreKraft plugin

This plugin is based and contains code from the 

* [quickjs](https://github.com/bellard/quickjs) engine by Fabrice Bellard 
* [QuickJS.Net](https://github.com/vmas/QuickJS.NET) wrapper library by [vmas](https://github.com/vmas)

## Overview

The plugin is still in alpha state and supports only data loader usage, custom (_node plugin_) will be supported later along the lines of the current syntax and usage pattern.

The plugin enables usage of globally (not as Javascript modules) loaded Javascript pieces either from files or from chunks of script and calling a function from the resulting Javascript. The call supports passing parameters in the typical CoreKraft manner - string, boolean and numeric literals and parameters from the node. The result of the called function can be used in a few different manners (see mode later) - can be put in a single value of a single node result or can be transformed into one or multiple results.

## Configuration

Once built and/or copied into the Modules/_PluginsReferences directory entries like the one described below can be included in the Configuration.json of the CoreKraft modules where it will be used.

```JSON
"NodesDataLoader": [
    //... other plugins ...
    {
        "Name": "myquickjs",
        "ImplementationAsString": "Ccf.Ck.SysPlugins.QuickJS.QuickJSImp, Ccf.Ck.SysPlugins.QuickJS",
        "InterfaceAsString": "Ccf.Ck.SysPlugins.Interfaces.IDataLoaderPlugin, Ccf.Ck.SysPlugins.Interfaces",
        "Default": true,
        "CustomSettings": {
            "file": "@moduleroot@/Data/js/root.js",
            "mode": "SingleValue",
            "propertyname": "parsedresult",
            "stack": "256",
            "memory": "1002400",
            "basepath": "@moduleroot@/Data/js/",
            "gcthreshold": "512"
        }
    }

    //... other plugins ...
]
```

All the currently supported custom settings are illustrated above:

* `file` - root file to always load. Additional files and fragments can be included when the plugin is used in specific node, but this file will always be loaded first. It is required (for now).

* `mode` - How to deal with the result of the called Javascript function. The possible options are:

    * `SingleValue` - (default) - puts the result into a new result with a single entry with name specified in the `propertyname` parameter. The result can be number, boolean, string.

    * `Jsonparse` - The result is assumed to be JSON string. It is parsed and expected to be either array of objects or a single object. For each object one new result is created by the data loader.
    
    * `Node` - Not implemented yet.

    * `Asjson` - (not tested) The result is stringified and set into a single element of a single result. The name of the element is specified in the `propertyname` parameter. 

* `propertyname` - A name of property (element) in the result for the cases in which the result is put as a single value there (see `SingleValue` and `Asjson` above).

* `basepath` - required if any #include statements are used in the query of the node where the plugin is used. Specifies the path against which the included files will be searched. E.g. if base path is set like above you can use (see details below) `#include 'file1.js';` and the file is expected to be in the `@moduleroot@/Data/js/` directory.

* `stack` - Limits the size of the stack. Specified in KBytes.

* `memory` - Specifies the memory limit in KBytes. E.g. the example above sets limit of 1002400 KBytes, which roughly translates to a gigabyte.

* `gcthreshold` - Threashold for the quickjs garbage collector. Specified in KBytes.

**Recommendations:**

Specifying limits is strongly recommended. Unlimited memory can be set by setting memory to "0", but can be dangerous, because system level exception which will occur when the memory usage goes too high will crash the entire process (the CoreKraft) as whole.

Design of the Javascripts can vary, but a less error prone approach will be to use the file specified in the file setting as a shared library between the actual scripts being used. 

## Usage in nodes

Any number of nodes in any number of nodesets can use variety of configurations of the plugin. Two modes of operation are planned, but currently only one of them is fully implemented - the load-call-unload mode. In this mode a new Javascript engine is instantiated for each node execution, it is used and immediately disposed of. The other mode of operation is permanent one - where the engine will live for long periods, often from its first usage to the shutdown/restart of the system. That second mode is not currently fully implemented. It enables retention of some state in the engine between invocations from different nodes with all the positive and negative consequences. So, the details below apply to the load-call-unload mode only - it is the default one and does not require configuration setting.

Example node:

```JSON
// ... other nodes ...
{
    "nodekey": "mynode",
    "datapluginname": "myquickjs",
    "islist": 0,
    "read": {
        "parameters": [
            {"name": "file1", "Expression": "GetFrom('parent', 'filename')"},
            {"name": "file2", "Expression": "'hardcodedfile.js'"},
            {"name": "src", "Expression": "GetFrom('parent', 'src')"}

        ],
        "select": {
            "query": "#include file1; #append 'var g_b=400;'; #include file2; #append src; #append 'var g_a=200;'; getresult('stuff1',123);"
        }
    }
}
// ... other nodes ...
```

The above example will make more sense if you assume it is a child of another node that supplies the `filename` and the `src` values in its result. This will probably be the actual case if a solution with dynamically constructed Javascript is required.

The query can be loaded from a file if `loadquery` is used insted of `query`, but this will be really needed only if the pieces included/appended are too many. The general syntax of the query is:

Any number of `#include` statements can be mixed with any number of `#append` statements.
```Javascript
#include file1;
#append ' ... some code ... ';
#include 'path/hardocodedfile.js';
#append src;
myfunction(arg1,arg2);
```

The include and append statements must be followed with a function call statement (see myfunction above). All the statements are separated by the semicolon character ; and can be placed on a single line if desired (especially if they are not in a file loaded by using loadquery, but specified in-place in a `query` setting).

`#include` - includes/appends the content of a file to the Javascript loaded. The file path is calculated starting at the `basepath` specified in the Configuration.json (see the Configuration section above). This setting is required if any include statement is used. The file path can be specified as a sting literal in-place or read from a parameter. So, for example, if the nodeset is using different Javascript file depending on some condition - the file names can be composed in the parent node and accessed through parameters (identifiers without quotes in the include statement). The parameter names must start with a letter and can contain alphanumeric characters after that, plus underscore.

`#append` - appends a piece of script as text. Like with the include statement it can be specified as literal string in single quotes or as parameter name (the name of hte parameter without quotes). This approach enables scripts to be composed from pieces fetched form anywhere - even database. The parent node usually fetches the content from somewhere and puts it in the generated result(s) from where it is accessed using GetFrom in the local node parameter and then passed to an `#append` statement.

`function call` - single function call is allowed. Any text after it will be ignored or even cause error (in future versions for sure). The parts are:

{functionname} ( {arg}, {arg}, {arg} ... )

Where the `functionname` is the case sensitive name of a function implemented in the loaded Javascript (the function can be in any of the pieces or even in the root script loaded according to the `file` configuration setting). 

The `arg`-s can be any number - all will be passed the called function, but how many will actually be used depends on the function implementation itself. Each `arg` can be either a literal number (integer or floating point without exponent), string literal (in single quotes), `true`, `false`, `null` or a name of a parameter configured in the node parameters. Currently supported types are numerics, booleans, strings and null. Whenever more complex structures are to be passed to the function, they should be stringified into JSON and parsed inside the function.

The function must return a result of on of these types too. See the `mode` in the Configuration above.

## Important requirements and advise

When using include or append statements, they should contain a syntactically complete piece of javascript. E.g. You cannot `#append 'var a ='; #append '10';` neither include files with similarly incomplete Javascript fragments.

The plugin is mainly designed for processing of relatively large pieces of data and will not be effective if used to perform small operations - use ActionQuery for that purpose. Still another consideration is a result of this purpose - the Javascript processing big chunks of JSON can consume significant amounts of memory and if that is the case one should find a way to avoid executing in parallel too many of the nodes using quickjs. One of the possible solutions is to use the task scheduling in CoreKraft (see `IndirectCallService` in the C# code and ScheduleCallRead/ScheduleCallWrite functions in 
`internalcalls` library of ActionQuery). The default scheduler works on a single thread in order to keep most of the CoreKraft resources dedicated to its WEB serving responsibilities, but quickjs is quick enough to make this enough for most purposes. Multithreaded scheduler is in the works, but its usage will be recommended for service instances of CoreKraft and not for regular Web servers.



