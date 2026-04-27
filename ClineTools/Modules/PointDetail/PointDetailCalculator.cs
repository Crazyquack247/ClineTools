using System;
using System.Globalization;

namespace ClineTools.Modules.PointDetail
{
    public class PointDetailResult
    {
        public string PDIAMETER { get; set; } = "";
        public string PtMargin { get; set; } = "";
        public string PtAoC { get; set; } = "";
        public string PtHone { get; set; } = "";
        public string PtClearanceDia { get; set; } = "";
        public string PtConeRelief { get; set; } = "";
        public string PtGashRadius { get; set; } = "";
        public string PtChislePointThck { get; set; } = "";
        public string PtFluteRadius { get; set; } = "";
        public string PtBackTaper { get; set; } = "";
        public string PtSecondaryRAngle { get; set; } = "";
        public string PtKLand { get; set; } = "";
    }

    public static class PointDetailCalculator
    {
        // Keep tolerances exactly as they are in your current logic.
        private const string AocTolMm = "±0.10";
        private const string AocTolIn_GDrill = "±.004";
        private const string AocTolIn_2Flute = "±0.005";

        private static string FormatDual(
            double mmValue,
            string mmSuffix = null,
            string inSuffix = null,
            string tolMm = null,
            string tolIn = null,
            string mmFmt = "0.00",
            string inFmt = ".000")
        {
            double inValue = mmValue / 25.4;

            string mmLine = "[" + mmValue.ToString(mmFmt, CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(tolMm)) mmLine += " " + tolMm;
            mmLine += "]";
            if (!string.IsNullOrWhiteSpace(mmSuffix)) mmLine += " " + mmSuffix;

            string inLine = inValue.ToString(inFmt, CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(tolIn)) inLine += " " + tolIn;
            if (!string.IsNullOrWhiteSpace(inSuffix)) inLine += " " + inSuffix;

            return mmLine + "\r\n" + inLine;
        }

        private static string FormatDualWithPrefix(
            double mmValue,
            string prefix,               // "R" or "Ø"
            string tolMm = null,
            string tolIn = null,
            string mmFmt = "0.0",
            string inFmt = ".000")
        {
            double inValue = mmValue / 25.4;

            string mmLine = $"[{prefix}{mmValue.ToString(mmFmt, CultureInfo.InvariantCulture)}";
            if (!string.IsNullOrWhiteSpace(tolMm)) mmLine += $" {tolMm}";
            mmLine += "]";

            string inLine = prefix + inValue.ToString(inFmt, CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(tolIn)) inLine += $" {tolIn}";

            return mmLine + "\r\n" + inLine;
        }

        private static string FormatAngle(string angleText, string suffix = null)
        {
            if (string.IsNullOrWhiteSpace(angleText)) return "";
            return string.IsNullOrWhiteSpace(suffix) ? angleText : (angleText + " " + suffix);
        }

        // Formats:
        // [25.40]
        // 1.000
        private static string DualMmToBracketedMmAndIn(double mm)
        {
            double inches = mm / 25.4;
            return $"[{mm.ToString("0.00", CultureInfo.InvariantCulture)}]\r\n" +
                   $"{inches.ToString(".000", CultureInfo.InvariantCulture)}";
        }

        // Parses "0.07" etc as mm then returns dual-line format.
        private static string DualFromStringMm(string mmString)
        {
            if (string.IsNullOrWhiteSpace(mmString)) return "";

            if (!double.TryParse(mmString, NumberStyles.Float, CultureInfo.InvariantCulture, out double mm))
                return mmString;

            return DualMmToBracketedMmAndIn(mm);
        }

        private static string FormatDiameterInWithBracketedMm(double mm)
        {
            double inches = mm / 25.4;
            return inches.ToString(".000", CultureInfo.InvariantCulture) +
                   " [" +
                   mm.ToString("0.00", CultureInfo.InvariantCulture) +
                   "]";
        }

        private static string GetPtKLandText(double pDiameterMm)
        {
            if (pDiameterMm >= 26.0) return "[0.14-0.20 X 25°]\r\n.0065-.0085 X 25°";
            if (pDiameterMm >= 21.0) return "[0.12-0.18 X 25°]\r\n.0055-.0075 X 25°";
            if (pDiameterMm >= 14.0) return "[0.11-0.16 X 25°]\r\n.0045-.0065 X 25°";
            if (pDiameterMm >= 12.0) return "[0.09-0.14 X 25°]\r\n.0035-.0055 X 25°";
            if (pDiameterMm >= 8.0) return "[0.08-0.13 X 25°]\r\n.0030-.0050 X 25°";
            if (pDiameterMm >= 6.0) return "[0.06-0.11 X 25°]\r\n.0025-.0045 X 25°";
            return "[0.05-0.10 X 25°]\r\n.0020-.0040 X 25°";
        }

