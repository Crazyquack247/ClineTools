using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop.swcommands;
using System;
using System.IO;
using System.Windows.Forms;
using ClineTools.Modules.OnSave;

namespace ClineTools.Modules
{
    public partial class OnSaveModule : IModule
    {
        private ISldWorks swApp;
        private bool _isFormOpen = false;
        
        // Save command IDs

        private readonly int _saveCommandId = (int)swCommands_e.swCommands_Save;
        private readonly int _saveAsCommandId = (int)swCommands_e.swCommands_SaveAs;
        private readonly int _saveLocalCommandId = (int)swCommands_e.swCommands_SaveLocally;

        public void Initialize(ISldWorks swApp)
        {
            this.swApp = swApp;

            // Attach to SolidWorks event interface

            ((DSldWorksEvents_Event)swApp).CommandOpenPreNotify += OnCommandPre;
        }

        public void Terminate()
        {
            ((DSldWorksEvents_Event)swApp).CommandOpenPreNotify -= OnCommandPre;
        }

        private int OnCommandPre(int command, int userActivationType)
        {
            try
            {
                // Allow only true Save or Save As commands

                bool isSaveCommand = false;

                if (command == _saveCommandId || command == _saveAsCommandId || command == _saveLocalCommandId)
                {
                    isSaveCommand = true;
                }

                if (isSaveCommand == false)
                {
                    return 0; // Ignore everything else (menus, settings, etc.)
                }

                // Only trigger if it's Save As or first Save on a new doc
                
                if (command == _saveAsCommandId || IsFirstSave())
                {
                    bool allow = HandleSaveIntercept();
                    if (!allow)
                        return 1; // cancel SolidWorks' built-in Save/Save-As window
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error handling save command: " + ex.Message);
            }

            return 0;
        }

        // Helper: detect unsaved document

        private bool IsFirstSave()
        {
            ModelDoc2 doc = swApp.IActiveDoc2;
            if (doc == null) return false;

            string path = doc.GetPathName();
            return string.IsNullOrEmpty(path);
        }

        private bool HandleSaveIntercept()
        {
            if (_isFormOpen) return false;

            ModelDoc2 doc = swApp.IActiveDoc2;
            if (doc == null) return false;

            _isFormOpen = true;
            bool allowSave = false;   // assume we’ll handle it ourselves

            string defaultPath = GetDefaultSavePath(doc);
            OnSave.OnSaveMenu form = new OnSaveMenu(defaultPath, doc.GetType());

            DialogResult result = form.ShowDialog();

            if (result == DialogResult.OK)
            {
                string fileName = form.FileName;
                string description = form.Description;
                string folderPath = form.SelectedFolderPath;
                string fullPath = Path.Combine(folderPath, fileName + form.SelectedExtension);

                bool saved = doc.SaveAs(fullPath);
                if (saved)
                {
                    ApplyWindowsMetadata(fullPath, description, "");
                    // we handled the save → stop SolidWorks from running its own dialog
                    allowSave = false;
                }
                else
                {
                    MessageBox.Show("Save failed or was cancelled.", "Save Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    // optional: let SolidWorks open its Save-As if our save truly failed
                    allowSave = true;
                }
            }
            else
            {
                // user cancelled → stop default dialog too
                allowSave = false;
            }

            _isFormOpen = false;
            return allowSave;
        }

        private string GetDefaultSavePath(ModelDoc2 doc)
        {
            try
            {
                // If the document already has a path, use its directory

                string path = doc.GetPathName();
                if (!string.IsNullOrEmpty(path))
                    return Path.GetDirectoryName(path);

                // Otherwise, use SolidWorks' current working directory

                string workDir = swApp.GetCurrentWorkingDirectory();
                if (!string.IsNullOrEmpty(workDir))
                    return workDir;

                // Fallback to user's Documents folder

                return System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            }
            catch
            {
                // Just in case, fall back safely

                return System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            }
        }

        private void ApplyWindowsMetadata(string filePath, string description, string v)
        {
            try
            {
                // Get document from path

                int errors = 0;
                ModelDoc2 doc = (ModelDoc2)swApp.ActivateDoc3(filePath, true, (int)swRebuildOnActivation_e.swUserDecision, ref errors);

                if (doc == null)
                {
                    MessageBox.Show("Unable to access document for metadata writing.", "Error");
                    return;
                }

                // Access the custom property manager

                CustomPropertyManager propMgr = doc.Extension.CustomPropertyManager[""];

                // Add or update custom properties

                if (!string.IsNullOrWhiteSpace(description))
                    propMgr.Add3("Description", (int)swCustomInfoType_e.swCustomInfoText, description, (int)swCustomPropertyAddOption_e.swCustomPropertyReplaceValue);

                // Force a save to embed the changes

                doc.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref errors, ref errors);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to write SolidWorks metadata: " + ex.Message);
            }
        }
    }
}

