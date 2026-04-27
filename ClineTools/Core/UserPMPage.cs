using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop.swpublished;
using System;

namespace ClineTools
{
    public class UserPMPage
    {
        private readonly SwAddin _addin;
        private readonly ISldWorks _swApp;

        private IPropertyManagerPage2 _page;
        private PMPHandler _handler;

        #region Property Manager Page Controls

        // Groups
        private IPropertyManagerPageGroup _group1;
        private IPropertyManagerPageGroup _group2;

        // Controls
        private IPropertyManagerPageTextbox _textbox1;
        private IPropertyManagerPageCheckbox _checkbox1;
        private IPropertyManagerPageOption _option1;
        private IPropertyManagerPageOption _option2;
        private IPropertyManagerPageOption _option3;
        private IPropertyManagerPageListbox _list1;

        private IPropertyManagerPageSelectionbox _selection1;
        private IPropertyManagerPageNumberbox _num1;
        private IPropertyManagerPageCombobox _combo1;

        // IDs (kept identical to your original file) :contentReference[oaicite:5]{index=5}
        public const int group1ID = 0;
        public const int group2ID = 1;

        public const int textbox1ID = 2;
        public const int checkbox1ID = 3;
        public const int option1ID = 4;
        public const int option2ID = 5;
        public const int option3ID = 6;
        public const int list1ID = 7;

        public const int selection1ID = 8;
        public const int num1ID = 9;
        public const int combo1ID = 10;

        #endregion

        public UserPMPage(SwAddin addin)
        {
            _addin = addin;

            if (_addin == null)
            {
                System.Windows.Forms.MessageBox.Show("SwAddin not set.", "ClineTools");
                return;
            }

            _swApp = (ISldWorks)_addin.SwApp;
            CreatePropertyManagerPage();
        }

        public void Show()
        {
            _page?.Show();
        }

        private void CreatePropertyManagerPage()
        {
            int errors = -1;

            int options =
                (int)swPropertyManagerPageOptions_e.swPropertyManagerOptions_OkayButton |
                (int)swPropertyManagerPageOptions_e.swPropertyManagerOptions_CancelButton;

            _handler = new PMPHandler(_addin);

            _page = (IPropertyManagerPage2)_swApp.CreatePropertyManagerPage(
                "Sample PMP",
                options,
                _handler,
                ref errors);

            if (_page == null || errors != (int)swPropertyManagerPageStatus_e.swPropertyManagerPage_Okay)
                return;

            try
            {
                AddControls();
            }
            catch (Exception ex)
            {
                _swApp.SendMsgToUser2(ex.Message, 0, 0);
            }
        }

        // Controls display top-to-bottom in the order they are added.
        private void AddControls()
        {
            // Groups
            int group1Options =
                (int)swAddGroupBoxOptions_e.swGroupBoxOptions_Expanded |
                (int)swAddGroupBoxOptions_e.swGroupBoxOptions_Visible;

            _group1 = (IPropertyManagerPageGroup)_page.AddGroupBox(group1ID, "Sample Group 1", group1Options);

            int group2Options =
                (int)swAddGroupBoxOptions_e.swGroupBoxOptions_Checkbox |
                (int)swAddGroupBoxOptions_e.swGroupBoxOptions_Visible;

            _group2 = (IPropertyManagerPageGroup)_page.AddGroupBox(group2ID, "Sample Group 2", group2Options);

            short leftAlign = (short)swPropertyManagerPageControlLeftAlign_e.swControlAlign_LeftEdge;

            int ctlOptions =
                (int)swAddControlOptions_e.swControlOptions_Enabled |
                (int)swAddControlOptions_e.swControlOptions_Visible;

            // Group 1 controls
            _textbox1 = (IPropertyManagerPageTextbox)_group1.AddControl(
                textbox1ID,
                (int)swPropertyManagerPageControlType_e.swControlType_Textbox,
                "Type Here",
                leftAlign,
                ctlOptions,
                "This is an example textbox");

            _checkbox1 = (IPropertyManagerPageCheckbox)_group1.AddControl(
                checkbox1ID,
                (int)swPropertyManagerPageControlType_e.swControlType_Checkbox,
                "Sample Checkbox",
                leftAlign,
                ctlOptions,
                "This is a sample checkbox");

            _option1 = (IPropertyManagerPageOption)_group1.AddControl(
                option1ID,
                (int)swPropertyManagerPageControlType_e.swControlType_Option,
                "Option1",
                leftAlign,
                ctlOptions,
                "Radio Buttons");

            _option2 = (IPropertyManagerPageOption)_group1.AddControl(
                option2ID,
                (int)swPropertyManagerPageControlType_e.swControlType_Option,
                "Option2",
                leftAlign,
                ctlOptions,
                "Radio Buttons");

            _option3 = (IPropertyManagerPageOption)_group1.AddControl(
                option3ID,
                (int)swPropertyManagerPageControlType_e.swControlType_Option,
                "Option3",
                leftAlign,
                ctlOptions,
                "Radio Buttons");

            _list1 = (IPropertyManagerPageListbox)_group1.AddControl(
                list1ID,
                (int)swPropertyManagerPageControlType_e.swControlType_Listbox,
                "Sample Listbox",
                leftAlign,
                ctlOptions,
                "List of selectable items");

            if (_list1 != null)
            {
                string[] items = { "One Fish", "Two Fish", "Red Fish", "Blue Fish" };
                _list1.Height = 50;
                _list1.AddItems(items);
            }

            // Group 2 controls
            _selection1 = (IPropertyManagerPageSelectionbox)_group2.AddControl(
                selection1ID,
                (int)swPropertyManagerPageControlType_e.swControlType_Selectionbox,
                "Sample Selection",
                leftAlign,
                ctlOptions,
                "Displays features selected in main view");

            if (_selection1 != null)
            {
                int[] filter = { (int)swSelectType_e.swSelEDGES, (int)swSelectType_e.swSelVERTICES };
                _selection1.Height = 40;
                _selection1.SetSelectionFilters(filter);
            }

            _num1 = (IPropertyManagerPageNumberbox)_group2.AddControl(
                num1ID,
                (int)swPropertyManagerPageControlType_e.swControlType_Numberbox,
                "Sample Numberbox",
                leftAlign,
                ctlOptions,
                "Allows for numerical input");

            if (_num1 != null)
            {
                _num1.Value = 50.0;
                _num1.SetRange(
                    (int)swNumberboxUnitType_e.swNumberBox_UnitlessDouble,
                    0.0,
                    100.0,
                    0.01,
                    true);
            }

            _combo1 = (IPropertyManagerPageCombobox)_group2.AddControl(
                combo1ID,
                (int)swPropertyManagerPageControlType_e.swControlType_Combobox,
                "Sample Combobox",
                leftAlign,
                ctlOptions,
                "Combo list");

            if (_combo1 != null)
            {
                string[] items = { "One Fish", "Two Fish", "Red Fish", "Blue Fish" };
                _combo1.AddItems(items);
                _combo1.Height = 50;
            }
        }
    }
}