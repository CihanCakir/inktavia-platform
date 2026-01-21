using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Aizen.Core.Common.Abstraction.Util;
using Aizen.Core.Common.Abstraction.Data.AutoSetters;

namespace Aizen.Core.Data.Mongo.Document
{
    public abstract class AizenDocumentBase
    {
        public virtual List<AizenGlobalFilter> GetGlobalFilters()
        {
            return new()
            {
                new AizenGlobalFilter(
                    nameof(this.IsDeleted),
                    AizenGlobalFilterExpressionType.Equal,
                    AizenGlobalFilterOperationType.And,
                    false)
            };
        }

        [BsonId]
        [BsonElement("_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("isDeleted")]
        public bool IsDeleted { get; set; }
    }

    public abstract class AizenDefinitionDocumentBase : AizenDocumentBase
    {
        public virtual AizenCreationDefinition GetCreationDefinition()
        {
            return new AizenCreationDefinition()
            {
                CreatedBy = new AizenCreationDefinitionItem(nameof(this.CreatedBy)),
                CreatedOn = new AizenCreationDefinitionItem(nameof(this.CreatedOn))
            };
        }

        public virtual AizenModificationDefinition GetModificationDefinition()
        {
            return new AizenModificationDefinition()
            {
                ModifiedBy = new AizenModificationDefinitionItem(nameof(this.ModifiedBy)),
                ModifiedOn = new AizenModificationDefinitionItem(nameof(this.ModifiedOn))
            };
        }

        [BsonElement("createdBy")]
        public long CreatedBy { get; set; }

        [BsonElement("createdOn")]
        public DateTime CreatedOn { get; set; } = AizenDateTime.Now;

        [BsonElement("modifiedBy")]
        public long ModifiedBy { get; set; }

        [BsonElement("modifiedOn")]
        public DateTime ModifiedOn { get; set; } = AizenDateTime.Now;
    }
}
