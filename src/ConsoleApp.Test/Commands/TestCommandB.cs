using ConsoleAppBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApp.Test.Commands
{
    [ConsoleCommand("testb", "This is used to execute the second test!")]
    public static class TestCommandB
    {
    }

    [ConsoleCommand("testc", "Yet another command in this collection of commands for our testing purposes!")]
    public static class TestCommandC
    {
    }
}
