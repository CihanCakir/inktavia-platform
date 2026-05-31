namespace Aizen.Modules.ReferenceData.Abstraction.Request.LookupGroup;

public sealed class MoveLookupGroupRequest
{
    public long LookupGroupId { get; set; }
    public long? NewParentLookupGroupId { get; set; }
}
