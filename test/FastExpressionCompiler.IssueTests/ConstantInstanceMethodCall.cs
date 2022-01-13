using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Reflection;
using System;
using System.Linq.Expressions;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class AugheyIssue : ITest
    {
        public int Run()
        {
            Test_instance_call_without_ifthen();
            Test_instance_call();
            return 2;
        }

        [Test]
        public void Test_instance_call()
        {
            var expr = CreateExpression();

            var fsys = expr.CompileSys();

            fsys.PrintIL("sys");

            var f = expr.CompileFast();

            f.PrintIL("fast");

            Assert.IsNotNull(f);

            Assert.AreEqual(314, f(true));

            GenerateAssemblyManually(expr);
        }

         [Test]
        public void Test_instance_call_without_ifthen()
        {
            var expr = CreateNonIfThenExpression();

            var f = expr.CompileFast(true);

            Assert.IsNotNull(f);

            Assert.AreEqual(8675309, f(true));

            GenerateAssemblyManually(expr);
        }

        private static void GenerateAssemblyManually(Expression<Func<bool,int>> expr)
        {
            var dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("temp"), AssemblyBuilderAccess.Run);
            var dynamicModule = dynamicAssembly.DefineDynamicModule("temp_module");
            var dynamicType = dynamicModule.DefineType("temp_type");
            // create a dynamic method
            var dynamicMethod = dynamicType.DefineMethod("temp_method", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new[]{typeof(bool)});
            // get the IL generator and put the code there
            var il = dynamicMethod.GetILGenerator();

            expr.CompileFastToIL(il);

            dynamicType.CreateType();
        }


        class TestMethods
        {
            public int count = 0;
            public TestMethods(int initialcount)
            {
                count = initialcount;
            }
            public int InstanceMethod()
            {
                return count;
            }
        }

        private Expression<Func<bool,int>> CreateExpression()
        {
            var instance = new TestMethods(314);
            var call = Expression.Call(Expression.Constant(instance), typeof(TestMethods).GetMethod("InstanceMethod")!);

            var branchparam = Expression.Parameter(typeof(bool), "branch");
            var localint = Expression.Variable(typeof(int), "ret");
            var setlocaltocall = Expression.Assign(localint, call);
            var program = Expression.Block(
                new[] { localint },
                Expression.Assign(localint, Expression.Constant(0)),
                Expression.IfThen(branchparam, setlocaltocall),
                Label(Label(typeof(int)), localint)
            );

            var fe = Lambda<Func<bool,int>>(program, branchparam);

            return fe;
        }

          private Expression<Func<bool, int>> CreateNonIfThenExpression()
        {
            var instance = new TestMethods(8675309);
            var call = Expression.Call(Expression.Constant(instance), typeof(TestMethods).GetMethod("InstanceMethod")!);

            var localint = Expression.Variable(typeof(int), "ret");
            var branchparam = Expression.Parameter(typeof(bool), "branch");
            var setlocaltocall = Expression.Assign(localint, call);
            var program = Expression.Block(
                new[] { localint },
                Expression.Assign(localint, Expression.Constant(0)),
                setlocaltocall,
                Label(Label(typeof(int)), localint)
            );

            var fe = Lambda<Func<bool,int>>(program, branchparam);

            return fe;
        }

    }
}
