using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppBase
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public class ConsoleBase
    {
        public string ConsoleAppName { get; }

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
            var asm = Assembly.Load(new AssemblyName(assemblyName));
            var commandTypes = asm.GetTypes()
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

                // First of all, parse the parameters
                // Assume a '{command}* {--parametername value}*' format in the list of arguments
                // This means we parse commands until we found the first '--parameter'
                ExtractParameters(args, commands, parameters);

                // Find the command type that matches the "parent-command" (the class) and the "child-command" (the method)
                // Future feature: support nesting of commands (class-class-class-method)
                var commandMethod = FindCommandMethod(commands, commandTypes);

                // Get the list of parameters which needed to be passed into the method and their types
                var paramsForCall = CompileParameters(parameters, commandMethod);

                // Finally call the actual command for execution
                commandMethod.Invoke(null, paramsForCall.ToArray());
            }
            catch (ArgumentException ex)
            {
                ConsoleFeatures.WriteMessage($"Usage: {this.ConsoleAppName} command [subcommand] {{parameters}}");
                ConsoleFeatures.WriteError(ex.Message);
                PrintAvailabeCommands(commandTypes);
            }
        }

        private void ExtractParameters(string[] args, List<string> commands, Dictionary<string, string> parameters)
        {
            int argParseIdx = 0;

            // First collect all the commands (which are not pre-fixed by '--')
            do
            {
                if (argParseIdx >= args.Length) break;
                if (args[argParseIdx].StartsWith("-")) break;
                commands.Add(args[argParseIdx]);
                argParseIdx++;
            } while (true);

            if (commands.Count == 0)
                throw new ArgumentException("Expected at least one command passed in. Please review the list of available commands!");
            if (commands.Count > 2)
                throw new ArgumentException("The current version supports commands with one sub-command, only!");

            // Second collect all parameters and their values
            do
            {
                if (argParseIdx >= args.Length) break;
                var paramName = args[argParseIdx++];
                var paramValue = (argParseIdx < args.Length ? args[argParseIdx++] : null);
                if (!paramName.StartsWith("--") && !paramName.StartsWith("-"))
                    throw new ArgumentException($"Expected parameter with format '--parametername' instead of a command '{paramName}' at this place!");
                parameters.Add(paramName, paramValue);
            } while (true);
        }

        private MethodInfo FindCommandMethod(List<string> commands, Type[] commandTypes)
        {
            // Currently I only support 'command command {--parameters}', but in the future I'll extend this to
            // recursively search for nested command classes up until it reaches the last class in hierarchy to find the method.
            var commandName = commands[0].ToLower();
            var subCommandName = (commands.Count >= 2 ? commands[1].ToLower() : string.Empty);

            // Find the command class
            var selectedCommandType = default(Type);
            foreach (var commandType in commandTypes)
            {
                var commandInfo = (ConsoleCommandAttribute)(commandType.GetTypeInfo().GetCustomAttribute(typeof(ConsoleCommandAttribute)));
                if (commandInfo.CommandName.ToLower() == commandName)
                {
                    selectedCommandType = commandType;
                    break;
                }
            }
            if (selectedCommandType == null)
                throw new ArgumentException($"Unknown command {commandName} specified in the list of parameters. Please review the list of available commands!");

            // Next find the command-method based on the sub-command type
            var selectedCommandMethod = default(MethodInfo);
            var selectedDefaultCommandMethod = default(MethodInfo);
            foreach (var m in selectedCommandType.GetMethods(BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Static))
            {
                var commandInfo = (ConsoleCommandAttribute)(m.GetCustomAttribute(typeof(ConsoleCommandAttribute)));
                if ((commandInfo != null) && (commandInfo.CommandName.ToLower() == subCommandName))
                {
                    selectedCommandMethod = m;
                    break;
                }
                else if (m.GetCustomAttribute(typeof(ConsoleCommandDefaultMethodAttribute)) != null)
                {
                    selectedDefaultCommandMethod = m;
                }
            }
            if (selectedCommandMethod != null)
                return selectedCommandMethod;
            else if (selectedDefaultCommandMethod != null)
                return selectedDefaultCommandMethod;
            else
                throw new ArgumentException($"Unknown command {subCommandName} specified in the list of parameters. Please review the list of available commands!");
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
                                        parameters[paramName] : parameters[paramShortName]);
                    if ((paramValue == null) && (p.ParameterType.FullName != "System.Boolean"))
                        throw new ArgumentException($"Missing parameter value for parameter {p.Name.ToLower()} of command {commandMethod.Name.ToLower()}!");
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
                        throw new ArgumentException($"Invalid value passed in for parameter {paramName}: {paramValue}!");
                    }
                }
                else if (!p.HasDefaultValue)
                {
                    throw new ArgumentException($"Missing value for parameter {p.Name.ToLower()} for command specified!");
                }
            }
            return parametersForCall;
        }


        private void PrintAvailabeCommands(Type[] commandTypes)
        {
            ConsoleFeatures.WriteMessage($"Usage: {ConsoleAppName} command [command] [{{--parameterName parameterValue}}]");
            foreach (var cmd in commandTypes)
            {
                var cmdAttr = (ConsoleCommandAttribute)(cmd.GetTypeInfo().GetCustomAttribute(typeof(ConsoleCommandAttribute)));
                ConsoleFeatures.WriteMessage($"{cmdAttr.CommandName}\t\t{cmdAttr.CommandDescription}");
            }
        }

    }
}
