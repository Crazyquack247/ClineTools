// Modules/Stacker/Decoders/StackerDecoders.cs
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using ClineTools.Modules.Stacker.Sin;

namespace ClineTools.Modules.Stacker.Decoders
{
    /// <summary>
    /// Single entry-point decoder that understands multiple SIN families.
    /// Uses patterns defined in SinFormats. C# 8 compatible.
    /// 
    /// You can add or change formats in SinFormats.cs without touching this file.
    /// </summary>
    public sealed class StackerDecoders : ISinDecoder
    {
        // Not used when we extract type from the returned card, but interface demands it.
        public string TypeName { get { return "StackerMulti"; } }

        public bool CanHandle(string normalizedSin)
        {
            return
                // Inserts (industry)
                SinFormats.RxInsertTriplet.IsMatch(normalizedSin) ||
                SinFormats.RxInsertSix.IsMatch(normalizedSin) ||
                // Insert Screw
                SinFormats.RxInsertScrew.IsMatch(normalizedSin) ||
                // Clamp / LockPin / Shim
                SinFormats.RxClamp.IsMatch(normalizedSin) ||
                SinFormats.RxLockPin.IsMatch(normalizedSin) ||
                SinFormats.RxShim.IsMatch(normalizedSin);
        }

        public object DecodeToCard(string normalizedSin)
        {
            // Priority: specific patterns first
            if (SinFormats.RxInsertTriplet.IsMatch(normalizedSin)) return DecodeInsertTriplet(normalizedSin);
            if (SinFormats.RxInsertSix.IsMatch(normalizedSin)) return DecodeInsertSix(normalizedSin);
            if (SinFormats.RxInsertScrew.IsMatch(normalizedSin)) return DecodeInsertScrew(normalizedSin);
            if (SinFormats.RxClamp.IsMatch(normalizedSin)) return DecodeClamp(normalizedSin);
            if (SinFormats.RxLockPin.IsMatch(normalizedSin)) return DecodeLockPin(normalizedSin);
            if (SinFormats.RxShim.IsMatch(normalizedSin)) return DecodeShim(normalizedSin);

            throw new ArgumentException("No decoder recognized this SIN.");
        }

        // ---------------------------
        // #region INSERT (industry)
        // ---------------------------

        #region INSERT

        private static object DecodeInsertTriplet(string normalizedSin)
        {
            // e.g., CNMG-332
            var m = SinFormats.RxInsertTriplet.Match(normalizedSin);
            if (!m.Success) throw new ArgumentException("Invalid insert SIN (triplet).");
            string shape = m.Groups[1].Value;   // e.g., CNMG, CCMT, etc.
            string digits = m.Groups[2].Value;  // e.g., 332

            double icMm = TripletIcToMm(digits[0]);
            double thMm = TripletThToMm(digits[1]);
            double holeMm = GuessHoleMm(shape, icMm);
            string rake = InferRakeFromShape(shape); // "neutral" (N) or "positive"

            return new
            {
                type = "Insert",
                sin = normalizedSin,
                ic = new { value = icMm, unit = "mm" },
                thickness = new { value = thMm, unit = "mm" },
                rake = rake,
                hole_d = new { value = holeMm, unit = "mm" }
            };
        }

        private static object DecodeInsertSix(string normalizedSin)
        {
            // e.g., CCMT 120408 => 12 04 08; we use IC and TH only
            var m = SinFormats.RxInsertSix.Match(normalizedSin);
            if (!m.Success) throw new ArgumentException("Invalid insert SIN (six digits).");
            string shape = m.Groups[1].Value;
            string six = m.Groups[2].Value;

            int a = int.Parse(six.Substring(0, 2), CultureInfo.InvariantCulture);
            int b = int.Parse(six.Substring(2, 2), CultureInfo.InvariantCulture);
            // int c = int.Parse(six.Substring(4, 2), CultureInfo.InvariantCulture); // corner radius (unused)

            double icMm = a;            // simple ISO-ish mapping: "12" => 12 mm
            double thMm = b / 10.0;     // "04" => 4.0 mm
            double holeMm = GuessHoleMm(shape, icMm);
            string rake = InferRakeFromShape(shape);

            return new
            {
                type = "Insert",
                sin = normalizedSin,
                ic = new { value = icMm, unit = "mm" },
                thickness = new { value = thMm, unit = "mm" },
                rake = rake,
                hole_d = new { value = holeMm, unit = "mm" }
            };
        }

        // ---- small helpers (tune here or swap for table lookups) ----
        private static double TripletIcToMm(char code)
        {
            // Adjust to your shop’s convention.
            switch (code)
            {
                case '2': return 9.525;   // 3/8"
                case '3': return 12.7;    // 1/2"
                case '4': return 15.875;  // 5/8"
                case '5': return 19.05;   // 3/4"
                default: return 12.7;
            }
        }

        private static double TripletThToMm(char code)
        {
            // Tune to your library
            switch (code)
            {
                case '1': return 2.38;
                case '2': return 3.18;
                case '3': return 3.97;
                case '4': return 4.76;
                case '5': return 6.35;
                default: return 3.18;
            }
        }

        private static string InferRakeFromShape(string shape)
        {
            // 2nd letter often indicates relief/rake ("N" ≈ 0° neutral)
            if (!string.IsNullOrEmpty(shape) && shape.Length >= 2 && shape[1] == 'N') return "neutral";
            return "positive";
        }

        private static double GuessHoleMm(string shape, double icMm)
        {
            // Safe placeholder heuristic
            if (icMm <= 10) return 3.4;
            if (icMm <= 12.7) return 4.4;
            if (icMm <= 15.9) return 5.2;
            return 6.4;
        }

        #endregion

