namespace Aizen.Core.Data.Mongo.Attributes
{
    /// <summary>
    /// Use to customize collection information.
    /// </summary>
    public class AizenCollectionInfoAttribute : System.Attribute
    {
        /// <value>Name of the collection</value>
        public string CollectionName { get; set; }
    }
}
