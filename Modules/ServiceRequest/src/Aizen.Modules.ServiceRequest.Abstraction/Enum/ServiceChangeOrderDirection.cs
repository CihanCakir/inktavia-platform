namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

/// <summary>
/// BE-S11b — whether an applied change order <b>adds</b> to the customer's effective total (extra work → a new incremental
/// economics snapshot + escrow/split via P8/P9) or <b>reduces</b> it (removed work → a P10 refund/allocation against the
/// original transaction). The same line inputs + the same offer calculation produce the amount either way; the direction
/// picks the rail. Never mutates the accepted snapshot.
/// </summary>
public enum ServiceChangeOrderDirection
{
    /// <summary>Extra work — increases the effective total (new snapshot + incremental escrow via P8/P9).</summary>
    Increase = 1,
    /// <summary>Removed/reduced work — issues a P10 refund of the delta against the original escrow (no new snapshot).</summary>
    Decrease = 2,
}
