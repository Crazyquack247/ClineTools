namespace ClineTools
{
    partial class ToolWizForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ToolWizForm));
            this.splProperties = new System.Windows.Forms.Splitter();
            this.cmbShankSelector = new System.Windows.Forms.ComboBox();
            this.btnGenerate = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnAddStep = new System.Windows.Forms.Button();
            this.txtToolLength = new System.Windows.Forms.TextBox();
            this.lblToolLength = new System.Windows.Forms.Label();
            this.pnlProperties = new System.Windows.Forms.Panel();
            this.pbToolLength = new System.Windows.Forms.PictureBox();
            this.pbShankDisp = new System.Windows.Forms.PictureBox();
            this.pbLine1 = new System.Windows.Forms.PictureBox();
            this.pbHLine1 = new System.Windows.Forms.PictureBox();
            this.fileSystemWatcher1 = new System.IO.FileSystemWatcher();
            this.flwpStepProperties = new System.Windows.Forms.FlowLayoutPanel();
            this.pbToolPreview = new System.Windows.Forms.PictureBox();
            this.pnlProperties.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbToolLength)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbShankDisp)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbLine1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbHLine1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbToolPreview)).BeginInit();
            this.SuspendLayout();
            // 
            // splProperties
            // 
            this.splProperties.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.splProperties.Dock = System.Windows.Forms.DockStyle.Right;
            this.splProperties.Location = new System.Drawing.Point(1028, 0);
            this.splProperties.Name = "splProperties";
            this.splProperties.Size = new System.Drawing.Size(271, 475);
            this.splProperties.TabIndex = 3;
            this.splProperties.TabStop = false;
            // 
            // cmbShankSelector
            // 
            this.cmbShankSelector.AllowDrop = true;
            this.cmbShankSelector.FormattingEnabled = true;
            this.cmbShankSelector.Location = new System.Drawing.Point(129, 68);
            this.cmbShankSelector.Margin = new System.Windows.Forms.Padding(4);
            this.cmbShankSelector.Name = "cmbShankSelector";
            this.cmbShankSelector.Size = new System.Drawing.Size(88, 25);
            this.cmbShankSelector.TabIndex = 4;
            this.cmbShankSelector.Text = "Shank";
            // 
            // btnGenerate
            // 
            this.btnGenerate.Location = new System.Drawing.Point(1039, 395);
            this.btnGenerate.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Size = new System.Drawing.Size(248, 31);
            this.btnGenerate.TabIndex = 7;
            this.btnGenerate.Text = "Generate";
            this.btnGenerate.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(1039, 432);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(248, 31);
            this.btnCancel.TabIndex = 8;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnAddStep
            // 
            this.btnAddStep.Location = new System.Drawing.Point(933, 210);
            this.btnAddStep.Name = "btnAddStep";
            this.btnAddStep.Size = new System.Drawing.Size(89, 31);
            this.btnAddStep.TabIndex = 9;
            this.btnAddStep.Text = "Add Step";
            this.btnAddStep.UseVisualStyleBackColor = true;
            // 
            // txtToolLength
            // 
            this.txtToolLength.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtToolLength.Location = new System.Drawing.Point(51, 34);
            this.txtToolLength.Name = "txtToolLength";
            this.txtToolLength.Size = new System.Drawing.Size(206, 25);
            this.txtToolLength.TabIndex = 10;
            // 
            // lblToolLength
            // 
            this.lblToolLength.AutoSize = true;
            this.lblToolLength.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblToolLength.Location = new System.Drawing.Point(7, 8);
            this.lblToolLength.Name = "lblToolLength";
            this.lblToolLength.Size = new System.Drawing.Size(82, 17);
            this.lblToolLength.TabIndex = 11;
            this.lblToolLength.Text = "Tool Length";
            // 
            // pnlProperties
            // 
            this.pnlProperties.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlProperties.AutoSize = true;
            this.pnlProperties.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlProperties.Controls.Add(this.flwpStepProperties);
            this.pnlProperties.Controls.Add(this.pbHLine1);
            this.pnlProperties.Controls.Add(this.txtToolLength);
            this.pnlProperties.Controls.Add(this.lblToolLength);
            this.pnlProperties.Controls.Add(this.pbToolLength);
            this.pnlProperties.Location = new System.Drawing.Point(1028, 0);
            this.pnlProperties.Margin = new System.Windows.Forms.Padding(3, 3, 3, 6);
            this.pnlProperties.MaximumSize = new System.Drawing.Size(271, 2);
            this.pnlProperties.Name = "pnlProperties";
            this.pnlProperties.Size = new System.Drawing.Size(271, 2);
            this.pnlProperties.TabIndex = 12;
            // 
            // pbToolLength
            // 
            this.pbToolLength.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbToolLength.Image = ((System.Drawing.Image)(resources.GetObject("pbToolLength.Image")));
            this.pbToolLength.Location = new System.Drawing.Point(10, 28);
            this.pbToolLength.Name = "pbToolLength";
            this.pbToolLength.Size = new System.Drawing.Size(35, 35);
            this.pbToolLength.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbToolLength.TabIndex = 13;
            this.pbToolLength.TabStop = false;
            // 
            // pbShankDisp
            // 
            this.pbShankDisp.Location = new System.Drawing.Point(31, 126);
            this.pbShankDisp.Name = "pbShankDisp";
            this.pbShankDisp.Size = new System.Drawing.Size(273, 221);
            this.pbShankDisp.TabIndex = 6;
            this.pbShankDisp.TabStop = false;
            // 
            // pbLine1
            // 
            this.pbLine1.BackColor = System.Drawing.SystemColors.Control;
            this.pbLine1.Image = ((System.Drawing.Image)(resources.GetObject("pbLine1.Image")));
            this.pbLine1.Location = new System.Drawing.Point(310, 15);
            this.pbLine1.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.pbLine1.Name = "pbLine1";
            this.pbLine1.Size = new System.Drawing.Size(34, 441);
            this.pbLine1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbLine1.TabIndex = 5;
            this.pbLine1.TabStop = false;
            // 
            // pbHLine1
            // 
            this.pbHLine1.Image = ((System.Drawing.Image)(resources.GetObject("pbHLine1.Image")));
            this.pbHLine1.Location = new System.Drawing.Point(3, 65);
            this.pbHLine1.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.pbHLine1.Name = "pbHLine1";
            this.pbHLine1.Size = new System.Drawing.Size(267, 27);
            this.pbHLine1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbHLine1.TabIndex = 29;
            this.pbHLine1.TabStop = false;
            // 
            // fileSystemWatcher1
            // 
            this.fileSystemWatcher1.EnableRaisingEvents = true;
            this.fileSystemWatcher1.SynchronizingObject = this;
            // 
            // flwpStepProperties
            // 
            this.flwpStepProperties.AutoSize = true;
            this.flwpStepProperties.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flwpStepProperties.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flwpStepProperties.Location = new System.Drawing.Point(-1, 92);
            this.flwpStepProperties.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this.flwpStepProperties.Name = "flwpStepProperties";
            this.flwpStepProperties.Size = new System.Drawing.Size(0, 0);
            this.flwpStepProperties.TabIndex = 30;
            this.flwpStepProperties.WrapContents = false;
            // 
            // pbToolPreview
            // 
            this.pbToolPreview.Location = new System.Drawing.Point(351, 15);
            this.pbToolPreview.Name = "pbToolPreview";
            this.pbToolPreview.Size = new System.Drawing.Size(576, 441);
            this.pbToolPreview.TabIndex = 13;
            this.pbToolPreview.TabStop = false;
            // 
            // ToolWizForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1299, 475);
            this.Controls.Add(this.pbToolPreview);
            this.Controls.Add(this.pnlProperties);
            this.Controls.Add(this.btnAddStep);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnGenerate);
            this.Controls.Add(this.pbShankDisp);
            this.Controls.Add(this.pbLine1);
            this.Controls.Add(this.cmbShankSelector);
            this.Controls.Add(this.splProperties);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "ToolWizForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Tool Wizard";
            this.pnlProperties.ResumeLayout(false);
            this.pnlProperties.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbToolLength)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbShankDisp)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbLine1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbHLine1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbToolPreview)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Splitter splProperties;
        private System.Windows.Forms.PictureBox pbShankDisp;
        private System.Windows.Forms.PictureBox pbLine1;
        private System.Windows.Forms.ComboBox cmbShankSelector;
        private System.Windows.Forms.Button btnGenerate;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnAddStep;
        private System.Windows.Forms.TextBox txtToolLength;
        private System.Windows.Forms.Label lblToolLength;
        private System.Windows.Forms.PictureBox pbToolLength;
        private System.Windows.Forms.Panel pnlProperties;
        private System.Windows.Forms.PictureBox pbHLine1;
        private System.IO.FileSystemWatcher fileSystemWatcher1;
        private System.Windows.Forms.FlowLayoutPanel flwpStepProperties;
        private System.Windows.Forms.PictureBox pbToolPreview;
    }
}