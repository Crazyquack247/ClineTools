using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClineTools
{
    public partial class ToolWizForm : Form
    {
        public ToolWizForm()
        {
            InitializeComponent();
        }

        private List<(double length, double diameter)> steps = new();
        private double shankLength = 2.0;  // example
        private double shankDiameter = 0.5;

        private void pbToolPreview_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            // margins and scaling
            float margin = 20f;
            float width = pbToolPreview.ClientSize.Width - 2 * margin;
            float height = pbToolPreview.ClientSize.Height - 2 * margin;

            // compute total tool length
            double totalLength = shankLength + steps.Sum(s => s.length);
            double maxDia = Math.Max(shankDiameter, steps.Count > 0 ? steps.Max(s => s.diameter) : 0);

            // scaling factors (inches → pixels)
            float scaleY = (float)(height / totalLength);
            float scaleX = (float)(width / (maxDia * 2));

            float y = margin; // top start
            float centerX = pbToolPreview.ClientSize.Width / 2f;

            using var pen = new Pen(Color.Black, 2);
            using var fill = new SolidBrush(Color.LightSteelBlue);

            // Draw shank
            float shankRadius = (float)(shankDiameter / 2 * scaleX);
            float shankHeight = (float)(shankLength * scaleY);
            g.FillRectangle(fill, centerX - shankRadius, y, shankRadius * 2, shankHeight);
            g.DrawRectangle(pen, centerX - shankRadius, y, shankRadius * 2, shankHeight);
            y += shankHeight;

            // Draw each step
            foreach (var step in steps)
            {
                float stepRadius = (float)(step.diameter / 2 * scaleX);
                float stepHeight = (float)(step.length * scaleY);

                // Draw rectangle representing cylindrical step
                g.FillRectangle(fill, centerX - stepRadius, y, stepRadius * 2, stepHeight);
                g.DrawRectangle(pen, centerX - stepRadius, y, stepRadius * 2, stepHeight);

                y += stepHeight;
            }

            // Draw centerline
            using var axisPen = new Pen(Color.Gray, 1) { DashStyle = DashStyle.Dash };
            g.DrawLine(axisPen, centerX, margin, centerX, pbToolPreview.ClientSize.Height - margin);
        }
    }
}
