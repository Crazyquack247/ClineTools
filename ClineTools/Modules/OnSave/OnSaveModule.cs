using System;
using System.IO;
using System.Windows.Forms;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swcommands;
using SolidWorks.Interop.swconst;

namespace ClineTools.Modules
{
    public partial class OnSaveModule : IModule
    {
        private ISldWorks _swApp;
        private DSldWorksEvents_Event _swEvents;
        private bool _isFormOpen;

        // -------------------- Save command IDs --------------------
        private const int SaveCommandId = (int)swCommands_e.swCommands_Save;
        private const int SaveAsCommandId = (int)swCommands_e.swCommands_SaveAs;
        private const int SaveLocalCommandId = (int)swCommands_e.swCommands_SaveLocally;

        // -------------------- Create-from command IDs (SW 2025) --------------------
        private const int MakeDrawingFromPartAssyCommandId =
            (int)swCommands_e.swCommands_MakeDrawingFromPartAssembly;   // 461

        private const int MakeAssemblyFromPartAssyCommandId =
            (int)swCommands_e.swCommands_MakeAssemblyFromPartAssembly; // 462

        // -------------------- Pending inheritance --------------------
        private PendingInherit _pending;

        private struct PendingInherit
        {
            public bool HasValue;
            public int ExpectedDocType;
            public string FileBaseName;
            public string Description;
            public string FolderPath;
            public DateTime CreatedUtc;
        }

        // ============================================================
        //  IModule
        // ============================================================
        public void Initialize(ISldWorks swApp)
        {
            _swApp = swApp;
            _swEvents = (DSldWorksEvents_Event)_swApp;

            _swEvents.CommandOpenPreNotify += OnCommandPre;
        }

        public void Terminate()
        {
            if (_swEvents != null)
            {
                _swEvents.CommandOpenPreNotify -= OnCommandPre;
                _swEvents = null;
            }

            _swApp = null;
        }

        // ============================================================
        //  Command Intercept
        // ============================================================
        private int OnCommandPre(int command, int userActivationType)
        {
            try
            {
                string cmdName =
            Enum.GetName(typeof(swCommands_e), command) ?? "UNKNOWN";

                DebugTrace.LogCommand(
                    "OnCommandPre",
                    command,
                    cmdName,
                    userActivationType
                );

                // ---------- Create Drawing from Part / Assembly ----------
                if (command == MakeDrawingFromPartAssyCommandId)
                {
                    CaptureInheritanceSeed((int)swDocumentTypes_e.swDocDRAWING);
                    return 0;
                }

                // ---------- Create Assembly from Part / Assembly ----------
                if (command == MakeAssemblyFromPartAssyCommandId)
                {
                    CaptureInheritanceSeed((int)swDocumentTypes_e.swDocASSEMBLY);
                    return 0;
                }

                // ---------- Save handling ----------
                bool isSaveCommand =
                    command == SaveCommandId ||
                    command == SaveAsCommandId ||
                    command == SaveLocalCommandId;

                if (!isSaveCommand)
                    return 0;

                if (command == SaveAsCommandId || IsFirstSave())
                {
                    HandleSaveIntercept();
                    return 1; // always suppress SolidWorks dialog
                }
            }
            catch (Exception ex)
            {
                DebugTrace.DumpOnError(ex, "OnSaveModule.OnCommandPre");
                MessageBox.Show("Save interception error:\n" + ex.Message);
            }

            return 0;
        }

        // ============================================================
        //  Inheritance Capture
        // ============================================================
        private void CaptureInheritanceSeed(int expectedDocType)
        {
            try
            {
                var src = _swApp?.IActiveDoc2;
                if (src == null) return;

                string srcPath = src.GetPathName();
                if (string.IsNullOrWhiteSpace(srcPath))
                    return;

                string folder = Path.GetDirectoryName(srcPath);

                _pending = new PendingInherit
                {
                    HasValue = true,
                    ExpectedDocType = expectedDocType,
                    FileBaseName = Path.GetFileNameWithoutExtension(srcPath),
                    Description = GetDescriptionFromDoc(src),
                    FolderPath = folder,
                    CreatedUtc = DateTime.UtcNow
                };

                DebugTrace.Log($"Captured inheritance seed: '{_pending.FileBaseName}'");
            }
            catch (Exception ex)
            {
                DebugTrace.DumpOnError(ex, "CaptureInheritanceSeed");
            }
        }

