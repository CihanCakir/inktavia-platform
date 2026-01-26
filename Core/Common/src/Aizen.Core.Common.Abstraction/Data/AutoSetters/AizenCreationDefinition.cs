namespace Aizen.Core.Common.Abstraction.Data.AutoSetters
{
    public sealed class AizenCreationDefinition
    {
        public AizenCreationDefinition()
        {
        }


        public AizenCreationDefinition(
            AizenCreationDefinitionItem createdOn,
            AizenCreationDefinitionItem createdBy)
        {
            this.CreatedOn = createdOn;
            this.CreatedBy = createdBy;
        }

        public AizenCreationDefinitionItem CreatedOn { get; init; }
        public AizenCreationDefinitionItem CreatedBy { get; init; }
    }

    public sealed class AizenCreationDefinitionItem
    {
        public AizenCreationDefinitionItem(
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
