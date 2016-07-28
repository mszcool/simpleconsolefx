using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

#if NETSTANDARD1_6

using Microsoft.DotNet.InternalAbstractions;

#endif

namespace ConsoleAppBase
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public class ConsoleBase
    {

        public string ConsoleAppName { get; }
        public Action PrintInfo { get; set; }

        private Assembly AppAssembly { get; set; }

        public ConsoleBase(string name)
        {
            ConsoleAppName = name;
        }

#if NET451 || NET461
        public void Run(string[] args)
        {
            // Gets the calling assembly to retrieve the types annoated with the [ConsoleCommand] attribte
            var asm = Assembly.GetCallingAssembly();
            var commandTypes = asm.GetTypes()
                                  .Where(t => t.GetCustomAttribute(typeof(ConsoleCommandAttribute)) != null)
                                  .ToArray();

            // Finally run the actual core logic
            RunInternal(args, commandTypes);
        }

#else
        public void Run(string[] args, string assemblyName)
        {
            // Gets the assembly through the passed in assembly name for retrieving the types annoated with [ConsoleCommand] attribte
            AppAssembly = Assembly.Load(new AssemblyName(assemblyName));
            var commandTypes = AppAssembly.GetTypes()
                                          .Where(t => t.GetTypeInfo().GetCustomAttribute<ConsoleCommandAttribute>() != null)
                                          .ToArray();
            RunInternal(args, commandTypes);
        }
#endif

        private void RunInternal(string[] args, Type[] commandTypes)
        {
            try
            {
                var commands = new List<string>();
                var parameters = new Dictionary<string, string>();

                // First of all, filter the command types by root-commands, only
                commandTypes = commandTypes.Where(t => t.GetTypeInfo().GetCustomAttribute<ConsoleCommandAttribute>().IsRootCommand).ToArray();

                // First of all, parse the parameters
                // Assume a '{command}* {--parametername value}*' format in the list of arguments
                // This means we parse commands until we found the first '--parameter'
                var generalParameters = ExtractParameters(args, commands, parameters);

                // Find the command type that matches the "parent-command" (the class) and the "child-command" (the method)
                // Future feature: support nesting of commands (class-class-class-method)
                var commandTypeFound = default(Type);
                var commandMethodFound = default(MethodInfo);
                var selectedCommandType = FindCommandClass(commands, commandTypes, out commandTypeFound);
                if (selectedCommandType != null)
                    commandMethodFound = FindCommandMethod(selectedCommandType, commands, commandTypes);

                // Is a version needed?
                if (generalParameters.VersionNeeded)
                {
                    PrintVersion();
                    return;
                }

                // If a "--help" or "-?" parameter was passed in as last parameter, print out help and stop execution
                if (generalParameters.HelpNeeded)
                {
                    PrintAvailabeCommands(commandTypes, commandMethodFound != null ? commandMethodFound.DeclaringType : selectedCommandType);
                    return;
                }
                else if (commandMethodFound == null)
                {
                    throw new ConsoleExecutionException($"Unknown command {string.Join(" ", commands.ToArray())} specified in the list of parameters. Please review the list of available commands!");
                }

                // Get the list of parameters which needed to be passed into the method and their types
                var paramsForCall = CompileParameters(parameters, commandMethodFound);

                // Finally call the actual command for execution
                commandMethodFound.Invoke(null, paramsForCall.ToArray());
            }
            catch (ConsoleExecutionException ex)
            {
                ConsoleFeatures.WriteMessage($"Usage: {this.ConsoleAppName} command [subcommand] {{parameters}}");
                ConsoleFeatures.WriteError(ex.Message);
                PrintAvailabeCommands(commandTypes, ex.HelpContextCommandType);
            }
        }

        private ConsoleGeneralParameters ExtractParameters(string[] args, List<string> commands, Dictionary<string, string> parameters)
        {
            bool helpNeeded = false;
            bool versionNeeded = false;
            int argParseIdx = 0;

            // First collect all the commands (which are not pre-fixed by '--')
            do
            {
                if (argParseIdx >= args.Length) break;
                if (args[argParseIdx].StartsWith("-")) break;
                commands.Add(args[argParseIdx]);
                argParseIdx++;
            } while (true);

            if (commands.Count == 0) helpNeeded = true;

            // Second collect all parameters and their values
            var paramName = default(string);
            var paramValue = default(string);
            do
            {
                if (argParseIdx >= args.Length) break;
                paramName = args[argParseIdx++];
                if (!paramName.StartsWith("--") && !paramName.StartsWith("-"))
                    throw new ConsoleExecutionException($"Expected parameter with format '--parametername' instead of a command '{paramName}' at this place!");
                else if (paramName.Equals("--help") || paramName.Equals("-?") || paramName.Equals("-h"))
                    helpNeeded = true;
                else if (paramName.Equals("--version") || paramName.Equals("-v"))
                    versionNeeded = true;
                else
                {
                    // Check if the next parameter is a value or a parameter
                    paramValue = null;
                    if (argParseIdx < args.Length)
                        paramValue = (args[argParseIdx].StartsWith("--") ? null : args[argParseIdx++]);

                    parameters.Add(paramName, paramValue);
                }
            } while (true);

            // Generic parameters list
            return new ConsoleGeneralParameters { HelpNeeded = helpNeeded, VersionNeeded = versionNeeded };
        }

        private Type FindCommandClass(List<string> commands, Type[] commandTypes, out Type lastCommandTypeIdentified)
        {
            // No command found at all
            lastCommandTypeIdentified = null;

            // Get the command to find
            var cmdToFind = commands.FirstOrDefault();
            if (cmdToFind == null) return null;

            // If no command types are here, skip all
            if (commandTypes == null) return null;
            if (commandTypes.Length == 0) return null;

            // Find the command in the list of commands
            foreach (var ct in commandTypes)
            {
                var cmdAttr = ct.GetTypeInfo().GetCustomAttribute<ConsoleCommandAttribute>();
                if (cmdAttr.CommandName.ToLower().Equals(cmdToFind.ToLower()))
                {
                    commands.RemoveAt(0);

                    if (cmdAttr.ChildCommands == null)
                        return ct;
                    else
                    {
                        var ctChild = FindCommandClass(commands, cmdAttr.ChildCommands, out lastCommandTypeIdentified);
                        if (ctChild == null) return ct;
                        else return ctChild;
                    }
                }
                lastCommandTypeIdentified = ct;
            }

            // Nothing found!!
            return null;
        }

        private MethodInfo FindCommandMethod(Type selectedCommandType, List<string> commands, Type[] commandTypes)
        {
            // Next find the command-method based on the sub-command type
            var commandMethodName = commands.LastOrDefault();
            var selectedCommandMethod = default(MethodInfo);
            var selectedDefaultCommandMethod = default(MethodInfo);
            foreach (var m in selectedCommandType.GetMethods(BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Static))
            {
                var commandInfo = (ConsoleCommandAttribute)(m.GetCustomAttribute(typeof(ConsoleCommandAttribute)));
                if ((commandInfo != null) && (commandInfo.CommandName.ToLower() == commandMethodName))
                {
                    selectedCommandMethod = m;
                    break;
                }
                else if (m.GetCustomAttribute(typeof(ConsoleCommandDefaultMethodAttribute)) != null)
                {
                    // TODO: fix this bug - when a command is passed in that is unknown, it always falls back to the default one
                    selectedDefaultCommandMethod = m;
                }
            }
            if ((selectedCommandMethod == null) && (selectedDefaultCommandMethod != null))
                return selectedDefaultCommandMethod;
            else
                return selectedCommandMethod;
        }

        private List<object> CompileParameters(Dictionary<string, string> parameters, MethodInfo commandMethod)
        {
            var parametersForCall = new List<object>();
            foreach (var p in commandMethod.GetParameters())
            {
                var paramName = $"--{p.Name.ToLower()}";
                var paramShortName = string.Empty;

                var paramAttr = p.GetCustomAttribute(typeof(ConsoleCommandParameterAttribute)) as ConsoleCommandParameterAttribute;
                if (paramAttr != null)
                {
                    paramName = $"--{paramAttr.ParameterName.ToLower()}";
                    paramShortName = $"-{paramAttr.ParameterShortName.ToLower()}";
                }

                if (parameters.ContainsKey(paramName) || parameters.ContainsKey(paramShortName))
                {
                    var paramValue = (parameters.ContainsKey(paramName) ?
                                        parameters[paramName]
                                        : parameters[paramShortName]);

                    if ((paramValue == null) && (p.ParameterType.FullName != "System.Boolean"))
                        throw new ConsoleExecutionException($"Missing parameter value for parameter {p.Name.ToLower()} of command {commandMethod.Name.ToLower()}!");

                    try
                    {
                        switch (p.ParameterType.FullName)
                        {
                            case "System.Boolean":
                                if (paramValue == null)
                                    parametersForCall.Add(false);
                                else
                                    parametersForCall.Add(Boolean.Parse(paramValue));
                                break;
                            case "System.Int16":
                                parametersForCall.Add(Int16.Parse(paramValue));
                                break;
                            case "System.Int32":
                                parametersForCall.Add(Int32.Parse(paramValue));
                                break;
                            case "System.Int64":
                                parametersForCall.Add(Int64.Parse(paramValue));
                                break;
                            case "System.String":
                                parametersForCall.Add(paramValue);
                                break;
                            case "System.DateTime":
                                parametersForCall.Add(DateTime.Parse(paramValue));
                                break;
                            case "System.Uri":
                                parametersForCall.Add(new Uri(paramValue, UriKind.RelativeOrAbsolute));
                                break;
                            default:
                                if (p.ParameterType.GetTypeInfo().BaseType.FullName == "System.Enum")
                                    parametersForCall.Add(Enum.Parse(p.ParameterType, paramValue));
                                else
                                    throw new NotImplementedException("Bug in your application: the type you are using iny our command-method is not supported by the framework, yet!");
                                break;
                        }
                    }
                    catch (Exception)
                    {
                        throw new ConsoleExecutionException($"Invalid value passed in for parameter {paramName}: {paramValue}!");
                    }
                }
                else if (!p.HasDefaultValue)
                {
                    throw new ConsoleExecutionException($"Missing value for parameter {p.Name.ToLower()} for command specified!");
                }
                else
                {
                    parametersForCall.Add(p.DefaultValue);
                }
            }
            return parametersForCall;
        }

        private void PrintVersion()
        {
            // Get the version information for the application
            var versionAttr = AppAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var appVersion = versionAttr.InformationalVersion;

            // Get the runtime information
#if NET451 || NET461
            var runtimeEnvVer = AppAssembly.ImageRuntimeVersion;
#else
            var runtimeEnvVer = typeof(RuntimeEnvironment).GetTypeInfo()
                                                          .Assembly
                                                          .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                                          .InformationalVersion;
#endif

            // Now print the version information
            ConsoleFeatures.WriteMessage("");
            ConsoleFeatures.WriteMessage($"{appVersion} (runtime: {runtimeEnvVer}).");
            ConsoleFeatures.WriteMessage("");
        }

        private void PrintAvailabeCommands(Type[] commandTypes, Type currentCommandContextForHelp)
        {
            // Print the general information
            PrintInfo?.Invoke();

            // Next print the basic usage
            ConsoleFeatures.WriteMessage("");
            ConsoleFeatures.WriteMessage($"Usage: {ConsoleAppName} command [command] [{{--parameterName parameterValue}}]");
            ConsoleFeatures.WriteMessage("");

            // Finally print the help for the current context
            if (currentCommandContextForHelp == null)
            {
                ConsoleFeatures.WriteMessage("Commands:");
                foreach (var cmd in commandTypes)
                {
                    var cmdAttr = cmd.GetTypeInfo().GetCustomAttribute<ConsoleCommandAttribute>();
                    ConsoleFeatures.WriteMessage($"  {cmdAttr.CommandName}\t\t{cmdAttr.CommandDescription}");
                }
            }
            else
            {
                var cmdAttr = currentCommandContextForHelp.GetTypeInfo().GetCustomAttribute<ConsoleCommandAttribute>();

                // First print the description and then the Commands
                ConsoleFeatures.WriteMessage($"{cmdAttr.CommandDescription}");
                ConsoleFeatures.WriteMessage("");
                ConsoleFeatures.WriteMessage("Commands:");
                ConsoleFeatures.WriteMessage("");

                // Get the default command and output its parameters
                var defaultMethod = currentCommandContextForHelp
                                        .GetMethods(BindingFlags.Public | BindingFlags.Static)
                                        .Where(m => m.GetCustomAttribute<ConsoleCommandDefaultMethodAttribute>() != null)
                                        .FirstOrDefault();
                // Just print the command name and its description or the entire default method documentation
                if (defaultMethod != null)
                {
                    var defaultMethodDescription = defaultMethod.GetCustomAttribute<ConsoleCommandDefaultMethodAttribute>();

                    ConsoleFeatures.WriteMessage(defaultMethodDescription.CommandDescription);
                    ConsoleFeatures.WriteMessage($"  {cmdAttr.CommandName} [parameters]");
                    PrintMethodHelp(defaultMethod, prefix: "  ");
                }

                // Get all the detailed methods annoated with the attribute of this specific type
                ConsoleFeatures.WriteMessage("");
                var hasChildCommands = (cmdAttr.ChildCommands != null && cmdAttr.ChildCommands.Length > 0);
                var commandMethods = currentCommandContextForHelp
                                        .GetMethods(BindingFlags.Public | BindingFlags.Static)
                                        .Where(m => m.GetCustomAttributes<ConsoleCommandAttribute>().Count() > 0);

                // Print out the direct method help
                foreach (var item in commandMethods)
                {
                    PrintMethodHelp(item, parentName: cmdAttr.CommandName, prefix: "");
                    ConsoleFeatures.WriteMessage("");
                }

                // Print out the remaining sub-commands' help text
                ConsoleFeatures.WriteMessage("More Commands:");
                if (cmdAttr.ChildCommands != null && cmdAttr.ChildCommands.Length > 0)
                {
                    foreach (var item in cmdAttr.ChildCommands)
                    {
                        var attr = item.GetTypeInfo().GetCustomAttribute<ConsoleCommandAttribute>();
                        ConsoleFeatures.WriteMessage($"  {attr.CommandName}\t{attr.CommandDescription}");
                    }
                }
            }

            ConsoleFeatures.WriteMessage("");
            ConsoleFeatures.WriteMessage("Options:");
            ConsoleFeatures.WriteMessage("  -h, -?, --help\tOutput usage information");
            ConsoleFeatures.WriteMessage("  -v, --version\tOutput version information");
            ConsoleFeatures.WriteMessage("");
        }

        private void PrintMethodHelp(MethodInfo mi, string parentName = "", string prefix = "")
        {
            var parentString = "";
            if (!string.IsNullOrEmpty(parentName)) parentString = $"{parentName} ";

            var attr = mi.GetCustomAttribute<ConsoleCommandAttribute>();
            if (attr != null)
            {
                ConsoleFeatures.WriteMessage($"{prefix}{attr.CommandDescription}");
                ConsoleFeatures.WriteMessage($"{prefix}  {parentString}{attr.CommandName} [parameters]");
            }

            var parametersOfMethod = mi.GetParameters();
            foreach (var item in parametersOfMethod)
            {
                var paramDefaultValueHelp = (item.HasDefaultValue
                                             ? item.DefaultValue.ToString()
                                             : "n/a");

                var methodAttr = item.GetCustomAttribute<ConsoleCommandParameterAttribute>();
                if (methodAttr != null)
                {
                    var shortParamHelp = (string.IsNullOrEmpty(methodAttr.ParameterShortName)
                                          ? "n/a"
                                          : $"-{methodAttr.ParameterShortName}");

                    ConsoleFeatures.WriteMessage($"{prefix}  --{methodAttr.ParameterName} " +
                                                 $"(short: {shortParamHelp}) " +
                                                 $"{item.ParameterType.Name} " +
                                                 $"(default: {paramDefaultValueHelp})");
                }
                else
                {
                    ConsoleFeatures.WriteMessage($"{prefix}  --{item.Name} " +
                                                 $"(short: n/a) " +
                                                 $"{item.ParameterType.Name} " +
                                                 $"(default: {paramDefaultValueHelp})");
                }
            }
        }
    }
}