        /// <summary>
        /// Computes point-detail attributes using your rules.
        /// Thresholds are treated as millimeters.
        /// Numeric values are formatted:
        /// [mm.mm]
        /// in.inin
        /// </summary>
        public static PointDetailResult Calculate(double diameterValue, string unit)
        {
            double diaMm = unit.Equals("in", StringComparison.OrdinalIgnoreCase)
                ? diameterValue * 25.4
                : diameterValue;

            var r = new PointDetailResult
            {
                PDIAMETER = FormatDiameterInWithBracketedMm(diaMm)
            };

            // PtMargin = dia * 0.065 (mm)
            double ptMarginMm = diaMm * 0.065;
            r.PtMargin = FormatDual(
                ptMarginMm,
                tolMm: diaMm > 11.81 ? "±0.2" : diaMm > 6.01 ? "±0.15" : "±0.1",
                tolIn: diaMm > 0.4646 ? "±.008" : diaMm > 0.2362 ? "±.006" : "±.004"
            );

            // PtAoC = dia * 0.086 (mm)
            double ptAoCmm = diaMm * 0.086;
            r.PtAoC = FormatDual(ptAoCmm, tolMm: AocTolMm, tolIn: AocTolIn_GDrill);

            // PtHone (mm values given as strings)
            r.PtHone = DualFromStringMm(
                diaMm > 15.0 ? "0.07" :
                diaMm > 9.5 ? "0.06" :
                diaMm > 6.0 ? "0.04" :
                diaMm > 3.75 ? "0.03" : "0.02"
            );

            // PtClearanceDia = dia * 0.93 (mm)
            double clearanceMm = diaMm * 0.93;
            r.PtClearanceDia = FormatDualWithPrefix(clearanceMm, "Ø");

            // PtConeRelief (angle strings)
            string coneRelief =
                diaMm > 15.0 ? "8°" :
                diaMm > 9.5 ? "9°" :
                diaMm > 6.0 ? "10°" :
                diaMm > 3.75 ? "11°" : "12°";
            r.PtConeRelief = FormatAngle(coneRelief, "±1°");

            // PtGashRadius
            double gashRadiusMm =
                diaMm > 15.0 ? 1.5 :
                diaMm > 9.5 ? 1.0 :
                diaMm > 6.0 ? 0.5 : 0.2;
            r.PtGashRadius = FormatDualWithPrefix(gashRadiusMm, "R");

            // PtChislePointThck = dia * 0.038 (mm)
            double chiselMm = diaMm * 0.038;
            r.PtChislePointThck = FormatDual(
                chiselMm,
                mmSuffix: "0.05",
                inSuffix: ".002"
            );

            // PtFluteRadius
            double fluteRadiusMm =
                diaMm > 19.0 ? 3.6 :
                diaMm > 11.8 ? 3.0 :
                diaMm > 7.5 ? 2.0 :
                diaMm > 4.75 ? 1.3 : 0.8;
            r.PtFluteRadius = FormatDualWithPrefix(fluteRadiusMm, "R");

            // PtBackTaper
            double backTaperMm =
                diaMm > 25.4 ? 0.03 :
                diaMm > 15.875 ? 0.02 : 0.01;
            r.PtBackTaper = FormatDual(
                backTaperMm,
                mmSuffix: "PER 1MM",
                inSuffix: "PER 1IN"
            );

            // PtSecondaryRAngle = PtAoC * 2.5 (numeric mm)
            double ptSecondaryMm = ptAoCmm * 2.5;
            r.PtSecondaryRAngle = FormatDual(ptSecondaryMm, tolMm: AocTolMm, tolIn: AocTolIn_GDrill);

            return r;
        }

        public static PointDetailResult Calculate2FluteSpiral(double diameterValue, string unit)
        {
            double diaMm = unit.Equals("in", StringComparison.OrdinalIgnoreCase)
                ? diameterValue * 25.4
                : diameterValue;

            var r = new PointDetailResult
            {
                PDIAMETER = FormatDiameterInWithBracketedMm(diaMm)
            };

            // PtGashRadius: (PDIAMETER * 0.14)
            double gashMm = diaMm * 0.14;
            r.PtGashRadius = FormatDualWithPrefix(gashMm, "R");

            // PtAoC: (PDIAMETER * 0.075)
            double aocMm = diaMm * 0.075;
            r.PtAoC = FormatDual(aocMm, tolMm: AocTolMm, tolIn: AocTolIn_2Flute);

            // PtKLand: nested IF text
            r.PtKLand = GetPtKLandText(diaMm);

            return r;
        }
    }
}