        private bool TryConsumeInheritanceSeed(
            ModelDoc2 doc,
            out string fileBase,
            out string description,
            out string folderPath)
        {
            fileBase = null;
            description = null;
            folderPath = null;

            if (!_pending.HasValue)
                return false;

            if ((DateTime.UtcNow - _pending.CreatedUtc).TotalSeconds > 180)
            {
                _pending = default;
                return false;
            }

            if (doc.GetType() != _pending.ExpectedDocType)
                return false;

            if (!string.IsNullOrWhiteSpace(doc.GetPathName()))
                return false;

            fileBase = _pending.FileBaseName;
            description = _pending.Description;
            folderPath = _pending.FolderPath;
            _pending = default;

            return true;
        }

        // ============================================================
        //  Save Intercept
        // ============================================================
        private void HandleSaveIntercept()
        {
            if (_isFormOpen)
                return;

            var doc = _swApp?.IActiveDoc2;
            if (doc == null)
                return;

            _isFormOpen = true;

            try
            {
                string defaultPath = GetDefaultSavePath(doc);

                TryConsumeInheritanceSeed(
                    doc,
                    out string inheritedName,
                    out string inheritedDescription,
                    out string inheritedFolder);

                if (!string.IsNullOrWhiteSpace(inheritedFolder))
                    defaultPath = inheritedFolder;

                var form = new OnSave.OnSaveMenu(
                    defaultPath,
                    doc.GetType(),
                    GetDescriptionFromOpenFile,
                    inheritedName,
                    inheritedDescription);

                if (form.ShowDialog() == DialogResult.OK)
                {
                    string fullPath = Path.Combine(
                        form.SelectedFolderPath,
                        form.FileName + form.SelectedExtension);

                    bool saved;

                    if (IsExportExtension(form.SelectedExtension))
                    {
                        int errs = 0, warns = 0;
                        saved = doc.Extension.SaveAs(
                            fullPath,
                            (int)swSaveAsVersion_e.swSaveAsCurrentVersion,
                            (int)swSaveAsOptions_e.swSaveAsOptions_Silent,
                            null,
                            ref errs,
                            ref warns);
                    }
                    else
                    {
                        saved = doc.SaveAs(fullPath);
                    }

                    if (saved)
                        ApplyWindowsMetadata(fullPath, form.Description, "");
                }
            }
            finally
            {
                _isFormOpen = false;
            }
        }

        // ============================================================
        //  Helpers
        // ============================================================
        private bool IsFirstSave()
        {
            var doc = _swApp?.IActiveDoc2;
            return doc != null && string.IsNullOrWhiteSpace(doc.GetPathName());
        }

        private static bool IsExportExtension(string ext)
        {
            ext = (ext ?? string.Empty).ToLowerInvariant();
            return ext == ".step" || ext == ".stl" || ext == ".x_t";
        }

        private string GetDescriptionFromDoc(ModelDoc2 doc)
        {
            try
            {
                var cpm = doc.Extension.CustomPropertyManager[""];
                string val, res;

                foreach (var name in new[] { "Description", "DESCRIPTION" })
                {
                    if (cpm.Get4(name, false, out val, out res))
                        return string.IsNullOrWhiteSpace(res) ? val : res;
                }
            }
            catch { }

            return "";
        }

        // -------------------- Existing helpers (unchanged) --------------------
        private string GetDefaultSavePath(ModelDoc2 doc)
        {
            try
            {
                string path = doc.GetPathName();
                if (!string.IsNullOrWhiteSpace(path))
                    return Path.GetDirectoryName(path);
            }
            catch { }

            return System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        }

        private string GetDescriptionFromOpenFile(string filePath)
        {
            // KEEP your existing implementation here unchanged
            return null;
        }

        private void ApplyWindowsMetadata(string filePath, string title, string subject)
        {
            // KEEP your existing implementation here unchanged
        }
    }
}