using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SolidWorks.Interop.sldworks;

namespace ClineTools.Modules.Release
{
    public class ReleaseModule : IModule
    {
        private ISldWorks _swApp;

        public void Initialize(ISldWorks swApp)
        {
            _swApp = swApp;
        }

        public void Terminate()
        {
        }

        // Entry point called from the CommandManager button
        public void RunReleaseProcess()
        {
            try
            {
                DebugTrace.Log("ReleaseModule.RunReleaseProcess: started.");

                ModelDoc2 doc = _swApp?.IActiveDoc2;
                if (doc == null)
                {
                    MessageBox.Show("No active document.", "Release to MFG");
                    return;
                }

                string fullPath = doc.GetPathName();
                if (string.IsNullOrWhiteSpace(fullPath))
                {
                    MessageBox.Show("Please save the document before releasing.", "Release to MFG");
                    return;
                }

                string sourceDir = Path.GetDirectoryName(fullPath);
                string baseName = Path.GetFileNameWithoutExtension(fullPath);

                // Derive Engineering Transfer folder from the active file path
                string engJobFolder = ComputeEngineeringTransferFolder(sourceDir);
                if (engJobFolder == null)
                {
                    MessageBox.Show(
                        "Active file is not under the expected engineering directory:\n" +
                        ReleaseConfig.EngineerDrawingRoot,
                        "Release to MFG",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                DialogResult confirm = MessageBox.Show(
                    $"Would you like to release \"{baseName}\" to manufacturing?",
                    "Confirm Release",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes)
                    return;

                List<string> sourceFiles;

                using (var loading = new ReleaseLoadingDialog())
                {
                    loading.Show();
                    loading.UpdateStatus("locating files");
                    Application.DoEvents();

                    // Locate all files whose name contains the baseName, skip temp "~" files
                    sourceFiles = Directory
                        .EnumerateFiles(sourceDir)
                        .Where(f =>
                        {
                            string fileName = Path.GetFileName(f);
                            if (fileName.StartsWith("~"))
                                return false;

                            // Only SW native document types
                            string ext = Path.GetExtension(f).ToLowerInvariant();
                            if (ext != ".sldprt" && ext != ".sldasm" && ext != ".slddrw")
                                return false;

                            string nameNoExt = Path.GetFileNameWithoutExtension(f);
                            return nameNoExt.IndexOf(baseName, StringComparison.OrdinalIgnoreCase) >= 0;
                        })
                        .ToList();

                    System.Threading.Thread.Sleep(200);
                    loading.UpdateStatus("compiling list");
                    Application.DoEvents();

                    System.Threading.Thread.Sleep(200);
                }

                using (var rform = new ReleaseForm())
                {
                    rform.InitializeFromModule(sourceDir, sourceFiles, engJobFolder, _swApp);
                    rform.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                DebugTrace.DumpOnError(ex, "ReleaseModule.RunReleaseProcess");
                MessageBox.Show("Release failed:\n" + ex.Message, "Release Error");
            }
        }

        // Maps:
        // F:\Engineer\AUTOCAD\DRAWING FILES\<CompanyName>\<?>\<PartNumber>\REV...\ 
        // -> F:\Edgecam\Engineering Transfer\<CompanyName>\<PartNumber>\
        //
        // NOTE:
        //  - <CompanyName> is always the first folder under EngineerDrawingRoot.
        //  - There may be one or more intermediate folders between CompanyName and PartNumber.
        //  - PartNumber is the folder immediately before the first "REV*" folder (REV -, REV A, etc.).
        private static string ComputeEngineeringTransferFolder(string sourceDir)
        {
            try
            {
                string engineerRoot = Path.GetFullPath(ReleaseConfig.EngineerDrawingRoot.TrimEnd('\\', '/'));
                string dirFull = Path.GetFullPath(sourceDir);

                if (!dirFull.StartsWith(engineerRoot, StringComparison.OrdinalIgnoreCase))
                    return null;

                // Relative path under the engineer root:
                //   e.g. "CASE\SW\SOMETHING\CT-123\REV -\"
                string relative = dirFull.Substring(engineerRoot.Length)
                                         .TrimStart('\\', '/');

                var parts = relative
                    .Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2)
                    return null; // need at least CompanyName + PartNumber...

                // First segment is always the company / customer
                string companyName = parts[0];

                // Find the first "REV*" segment from the end, then take the folder before it as PartNumber
                int revIndex = -1;
                for (int i = parts.Length - 1; i >= 1; i--)
                {
                    string p = parts[i];
                    if (p.StartsWith("REV", StringComparison.OrdinalIgnoreCase))
                    {
                        revIndex = i;
                        break;
                    }
                }

                // If no REV folder is found, fall back to assuming the last segment is the part number
                if (revIndex <= 0)
                {
                    string fallbackPart = parts[parts.Length - 1];
                    string edgeRootFallback = Path.GetFullPath(ReleaseConfig.EdgecamTransferRoot.TrimEnd('\\', '/'));
                    return Path.Combine(edgeRootFallback, companyName, fallbackPart);
                }

                // Folder immediately before REV* is the PartNumber
                string partNumberFolder = parts[revIndex - 1];

                string edgeRoot = Path.GetFullPath(ReleaseConfig.EdgecamTransferRoot.TrimEnd('\\', '/'));
                string engJobFolder = Path.Combine(edgeRoot, companyName, partNumberFolder);

                return engJobFolder;
            }
            catch
            {
                return null;
            }
        }
    }
}