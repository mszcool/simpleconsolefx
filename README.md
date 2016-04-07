# SimpleConsoleFx
## A Little .NET Core Based Cross-Platform Console App Framework

Over the past months I found myself more and more testing Web APIs of all flavors by writing Console-Applications. It's a habit of mine that I always try to avoid too much hard-coding in those applications and keeping them flexible through arguments etc. That means, I typically find myself parsing and validating args[]-arrays passed into the main()-method over and over again. 

What I wanted is just writing clean, easy-to-read methods that help me testing out APIs but at the same time help me teaching those APIs to others. So wouldn't it be cool if I just could write classes and methods which are automatically translated into commands and parameters passed in as arguments to the Console Application?

Think about the Azure Cross-Platform CLI, for example:

  `azure vm create --vm-name mariosample --vm-size Small --location "North Europe"`
  
In that example, the translation could look as follows:
* `azure`: Translates into the command application executable
* `vm `: Translates into a class called `VirtualMachineCommands`
* `create`: Translates into a method called `CreateVm` of the class `VirtualMachineCommands`
*  `--vm-name`: Translates into a parameter `vmName` of the method `CreateVm`
* etc. etc.

That is exactly what this first attempt of my simple framework is doing (with some limitations:)). The source code above contains one Assembly that forms the base of the framework and does all the heavy Reflection-lifting to make this happen. The second project is a sample Console application that tests the basics by providing some sample commands.

**Note:** This is a spare-time activity and I work on it when I have time. So things are not perfect and things are kept pragmatic and simple.