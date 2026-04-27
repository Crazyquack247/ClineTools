using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace ClineTools.Modules.Release
{
    public partial class ReleaseForm : Form
    {
        private readonly List<string> _sourceFiles = new List<string>();
        private readonly List<string> _previewFiles = new List<string>();
        private string _engineeringFolder = string.Empty;
        private ISldWorks _swApp;

        public ReleaseForm()
        {
            InitializeComponent();

            clbFiles.CheckOnClick = true;
            clbPreview.CheckOnClick = true;

            txtTargetFolder.TextChanged += txtTargetFolder_TextChanged;
        }

        public void InitializeFromModule(
            string sourceFolder,
            IList<string> sourceFiles,
            string engineeringFolder,
            ISldWorks swApp)
        {
            _swApp = swApp;
            _engineeringFolder = engineeringFolder;

            _sourceFiles.Clear();
            _sourceFiles.AddRange(sourceFiles);

            _previewFiles.Clear(); // rebuilt from selections

            lblFilePath.Text = sourceFolder;
            lblFileNumber.Text = $"{_sourceFiles.Count} files located under:";

            txtTargetFolder.Text = _engineeringFolder;

            clbFiles.Items.Clear();
            foreach (var f in _sourceFiles)
            {
                clbFiles.Items.Add(Path.GetFileName(f), true);
            }

            clbFiles.ItemCheck -= clbFiles_ItemCheck;
            clbFiles.ItemCheck += clbFiles_ItemCheck;

            RebuildPreviewList();
        }

        private void RebuildPreviewList()
        {
            _previewFiles.Clear();
            clbPreview.Items.Clear();

            if (string.IsNullOrEmpty(_engineeringFolder) || _sourceFiles.Count == 0)
                return;

            var groups = BuildCheckedSourceGroups();

            foreach (var kvp in groups)
            {
                string baseName = kvp.Key;
                var g = kvp.Value;

                // Main model .x_t (requires checked asm or part)
                if (!string.IsNullOrEmpty(g.Asm) || !string.IsNullOrEmpty(g.Part))
                {
                    string xtPath = Path.Combine(_engineeringFolder, baseName + ".x_t");
                    _previewFiles.Add(xtPath);
                    clbPreview.Items.Add(Path.GetFileName(xtPath), true);

                    // TURN / PFB / GB configs (require checked part)
                    if (!string.IsNullOrEmpty(g.Part))
                    {
                        var specials = GetSpecialConfigSuffixes(g.Part);
                        foreach (string suffix in specials)
                        {
                            string specPath = Path.Combine(_engineeringFolder, $"{baseName} {suffix}.x_t");
                            _previewFiles.Add(specPath);
                            clbPreview.Items.Add(Path.GetFileName(specPath), true);
                        }
                    }
                }

                // PDF (requires checked drawing)
                if (!string.IsNullOrEmpty(g.Drw))
                {
                    string pdfPath = Path.Combine(_engineeringFolder, baseName + ".pdf");
                    _previewFiles.Add(pdfPath);
                    clbPreview.Items.Add(Path.GetFileName(pdfPath), true);
                }
            }
        }

        private Dictionary<string, (string Part, string Asm, string Drw)> BuildCheckedSourceGroups()
        {
            var groups = new Dictionary<string, (string Part, string Asm, string Drw)>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < clbFiles.Items.Count; i++)
            {
                if (!clbFiles.GetItemChecked(i))
                    continue;

                string path = _sourceFiles[i];
                string ext = Path.GetExtension(path).ToLowerInvariant();
                string baseName = Path.GetFileNameWithoutExtension(path);

                if (!groups.TryGetValue(baseName, out var g))
                    g = (null, null, null);

                switch (ext)
                {
                    case ".sldprt":
                        g.Part = path;
                        break;
                    case ".sldasm":
                        g.Asm = path;
                        break;
                    case ".slddrw":
                        g.Drw = path;
                        break;
                }

                groups[baseName] = g;
            }

            return groups;
        }

        private void clbFiles_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            BeginInvoke(new Action(RebuildPreviewList));
        }

        private List<string> GetSpecialConfigSuffixes(string partPath)
        {
            var result = new List<string>();
            if (_swApp == null)
                return result;

            bool openedHere;
            ModelDoc2 doc = OpenOrGetDocument(partPath, out openedHere);
            if (doc == null)
                return result;

            try
            {
                object namesObj = doc.GetConfigurationNames();
                if (!(namesObj is string[] configNames) || configNames.Length == 0)
                    return result;

                if (configNames.Any(n => n.IndexOf("turn", StringComparison.OrdinalIgnoreCase) >= 0))
                    result.Add("TURN");

                if (configNames.Any(n => n.IndexOf("pfb", StringComparison.OrdinalIgnoreCase) >= 0))
                    result.Add("PFB");

                if (configNames.Any(n => n.IndexOf("gb", StringComparison.OrdinalIgnoreCase) >= 0))
                    result.Add("GB");
            }
            finally
            {
                if (openedHere && doc != null)
                    _swApp.CloseDoc(doc.GetTitle());
            }

            return result;
        }

        private void lblFileNumber_Click(object sender, EventArgs e) { }
        private void lblFilePath_Click(object sender, EventArgs e) { }
        private void clbFiles_SelectedIndexChanged(object sender, EventArgs e) { }
        private void clbPreview_SelectedIndexChanged(object sender, EventArgs e) { }

        private void txtTargetFolder_TextChanged(object sender, EventArgs e)
        {
            _engineeringFolder = txtTargetFolder.Text.Trim();
            RebuildPreviewList();
        }

        private void btnRelease_Click(object sender, EventArgs e)
        {
            if (_swApp == null)
            {
                MessageBox.Show("SolidWorks application reference not available.", "Release to MFG");
                return;
            }

            if (clbPreview.CheckedItems.Count == 0)
            {
                MessageBox.Show(
                    "No files are selected to release. Check at least one item in the preview list.",
                    "Release to MFG",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_engineeringFolder))
            {
                MessageBox.Show(
                    "Please enter a valid Engineering Transfer folder path before releasing.",
                    "Release to MFG",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Safety guardrails
            //if (!PathSafety.TryValidateEngineeringTransferFolder(_engineeringFolder, out string safetyReason))
            //{
            //    MessageBox.Show(
            //        safetyReason,
            //        "Release to MFG – Safety Check Failed",
            //        MessageBoxButtons.OK,
            //        MessageBoxIcon.Error);
            //    return;
            //}

            try
            {
                Directory.CreateDirectory(_engineeringFolder);

                // Archive behavior:
                // - If export folder contains files, move them to ARCHIVE.
                // - ARCHIVE holds only one "previous version": clear it first.
                var existingTopLevelFiles = Directory
                    .EnumerateFiles(_engineeringFolder, "*", SearchOption.TopDirectoryOnly)
                    .ToList();

                if (existingTopLevelFiles.Count > 0)
                {
                    var choice = MessageBox.Show(
                        "The selected export folder already contains files.\n\n" +
                        "Click YES to move the existing files into an ARCHIVE subfolder (overwriting the previous archive),\n" +
                        "then export the new files.\n\n" +
                        "Click NO to cancel.\n\n" +
                        $"Folder:\n{_engineeringFolder}",
                        "Files Detected",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (choice != DialogResult.Yes)
                        return;

                    string archiveDir = Path.Combine(_engineeringFolder, "ARCHIVE");
                    Directory.CreateDirectory(archiveDir);

                    ClearDirectoryContents(archiveDir);

                    foreach (var file in existingTopLevelFiles)
                    {
                        try
                        {
                            string dest = Path.Combine(archiveDir, Path.GetFileName(file));

                            if (File.Exists(dest))
                                File.Delete(dest);

                            File.Move(file, dest);
                        }
                        catch
                        {
                            // Non-fatal; export results will still report per-file outcomes.
                        }
                    }
                }

                var results = new List<ExportResult>();
                var groups = BuildCheckedSourceGroups();

                for (int i = 0; i < clbPreview.Items.Count; i++)
                {
                    if (!clbPreview.GetItemChecked(i))
                        continue;

                    string targetPath = _previewFiles[i];
                    string targetExt = Path.GetExtension(targetPath).ToLowerInvariant();
                    string fileNoExt = Path.GetFileNameWithoutExtension(targetPath); // "CT-123" or "CT-123 TURN"

                    string baseName = fileNoExt;
                    string suffix = null;

                    var tokens = fileNoExt.Split(' ');
                    if (tokens.Length >= 2)
                    {
                        string last = tokens[tokens.Length - 1].ToUpperInvariant();
                        if (last == "TURN" || last == "PFB" || last == "GB")
                        {
                            suffix = last;
                            baseName = string.Join(" ", tokens.Take(tokens.Length - 1));
                        }
                    }

                    if (!groups.TryGetValue(baseName, out var g))
                    {
                        results.Add(new ExportResult
                        {
                            TargetPath = targetPath,
                            Success = false,
                            Detail = $"No checked source files were found for base name '{baseName}'."
                        });
                        continue;
                    }

                    bool ok;
                    string detail;

                    if (targetExt == ".x_t")
                    {
                        if (suffix == null)
                        {
                            string sourcePath =
                                !string.IsNullOrEmpty(g.Asm) ? g.Asm :
                                !string.IsNullOrEmpty(g.Part) ? g.Part :
                                null;

                            if (string.IsNullOrEmpty(sourcePath))
                            {
                                ok = false;
                                detail = $"No checked assembly or part is available to generate '{targetPath}'.";
                            }
                            else
                            {
                                ok = ExportSolidWorksFile(sourcePath, targetPath, out detail);
                            }
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(g.Part))
                            {
                                ok = false;
                                detail = $"No checked part is available to generate '{targetPath}' ({suffix}).";
                            }
                            else
                            {
                                string key = suffix.ToLowerInvariant(); // "turn"/"pfb"/"gb"
                                ok = ExportConfigToXt(g.Part, key, targetPath, out detail);
                            }
                        }
                    }
                    else if (targetExt == ".pdf")
                    {
                        if (string.IsNullOrEmpty(g.Drw))
                        {
                            ok = false;
                            detail = $"No checked drawing is available to generate '{targetPath}'.";
                        }
                        else
                        {
                            ok = ExportSolidWorksFile(g.Drw, targetPath, out detail);
                        }
                    }
                    else
                    {
                        ok = false;
                        detail = $"Unsupported target extension '{targetExt}'.";
                    }

                    results.Add(new ExportResult
                    {
                        TargetPath = targetPath,
                        Success = ok,
                        Detail = detail
                    });
                }

                ShowReleaseResultsDialog(results);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An error occurred during release:\n" + ex.Message,
                    "Release Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static void ClearDirectoryContents(string dir)
        {
            try
            {
                if (!Directory.Exists(dir))
                    return;

                foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
                {
                    try { File.Delete(file); } catch { }
                }

                foreach (var subDir in Directory.EnumerateDirectories(dir, "*", SearchOption.TopDirectoryOnly))
                {
                    try { Directory.Delete(subDir, true); } catch { }
                }
            }
            catch
            {
                // Never let cleanup crash the add-in
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private bool ExportConfigToXt(string partPath, string configKey, string targetPath, out string errorDetail)
        {
            errorDetail = string.Empty;

            bool openedHere;
            ModelDoc2 doc = OpenOrGetDocument(partPath, out openedHere);
            if (doc == null)
            {
                errorDetail = $"Failed to open part '{partPath}' for configuration export.";
                return false;
            }

            try
            {
                object namesObj = doc.GetConfigurationNames();
                if (!(namesObj is string[] configNames) || configNames.Length == 0)
                {
                    errorDetail = $"No configurations found in part '{partPath}'.";
                    return false;
                }

                string match = configNames
                    .FirstOrDefault(n => n.IndexOf(configKey, StringComparison.OrdinalIgnoreCase) >= 0);

                if (string.IsNullOrEmpty(match))
                {
                    errorDetail = $"No configuration containing '{configKey}' was found in part '{partPath}'.";
                    return false;
                }

                bool shown = doc.ShowConfiguration2(match);
                if (!shown)
                {
                    errorDetail = $"Failed to activate configuration '{match}' in part '{partPath}'.";
                    return false;
                }

                int err = 0;
                int warn = 0;
                bool result = doc.Extension.SaveAs(
                    targetPath,
                    (int)swSaveAsVersion_e.swSaveAsCurrentVersion,
                    (int)swSaveAsOptions_e.swSaveAsOptions_Silent,
                    null,
                    ref err,
                    ref warn);

                if (!result)
                {
                    errorDetail = $"SaveAs failed for '{targetPath}' from config '{match}' in part '{partPath}'. Error={err}, Warning={warn}.";
                    return false;
                }

                if (err != 0)
                {
                    errorDetail = $"Exported '{targetPath}' from config '{match}', but error code {err} was reported (warning={warn}).";
                    return false;
                }

                errorDetail = $"Exported '{targetPath}' from config '{match}' successfully.";
                return true;
            }
            finally
            {
                if (openedHere && doc != null)
                    _swApp.CloseDoc(doc.GetTitle());
            }
        }

        private bool ExportSolidWorksFile(string sourcePath, string targetPath, out string errorDetail)
        {
            errorDetail = string.Empty;

            bool openedHere;
            ModelDoc2 doc = OpenOrGetDocument(sourcePath, out openedHere);
            if (doc == null)
            {
                errorDetail = $"Failed to open source document '{sourcePath}'.";
                return false;
            }

            try
            {
                int err = 0;
                int warn = 0;

                bool result = doc.Extension.SaveAs(
                    targetPath,
                    (int)swSaveAsVersion_e.swSaveAsCurrentVersion,
                    (int)swSaveAsOptions_e.swSaveAsOptions_Silent,
                    null,
                    ref err,
                    ref warn);

                if (!result)
                {
                    errorDetail = $"SolidWorks SaveAs failed for '{targetPath}' from '{sourcePath}'. Error={err}, Warning={warn}.";
                    return false;
                }

                if (err != 0)
                {
                    errorDetail = $"Exported '{targetPath}' from '{sourcePath}', but SaveAs reported error code {err} (warning={warn}).";
                    return false;
                }

                errorDetail = $"Exported '{targetPath}' from '{sourcePath}' successfully.";
                return true;
            }
            finally
            {
                if (openedHere && doc != null)
                    _swApp.CloseDoc(doc.GetTitle());
            }
        }

        private ModelDoc2 OpenOrGetDocument(string sourcePath, out bool openedHere)
        {
            openedHere = false;

            try
            {
                string fileName = Path.GetFileName(sourcePath);

                ModelDoc2 doc = _swApp.GetOpenDocumentByName(fileName) as ModelDoc2;
                if (doc != null)
                    return doc;

                int docType = GetDocTypeFromExtension(Path.GetExtension(sourcePath));
                if (docType == 0)
                    return null;

                int errors = 0;
                int warnings = 0;

                doc = _swApp.OpenDoc6(
                    sourcePath,
                    docType,
                    (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
                    "",
                    ref errors,
                    ref warnings) as ModelDoc2;

                if (doc != null)
                    openedHere = true;

                return doc;
            }
            catch
            {
                return null;
            }
        }

        private class ExportResult
        {
            public string TargetPath { get; set; }
            public bool Success { get; set; }
            public string Detail { get; set; }
        }

        private void ShowReleaseResultsDialog(List<ExportResult> results)
        {
            int successCount = results.Count(r => r.Success);
            int failCount = results.Count(r => !r.Success);

            var dlg = new Form
            {
                Text = "Release to Manufacturing – Summary",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                Width = 800,
                Height = 400
            };

            var lblSummary = new Label
            {
                AutoSize = true,
                Text = $"Release complete. Successful exports: {successCount}, Failed exports: {failCount}",
                Dock = DockStyle.Top,
                Padding = new Padding(10, 10, 10, 5)
            };

            var list = new ListView
            {
                View = System.Windows.Forms.View.Details,
                FullRowSelect = true,
                GridLines = true,
                Dock = DockStyle.Fill
            };

            list.Columns.Add("Status", 80);
            list.Columns.Add("Target File", 260);
            list.Columns.Add("Details", 400);

            foreach (var r in results)
            {
                var item = new ListViewItem(r.Success ? "Success" : "Failed");
                item.SubItems.Add(r.TargetPath);
                item.SubItems.Add(r.Detail ?? string.Empty);
                list.Items.Add(item);
            }

            var btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                Width = 100,
                Height = 28
            };

            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 45
            };

            btnOk.Left = bottomPanel.Width - btnOk.Width - 10;
            btnOk.Top = (bottomPanel.Height - btnOk.Height) / 2;
            btnOk.Anchor = AnchorStyles.Right | AnchorStyles.Top;

            bottomPanel.Controls.Add(btnOk);

            dlg.Controls.Add(list);
            dlg.Controls.Add(bottomPanel);
            dlg.Controls.Add(lblSummary);

            dlg.AcceptButton = btnOk;

            bottomPanel.Resize += (s, e) =>
            {
                btnOk.Left = bottomPanel.Width - btnOk.Width - 10;
                btnOk.Top = (bottomPanel.Height - btnOk.Height) / 2;
            };

            dlg.ShowDialog(this);
        }

        private int GetDocTypeFromExtension(string ext)
        {
            switch (ext.ToLowerInvariant())
            {
                case ".sldprt": return (int)swDocumentTypes_e.swDocPART;
                case ".sldasm": return (int)swDocumentTypes_e.swDocASSEMBLY;
                case ".slddrw": return (int)swDocumentTypes_e.swDocDRAWING;
                default: return 0;
            }
        }
    }
}