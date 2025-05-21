// See https://aka.ms/new-console-template for more information
using AssemblyGen;
using System.Reflection.Emit;

Console.WriteLine("Hello, World!");


var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(new System.Reflection.AssemblyName(Identifier.Random()), AssemblyBuilderAccess.RunAndCollect);
var moduleBuilder = asmBuilder.DefineDynamicModule(Identifier.Random());