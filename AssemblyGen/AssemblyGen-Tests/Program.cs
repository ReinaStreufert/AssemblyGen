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

var testIterator = testIteratorGenerator.CompileIterator<Func<int, int, IEnumerable<int>, IEnumerable<int>>>
    (typeof(int), additiveAParam, additiveBParam, inputNumbersParam);

var myNumbers = new int[10];
for (int i = 0; i < myNumbers.Length; i++)
    myNumbers[i] = i;

foreach (var n in testIterator(10, -10, myNumbers))
    Console.WriteLine(n);
Console.ReadLine();