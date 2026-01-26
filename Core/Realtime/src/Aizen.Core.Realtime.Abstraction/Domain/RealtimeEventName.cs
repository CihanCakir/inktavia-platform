namespace Aizen.Core.Realtime.Abstraction.Domain;
/// <summary>
/// Basit event name wrapper: Domain (e.g. "activity") ve Name (e.g. "activity.created").
/// </summary>
public sealed record RealtimeEventName(string Domain, string Name);