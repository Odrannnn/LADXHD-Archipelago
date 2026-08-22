using System;
using System.Globalization;

namespace ProjectZ.InGame.Archipelago
{
    public static class ArchipelagoLocationKey
    {
        public static string Script(string scriptKey, int actionIndex)
        {
            if (string.IsNullOrWhiteSpace(scriptKey))
                throw new ArgumentException("A script key is required.", nameof(scriptKey));
            if (actionIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(actionIndex));

            return "script:" + Uri.EscapeDataString(scriptKey) + ":" +
                   actionIndex.ToString(CultureInfo.InvariantCulture);
        }

        public static string Shop(int price)
        {
            if (price < 0)
                throw new ArgumentOutOfRangeException(nameof(price));

            return "shop:" + price.ToString(CultureInfo.InvariantCulture);
        }

        public static string Event(string eventKey)
        {
            if (string.IsNullOrWhiteSpace(eventKey))
                throw new ArgumentException("An event key is required.", nameof(eventKey));

            return "event:" + Uri.EscapeDataString(eventKey);
        }

        public static string PersistentCheck(long locationId)
        {
            return "ap_location_" + locationId.ToString(CultureInfo.InvariantCulture);
        }
    }
}
