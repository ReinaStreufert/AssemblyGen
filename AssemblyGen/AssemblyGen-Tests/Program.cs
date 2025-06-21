// See https://aka.ms/new-console-template for more information
using AssemblyGen;
using AssemblyGen_Tests;
using System.Reflection;
using System.Reflection.Emit;


var inputNumbersParam = typeof(IEnumerable<int>).AsParameter();
var additiveAParam = typeof(int).AsParameter();
var additiveBParam = typeof(int).AsParameter();

MethodGenerator testIteratorGenerator = ctx =>
{
    var additiveA = ctx.GetArgument(additiveAParam);
    var additiveB = ctx.GetArgument(additiveBParam);
    var alternate = ctx.DeclareLocal(typeof(bool));
    alternate.Assign(ctx.Constant(false));
    var foreachLoop = ctx.BeginForeachLoop(ctx.GetArgument(inputNumbersParam), out var inputNumber);
    var additive = ctx.DeclareLocal(typeof(int));
    var pickStatement = ctx.BeginIfStatement(alternate);
    additive.Assign(additiveA);
    var pickElse = pickStatement.Else();
    additive.Assign(additiveB);
    pickElse.End();
    ctx.Return(inputNumber.Operation(BinaryOperator.Plus, additive));
    alternate.Assign(alternate.Operation(UnaryOperator.Not));
    foreachLoop.End();
    ctx.Return();
};

var persistedAssembly = new PersistedAssemblyBuilder(new AssemblyName("IteratorDebugPersistsed"), Assembly.GetAssembly(typeof(object))!);
var module = persistedAssembly.DefineDynamicModule(Identifier.Random());
var typeBuilder = module.DefineType("IteratorDebug");
typeBuilder.DefineIterator("TestIterator", MethodAttributes.Public | MethodAttributes.Static, testIteratorGenerator, typeof(int), additiveAParam, additiveBParam, inputNumbersParam);
typeBuilder.CreateType();
persistedAssembly.Save("IteratorDebugPersisted.dll");

/*var testIterator = testIteratorGenerator.CompileIterator<Func<int, int, IEnumerable<int>, IEnumerable<int>>>
    (typeof(int), additiveAParam, additiveBParam, inputNumbersParam);

var myNumbers = new int[10];
for (int i = 0; i < myNumbers.Length; i++)
    myNumbers[i] = i;

var enumerable = testIterator(10, -10, myNumbers);
var enumerator = enumerable.GetEnumerator();
while (enumerator.MoveNext())
    Console.WriteLine(enumerator.Current);*/