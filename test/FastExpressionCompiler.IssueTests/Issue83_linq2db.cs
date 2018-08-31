﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using NUnit.Framework;

#pragma warning disable IDE1006 // Naming Styles for linq2db

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
[TestFixture]
    public class Issue83_linq2db
    {
        [Test]
        public void String_to_number_conversion_using_convert_with_method()
        {
            var from = typeof(string);
            var to = typeof(int);

            var p = Parameter(from, "p");

            var body = Condition(
                NotEqual(p, Constant(null, from)),
                Convert(p, to, to.GetTypeInfo().DeclaredMethods.First(x=> x.Name == "Parse" && x.GetParameters().Length==1 && x.GetParameters()[0].ParameterType == from)),
                Constant(0));

            var expr = Expression.Lambda<Func<string, int>>(body, p);

            var compiled = expr.CompileFast();

            Assert.AreEqual(10, compiled("10"));
        }

        interface IQueryRunner
        {
            IDataContext DataContext { get; }
            Expression Expression { get; }
            object[] Parameters { get; }
        }

        interface IDataContext
        {
        }

        public interface IDataRecord
        {
            Guid GetGuid(int i);
            int GetInt32(int i);
            object GetValue(int i);
            bool IsDBNull(int i);
        }

        interface IDataReader : IDataRecord
        {
        }

        class DataContext : IDataContext
        {
        }

        class QueryRunner : IQueryRunner
        {
            IDataContext IQueryRunner.DataContext => new DataContext();

            Expression IQueryRunner.Expression => Expression.Constant(null);

            object[] IQueryRunner.Parameters => Array.Empty<object>();
        }

        class SQLiteDataReader : IDataReader
        {
            private readonly bool _dbNull;

            public SQLiteDataReader(bool dbNull)
            {
                _dbNull = dbNull;
            }

            public bool IsDBNull(int idx)
            {
                return _dbNull;
            }

            public int GetInt32(int idx)
            {
                return 1;
            }

            public Guid GetGuid(int idx)
            {
                return new Guid("ef129165-6ffe-4df9-bb6b-bb16e413c883");
            }

            public object GetValue(int idx)
            {
                return MyDbNull.Value;
            }
        }

        class MyDbNull
        {
            public static MyDbNull Value => new MyDbNull();

        }

        public enum TypeCodeEnum
        {
            Base,
            A,
            A1,
            A2,
        }

        class InheritanceTests
        {
            

            public abstract class InheritanceBase
            {
                public Guid GuidValue { get; set; }

                public virtual TypeCodeEnum TypeCode
                {
                    get { return TypeCodeEnum.Base; }
                }
            }

            public abstract class InheritanceA : InheritanceBase
            {
                public List<InheritanceB> Bs { get; set; }

                public override TypeCodeEnum TypeCode
                {
                    get { return TypeCodeEnum.A; }
                }
            }

            public class InheritanceB : InheritanceBase
            {
            }

            public class InheritanceA2 : InheritanceA
            {
                public override TypeCodeEnum TypeCode
                {
                    get { return TypeCodeEnum.A2; }
                }
            }

            public class InheritanceA1 : InheritanceA
            {
                public override TypeCodeEnum TypeCode
                {
                    get { return TypeCodeEnum.A1; }
                }
            }
        }

        class TableBuilder
        {
            public class TableContext
            {
                public static object OnEntityCreated(IDataContext context, object entity)
                {
                    return entity;
                }
            }
        }

        public enum Test
        {
            One,
            Two
        }

        [Test][Ignore("fixme in LE")]
        public void linq2db_NullReferenceException()
        {
            var a1 = Parameter(typeof(IQueryRunner), "qr");
            var a2 = Parameter(typeof(IDataContext), "dctx");
            var a3 = Parameter(typeof(IDataReader), "rd");
            var a4 = Parameter(typeof(Expression), "expr");
            var a5 = Parameter(typeof(object[]), "ps");

            var ldr = Variable(typeof(SQLiteDataReader), "ldr");
            var mapperBody = Block(
                new[] { ldr },
                Assign(ldr, Convert(a3, typeof(SQLiteDataReader)) ),
                Condition(
                    Equal(
                        Condition(
                            Call(ldr, nameof(SQLiteDataReader.IsDBNull), null, Constant(0)),
                            Constant(TypeCodeEnum.Base),
                            Convert(
                                Call(ldr, nameof(SQLiteDataReader.GetInt32), null, Constant(0)),
                                typeof(TypeCodeEnum))),
                        Constant(TypeCodeEnum.A1)),
                    Convert(
                        Convert(
                            Call(
                                typeof(TableBuilder.TableContext).GetMethod(nameof(TableBuilder.TableContext.OnEntityCreated)),
                                a2,
                                MemberInit(
                                    New(typeof(InheritanceTests.InheritanceA1)),
                                    Bind(
                                        typeof(InheritanceTests.InheritanceA1).GetProperty("GuidValue"),
                                        Condition(
                                            Call(ldr, nameof(SQLiteDataReader.IsDBNull), null, Constant(1)),
                                            Constant(Guid.Empty),
                                            Call(ldr, nameof(SQLiteDataReader.GetGuid), null, Constant(1))))
                                    )
                                ),
                            typeof(InheritanceTests.InheritanceA1)),
                        typeof(InheritanceTests.InheritanceA)),
                    Convert(
                        Convert(
                            Call(
                                typeof(TableBuilder.TableContext).GetMethod(nameof(TableBuilder.TableContext.OnEntityCreated)),
                                a2,
                                MemberInit(
                                    New(typeof(InheritanceTests.InheritanceA2)),
                                    Bind(
                                        typeof(InheritanceTests.InheritanceA2).GetProperty("GuidValue"),
                                        Condition(
                                            Call(ldr, nameof(SQLiteDataReader.IsDBNull), null, Constant(1)),
                                            Constant(Guid.Empty),
                                            Call(ldr, nameof(SQLiteDataReader.GetGuid), null, Constant(1))))
                                    )
                                ),
                            typeof(InheritanceTests.InheritanceA2)),
                        typeof(InheritanceTests.InheritanceA))));

            var mapper = Lambda<Func<IQueryRunner, IDataContext, IDataReader, Expression, object[], InheritanceTests.InheritanceA>>(mapperBody, a1, a2, a3, a4, a5);

            var p1 = Parameter(typeof(IQueryRunner), "qr");
            var p2 = Parameter(typeof(IDataReader), "dr");


            var body = Invoke(
                mapper,
                p1,
                Property(p1, nameof(IQueryRunner.DataContext)),
                p2,
                Property(p1, nameof(IQueryRunner.Expression)),
                Property(p1, nameof(IQueryRunner.Parameters)));

            var lambda = Lambda<Func<IQueryRunner, IDataReader, InheritanceTests.InheritanceA>>(body, p1, p2);


            var compiled = lambda.CompileFast();
           
            // NRE during execution of nested function
            var res = compiled(new QueryRunner(), new SQLiteDataReader(false));

            Assert.IsNotNull(res);
            Assert.AreEqual(TypeCodeEnum.A2, res.TypeCode);
            Assert.AreEqual(new Guid("ef129165-6ffe-4df9-bb6b-bb16e413c883"), res.GuidValue);
        }

        enum Enum2
        {
            Value1 = 1,
            Value2 = 2,
        }

        enum Enum3
        {
            Value1 = 1,
            Value2 = 2,
        }

        [Test]
        public void Enum_to_enum_conversion()
        {
            var from = typeof(Enum3);
            var to = typeof(Enum2);

            var p = Parameter(from, "p");

            var body = Convert(
                Convert(p, typeof(int)),
                to);

            var expr = Lambda<Func<Enum3, Enum2>>(body, p);

            var compiled = expr.CompileFast();

            Assert.AreEqual(Enum2.Value2, compiled(Enum3.Value2));
        }

        [Test]
        public void AccessViolationException_on_nullable_char_convert_to_object()
        {
            var body = Convert(
                Constant(' ', typeof(char?)),
                typeof(object));

            var expr = Lambda<Func<object>>(body);

            var compiled = expr.CompileFast();

            Assert.AreEqual(' ', compiled());
        }

        public static int CheckNullValue(IDataRecord reader, object context)
        {
            if (reader.IsDBNull(0))
                throw new InvalidOperationException(
                    $"Function {context} returns non-nullable value, but result is NULL. Use nullable version of the function instead.");
            return 0;
        }

#if !LIGHT_EXPRESSION
        public static object ConvertDefault(object value, Type conversionType)
        {
            try
            {
                return System.Convert.ChangeType(value, conversionType
#if !NETSTANDARD1_6
                    , Thread.CurrentThread.CurrentCulture
#endif
                    );
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot convert value '{value}' to type '{conversionType.FullName}'", ex);
            }
        }

        [Test]
        public void linq2db_InvalidProgramException()
        {
            var a1 = Parameter(typeof(IQueryRunner), "qr");
            var a2 = Parameter(typeof(IDataContext), "dctx");
            var a3 = Parameter(typeof(IDataReader), "rd");
            var a4 = Parameter(typeof(Expression), "expr");
            var a5 = Parameter(typeof(object[]), "ps");

            var ldr = Variable(typeof(SQLiteDataReader), "ldr");
            var mapperBody = Block(
                new[] { ldr },
                Assign(ldr, Convert(a3, typeof(SQLiteDataReader))),
                Convert(
                    Block(
                        Call(GetType().GetMethod(nameof(CheckNullValue)), a3, Constant("Average")),
                        Condition(
                            Call(ldr, nameof(SQLiteDataReader.IsDBNull), null, Constant(0)),
                            Constant(0d),
                            Convert(
                                Call(
                                    GetType().GetMethod(nameof(ConvertDefault)),
                                    Convert(
                                        Convert(
                                            Call(ldr, nameof(SQLiteDataReader.GetValue), null, Constant(0)),
                                            typeof(object)),
                                        typeof(object)),
                                    Constant(typeof(double))),
                                typeof(double)))),
                    typeof(object)));

            var mapper = Lambda<Func<IQueryRunner, IDataContext, IDataReader, Expression, object[], object>>(mapperBody, a1, a2, a3, a4, a5);

            var p1 = Parameter(typeof(IQueryRunner), "qr");
            var p2 = Parameter(typeof(IDataReader), "dr");


            var body = Invoke(
                mapper,
                p1,
                Property(p1, nameof(IQueryRunner.DataContext)),
                p2,
                Property(p1, nameof(IQueryRunner.Expression)),
                Property(p1, nameof(IQueryRunner.Parameters)));

            var lambda = Lambda<Func<IQueryRunner, IDataReader, object>>(body, p1, p2);


            var compiled = lambda.CompileFast();

            Assert.Throws<InvalidOperationException>(() => compiled(new QueryRunner(), new SQLiteDataReader(true)));
        }

        [Test]
        public void TestDoubleConvertSupported()
        {
            var lambda = Lambda<Func<object>>(Convert(
                Convert(
                    Constant("aa"),
                    typeof(object)),
                typeof(object)));


            var compiled1 = lambda.Compile();
            var compiled2 = lambda.CompileFast(true);

            Assert.AreEqual("aa", compiled1());
            Assert.AreEqual("aa", compiled2());
        }

        [Test]
        public void TestFirstLambda()
        {
            var a1 = Parameter(typeof(IQueryRunner), "qr");
            var a2 = Parameter(typeof(IDataContext), "dctx");
            var a3 = Parameter(typeof(IDataReader), "rd");
            var a4 = Parameter(typeof(Expression), "expr");
            var a5 = Parameter(typeof(object[]), "ps");

            var ldr = Variable(typeof(SQLiteDataReader), "ldr");
            var mapperBody = Block(
                new[] { ldr },
                Assign(ldr, Convert(a3, typeof(SQLiteDataReader))),
                Convert(
                    Block(
                        Call(GetType().GetMethod(nameof(CheckNullValue)), a3, Constant("Average")),
                        Condition(
                            Call(ldr, nameof(SQLiteDataReader.IsDBNull), null, Constant(0)),
                            Constant(0d),
                            Convert(
                                Call(
                                    GetType().GetMethod(nameof(ConvertDefault)),
                                    Convert(
                                        Convert(
                                            Call(ldr, nameof(SQLiteDataReader.GetValue), null, Constant(0)),
                                            typeof(object)),
                                        typeof(object)),
                                    Constant(typeof(double))),
                                typeof(double)))),
                    typeof(object)));

            var mapper = Lambda<Func<IQueryRunner, IDataContext, IDataReader, Expression, object[], object>>(mapperBody, a1, a2, a3, a4, a5);

            var compiled1 = mapper.Compile();
            var compiled2 = mapper.CompileFast(true);

            Assert.Throws<NullReferenceException>(() => compiled1(null, null, null, null, null));
            Assert.Throws<NullReferenceException>(() => compiled2(null, null, null, null, null));
        }
#endif
    }
}
