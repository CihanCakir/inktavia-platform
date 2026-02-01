using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aizen.Modules.Payment.Abstraction.Enum
{
    public enum TransactionContextType
    {
        Unknown = 0,
        Activity = 1,
        Product = 2,
        Subscription = 3
    }

    public enum TransactionContextSubType
    {
        None = 0,
        // Activity
        Schedule = 10,
        Invitation = 11,
        // Product
        Variant = 20,
        // Subscription
        PlanTier = 30
    }

}