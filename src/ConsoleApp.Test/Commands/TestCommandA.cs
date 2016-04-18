using ConsoleAppBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApp.Test.Commands
{
    [
        ConsoleCommand
        (
            name: "testa", 
            description: "That is the main command for this application!",
            children: new Type[] { typeof(TestCommandA1), typeof(TestCommandA2) }
        )
    ]
    public static class TestCommandA
    {
        [ConsoleCommandDefaultMethod]
        public static void DefaultCommand()
        {
            ConsoleFeatures.WriteMessage("You called the default execution path for this command!");
        }

        [ConsoleCommand(name: "commanda")]
        public static void CommandA(string paramA, Uri paramC, int paramB = 1111)
        {
            ConsoleFeatures.WriteMessage($"CommandA{Environment.NewLine}--paramA={paramA}{Environment.NewLine}--paramB={paramB}{Environment.NewLine}--paramD={paramC}");
        }

        [ConsoleCommand(name: "commandb")]
        public static void CommandB([ConsoleCommandParameter(name: "parama", shortName: "pa")] string paramA,
                                    [ConsoleCommandParameter(name: "paramb", shortName: "pb")] DateTime paramB,
                                    [ConsoleCommandParameter(name: "paramc", shortName: "pc")] int paramC)
        {
            ConsoleFeatures.WriteMessage($"CommandB{Environment.NewLine}--paramA={paramA}{Environment.NewLine}--paramB={paramB}{Environment.NewLine}--paramD={paramC}");
        }
    }

    [ConsoleCommand(name: "a1", isRoot: false)]
    public static class TestCommandA1
    {
        [ConsoleCommandDefaultMethod]
        public static void DefaultCommand(string paramA)
        {
            ConsoleFeatures.WriteMessage($"default command for TestCommandA1 called!");
        }

        [ConsoleCommand(name: "run")]
        public static void CommandA1(string paramA, int paramB)
        {
            ConsoleFeatures.WriteMessage($"CommandA1 of TestCommandA1 called with {paramA} and {paramB}!!");
        }
    }

    [ConsoleCommand(name: "a2", isRoot: false)]
    public static class TestCommandA2
    {
        [ConsoleCommandDefaultMethod]
        public static void DefaultCommand()
        {

        }

        [ConsoleCommand(name: "run")]
        public static void CommandA1(string paramA, int paramB)
        {
            ConsoleFeatures.WriteMessage($"CommandA1 of TestCommandA1 called with {paramA} and {paramB}!!");
        }
    }
}
