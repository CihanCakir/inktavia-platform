using System;

namespace Aizen.Core.Common.Abstraction.Util
{
    public static class AizenDateTime
    {
        private static DateTime? _customDate;

        public static DateTime Now => _customDate ?? DateTime.Now;

        public static DateTime UtcNow => _customDate ?? DateTime.UtcNow;

        public static DateTime Today => _customDate ?? DateTime.Today;

        public static void Set(DateTime customDate) => _customDate = customDate;

        public static void Reset() => _customDate = null;
    }
}
