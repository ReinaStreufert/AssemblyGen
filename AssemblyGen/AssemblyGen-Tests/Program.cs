// See https://aka.ms/new-console-template for more information
using AssemblyGen;
using AssemblyGen_Tests;
using System.Reflection;
using System.Reflection.Emit;

Console.Write("Set condition: ");
var conditionName = Console.ReadLine()!.ToUpper();

var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Identifier.Random()), AssemblyBuilderAccess.RunAndCollect);
var moduleBuilder = asmBuilder.DefineDynamicModule(Identifier.Random());
var typeBuilder = moduleBuilder.DefineType("Generated", System.Reflection.TypeAttributes.Public, null);
typeBuilder.AddInterfaceImplementation(typeof(IGenerated));

var setParam = typeof(ConditionSet).AsParameter();
var repititionCountParam = typeof(int).AsParameter();
typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
var writeTextBuilder = typeBuilder.DefineMethod("WriteTextFactory.imp", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final, (ctx) =>
{
    var conditionSet = ctx.GetArgument(setParam);
    var repitionCount = ctx.GetArgument(repititionCountParam);
    var textParam = typeof(string).AsParameter();
    var lambdaBlock = ctx.BeginLambda(typeof(void), textParam);
    var text = ctx.GetArgument(textParam);
    var iteration = ctx.DeclareLocal(typeof(int));
    iteration.Assign(ctx.Constant(0));
    var repititionLoop = ctx.BeginLoop();
    var checkEndStatement = ctx.BeginIfStatement(
        iteration.Operation(BinaryOperator.LessThan, repitionCount).Operation(UnaryOperator.Not));
    repititionLoop.Break();
    checkEndStatement.End();
    var messageText = ctx.DeclareLocal(typeof(string));
    var ifStatement = ctx.BeginIfStatement(conditionSet.GetFieldOrProperty(conditionName));
    messageText.Assign(text);
    var elseStatement = ifStatement.Else();
    messageText.Assign(ctx.Constant("hidden"));
    elseStatement.End();
    var console = ctx.StaticType(typeof(Console));
    console.CallMethod(nameof(Console.WriteLine), messageText);
    iteration.Assign(iteration.Operation(BinaryOperator.Plus, ctx.Constant(1)));
    repititionLoop.End();
    ctx.Return();
    lambdaBlock.End();
    ctx.Return(lambdaBlock.ToDelegate(typeof(Action<string>)));
}, typeof(Action<string>), setParam, repititionCountParam);
typeBuilder.DefineMethodOverride(writeTextBuilder, typeof(IGenerated).GetMethod(nameof(IGenerated.WriteTextFactory))!);

var t = typeBuilder.CreateType();
var generatedInst = (IGenerated)Activator.CreateInstance(t)!;
var conditionSet = new ConditionSet();
conditionSet.A = true;
conditionSet.C = false;
var writeTextAction = generatedInst.WriteTextFactory(conditionSet, 5);
while (true)
{
    var echoText = Console.ReadLine()!;
    writeTextAction(echoText);
    conditionSet.B = !conditionSet.B;
}