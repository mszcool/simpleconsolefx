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
            description: "Commands to manage the tests aaaa for this tool!",
            children: new Type[] { typeof(TestCommandA1), typeof(TestCommandA2) }
        )
    ]
    public static class TestCommandA
    {
        [ConsoleCommandDefaultMethod("Runs the test command A itself!")]
        public static void DefaultCommand(int param1, string param2)
        {
            ConsoleFeatures.WriteMessage("You called the default execution path for this command!");
        }

        [ConsoleCommand(name: "commanda", description: "run this to run cmd-a")]
        public static void CommandA(string paramA, Uri paramC, int paramB = 1111)
        {
            ConsoleFeatures.WriteMessage($"CommandA{Environment.NewLine}--paramA={paramA}{Environment.NewLine}--paramB={paramB}{Environment.NewLine}--paramD={paramC}");
        }

        [ConsoleCommand(name: "commandb", description: "run this to run cmd-b")]
        public static void CommandB([ConsoleCommandParameter(name: "parama", shortName: "pa")] string paramA,
                                    [ConsoleCommandParameter(name: "paramb", shortName: "pb")] DateTime paramB,
                                    [ConsoleCommandParameter(name: "paramc", shortName: "pc")] int paramC)
        {
            ConsoleFeatures.WriteMessage($"CommandB{Environment.NewLine}--paramA={paramA}{Environment.NewLine}--paramB={paramB}{Environment.NewLine}--paramD={paramC}");
        }
    }

    [ConsoleCommand(
        name: "a1",
        description: "Commands to manage the functions a1 for this tool!", 
        isRoot: false)]
    public static class TestCommandA1
    {
        [ConsoleCommand(name: "run", description: "Runs the command A1, indeed!")]
        public static void CommandA1(string paramA, int paramB)
        {
            ConsoleFeatures.WriteMessage($"CommandA1 of TestCommandA1 called with {paramA} and {paramB}!!");
        }
    }

    [ConsoleCommand(
        name: "a2",
        description: "Commands to manage the functions a2 for this tool!", 
        isRoot: false)]
    public static class TestCommandA2
    {
        [ConsoleCommandDefaultMethod(description: "Runs the test command A2, itself!")]
        public static void DefaultCommand()
        {

        }

        [ConsoleCommand(name: "run", description: "Run this command to run a runner!")]
        public static void CommandA1(string paramA, int paramB)
        {
            ConsoleFeatures.WriteMessage($"CommandA1 of TestCommandA1 called with {paramA} and {paramB}!!");
        }

        [ConsoleCommand(name: "berun", description: "Run this command to be a runner!")]
        public static void CommandA2(string paramA, int paramB)
        {
            Console.WriteLine("CommandA2 of TestCommandA1 called!!");
        }
    }
}
