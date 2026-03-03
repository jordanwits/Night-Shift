using System.Collections.Generic;
using UnityEngine;

namespace NightShift.Generation
{
    /// <summary>
    /// Debug utility to validate that floor pieces from different MallSections do not overlap.
    /// Shift+F8 toggles validation; when enabled, runs after each mall generation.
    /// </summary>
    public static class FloorOverlapValidator
    {
        public static bool ValidationEnabled { get; set; }

        /// <summary>Run overlap check on all FloorCore pieces. Log warnings if any overlaps detected.</summary>
        public static void ValidateIfEnabled(IReadOnlyList<MallSection> sections)
        {
            if (!ValidationEnabled || sections == null || sections.Count == 0)
                return;

            var pieces = new List<(MallSection section, string pieceName, Bounds bounds)>();
            foreach (var s in sections)
            {
                if (s == null) continue;
                var floor = s.transform.Find("Floor");
                if (floor == null) continue;

                foreach (Transform child in floor)
                {
                    if (child == null || !child.name.StartsWith("FloorCore")) continue;
                    var r = child.GetComponent<Renderer>();
                    if (r == null) continue;

                    pieces.Add((s, child.name, r.bounds));
                }
            }

            int overlaps = 0;
            for (int i = 0; i < pieces.Count; i++)
            {
                for (int j = i + 1; j < pieces.Count; j++)
                {
                    var a = pieces[i];
                    var b = pieces[j];
                    if (a.section == b.section) continue;

                    if (a.bounds.Intersects(b.bounds))
                    {
                        overlaps++;
                        Debug.LogWarning($"[FLOOR OVERLAP] SectionA={a.section.name} SectionB={b.section.name} " +
                            $"PieceA={a.pieceName} PieceB={b.pieceName} " +
                            $"BoundsA={a.bounds} BoundsB={b.bounds}");
                    }
                }
            }

            if (overlaps > 0)
                Debug.LogWarning($"[FLOOR OVERLAP] Total overlaps: {overlaps}");
            else
                Debug.Log($"[FloorOverlapValidator] No floor overlaps detected ({pieces.Count} pieces checked).");
        }
    }
}
