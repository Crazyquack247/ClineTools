using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using SolidWorks.Interop.swconst;

namespace ClineTools.Modules.OnSave
{
    public partial class OnSaveMenu : Form
    {
        public OnSaveMenu(string defaultPath = "", int docType = (int)swDocumentTypes_e.swDocPART)
        {
            InitializeComponent();

            // Populate file type dropdown

            comboBox1.Items.AddRange(new object[]
            {
                "Part (*.SLDPRT)",
                "Assembly (*.SLDASM)",
                "Drawing (*.SLDDRW)",
                "STEP File (*.STEP)",
                "STL File (*.STL)",
                "Parasolid (*.X_T)"
            });

            // Auto-select based on SolidWorks document type

            switch ((swDocumentTypes_e)docType)
            {
                case swDocumentTypes_e.swDocPART:
                    comboBox1.SelectedIndex = 0;
                    break;
                case swDocumentTypes_e.swDocASSEMBLY:
                    comboBox1.SelectedIndex = 1;
                    break;
                case swDocumentTypes_e.swDocDRAWING:
                    comboBox1.SelectedIndex = 2;
                    break;
                default:
                    comboBox1.SelectedIndex = 0;
                    break;
            }

            // Auto-fill default save location

            if (!string.IsNullOrWhiteSpace(defaultPath))
            {
                SelectedFolderPath = defaultPath;
                lblFolderPath.Text = defaultPath;
            }
        }

        public string SelectedExtension
        {
            get
            {
                switch (comboBox1.SelectedIndex)
                {
                    case 0: return ".SLDPRT";
                    case 1: return ".SLDASM";
                    case 2: return ".SLDDRW";
                    case 3: return ".STEP";
                    case 4: return ".STL";
                    case 5: return ".X_T";
                    default: return ".SLDPRT";
                }
            }
        }

        // Public properties that the module reads 
        public string FileName => txtFilename.Text.Trim();
        public string Description => txtDescription.Text.Trim();
        public string SelectedFolderPath { get; private set; } = "";

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                dialog.Title = "Select a folder to save the file";
                dialog.InitialDirectory = Directory.Exists(SelectedFolderPath)
                    ? SelectedFolderPath
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    SelectedFolderPath = dialog.FileName;
                    lblFolderPath.Text = SelectedFolderPath;
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // Validate all required fields

            if (string.IsNullOrWhiteSpace(FileName))
            {
                MessageBox.Show("Please enter a file name.", "Missing Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFilename.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(Description))
            {
                MessageBox.Show("Please enter a description.", "Missing Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDescription.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedFolderPath))
            {
                MessageBox.Show("Please choose a save location.", "Missing Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
