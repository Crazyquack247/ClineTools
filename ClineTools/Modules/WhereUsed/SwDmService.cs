using SolidWorks.Interop.swdocumentmgr;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClineTools.Modules.WhereUsed
{
    internal sealed class SwDmService : IDisposable
    {
        private readonly ISwDMApplication _app;

        public SwDmService(string licenseKey)
        {
            if (string.IsNullOrWhiteSpace(licenseKey))
                throw new ArgumentException("SwDM license key is missing.", nameof(licenseKey));

            var factory = new SwDMClassFactory();
            _app = factory.GetApplication(licenseKey) as ISwDMApplication;

            if (_app == null)
            {
                System.Windows.MessageBox.Show("SwDM Application failed to initialize. License key likely invalid.");
            }
            else
            {
                System.Windows.MessageBox.Show("SwDM Application initialized successfully.");
            }

            if (_app == null)
                throw new InvalidOperationException("Failed to create ISwDMApplication. Check your SwDM key and install.");
        }

        public List<string> GetAssemblyComponentPaths(string asmPath)
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            SwDmDocumentOpenError err;
            var doc = _app.GetDocument(asmPath, SwDmDocumentType.swDmDocumentAssembly, true, out err) as ISwDMDocument;

            if (doc == null || err != SwDmDocumentOpenError.swDmDocumentOpenErrorNone)
            {
                try
                {
                    DebugTrace.Log($"WhereUsed: SwDM failed to open assembly. Err={err} Path={asmPath}");
                }
                catch { }

                return results.ToList();
            }

            try
            {
                ISwDMConfigurationMgr cfgMgr = doc.ConfigurationManager;
                if (cfgMgr == null)
                {
                    try { DebugTrace.Log($"WhereUsed: SwDM ConfigurationManager is null. Path={asmPath}"); } catch { }
                    return results.ToList();
                }

                // 1) Try active configuration name
                string activeCfgName = null;
                try { activeCfgName = cfgMgr.GetActiveConfigurationName(); } catch { }

                // 2) If active isn't available, fall back to FIRST configuration name
                string firstCfgName = null;
                try
                {
                    object namesObj = null;

                    // GetConfigurationNames exists on many SwDM versions; it may return string[] or object[]
                    dynamic d = cfgMgr;
                    namesObj = d.GetConfigurationNames();

                    if (namesObj is string[] sArr && sArr.Length > 0)
                        firstCfgName = sArr[0];
                    else if (namesObj is object[] oArr && oArr.Length > 0)
                        firstCfgName = oArr[0] as string;
                    else if (namesObj is string sSingle && !string.IsNullOrWhiteSpace(sSingle))
                        firstCfgName = sSingle;
                }
                catch { }

                string cfgNameToUse = !string.IsNullOrWhiteSpace(activeCfgName)
                    ? activeCfgName
                    : firstCfgName;

                if (string.IsNullOrWhiteSpace(cfgNameToUse))
                {
                    try
                    {
                        DebugTrace.Log($"WhereUsed: SwDM could not determine a configuration name. Active=(null/empty) First=(null/empty) Path={asmPath}");
                    }
                    catch { }

                    return results.ToList();
                }

                ISwDMConfiguration cfg = null;
                try { cfg = cfgMgr.GetConfigurationByName(cfgNameToUse); } catch { cfg = null; }

                if (cfg == null)
                {
                    try
                    {
                        DebugTrace.Log($"WhereUsed: SwDM GetConfigurationByName failed. Name={cfgNameToUse} Active={activeCfgName ?? "(null)"} First={firstCfgName ?? "(null)"} Path={asmPath}");
                    }
                    catch { }

                    return results.ToList();
                }

                var cfg2 = cfg as ISwDMConfiguration2;
                if (cfg2 == null)
                {
                    try
                    {
                        DebugTrace.Log($"WhereUsed: SwDM configuration is not ISwDMConfiguration2. Name={cfgNameToUse} Path={asmPath}");
                    }
                    catch { }

                    return results.ToList();
                }

                object compsObj = null;
                try { compsObj = cfg2.GetComponents(); } catch { compsObj = null; }

                int compCount = 0;
                try
                {
                    if (compsObj is object[] arr) compCount = arr.Length;
                    else if (compsObj != null) compCount = 1;
                }
                catch { compCount = 0; }

                try
                {
                    DebugTrace.Log($"WhereUsed: SwDM GetComponents. Count={compCount} Cfg={cfgNameToUse} Active={activeCfgName ?? "(null)"} First={firstCfgName ?? "(null)"} Asm={asmPath}");
                }
                catch { }

                if (compsObj == null)
                    return results.ToList();

                var comps = compsObj as object[];
                if (comps == null)
                    comps = new[] { compsObj };

                foreach (var o in comps)
                {
                    if (o == null) continue;

                    try
                    {
                        string p = TryGetComponentPath(o);
                        if (!string.IsNullOrWhiteSpace(p))
                            results.Add(p);
                    }
                    catch
                    {
                        // ignore component read errors
                    }
                }

                return results.ToList();
            }
            finally
            {
                try { doc.CloseDoc(); } catch { }
            }
        }

        private static string TryGetComponentPath(object compObj)
        {
            if (compObj == null)
                return null;

            // Try common property names
            try
            {
                dynamic d = compObj;
                string p = d.PathName;
                if (!string.IsNullOrWhiteSpace(p))
                    return p;
            }
            catch { }

            // Try common method names across SwDM versions
            try
            {
                dynamic d = compObj;
                string p = d.GetPathName();
                if (!string.IsNullOrWhiteSpace(p))
                    return p;
            }
            catch { }

            try
            {
                dynamic d = compObj;
                string p = d.GetPathName2();
                if (!string.IsNullOrWhiteSpace(p))
                    return p;
            }
            catch { }

            // Last-resort reflection
            try
            {
                var t = compObj.GetType();

                var prop = t.GetProperty("PathName");
                if (prop != null)
                {
                    var v = prop.GetValue(compObj) as string;
                    if (!string.IsNullOrWhiteSpace(v))
                        return v;
                }

                var m1 = t.GetMethod("GetPathName");
                if (m1 != null)
                {
                    var v = m1.Invoke(compObj, null) as string;
                    if (!string.IsNullOrWhiteSpace(v))
                        return v;
                }

                var m2 = t.GetMethod("GetPathName2");
                if (m2 != null)
                {
                    var v = m2.Invoke(compObj, null) as string;
                    if (!string.IsNullOrWhiteSpace(v))
                        return v;
                }
            }
            catch { }

            return null;
        }

        public void Dispose()
        {
        }
    }
}