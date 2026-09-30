using System;
using System.Collections.Generic;

namespace CommNextRedux
{
    internal sealed class AntennaCatalogEntry
    {
        internal string PartName;
        internal string DisplayName;
        internal double RangeMeters;
        internal bool IsRelay;

        internal AntennaCatalogEntry(string partName, string displayName, double rangeMeters, bool isRelay)
        {
            PartName = partName;
            DisplayName = displayName;
            RangeMeters = rangeMeters;
            IsRelay = isRelay;
        }
    }

    internal static class AntennaCatalog
    {
        internal static readonly AntennaCatalogEntry[] Entries =
        {
            new AntennaCatalogEntry("antenna_0v_16", "Communotron 16", 500_000d, false),
            new AntennaCatalogEntry("antenna_0v_16s", "Communotron 16-S", 500_000d, false),
            new AntennaCatalogEntry("antenna_1v_dish_hg5", "HG-5", 5_000_000d, true),
            new AntennaCatalogEntry("antenna_0v_dish_ra-2", "RA-2", 2_000_000_000d, true),
            new AntennaCatalogEntry("antenna_1v_parabolic_dts-m1", "DTS-M1", 2_000_000_000d, false),
            new AntennaCatalogEntry("antenna_0v_dish_ra-15", "RA-15", 15_000_000_000d, true),
            new AntennaCatalogEntry("antenna_1v_dish_hg55", "HG-55", 15_000_000_000d, false),
            new AntennaCatalogEntry("antenna_1v_dish_hg55s", "HG-55S", 15_000_000_000d, false),
            new AntennaCatalogEntry("antenna_1v_dish_ra-100", "RA-100", 100_000_000_000d, true),
            new AntennaCatalogEntry("antenna_1v_dish_88-88", "Communotron 88-88", 100_000_000_000d, false)
        };

        internal static AntennaCatalogEntry FindByPartName(string partName)
        {
            if (string.IsNullOrEmpty(partName))
                return null;

            for (var i = 0; i < Entries.Length; i++)
            {
                if (Entries[i].PartName.Equals(partName, StringComparison.OrdinalIgnoreCase))
                    return Entries[i];
            }

            return null;
        }

        internal static AntennaCatalogEntry FindByDisplayName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                return null;

            for (var i = 0; i < Entries.Length; i++)
            {
                if (Entries[i].DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase))
                    return Entries[i];
            }

            return null;
        }

        internal static double GetRange(string partName, double fallbackRange)
        {
            var entry = FindByPartName(partName);
            return entry == null ? fallbackRange : entry.RangeMeters;
        }

        internal static bool IsRelay(string partName)
        {
            var entry = FindByPartName(partName);
            return entry != null && entry.IsRelay;
        }

        internal static List<string> GetDisplayNames()
        {
            var result = new List<string>(Entries.Length);
            for (var i = 0; i < Entries.Length; i++)
                result.Add(Entries[i].DisplayName);
            return result;
        }
    }
}
