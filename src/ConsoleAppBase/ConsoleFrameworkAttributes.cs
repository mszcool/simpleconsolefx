using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleAppBase
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ConsoleCommandAttribute : Attribute
    {
        public ConsoleCommandAttribute(string name, string description = "") 
        {
            CommandName = name;
            CommandDescription = description;
        }

        public string CommandName { get; }
        public string CommandDescription { get; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ConsoleCommandHelpMethodAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ConsoleCommandDefaultMethodAttribute : Attribute
    {
    }

    // Future feature: enable "short" versions of the parameters as well
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    public class ConsoleCommandParameterAttribute : Attribute
    {
        public ConsoleCommandParameterAttribute() { }
        public ConsoleCommandParameterAttribute(string name, string shortName = "", string description = "")
        {
            ParameterName = name;
            ParameterShortName = shortName;
            ParameterDescription = description;
        }

        public string ParameterName { get; }
        public string ParameterShortName { get; }
        public string ParameterDescription { get; }
    }
}
