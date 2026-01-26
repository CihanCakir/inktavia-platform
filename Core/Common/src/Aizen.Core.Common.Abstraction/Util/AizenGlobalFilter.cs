namespace Aizen.Core.Common.Abstraction.Util
{
    public class AizenGlobalFilter
    {
        public AizenGlobalFilter(
            string name,
            AizenGlobalFilterExpressionType type,
            AizenGlobalFilterOperationType operationType,
            object value)
        {
            this.Type = type;
            this.Name = name;
            this.OperationType = operationType;
            this.Value = value;
        }
        public string Name { get; set; }

        public AizenGlobalFilterExpressionType Type { get; set; }

        public AizenGlobalFilterOperationType OperationType { get; set; }

        public object Value { get; set; }
    }
}
