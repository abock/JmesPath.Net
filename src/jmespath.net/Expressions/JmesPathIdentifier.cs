using Newtonsoft.Json.Linq;

namespace DevLab.JmesPath.Expressions
{
    public class JmesPathIdentifier : JmesPathExpression
    {
        private readonly string name_;

        public JmesPathIdentifier(string name)
        {
            name_ = name;
        }

        public string Name => name_;

        protected override JmesPathArgument Transform(JToken json)
        {
            var jsonObject = json as JObject;
            return jsonObject?[name_];
        }

        public override JmesPathExpression Accept(Interop.ITransformVisitor visitor)
            => visitor.Visit(this);
    }
}