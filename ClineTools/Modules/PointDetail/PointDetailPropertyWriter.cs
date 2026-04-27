using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System.Globalization;

namespace ClineTools.Modules.PointDetail
{
    public static class PointDetailPropertyWriter
    {
        public static void WriteDrawingProps(ModelDoc2 model, PointDetailRequest req, PointDetailResult res)
        {
            if (model == null || model.Extension == null || req == null || res == null)
                return;

            var cpm = model.Extension.CustomPropertyManager[""]; // drawing doc-level properties

            // Store user input (as entered)
            SetTextProp(cpm, "POINT_TYPE", req.Type);
            SetTextProp(cpm, "POINT_DIA_INPUT", req.DiameterValue.ToString(CultureInfo.InvariantCulture));
            SetTextProp(cpm, "POINT_DIA_UNIT", req.Unit);

            // Store calculated values exactly as used for block attributes (strings)
            SetTextProp(cpm, "PDIAMETER", res.PDIAMETER);
            SetTextProp(cpm, "PtMargin", res.PtMargin);
            SetTextProp(cpm, "PtAoC", res.PtAoC);
            SetTextProp(cpm, "PtHone", res.PtHone);
            SetTextProp(cpm, "PtClearanceDia", res.PtClearanceDia);
            SetTextProp(cpm, "PtConeRelief", res.PtConeRelief);
            SetTextProp(cpm, "PtGashRadius", res.PtGashRadius);
            SetTextProp(cpm, "PtChislePointThck", res.PtChislePointThck);
            SetTextProp(cpm, "PtFluteRadius", res.PtFluteRadius);
            SetTextProp(cpm, "PtBackTaper", res.PtBackTaper);
            SetTextProp(cpm, "PtSecondaryRAngle", res.PtSecondaryRAngle);
            SetTextProp(cpm, "PtKLand", res.PtKLand);
        }

        private static void SetTextProp(CustomPropertyManager cpm, string name, string value)
        {
            if (cpm == null || string.IsNullOrWhiteSpace(name))
                return;

            cpm.Add3(
                name,
                (int)swCustomInfoType_e.swCustomInfoText,
                value ?? "",
                (int)swCustomPropertyAddOption_e.swCustomPropertyReplaceValue);
        }
    }
}