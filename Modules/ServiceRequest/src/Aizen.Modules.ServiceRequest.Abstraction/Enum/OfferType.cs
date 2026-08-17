namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

/// <summary>
/// BE-S11a — the offer's pricing nature at the offer level (§20.13). <b>Gates flow, never rewrites math.</b>
/// <list type="bullet">
///   <item><b>FixedPrice</b> — today's behaviour, unchanged. The acceptance economics + 8-equality are byte-identical to pre-S11.</item>
///   <item><b>EstimateRange</b> — the offer carries an estimate; the firm amount / any reconciliation rides the S11b change-order flow.</item>
///   <item><b>RequiresInspection</b> — acceptance authorises inspection; the firm/extra work comes as a post-acceptance change order.</item>
///   <item><b>TimeAndMaterials</b> — actuals accrue; billing beyond the initial authorised amount comes via change orders.</item>
/// </list>
/// The non-fixed types do NOT change the acceptance 8-equality — additional economics route through S11b (a new snapshot + split),
/// never by mutating the accepted snapshot. Default (and every pre-S11 row) is <see cref="FixedPrice"/>.
/// </summary>
public enum OfferType
{
    FixedPrice        = 1,
    EstimateRange     = 2,
    RequiresInspection = 3,
    TimeAndMaterials  = 4,
}
