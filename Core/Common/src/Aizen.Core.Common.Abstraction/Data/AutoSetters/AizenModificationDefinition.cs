namespace Aizen.Core.Common.Abstraction.Data.AutoSetters
{
    public sealed class AizenModificationDefinition
    {
        public AizenModificationDefinition()
        {
        }

        public AizenModificationDefinition(
            AizenModificationDefinitionItem modifiedBy,
            AizenModificationDefinitionItem modifiedOn)
        {
            this.ModifiedBy = modifiedBy;
            this.ModifiedOn = modifiedOn;
        }

        public AizenModificationDefinitionItem ModifiedBy { get; init; }
        public AizenModificationDefinitionItem ModifiedOn { get; init; }
    }

    public class AizenModificationDefinitionItem
    {
        public AizenModificationDefinitionItem(
            string propertyName,
            bool overrideExisting = true)
        {
            this.PropertyName = propertyName;
            this.OverrideExisting = overrideExisting;
        }

        public string PropertyName { get; }
        public bool OverrideExisting { get; }
    }
}
