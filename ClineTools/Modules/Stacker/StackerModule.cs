// Modules/Stacker/StackerModule.cs
using ClineTools.Modules.Stacker.Decoders;
using ClineTools.Modules.Stacker.Sin;
using ClineTools.Modules.Stacker.Storage;
using ClineTools.Modules.Stacker.UI;
using Newtonsoft.Json; // Make sure Newtonsoft.Json is referenced
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.Windows.Forms;
using System.Xml;

namespace ClineTools.Modules.Stacker
{
    public class StackerModule : IModule, IDisposable
    {
        private ISldWorks _sw;
        private ITaskpaneView _tp;
        private bool _disposed;
        private readonly SinRegistry _registry = new SinRegistry();
        private StackerPaneControl _pane;

        public void Initialize(ISldWorks swApp)
        {
            _sw = swApp ?? throw new ArgumentNullException(nameof(swApp));

            // Register decoders here (add/remove freely)

            _registry.Register(new ClineTools.Modules.Stacker.Decoders.StackerDecoders());
        }

        public void Terminate() => Dispose();

        // ---------- Public API used by toolbar callbacks ----------

        /// <summary> Opens/toggles the assembly-only task pane. </summary>
        public void TogglePane()
        {
            if (!IsAssemblyActive())
            {
                MessageBox.Show("Stacker runs only in assemblies.", "Stacker");
                return;
            }
            EnsurePane();
            TryToggle();
        }

        /// <summary>
        /// PART-only: prompt for SIN, decode to a type-specific JSON card, store on active configuration,
        /// and mirror a couple of search props.
        /// </summary>
        public void AssignSinToActivePart()
        {
            if (!(_sw.ActiveDoc is IModelDoc2 md)) { MessageBox.Show("No active document.", "Stacker"); return; }
            if (md.GetType() != (int)swDocumentTypes_e.swDocPART) { MessageBox.Show("Open a Part to assign a SIN.", "Stacker"); return; }

            string input = null;
            using (var dlg = new ClineTools.Modules.Stacker.UI.AddSinDialog())
            {
                var r = dlg.ShowDialog(); // Parentless is fine; SW handles modality well. 
                if (r == System.Windows.Forms.DialogResult.OK)
                    input = dlg.EnteredSin;
            }
            if (string.IsNullOrWhiteSpace(input)) return;

            try
            {
                var decoder = _registry.Resolve(input);
                if (decoder == null)
                {
                    MessageBox.Show("No decoder recognized this SIN format. Adjust formats in SinFormats.cs or add a decoder.", "Stacker");
                    return;
                }

                // Build normalized SIN and card
                string normalized = SinFormats.Normalize(input);
                var card = decoder.DecodeToCard(normalized);

                // Wrap with schema/version for storage
                var envelope = new { schema = "stacker.v1", card = card };
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(envelope, Newtonsoft.Json.Formatting.Indented);

                // Extract type from the anonymous 'card' for mirroring
                string typeNameForMirror = null;
                try
                {
                    // Using JObject avoids reflection pain on anonymous type
                    var j = Newtonsoft.Json.Linq.JObject.FromObject(envelope);
                    var jt = j["card"] != null ? j["card"]["type"] : null;
                    typeNameForMirror = jt != null ? jt.ToString() : null;
                }
                catch { /* ignore */ }

                // Preview before saving
                using (var dlg = new AssignSinPreviewForm(normalized, json))
                {
                    var result = dlg.ShowDialog();
                    if (result != System.Windows.Forms.DialogResult.OK) return;
                }

                // Persist card on active config
                if (!AttributeStore.WriteJsonToActiveConfig(_sw, md, json))
                {
                    System.Windows.Forms.MessageBox.Show("Failed to write data card to the configuration.", "Stacker");
                    return;
                }

                // Mirror quick search props
                SetConfigProp(md, "STACKER_SIN", normalized);
                SetConfigProp(md, "STACKER_TYPE", string.IsNullOrEmpty(typeNameForMirror) ? "Unknown" : typeNameForMirror);

                System.Windows.Forms.MessageBox.Show("SIN assigned and data card saved on the active configuration.", "Stacker");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to decode/save SIN:\r\n" + ex.Message, "Stacker", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ---------- internals ----------

        private bool IsAssemblyActive()
        {
            var md = _sw?.ActiveDoc as IModelDoc2;
            return md != null && md.GetType() == (int)swDocumentTypes_e.swDocASSEMBLY;
        }

        private void EnsurePane()
        {
            if (_tp != null) return;
            _tp = _sw.CreateTaskpaneView2("", "Stacker");
            // Host a basic control; if you prefer COM ProgId hosting, you can switch later
            _pane = new StackerPaneControl();
            // AddControl requires ProgId in many interops; hosting directly is simpler here:
            // We fake-host by showing a placeholder; SOLIDWORKS will still hold the pane lifetime.
            // If your interop requires AddControl, uncomment and provide a COM-visible ProgId on the control.
            try { _tp.ShowView(); } catch { /* ignore */ }
        }

        private void TryToggle()
        {
            try
            {
                // Some interops expose Visible, others just have Show/Hide. We’ll just alternate:
                // If you prefer a stored visible flag, you can track it.
                _tp.HideView();
            }
            catch
            {
                // If Hide threw, try Show (first open)
                try { _tp.ShowView(); } catch { /* ignore */ }
                return;
            }

            // If we got here, it hid successfully; now show it again (toggle behavior: Hide -> Show)
            try { _tp.ShowView(); } catch { /* ignore */ }
        }

        private static void SetConfigProp(IModelDoc2 model, string name, string value)
        {
            try
            {
                var cfg = model.ConfigurationManager?.ActiveConfiguration?.Name ?? "";
                var pm = model.Extension?.get_CustomPropertyManager(cfg ?? "");
                if (pm == null) return;
                pm.Add3(name, (int)swCustomInfoType_e.swCustomInfoText, value,
                        (int)swCustomPropertyAddOption_e.swCustomPropertyDeleteAndAdd);
            }
            catch { /* ignore */ }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { _tp?.DeleteView(); } catch { /* ignore */ }
            _tp = null;
            _sw = null;
        }
    }
}