using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using SolidWorks.Interop.swconst;

namespace ClineTools.Modules.OnSave
{
    public partial class OnSaveMenu : Form
    {
        private readonly Func<string, string> _descriptionProvider;

        public OnSaveMenu(
            string defaultPath,
            int docType,
            Func<string, string> descriptionProvider,
            string initialFileName,
            string initialDescription)
        {
            _descriptionProvider = descriptionProvider;

            InitializeComponent();
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;

            comboBox1.Items.AddRange(new object[]
            {
                "Part (*.SLDPRT)",
                "Assembly (*.SLDASM)",
                "Drawing (*.SLDDRW)",
                "STEP File (*.STEP)",
                "STL File (*.STL)",
                "Parasolid (*.X_T)"
            });

            switch ((swDocumentTypes_e)docType)
            {
                case swDocumentTypes_e.swDocPART: comboBox1.SelectedIndex = 0; break;
                case swDocumentTypes_e.swDocASSEMBLY: comboBox1.SelectedIndex = 1; break;
                case swDocumentTypes_e.swDocDRAWING: comboBox1.SelectedIndex = 2; break;
                default: comboBox1.SelectedIndex = 0; break;
            }

            if (!string.IsNullOrWhiteSpace(defaultPath))
            {
                SelectedFolderPath = defaultPath;
                lblFolderPath.Text = defaultPath;
            }

            if (!string.IsNullOrWhiteSpace(initialFileName))
                txtFilename.Text = initialFileName.Trim();

            UpdateDescriptionFieldState();

            if (!string.IsNullOrWhiteSpace(initialDescription) && txtDescription.Enabled)
                txtDescription.Text = initialDescription.Trim();
        }

        public string SelectedExtension =>
            comboBox1.SelectedIndex switch
            {
                0 => ".SLDPRT",
                1 => ".SLDASM",
                2 => ".SLDDRW",
                3 => ".STEP",
                4 => ".STL",
                5 => ".X_T",
                _ => ".SLDPRT"
            };

        public string FileName => txtFilename.Text.Trim();
        public string Description => txtDescription.Text.Trim();
        public string SelectedFolderPath { get; private set; } = "";

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                InitialDirectory = Directory.Exists(SelectedFolderPath)
                    ? SelectedFolderPath
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                SelectedFolderPath = dlg.FileName;
                lblFolderPath.Text = SelectedFolderPath;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FileName))
            {
                MessageBox.Show("Enter a file name.");
                txtFilename.Focus();
                return;
            }

            bool allowEmptyDesc = comboBox1.SelectedIndex >= 3;

            if (!allowEmptyDesc && string.IsNullOrWhiteSpace(Description))
            {
                MessageBox.Show("Enter a description.");
                txtDescription.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedFolderPath))
            {
                MessageBox.Show("Select a folder.");
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateDescriptionFieldState();
        }

        private void UpdateDescriptionFieldState()
        {
            bool allowEmpty = comboBox1.SelectedIndex >= 3;

            txtDescription.Enabled = !allowEmpty;
            label3.ForeColor = allowEmpty
                ? Color.LightGray
                : SystemColors.ControlText;

            if (allowEmpty)
                txtDescription.Text = "";
        }

        private void btnReplace_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = BuildFilterForCurrentType(),
                Multiselect = false
            };

            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            txtFilename.Text = Path.GetFileNameWithoutExtension(ofd.FileName);

            if (_descriptionProvider != null && txtDescription.Enabled)
            {
                var desc = _descriptionProvider(ofd.FileName);
                if (!string.IsNullOrWhiteSpace(desc))
                    txtDescription.Text = desc;
            }
        }

        private string BuildFilterForCurrentType() =>
            comboBox1.SelectedIndex switch
            {
                0 => "Part (*.SLDPRT)|*.sldprt",
                1 => "Assembly (*.SLDASM)|*.sldasm",
                2 => "Drawing (*.SLDDRW)|*.slddrw",
                3 => "STEP (*.STEP)|*.step;*.stp",
                4 => "STL (*.STL)|*.stl",
                5 => "Parasolid (*.X_T)|*.x_t",
                _ => "All Files (*.*)|*.*"
            };
    }
}