using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ClineTools.Modules.WhereUsed
{
    public class WhereUsedModule : IModule
    {
        private ISldWorks _swApp;

        public void Initialize(ISldWorks swApp)
        {
            _swApp = swApp;
        }

        public void Terminate()
        {
        }

        public void RunWhereUsed()
        {
            var doc = _swApp?.IActiveDoc2;
            if (doc == null)
            {
                MessageBox.Show("No active document.", "Where Used");
                return;
            }

            if (doc.GetType() != (int)swDocumentTypes_e.swDocPART)
            {
                MessageBox.Show("Where Used currently supports PART files only.", "Where Used");
                return;
            }

            string partPath = doc.GetPathName();
            if (string.IsNullOrWhiteSpace(partPath) || !File.Exists(partPath))
            {
                MessageBox.Show("Please save the part file before running Where Used.", "Where Used");
                return;
            }

            string root = ClineTools.Modules.Release.ReleaseConfig.EngineerDrawingRoot;
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                MessageBox.Show(
                    "EngineerDrawingRoot does not exist:\n" + root,
                    "Where Used",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string partFullPath = Path.GetFullPath(partPath);
            string partFileName = Path.GetFileName(partFullPath);

            string licenseKey = SwDmKeyStore.GetKeyOrPrompt();
            if (string.IsNullOrWhiteSpace(licenseKey))
                return;

            Cursor.Current = Cursors.WaitCursor;

            try
            {
                var usedBy = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                int swdmFailures = 0;
                int swdmEmptyLists = 0;

                using (var dm = new SwDmService(licenseKey))
                {
                    foreach (string asmPath in EnumerateAssembliesSafe(root))
                    {
                        if (AssemblyReferencesPart(dm, asmPath, partFullPath, partFileName, ref swdmFailures, ref swdmEmptyLists))
                            usedBy.Add(asmPath);
                    }
                }

                //DebugTrace.Log($"WhereUsed: SwDM failures = {swdmFailures}, empty component lists = {swdmEmptyLists}");
                MessageBox.Show(
    $"WhereUsed scan complete.\n\n" +
    $"Matches: {usedBy.Count}\n" +
    $"SwDM failures: {swdmFailures}\n" +
    $"Empty component lists: {swdmEmptyLists}",
    "Where Used Debug");

                using (var form = new WhereUsedForm(partFullPath, usedBy.OrderBy(p => p).ToList()))
                {
                    form.ShowDialog();
                }
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        // --------------------------------------------------------------------
        // SAFE, FULL RECURSIVE ENUMERATION (NO EARLY TERMINATION)
        // --------------------------------------------------------------------
        private static IEnumerable<string> EnumerateAssembliesSafe(string root)
        {
            var stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                string dir = stack.Pop();

                IEnumerable<string> asmFiles = null;
                try
                {
                    asmFiles = Directory.EnumerateFiles(
                        dir,
                        "*.sldasm",
                        SearchOption.TopDirectoryOnly);
                }
                catch { }

                if (asmFiles != null)
                {
                    foreach (var f in asmFiles)
                    {
                        if (string.IsNullOrWhiteSpace(f))
                            continue;

                        if (Path.GetFileName(f).StartsWith("~"))
                            continue;

                        yield return f;
                    }
                }

                IEnumerable<string> subDirs = null;
                try
                {
                    subDirs = Directory.EnumerateDirectories(
                        dir,
                        "*",
                        SearchOption.TopDirectoryOnly);
                }
                catch { }

                if (subDirs != null)
                {
                    foreach (var sub in subDirs)
                    {
                        if (!string.IsNullOrWhiteSpace(sub))
                            stack.Push(sub);
                    }
                }
            }
        }

        // --------------------------------------------------------------------
        // COMPONENT MATCHING
        // --------------------------------------------------------------------
        private static bool AssemblyReferencesPart(
    SwDmService dm,
    string asmPath,
    string partFullPath,
    string partFileName,
    ref int swdmFailures,
    ref int swdmEmptyLists)
        {
            try
            {
                var componentPaths = dm.GetAssemblyComponentPaths(asmPath);

                DebugTrace.Log($"WhereUsed: asm={asmPath} componentCount={(componentPaths == null ? -1 : componentPaths.Count)}");

                if (componentPaths == null || componentPaths.Count == 0)
                {
                    swdmEmptyLists++;

                    // Sample log: first 5, then every 250th (prevents spam)
                    if (swdmEmptyLists <= 5 || (swdmEmptyLists % 250) == 0)
                        DebugTrace.Log($"WhereUsed: SwDM returned 0 components (empty). asm={asmPath}");

                    return false;
                }

                // Sample log: show format of the first component path for first 3 assemblies that return anything
                if (swdmEmptyLists == 0 && componentPaths.Count > 0)
                {
                    // no-op: keeps logic simple
                }

                // Prefer full-path match when possible, fallback to filename match
                foreach (string c in componentPaths)
                {
                    if (string.IsNullOrWhiteSpace(c))
                        continue;

                    string cNorm;
                    try { cNorm = Path.GetFullPath(c); }
                    catch { cNorm = c; }

                    if (string.Equals(cNorm, partFullPath, StringComparison.OrdinalIgnoreCase))
                        return true;

                    if (string.Equals(Path.GetFileName(cNorm), partFileName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            }
            //catch (Exception ex)
            //{
            //    swdmFailures++;

            //    // Sample log: first 5, then every 250th
            //    if (swdmFailures <= 5 || (swdmFailures % 250) == 0)
            //    {
            //        DebugTrace.Log(
            //            $"WhereUsed: SwDM exception reading asm. Type={ex.GetType().Name} Msg={ex.Message} asm={asmPath}");
            //    }

            //    return false;
            //}
            catch (Exception ex)
            {
                swdmFailures++;

                if (swdmFailures == 1)
                {
                    MessageBox.Show(
                        "First SwDM failure:\n\n" +
                        "Type: " + ex.GetType().FullName + "\n\n" +
                        "Message: " + ex.Message + "\n\n" +
                        "Assembly: " + asmPath,
                        "Where Used Debug",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }

                // Sample log: first 5, then every 250th
                if (swdmFailures <= 5 || (swdmFailures % 250) == 0)
                {
                    DebugTrace.Log(
                        $"WhereUsed: SwDM exception reading asm. Type={ex.GetType().Name} Msg={ex.Message} asm={asmPath}");
                }

                return false;
            }
        }

        private static string NormalizeComponentRef(string raw)
        {
            string s = raw.Trim();

            int cut = s.IndexOf('^');
            if (cut > 0) s = s.Substring(0, cut);

            cut = s.IndexOf('@');
            if (cut > 0) s = s.Substring(0, cut);

            return s.Trim().Trim('"');
        }
    }
}