using SolidWorks.Interop.sldworks;
using System.Globalization;

namespace ClineTools.Modules.PointDetail
{
    public static class PointDetailAnchorProps
    {
        public const string PropX = "CT_POINTDETAIL_X_IN";
        public const string PropY = "CT_POINTDETAIL_Y_IN";

        public static bool TryGetAnchorInches(ModelDoc2 model, out double xIn, out double yIn)
        {
            xIn = 0;
            yIn = 0;

            if (model?.Extension == null)
                return false;

            var cpm = model.Extension.CustomPropertyManager[""];
            if (cpm == null)
                return false;

            string xVal, xRes;
            bool xWasResolved;
            cpm.Get5(PropX, false, out xVal, out xRes, out xWasResolved);

            string yVal, yRes;
            bool yWasResolved;
            cpm.Get5(PropY, false, out yVal, out yRes, out yWasResolved);

            string sx = (!string.IsNullOrWhiteSpace(xRes) ? xRes : xVal)?.Trim();
            string sy = (!string.IsNullOrWhiteSpace(yRes) ? yRes : yVal)?.Trim();

            if (string.IsNullOrWhiteSpace(sx) || string.IsNullOrWhiteSpace(sy))
                return false;

            if (!double.TryParse(sx, NumberStyles.Float, CultureInfo.InvariantCulture, out xIn))
                return false;

            if (!double.TryParse(sy, NumberStyles.Float, CultureInfo.InvariantCulture, out yIn))
                return false;

            return true;
        }
    }
}