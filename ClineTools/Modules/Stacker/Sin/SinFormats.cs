using System.Text.RegularExpressions;

namespace ClineTools.Modules.Stacker.Sin
{
    /// <summary>
    /// All SIN format knobs live here. Change these to adjust separators/patterns—no hunt across files.
    /// </summary>
    public static class SinFormats
    {
        // Common: treat '-', ' ', '_' as valid separators. You can add more if you want.
        public const string SepChars = @"\-\s_";
        public static readonly string SepClass = "[" + SepChars + "]";
        public static readonly string OptSep = "[" + SepChars + "]?";
        public static readonly string ReqSep = "[" + SepChars + "]+";

        /// <summary>
        /// Normalizes user input before matching (trim + collapse multiple spaces/dashes to single dash, uppercases).
        /// You can change the canonical separator below if you prefer space or underscore.
        /// </summary>
        public static string Normalize(string sin)
        {
            if (string.IsNullOrWhiteSpace(sin)) return string.Empty;
            var s = sin.Trim().ToUpperInvariant();
            // collapse any mix of separators to a single dash for canonical storage
            s = Regex.Replace(s, SepClass + "+", "-");
            return s;
        }

        // ----------------------
        // INSERTS (industry codes)
        // Supports: CNMG-332, CCMT 120408, etc.
        // Pattern pieces are centralized so you can adjust easily.
        // ----------------------
        public static readonly string InsertShape = @"[A-Z]{4}";      // e.g., CNMG, CCMT
        public static readonly string InsertDigitsTriplet = @"\d{3}"; // e.g., 332
        public static readonly string InsertDigitsSix = @"\d{6}";     // e.g., 120408

        // Full patterns (either "SHAPE-333" or "SHAPE 123456")
        public static readonly Regex RxInsertTriplet =
            new Regex("^(" + InsertShape + ")" + OptSep + "(" + InsertDigitsTriplet + ")$",
                      RegexOptions.Compiled);

        public static readonly Regex RxInsertSix =
            new Regex("^(" + InsertShape + ")" + ReqSep + "(" + InsertDigitsSix + ")$",
                      RegexOptions.Compiled);

        // ----------------------
        // INSERT SCREWS
        // IS-<TT>-<EEE>(-<MD>)?
        // TT = 2 digits thread code; EEE = 3-digit effective depth; MD = 2-digit shank code (optional)
        // ----------------------
        public static readonly Regex RxInsertScrew =
            new Regex(@"^IS" + ReqSep + @"(?<th>\d{2})" + ReqSep + @"(?<dep>\d{3})(?:" + ReqSep + @"(?<sh>\d{2}))?$",
                      RegexOptions.Compiled);

        // ----------------------
        // CLAMP: CL-<iface>
        // iface: 2–3 digits (even=metric, odd=imperial)
        // ----------------------
        public static readonly Regex RxClamp =
            new Regex(@"^CL" + ReqSep + @"(?<ix>\d{2,3})$",
                      RegexOptions.Compiled);

        // ----------------------
        // LOCK PIN: LP-<iface>-<len3>
        // len3: 3 digits; mm if iface even, hundredths-inch if odd
        // ----------------------
        public static readonly Regex RxLockPin =
            new Regex(@"^LP" + ReqSep + @"(?<ix>\d{2,3})" + ReqSep + @"(?<len>\d{3})$",
                      RegexOptions.Compiled);

        // ----------------------
        // SHIM: SH-<iface>-<thk3>
        // thk3: 3 digits; mm if iface even, hundredths-inch if odd
        // ----------------------
        public static readonly Regex RxShim =
            new Regex(@"^SH" + ReqSep + @"(?<ix>\d{2,3})" + ReqSep + @"(?<thk>\d{3})$",
                      RegexOptions.Compiled);
    }
}