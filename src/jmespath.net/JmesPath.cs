﻿using System;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using DevLab.JmesPath.Expressions;
using DevLab.JmesPath.Functions;
using DevLab.JmesPath.Interop;
using DevLab.JmesPath.Utils;

namespace DevLab.JmesPath
{
    public sealed class JmesPath
    {
        private readonly JmesPathFunctionFactory repository_;

        public JmesPath()
        {
            repository_ = new JmesPathFunctionFactory();
            foreach (var name in JmesPathFunctionFactory.Default.Names)
                repository_.Register(name, JmesPathFunctionFactory.Default[name]);
        }

        public IRegisterFunctions FunctionRepository => repository_;

        public JToken Transform(JToken token, string expression)
        {
            var jmesPath = Parse(expression);
            var result = jmesPath.Transform(token);
            return result.AsJToken();
        }

        public String Transform(string json, string expression)
        {
            var token = JToken.Parse(json);
            var result = Transform(token, expression);
            return result.AsString();
        }

        public sealed class Expression : JmesPathExpression
        {
            public JmesPathExpression InnerExpression { get; }

            internal Expression(JmesPathExpression expression)
            {
                InnerExpression = expression;
            }

            public string Transform(string document)
            {
                var token = JToken.Parse(document);
                var result = Transform(token);
                return result.AsJToken()?.AsString();
            }

            protected override JmesPathArgument Transform(JToken json)
            {
                return InnerExpression.Transform(json);
            }

            public override void Accept(IVisitor visitor)
            {
                base.Accept(visitor);
                InnerExpression.Accept(visitor);
            }

            public override JmesPathExpression Accept(ITransformVisitor visitor)
            {
                var visitedExpression = InnerExpression.Accept(visitor);
                return visitor.Visit(visitedExpression == InnerExpression
                    ? this
                    : new Expression(visitedExpression));
            }
        }

        public Expression Parse(string expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            return Parse(expression, repository_);
        }

        public Expression Parse(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(Expression));

            return Parse(stream, repository_);
        }

        public static Expression Parse(string expression, IFunctionRepository functionRepository)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            if (functionRepository == null)
                throw new ArgumentNullException(nameof(functionRepository));

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(expression)))
                return Parse(stream, functionRepository);
        }

        public static Expression Parse(Stream stream, IFunctionRepository functionRepository)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (functionRepository == null)
                throw new ArgumentNullException(nameof(functionRepository));

            var scanner = new JmesPathScanner(stream);
            scanner.InitializeLookaheadQueue();

            var analyzer = new JmesPathParser(scanner, functionRepository);
            if (!analyzer.Parse())
            {
                System.Diagnostics.Debug.Assert(false);
                throw new Exception("Error: syntax.");
            }

            // perform post-parsing syntax validation

            var syntax = new SyntaxVisitor();
            analyzer.Expression.Accept(syntax);

            return new Expression(analyzer.Expression);
        }
        private sealed class SyntaxVisitor : IVisitor
        {
            public void Visit(JmesPathExpression expression)
            {
                var projection = expression as JmesPathSliceProjection;
                if (projection?.Step != null && projection.Step.Value == 0)
                    throw new Exception("Error: invalid-value, a slice projection step cannot be 0.");
            }
        }
    }
}
