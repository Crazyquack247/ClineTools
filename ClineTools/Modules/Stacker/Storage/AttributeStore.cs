using System;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace ClineTools.Modules.Stacker.Storage
{
    /// <summary>
    /// Store/read the JSON on the ACTIVE CONFIGURATION.
    /// We still try to write a SW Attribute, but we READ from a config custom property (portable).
    /// </summary>
    public static class AttributeStore
    {
        private const string AttrName = "CT_STACKER_CARD";
        private const string ParamName = "json";
        private const string MirrorProp = "CT_STACKER_CARD_JSON"; // config-scoped property we read back

        public static bool WriteJsonToActiveConfig(ISldWorks sw, IModelDoc2 model, string json)
        {
            try
            {
                if (sw == null || model == null) return false;
                var cfg = model.ConfigurationManager != null ? model.ConfigurationManager.ActiveConfiguration : null;
                if (cfg == null) return false;

                // 1) Write as a SolidWorks Attribute (best-effort)
                try
                {
                    var def = sw.DefineAttribute(AttrName);
                    def.AddParameter(ParamName, (int)swParamType_e.swParamTypeString, "");
                    def.Register();

                    var entityOwner = (IEntity)cfg;
                    var attr = def.CreateInstance5(model, entityOwner, AttrName, 0);
                    if (attr != null)
                    {
                        var p = attr.GetParameter(ParamName) as IParameter;
                        if (p != null) p.SetStringValue(json);
                    }
                }
                catch
                {
                    // Ignore attribute failures; we still mirror to a property
                }

                // 2) Mirror to a configuration-scoped custom property (authoritative read path)
                var cfgName = cfg.Name ?? "";
                var pm = model.Extension != null ? model.Extension.get_CustomPropertyManager(cfgName) : null;
                if (pm == null) return false;

                pm.Add3(MirrorProp, (int)swCustomInfoType_e.swCustomInfoText, json,
                        (int)swCustomPropertyAddOption_e.swCustomPropertyDeleteAndAdd);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string ReadJsonFromActiveConfig(ISldWorks sw, IModelDoc2 model)
        {
            try
            {
                if (sw == null || model == null) return null;
                var cfg = model.ConfigurationManager != null ? model.ConfigurationManager.ActiveConfiguration : null;
                if (cfg == null) return null;

                var cfgName = cfg.Name ?? "";
                var pm = model.Extension != null ? model.Extension.get_CustomPropertyManager(cfgName) : null;
                if (pm == null) return null;

                string rawValue, resolvedValue;
                // Older SolidWorks interops: Get2 returns void
                pm.Get2(MirrorProp, out rawValue, out resolvedValue);

                // Prefer resolved; fall back to raw
                string candidate = !string.IsNullOrEmpty(resolvedValue) ? resolvedValue : rawValue;
                return string.IsNullOrEmpty(candidate) ? null : candidate;
            }
            catch
            {
                return null;
            }
        }
    }
}