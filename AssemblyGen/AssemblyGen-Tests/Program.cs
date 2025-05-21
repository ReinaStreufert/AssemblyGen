// See https://aka.ms/new-console-template for more information
using AssemblyGen;
using AssemblyGen_Tests;
using System.Reflection;
using System.Reflection.Emit;

Console.Write("Set condition: ");
var conditionName = Console.ReadLine()!.ToUpper();

var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(new System.Reflection.AssemblyName(Identifier.Random()), AssemblyBuilderAccess.RunAndCollect);
var moduleBuilder = asmBuilder.DefineDynamicModule(Identifier.Random());
var typeBuilder = moduleBuilder.DefineType("Generated", System.Reflection.TypeAttributes.Public, null);
typeBuilder.AddInterfaceImplementation(typeof(IGenerated));

var textParam = typeof(string).AsParameter();
var setParam = typeof(ConditionSet).AsParameter();
typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
var writeTextBuilder = typeBuilder.DefineMethod("WriteText.imp", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final, (ctx) =>
{
    var text = ctx.GetArgument(textParam);
    var conditionSet = ctx.GetArgument(setParam);
    var messageText = ctx.DeclareLocal(typeof(string));
    var ifStatement = ctx.BeginIfStatement(conditionSet.GetFieldOrProperty(conditionName));
    messageText.Assign(text);
    var elseStatement = ifStatement.Else();
    messageText.Assign(ctx.Constant("hidden"));
    elseStatement.End();
    var console = ctx.StaticType(typeof(Console));
    console.CallMethod(nameof(Console.WriteLine), messageText);
}, typeof(void), textParam, setParam);
typeBuilder.DefineMethodOverride(writeTextBuilder, typeof(IGenerated).GetMethod(nameof(IGenerated.WriteText))!);

var t = typeBuilder.CreateType();
var generatedInst = (IGenerated)Activator.CreateInstance(t)!;
var conditionSet = new ConditionSet();
conditionSet.A = true;
conditionSet.C = false;
while (true)
{
    var echoText = Console.ReadLine()!;
    generatedInst.WriteText(echoText, conditionSet);
    conditionSet.B = !conditionSet.B;
}