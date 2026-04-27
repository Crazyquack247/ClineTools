using SolidWorks.Interop.sldworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ClineTools.Modules.PointDetail
{
    public static class PointDetailBlockInserter
    {
        private const string AnchorToken = "[[CT_POINTDETAIL_ANCHOR]]";
        private const string UiTitle = "Insert Point Detail";
        private const double RequiredScale = 0.5;

        public static SketchBlockInstance InsertAtBottomLeft(ISldWorks swApp, ModelDoc2 model, string sldblkPath)
        {
            if (swApp == null) throw new ArgumentNullException(nameof(swApp));
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (string.IsNullOrWhiteSpace(sldblkPath)) throw new ArgumentException("Block path is required.", nameof(sldblkPath));
            if (!File.Exists(sldblkPath)) throw new FileNotFoundException("Block file not found.", sldblkPath);

            if (!(model is DrawingDoc draw))
                throw new InvalidOperationException("Active document is not a drawing.");

            // 1) Find anchor position
            if (!TryFindAnchorPointFromViews(draw, AnchorToken, out double xM, out double yM))
            {
                MessageBox.Show(
                    $"Anchor note not found.\n\nMake sure a NOTE containing:\n{AnchorToken}\nexists in the sheet format or sheet.",
                    UiTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return null;
            }

            // 2) Insert block at that position (ensure sketch context)
            bool enteredSketch = false;

            try
            {
                if (model.SketchManager?.ActiveSketch == null)
                {
                    model.SketchManager.InsertSketch(true);
                    enteredSketch = true;
                }

                var inst = InsertBlock(swApp, model, sldblkPath, xM, yM, angleRad: 0.0, scale: RequiredScale);

                if (inst == null)
                {
                    MessageBox.Show(
                        "Block insertion returned null.\n\nThis usually means SolidWorks rejected the insert (not in a valid sketch context).",
                        UiTitle,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return null;
                }

                model.GraphicsRedraw2();
                return inst;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Insert Point Detail failed:\n" + ex.Message,
                    UiTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return null;
            }
            finally
            {
                if (enteredSketch && model.SketchManager?.ActiveSketch != null)
                {
                    try { model.SketchManager.InsertSketch(true); }
                    catch { /* best-effort */ }
                }

                try { model.SetAddToDB(false); }
                catch { /* best-effort */ }
            }
        }

        public static SketchBlockInstance InsertAtStoredAnchor(
            ISldWorks swApp,
            ModelDoc2 model,
            string sldblkPath,
            double xIn,
            double yIn)
        {
            if (swApp == null) throw new ArgumentNullException(nameof(swApp));
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (string.IsNullOrWhiteSpace(sldblkPath)) throw new ArgumentException("Block path is required.", nameof(sldblkPath));
            if (!File.Exists(sldblkPath)) throw new FileNotFoundException("Block file not found.", sldblkPath);

            // Convert inches -> meters
            double xM = xIn * 0.0254;
            double yM = yIn * 0.0254;

            bool enteredSketch = false;

            try
            {
                if (model.SketchManager?.ActiveSketch == null)
                {
                    model.SketchManager.InsertSketch(true);
                    enteredSketch = true;
                }

                var inst = InsertBlock(swApp, model, sldblkPath, xM, yM, angleRad: 0.0, scale: RequiredScale);
                model.GraphicsRedraw2();
                return inst;
            }
            finally
            {
                if (enteredSketch && model.SketchManager?.ActiveSketch != null)
                {
                    try { model.SketchManager.InsertSketch(true); }
                    catch { /* best-effort */ }
                }

                try { model.SetAddToDB(false); }
                catch { /* best-effort */ }
            }
        }

        private static bool TryFindAnchorPointFromViews(DrawingDoc draw, string token, out double xM, out double yM)
        {
            xM = 0.0;
            yM = 0.0;

            // Drawing views chain: first view is the sheet, then sheet format / model views follow
            SolidWorks.Interop.sldworks.View view = draw.GetFirstView();

            while (view != null)
            {
                INote note = view.GetFirstNote();

                while (note != null)
                {
                    string text = note.GetText() ?? string.Empty;

                    if (text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        IAnnotation ann = note.GetAnnotation();
                        if (ann != null)
                        {
                            double[] pos = (double[])ann.GetPosition();
                            if (pos != null && pos.Length >= 2)
                            {
                                xM = pos[0];
                                yM = pos[1];
                                return true;
                            }
                        }
                    }

                    note = note.GetNext();
                }

                view = view.GetNextView();
            }

            return false;
        }

        private static SketchBlockInstance InsertBlock(
            ISldWorks swApp,
            ModelDoc2 model,
            string blkPath,
            double xM,
            double yM,
            double angleRad = 0.0,
            double scale = 1.0)
        {
            var mathUtil = (MathUtility)swApp.GetMathUtility();
            var pt = new[] { xM, yM, 0.0 };
            var mathPoint = (MathPoint)mathUtil.CreatePoint(pt);

            model.SetAddToDB(true);

            try
            {
                var sm = model.SketchManager;

                // If definition already exists, insert another instance; else create from file
                SketchBlockDefinition def = GetExistingDefinitionByFileName(sm, Path.GetFileName(blkPath));

                model.ClearSelection2(true);

                if (def != null)
                {
                    return (SketchBlockInstance)sm.InsertSketchBlockInstance(def, mathPoint, scale, angleRad);
                }

                def = (SketchBlockDefinition)sm.MakeSketchBlockFromFile(mathPoint, blkPath, false, scale, angleRad);
                object[] instances = (object[])def.GetInstances();

                return instances != null && instances.Length > 0 ? (SketchBlockInstance)instances[0] : null;
            }
            finally
            {
                try { model.SetAddToDB(false); }
                catch { /* best-effort */ }
            }
        }

        private static SketchBlockDefinition GetExistingDefinitionByFileName(SketchManager sm, string fileNameOnly)
        {
            int count = sm.GetSketchBlockDefinitionCount();
            if (count <= 0) return null;

            object[] defs = (object[])sm.GetSketchBlockDefinitions();
            if (defs == null) return null;

            foreach (var obj in defs)
            {
                if (!(obj is SketchBlockDefinition def)) continue;

                string existing = Path.GetFileName(def.FileName);
                if (string.Equals(existing, fileNameOnly, StringComparison.OrdinalIgnoreCase))
                    return def;
            }

            return null;
        }

        public static void PopulateAttributes(SketchBlockInstance inst, IDictionary<string, string> valuesByTag)
        {
            if (inst == null) throw new ArgumentNullException(nameof(inst));
            if (valuesByTag == null) throw new ArgumentNullException(nameof(valuesByTag));

            // Optional: build a set of existing attribute tags to help debug typos
            var existingTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            object attrsObj = inst.GetAttributes();

            if (attrsObj is object[] attrsArr)
            {
                foreach (var a in attrsArr)
                {
                    if (a is string tag && !string.IsNullOrWhiteSpace(tag))
                        existingTags.Add(tag);
                }
            }

            var missing = new List<string>();

            foreach (var kvp in valuesByTag)
            {
                string tag = kvp.Key;
                string val = kvp.Value ?? string.Empty;

                // Returns false if the tag doesn't exist or is read-only
                bool ok = inst.SetAttributeValue(tag, val);
                if (!ok)
                    missing.Add(tag);
            }

            if (missing.Count > 0)
            {
                string existing = existingTags.Count > 0
                    ? string.Join(", ", existingTags.OrderBy(x => x))
                    : "(Could not read attribute tags)";

                MessageBox.Show(
                    "Some block attributes could not be set:\n" +
                    string.Join("\n", missing) +
                    "\n\nAttributes found in this block instance:\n" + existing,
                    UiTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}