        // ---------------------------
        // #region INSERT SCREW (IS-tt-EEE(-md)?)
        // ---------------------------

        #region INSERT_SCREW

        private static object DecodeInsertScrew(string normalizedSin)
        {
            var m = SinFormats.RxInsertScrew.Match(normalizedSin);
            if (!m.Success) throw new ArgumentException("Invalid insert screw SIN.");

            string th = m.Groups["th"].Value;            // 2-digit thread code
            int dep = int.Parse(m.Groups["dep"].Value);  // 3-digit effective depth
            bool hasSh = m.Groups["sh"].Success;
            string sh = hasSh ? m.Groups["sh"].Value : null;

            var thread = ThreadLookup(th);                // { system, label, major_d, pitch|tpi }
            string unit = thread.system == "metric" ? "mm" : "inh";  // 'inh' = hundredths-inch ticks
            double depthPretty = thread.system == "metric" ? dep : dep / 100.0;

            double? shank = null;
            if (!string.IsNullOrEmpty(sh))
            {
                var shSpec = ShankLookup(sh, thread.system); // (value, unit)
                shank = shSpec.value;
            }

            return new
            {
                type = "InsertScrew",
                sin = normalizedSin,
                thread = thread,
                effective_depth = new { value = depthPretty, unit = unit },
                max_shank_d = shank.HasValue ? new { value = shank.Value, unit = unit } : null
            };
        }

        private static dynamic ThreadLookup(string code)
        {
            // Minimal built-in mapping. Move to external JSON later if you want.
            switch (code)
            {
                case "27": return new { system = "metric", label = "M6×1.0", major_d = 6.0, pitch = 1.0 };
                case "28": return new { system = "metric", label = "M8×1.25", major_d = 8.0, pitch = 1.25 };
                case "41": return new { system = "inch", label = "1/4-28 UNF", major_d = 0.25, tpi = 28 };
                case "42": return new { system = "inch", label = "5/16-24 UNF", major_d = 0.3125, tpi = 24 };
                default: return new { system = "metric", label = "M??×?", major_d = 0.0, pitch = 0.0 };
            }
        }

        private static (double value, string unit) ShankLookup(string code, string system)
        {
            if (system == "metric")
            {
                switch (code)
                {
                    case "50": return (5.0, "mm");
                    case "55": return (5.5, "mm");
                    case "60": return (6.0, "mm");
                    default: return (5.0, "mm");
                }
            }
            // Decimals for inches (pretty value)
            switch (code)
            {
                case "25": return (0.25, "in");
                case "31": return (0.3125, "in");
                case "37": return (0.375, "in");
                default: return (0.25, "in");
            }
        }

        #endregion

        // ---------------------------
        // #region CLAMP (CL-ix)
        // ---------------------------

        #region CLAMP

        private static object DecodeClamp(string normalizedSin)
        {
            var m = SinFormats.RxClamp.Match(normalizedSin);
            if (!m.Success) throw new ArgumentException("Invalid clamp SIN.");
            int ix = int.Parse(m.Groups["ix"].Value);
            string unit = (ix % 2 == 0) ? "mm" : "inh";

            return new
            {
                type = "Clamp",
                sin = normalizedSin,
                iface = ix,
                unit = unit,
                top = new { kind = "INTO", index = ix, unit = unit, capacity = 0, thickness = 0, mateRef = "STACKER_TOP" },
                bottom = new { kind = "ONTO", index = ix, unit = unit, capacity = 0, thickness = 0, mateRef = "STACKER_BOTTOM" }
            };
        }

        #endregion

        // ---------------------------
        // #region LOCK PIN (LP-ix-len)
        // ---------------------------

        #region LOCK_PIN

        private static object DecodeLockPin(string normalizedSin)
        {
            var m = SinFormats.RxLockPin.Match(normalizedSin);
            if (!m.Success) throw new ArgumentException("Invalid lock pin SIN.");
            int ix = int.Parse(m.Groups["ix"].Value);
            int len = int.Parse(m.Groups["len"].Value);
            string unit = (ix % 2 == 0) ? "mm" : "inh";
            double pretty = unit == "mm" ? len : len / 100.0;

            return new
            {
                type = "LockPin",
                sin = normalizedSin,
                iface = ix,
                unit = unit,
                top = new { kind = "ONTO", index = ix, unit = unit, capacity = 0, thickness = 0, mateRef = "STACKER_TOP" },
                bottom = new { kind = "INTO", index = ix, unit = unit, capacity = 0, thickness = 0, mateRef = "STACKER_BOTTOM" },
                pin_len = new { value = pretty, unit = unit }
            };
        }

        #endregion

        // ---------------------------
        // #region SHIM (SH-ix-thk)
        // ---------------------------

        #region SHIM

        private static object DecodeShim(string normalizedSin)
        {
            var m = SinFormats.RxShim.Match(normalizedSin);
            if (!m.Success) throw new ArgumentException("Invalid shim SIN.");
            int ix = int.Parse(m.Groups["ix"].Value);
            int thk = int.Parse(m.Groups["thk"].Value);
            string unit = (ix % 2 == 0) ? "mm" : "inh";
            double pretty = unit == "mm" ? thk : thk / 100.0;

            return new
            {
                type = "Shim",
                sin = normalizedSin,
                iface = ix,
                unit = unit,
                top = new { kind = "ONTO", index = ix, unit = unit, capacity = 0, thickness = 0, mateRef = "STACKER_TOP" },
                bottom = new { kind = "ONTO", index = ix, unit = unit, capacity = 0, thickness = 0, mateRef = "STACKER_BOTTOM" },
                thickness = new { value = pretty, unit = unit }
            };
        }

        #endregion
    }
}