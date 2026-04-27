using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swpublished;
using System;

namespace ClineTools
{
    public class PMPHandler : IPropertyManagerPage2Handler8
    {
        private readonly SwAddin _addin;
        private readonly ISldWorks _swApp;

        public PMPHandler(SwAddin addin)
        {
            _addin = addin ?? throw new ArgumentNullException(nameof(addin));
            _swApp = (ISldWorks)_addin.SwApp;
        }

        // SolidWorks PMP handlers: keep these non-empty to avoid GC edge cases during close.
        public void AfterClose() => KeepAlive();
        public void OnClose(int reason) => KeepAlive();

        private void KeepAlive()
        {
            // Preserve the intent of your original implementation: do *something* on close
            // so the CLR doesn't collect handler objects at an awkward time. :contentReference[oaicite:3]{index=3}
            int indentSize = System.Diagnostics.Debug.IndentSize;
            System.Diagnostics.Debug.WriteLine(indentSize);
            GC.KeepAlive(this);
        }

        public void AfterActivation() { }

        public int OnActiveXControlCreated(int id, bool status) => -1;

        public void OnButtonPress(int id) { }

        public void OnCheckboxCheck(int id, bool status) { }

        public void OnComboboxEditChanged(int id, string text) { }

        public void OnComboboxSelectionChanged(int id, int item) { }

        public void OnGroupCheck(int id, bool status) { }

        public void OnGroupExpand(int id, bool status) { }

        public bool OnHelp() => true;

        public bool OnKeystroke(int wparam, int message, int lparam, int id) => true;

        public void OnListboxSelectionChanged(int id, int item) { }

        public void OnListboxRMBUp(int id, int posX, int posY) { }

        public bool OnNextPage() => true;

        public void OnNumberboxChanged(int id, double val) { }

        public void OnOptionCheck(int id) { }

        public void OnPopupMenuItem(int id) { }

        public void OnPopupMenuItemUpdate(int id, ref int retval) { }

        public bool OnPreviousPage() => true;

        public bool OnPreview() => true;

        public void OnRedo() { }

        public void OnSelectionboxCalloutCreated(int id) { }

        public void OnSelectionboxCalloutDestroyed(int id) { }

        public void OnSelectionboxFocusChanged(int id) { }

        public void OnSelectionboxListChanged(int id, int item) { }

        public void OnSliderPositionChanged(int id, double value) { }

        public void OnSliderTrackingCompleted(int id, double value) { }

        public bool OnSubmitSelection(int id, object selection, int selType, ref string itemText) => true;

        public bool OnTabClicked(int id) => true;

        public void OnTextboxChanged(int id, string text) { }

        public void OnUndo() { }

        public void OnWhatsNew() { }

        public void OnGainedFocus(int id) { }

        public void OnLostFocus(int id) { }

        public int OnWindowFromHandleControlCreated(int id, bool status) => 0;
    }
}