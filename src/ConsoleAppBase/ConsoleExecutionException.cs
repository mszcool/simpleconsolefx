using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleAppBase
{
    public class ConsoleExecutionException : Exception
    {
        public Type HelpContextCommandType { get; private set; }

        public ConsoleExecutionException(string message)
            : base(message)
        {
            HelpContextCommandType = null;
        }

        public ConsoleExecutionException(string message, Type helpContextType)
            : base(message)
        {
            HelpContextCommandType = helpContextType;
        }

        public ConsoleExecutionException(string message, Exception innerException)
            : base(message, innerException)
        {
            HelpContextCommandType = null;
        }

        public ConsoleExecutionException(string message, Exception innerException, Type helpContextType)
            : base(message, innerException)
        {
            HelpContextCommandType = helpContextType;
        }
    }
}
