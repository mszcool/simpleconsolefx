using ConsoleAppBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApp.Test.Commands
{
    [ConsoleCommand(name: "testa", description: "That is the main command for this application!")]
    public static class TestCommandA
    {
        [ConsoleCommandDefaultMethod]
        public static void DefaultCommand()
        {
            ConsoleFeatures.WriteMessage("You called the default execution path for this command!");
        }

        [ConsoleCommand(name: "commanda")]
        public static void CommandA(string paramA, int paramB, Uri paramC)
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
}
