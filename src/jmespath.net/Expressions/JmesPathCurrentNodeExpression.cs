﻿using System;
using Newtonsoft.Json.Linq;

namespace DevLab.JmesPath.Expressions
{
    public class JmesPathCurrentNodeExpression : JmesPathExpression
    {
        protected override JmesPathArgument Transform(JToken json)
        {
            return json;
        }

        public override JmesPathExpression Accept(Interop.ITransformVisitor visitor)
            => visitor.Visit(this);
    }
}