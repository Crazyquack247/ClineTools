using System.Globalization;
using System.Windows.Forms;

namespace ClineTools.Modules.PointDetail
{
    public static class PointDetailWizard
    {
        private const string Title = "Insert Point Detail";

        public static bool TryGetRequest(IWin32Window owner, out PointDetailRequest request)
        {
            request = new PointDetailRequest();

            while (true)
            {
                // Step 1: Choose type
                using (var typeForm = new UI.SelectTypeForm())
                {
                    if (typeForm.ShowDialog(owner) != DialogResult.OK)
                        return false;

                    request.Type = typeForm.SelectedType;

                    // Step 2: Diameter + unit
                    using (var diaForm = new UI.EnterDiameterForm())
                    {
                        diaForm.Owner = typeForm;

                        while (true)
                        {
                            var result = diaForm.ShowDialog(typeForm);

                            // Back
                            if (result == DialogResult.Retry)
                                break;

                            // Cancel/close
                            if (result != DialogResult.OK)
                                return false;

                            // Parse diameter
                            if (!double.TryParse(
                                    diaForm.DiameterText,
                                    NumberStyles.Float,
                                    CultureInfo.InvariantCulture,
                                    out double dia))
                            {
                                MessageBox.Show(
                                    "Please enter a valid numeric diameter (example: 0.375 or 9.525).",
                                    Title,
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                                continue;
                            }

                            if (dia <= 0)
                            {
                                MessageBox.Show(
                                    "Diameter must be greater than zero.",
                                    Title,
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                                continue;
                            }

                            request.DiameterValue = dia;
                            request.Unit = diaForm.SelectedUnit;
                            return true;
                        }

                        // If we hit "Back", loop to re-open SelectTypeForm
                        continue;
                    }
                }
            }
        }
    }
}