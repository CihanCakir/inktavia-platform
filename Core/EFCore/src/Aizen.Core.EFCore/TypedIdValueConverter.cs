using Aizen.Core.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Aizen.Core.EFCore;

public class TypedIdValueConverter<TTypedIdValue> : ValueConverter<TTypedIdValue, Guid>
    where TTypedIdValue : AizenTypedIdValueBase
{
    public TypedIdValueConverter(ConverterMappingHints mappingHints = null)
        : base(id => id.Value, value => Create(value), mappingHints)
    {
    }

    private static TTypedIdValue Create(Guid id) => Activator.CreateInstance(typeof(TTypedIdValue), id) as TTypedIdValue;
}