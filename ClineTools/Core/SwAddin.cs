using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop.swpublished;
using SolidWorksTools;
using SolidWorksTools.File;

namespace ClineTools
{
    [Guid("ebd88e14-faa0-4384-86d5-8b2829475b2a"), ComVisible(true)]
    [SwAddin(
        Description = "Cline Tool macro library",
        Title = "ClineTools",
        LoadAtStartup = true)]
    public class SwAddin : ISwAddin
    {
        #region Local Variables

        static SwAddin()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        }

        private ISldWorks _swApp;
        private ICommandManager _cmdMgr;
        private int _addinId;
        private BitmapHandler _bmpHandler;
        private ModManager _moduleManager;

        // Global on/off switch for ClineTools functions
        private bool _clineToolsEnabled = true;
        private bool _modulesLoaded;

        public const int mainCmdGroupID = 5;
        public const int mainItemID1 = 0;
        public const int mainItemID2 = 1;
        public const int mainItemID3 = 2;
        public const int mainItemID4 = 3;
        public const int mainItemID5 = 4;
        public const int flyoutGroupID = 91;

        // Event handler state
        private Hashtable _openDocs = new Hashtable();
        private SldWorks _swEventPtr;

        // Property Manager
        private UserPMPage _ppage;

        // Public Properties
        public ISldWorks SwApp => _swApp;
        public ICommandManager CmdMgr => _cmdMgr;
        public Hashtable OpenDocs => _openDocs;

        #endregion

        #region SolidWorks Registration
        [ComRegisterFunctionAttribute]
        public static void RegisterFunction(Type t)
        {
            SwAddinAttribute swAttr = null;
            Type type = typeof(SwAddin);

            foreach (System.Attribute attr in type.GetCustomAttributes(false))
            {
                if (attr is SwAddinAttribute)
                {
                    swAttr = attr as SwAddinAttribute;
                    break;
                }
            }

            try
            {
                Microsoft.Win32.RegistryKey hklm = Microsoft.Win32.Registry.LocalMachine;
                Microsoft.Win32.RegistryKey hkcu = Microsoft.Win32.Registry.CurrentUser;

                string keyname = "SOFTWARE\\SolidWorks\\Addins\\{" + t.GUID.ToString() + "}";
                Microsoft.Win32.RegistryKey addinkey = hklm.CreateSubKey(keyname);
                addinkey.SetValue(null, 0);

                addinkey.SetValue("Description", swAttr.Description);
                addinkey.SetValue("Title", swAttr.Title);

                keyname = "Software\\SolidWorks\\AddInsStartup\\{" + t.GUID.ToString() + "}";
                addinkey = hkcu.CreateSubKey(keyname);
                addinkey.SetValue(null, Convert.ToInt32(swAttr.LoadAtStartup), Microsoft.Win32.RegistryValueKind.DWord);
            }
            catch (NullReferenceException nl)
            {
                Console.WriteLine("There was a problem registering this dll: SWattr is null. \n\"" + nl.Message + "\"");
                System.Windows.Forms.MessageBox.Show("There was a problem registering this dll: SWattr is null.\n\"" + nl.Message + "\"");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                System.Windows.Forms.MessageBox.Show("There was a problem registering the function: \n\"" + e.Message + "\"");
            }
        }

        [ComUnregisterFunctionAttribute]
        public static void UnregisterFunction(Type t)
        {
            try
            {
                Microsoft.Win32.RegistryKey hklm = Microsoft.Win32.Registry.LocalMachine;
                Microsoft.Win32.RegistryKey hkcu = Microsoft.Win32.Registry.CurrentUser;

                string keyname = "SOFTWARE\\SolidWorks\\Addins\\{" + t.GUID.ToString() + "}";
                hklm.DeleteSubKey(keyname);

                keyname = "Software\\SolidWorks\\AddInsStartup\\{" + t.GUID.ToString() + "}";
                hkcu.DeleteSubKey(keyname);
            }
            catch (NullReferenceException nl)
            {
                Console.WriteLine("There was a problem unregistering this dll: " + nl.Message);
                System.Windows.Forms.MessageBox.Show("There was a problem unregistering this dll: \n\"" + nl.Message + "\"");
            }
            catch (Exception e)
            {
                Console.WriteLine("There was a problem unregistering this dll: " + e.Message);
                System.Windows.Forms.MessageBox.Show("There was a problem unregistering this dll: \n\"" + e.Message + "\"");
            }
        }

        public SwAddin()
        {
        }

        #endregion

        #region ISwAddin Implementation
        public bool ConnectToSW(object thisSw, int cookie)
        {
            DebugTrace.Log("ConnectToSW: ENTER");

            try
            {
                _swApp = (ISldWorks)thisSw;
                _addinId = cookie;

                DebugTrace.Log("ConnectToSW: _swApp + _addinId set");

                // Module manager + modules
                _moduleManager = new ModManager(_swApp);
                DebugTrace.Log("ConnectToSW: ModManager created");

                _moduleManager.LoadModules();
                _modulesLoaded = true;
                _clineToolsEnabled = true;

                DebugTrace.Log("ConnectToSW: Modules loaded");

                // Callback info must be set early
                _swApp.SetAddinCallbackInfo(0, this, _addinId);
                DebugTrace.Log("ConnectToSW: SetAddinCallbackInfo complete");

                // Command manager + UI
                _cmdMgr = _swApp.GetCommandManager(cookie);
                DebugTrace.Log("ConnectToSW: GetCommandManager complete");

                AddCommandMgr();
                DebugTrace.Log("ConnectToSW: AddCommandMgr complete");

                // Events
                _swEventPtr = (SldWorks)_swApp;
                _openDocs = new Hashtable();

                DebugTrace.Log("ConnectToSW: Event ptr + openDocs set");

                AttachEventHandlers();
                DebugTrace.Log("ConnectToSW: AttachEventHandlers complete");

                // PMP
                AddPMP();
                DebugTrace.Log("ConnectToSW: AddPMP complete");

                DebugTrace.Log("ConnectToSW: SUCCESS");
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    DebugTrace.DumpOnError(ex, "ConnectToSW FAILURE");
                    DebugTrace.Log("ConnectToSW: FAIL (see DumpOnError entry)");
                }
                catch
                {
                    // Never throw from the catch
                }

                // IMPORTANT: show full exception, not just Message, because LoaderExceptions matter
                System.Windows.Forms.MessageBox.Show(
                    "ClineTools failed to load:\n\n" + ex,
                    "ClineTools Add-in Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);

                return false;
            }
        }

        public bool DisconnectFromSW()
        {
            try
            {
                RemoveCommandMgr();
                RemovePMP();
                DetachEventHandlers();

                if (_moduleManager != null)
                {
                    _moduleManager.UnloadModules();
                    _moduleManager = null;
                }

                _modulesLoaded = false;
                _clineToolsEnabled = false;

                if (_cmdMgr != null)
                {
                    Marshal.ReleaseComObject(_cmdMgr);
                    _cmdMgr = null;
                }

                if (_swApp != null)
                {
                    Marshal.ReleaseComObject(_swApp);
                    _swApp = null;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                return true;
            }
            catch
            {
                return true; // keep legacy behavior: SW expects disconnect to be resilient
            }
        }
        #endregion

        #region UI Methods
        public void AddCommandMgr()
        {
            ICommandGroup cmdGroup;
            if (_bmpHandler == null)
                _bmpHandler = new BitmapHandler();

            int cmdIndexCreatePlane, cmdIndexToggle, cmdIndexPoint;
            const string Title = "Cline Tools";
            const string ToolTip = "Cline Tools";

            int[] docTypes =
            {
                (int)swDocumentTypes_e.swDocASSEMBLY,
                (int)swDocumentTypes_e.swDocDRAWING,
                (int)swDocumentTypes_e.swDocPART
            };

            Assembly thisAssembly = Assembly.GetAssembly(GetType());

            int cmdGroupErr = 0;

            // Always rebuild the command group to avoid stale registry issues
            bool ignorePrevious = true;

            cmdGroup = _cmdMgr.CreateCommandGroup2(
                mainCmdGroupID, Title, ToolTip, "", -1, ignorePrevious, ref cmdGroupErr);

            cmdGroup.LargeIconList = _bmpHandler.CreateFileFromResourceBitmap("ClineTools.ToolbarLarge.bmp", thisAssembly);
            cmdGroup.SmallIconList = _bmpHandler.CreateFileFromResourceBitmap("ClineTools.ToolbarSmall.bmp", thisAssembly);
            cmdGroup.LargeMainIcon = _bmpHandler.CreateFileFromResourceBitmap("ClineTools.MainIconLarge.bmp", thisAssembly);
            cmdGroup.SmallMainIcon = _bmpHandler.CreateFileFromResourceBitmap("ClineTools.MainIconSmall.bmp", thisAssembly);

            int menuToolbarOption = (int)(swCommandItemType_e.swMenuItem | swCommandItemType_e.swToolbarItem);

            cmdIndexCreatePlane = cmdGroup.AddCommandItem2(
                "Create Length Plane", -1,
                "Create an offset reference plane in the active assembly",
                "Create Length Plane", 0,
                "CreateAssemblyPlaneFeature",
                "EnableOnlyInAssembly",
                mainItemID1, menuToolbarOption);

            cmdIndexToggle = cmdGroup.AddCommandItem2(
                "Toggle ClineTools", -1,
                "Enable or disable ClineTools functions",
                "Toggle ClineTools", 1,
                "ToggleClineTools",
                "EnableToggleClineTools",
                mainItemID2, menuToolbarOption);

            FlyoutGroup flyGroup = _cmdMgr.CreateFlyoutGroup(
                flyoutGroupID,
                "Release to MFG",
                "Release to MFG",
                "Release to MFG",
                cmdGroup.SmallMainIcon,
                cmdGroup.LargeMainIcon,
                cmdGroup.SmallIconList,
                cmdGroup.LargeIconList,
                "ReleaseFlyoutCallback",
                "ReleaseFlyoutEnable"
            );

            cmdIndexPoint = cmdGroup.AddCommandItem2(
                "Insert Point Detail", -1,
                "Insert point detail into current drawing",
                "Insert Point Detail", 2,
                "InsertPointDetail",
                "EnableInsertPointDetail",
                mainItemID4, menuToolbarOption);

            flyGroup.AddCommandItem(
                "Release to MFG",
                "Release current project to MFG",
                0,
                "ReleaseToMfg",
                "EnableReleaseToMfg"
            );

            flyGroup.AddCommandItem(
                "Release Legacy Project",
                "Release a legacy project (not yet implemented)",
                0,
                "ReleaseLegacyProject",
                "EnableReleaseLegacyProject"
            );

            int cmdIndexWhereUsed = cmdGroup.AddCommandItem2(
                "Where Used",
                -1,
                "Find assemblies that reference the active part",
                "Where Used",
                3,                      // icon index (reuse an existing one for now)
                "WhereUsed",
                "EnableWhereUsed",
                mainItemID5,
                menuToolbarOption
            );

            cmdGroup.HasToolbar = true;
            cmdGroup.HasMenu = true;
            cmdGroup.Activate();

            flyGroup.FlyoutType = (int)swCommandFlyoutStyle_e.swCommandFlyoutStyle_Simple;

            foreach (int type in docTypes)
            {
                CommandTab cmdTab = _cmdMgr.GetCommandTab(type, Title);

                if (cmdTab != null)
                {
                    _cmdMgr.RemoveCommandTab(cmdTab);
                    cmdTab = null;
                }

                cmdTab = _cmdMgr.AddCommandTab(type, Title);
                if (cmdTab == null)
                    continue;

                CommandTabBox cmdBox = cmdTab.AddCommandTabBox();

                var cmdIDs = new List<int>();
                var textTypes = new List<int>();

                cmdIDs.Add(cmdGroup.get_CommandID(cmdIndexCreatePlane));
                textTypes.Add((int)swCommandTabButtonTextDisplay_e.swCommandTabButton_TextBelow);

                cmdIDs.Add(flyGroup.CmdID);
                textTypes.Add(
                    (int)swCommandTabButtonTextDisplay_e.swCommandTabButton_TextBelow |
                    (int)swCommandTabButtonFlyoutStyle_e.swCommandTabButton_ActionFlyout);

                cmdIDs.Add(cmdGroup.get_CommandID(cmdIndexPoint));
                textTypes.Add((int)swCommandTabButtonTextDisplay_e.swCommandTabButton_TextBelow);

                cmdIDs.Add(cmdGroup.get_CommandID(cmdIndexToggle));
                textTypes.Add((int)swCommandTabButtonTextDisplay_e.swCommandTabButton_TextBelow);

                cmdIDs.Add(cmdGroup.get_CommandID(cmdIndexWhereUsed));
                textTypes.Add((int)swCommandTabButtonTextDisplay_e.swCommandTabButton_TextBelow);

                cmdBox.AddCommands(cmdIDs.ToArray(), textTypes.ToArray());
            }
        }

        public void RemoveCommandMgr()
        {
            try
            {
                _bmpHandler?.Dispose();
                _bmpHandler = null;

                _cmdMgr?.RemoveCommandGroup(mainCmdGroupID);
                _cmdMgr?.RemoveFlyoutGroup(flyoutGroupID);
            }
            catch
            {
                // keep legacy behavior: do not throw during unload
            }
        }

        public bool CompareIDs(int[] storedIDs, int[] addinIDs)
        {
            List<int> storedList = new List<int>(storedIDs);
            List<int> addinList = new List<int>(addinIDs);

            addinList.Sort();
            storedList.Sort();

            if (addinList.Count != storedList.Count)
                return false;

            for (int i = 0; i < addinList.Count; i++)
            {
                if (addinList[i] != storedList[i])
                    return false;
            }

            return true;
        }

        public bool AddPMP()
        {
            _ppage = new UserPMPage(this);
            return true;
        }

        public bool RemovePMP()
        {
            _ppage = null;
            return true;
        }
        #endregion

        #region UI Callbacks

        #region Create Length Plane

        // Offset Length Plane
        private static bool TryParseDistanceToMeters(string raw, out double meters)
        {
            meters = 0;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            string s = raw.Trim();

            var m = Regex.Match(
                s,
                @"^\s*([+-]?\d+(?:\.\d+)?)\s*([a-z""']+)?\s*$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (!m.Success) return false;

            if (!double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                return false;

            string unit = (m.Groups[2].Success ? m.Groups[2].Value : "").Trim().ToLowerInvariant();

            switch (unit)
            {
                case "":
                case "in":
                case "inch":
                case "inches":
                case "\"":
                    meters = value * 0.0254;
                    return true;

                case "mm":
                case "millimeter":
                case "millimeters":
                    meters = value / 1000.0;
                    return true;

                default:
                    return false;
            }
        }

        public void CreateAssemblyPlaneFeature()
        {
            if (!_clineToolsEnabled)
            {
                System.Windows.Forms.MessageBox.Show(
                    "ClineTools functions are currently disabled. Re-enable them using the \"Toggle ClineTools\" button in the ClineTools CommandManager tab to use this command.",
                    "ClineTools – Disabled",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
                return;
            }

            try
            {
                var doc = _swApp.ActiveDoc as ModelDoc2;
                if (doc == null || doc.GetType() != (int)swDocumentTypes_e.swDocASSEMBLY)
                {
                    System.Windows.Forms.MessageBox.Show("Open an assembly to use this feature.");
                    return;
                }

                string defaultText = "Offset Reference Plane";
                string input = Microsoft.VisualBasic.Interaction.InputBox(
                    "Offset distance from Right Plane",
                    defaultText
                );
                if (string.IsNullOrWhiteSpace(input)) return;

                if (!TryParseDistanceToMeters(input, out double offsetMeters))
                {
                    System.Windows.Forms.MessageBox.Show("Could not parse distance. Use proper formatting (e.g. 1.532in, 44mm, etc.");
                    return;
                }

                doc.ClearSelection2(true);
                bool sel = doc.Extension.SelectByID2("Right Plane", "PLANE", 0, 0, 0, false, 0, null, 0);
                if (!sel)
                {
                    System.Windows.Forms.MessageBox.Show(
                        "Could not select 'Right Plane'. Select a plane first or adjust the name for your locale."
                    );
                    return;
                }

                var featMgr = doc.FeatureManager;
                Feature newPlane = featMgr.InsertRefPlane(
                    (int)swRefPlaneReferenceConstraints_e.swRefPlaneReferenceConstraint_Distance,
                    offsetMeters, 0, 0, 0, 0
                );

                if (newPlane == null)
                {
                    System.Windows.Forms.MessageBox.Show("Failed to create reference plane.");
                    return;
                }

                string baseName = "LENGTH PLANE";
                int nextIndex = 2;
                bool baseExists = false;

                Feature feat = doc.FirstFeature();
                HashSet<string> existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                while (feat != null)
                {
                    string name = feat.Name;
                    if (!string.IsNullOrEmpty(name))
                    {
                        existingNames.Add(name);

                        if (name.Equals(baseName, StringComparison.OrdinalIgnoreCase))
                        {
                            baseExists = true;
                        }
                        else if (name.StartsWith(baseName + " ", StringComparison.OrdinalIgnoreCase))
                        {
                            string numPart = name.Substring(baseName.Length).Trim();
                            if (int.TryParse(numPart, out int num))
                                nextIndex = Math.Max(nextIndex, num + 1);
                        }
                    }
                    feat = feat.GetNextFeature();
                }

                string newName = !baseExists ? baseName : $"{baseName} {nextIndex}";
                newPlane.Name = newName;

                doc.ClearSelection2(true);

                _swApp.SendMsgToUser2(
                    $"Created new length plane: {newName}\nOffset {input.Trim()} from Right Plane.",
                    (int)swMessageBoxIcon_e.swMbInformation,
                    (int)swMessageBoxBtn_e.swMbOk
                );
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Error creating assembly feature: " + ex.Message);
            }
        }

        public int EnableOnlyInAssembly()
        {
            if (_swApp == null || !_clineToolsEnabled)
                return 0;

            var doc = _swApp.ActiveDoc as ModelDoc2;
            if (doc == null)
                return 0;

            return (doc.GetType() == (int)swDocumentTypes_e.swDocASSEMBLY) ? 1 : 0;
        }

        #endregion

        #region Release to MFG

        // Release flyout content is built dynamically when the user opens the dropdown
        public void ReleaseFlyoutCallback()
        {
            if (_cmdMgr == null)
                return;

            FlyoutGroup flyGroup = _cmdMgr.GetFlyoutGroup(flyoutGroupID);
            if (flyGroup == null)
                return;

            flyGroup.RemoveAllCommandItems();

            flyGroup.AddCommandItem(
                "Release Project",
                "Release current project to manufacturing",
                0,
                "ReleaseToMfg",
                "EnableReleaseToMfg");

            flyGroup.AddCommandItem(
                "Release Legacy Project",
                "Release a legacy project (not yet implemented)",
                1,
                "ReleaseLegacyProject",
                "EnableReleaseLegacyProject");
        }

        public int ReleaseFlyoutEnable()
        {
            return (_swApp != null && _swApp.ActiveDoc != null && _clineToolsEnabled) ? 1 : 0;
        }

        public void ReleaseToMfg()
        {
            if (_swApp == null)
            {
                System.Windows.Forms.MessageBox.Show(
                    "The SolidWorks application object is not available.",
                    "ClineTools – Release to MFG",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error
                );
                return;
            }

            if (!_clineToolsEnabled)
            {
                System.Windows.Forms.MessageBox.Show(
                    "ClineTools functions are currently disabled. Re-enable them using the \"Toggle ClineTools\" button in the ClineTools CommandManager tab before initiating a release to manufacturing.",
                    "ClineTools – Release to MFG",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
                return;
            }

            var doc = _swApp.ActiveDoc as ModelDoc2;
            if (doc == null)
            {
                System.Windows.Forms.MessageBox.Show(
                    "No active document was detected. Please open a model or drawing before initiating a release.",
                    "ClineTools – Release to MFG",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning
                );
                return;
            }

            bool hasUnsavedChanges = doc.GetSaveFlag();
            if (hasUnsavedChanges)
            {
                System.Windows.Forms.MessageBox.Show(
                    "The active document contains unsaved changes. Please save your file before releasing.",
                    "ClineTools – Release to MFG",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning
                );
                return;
            }

            try
            {
                var module = _moduleManager.GetModule<ClineTools.Modules.Release.ReleaseModule>();
                if (module == null)
                {
                    System.Windows.Forms.MessageBox.Show(
                        "The Release module is not available. Please contact your system administrator.",
                        "ClineTools – Release to MFG",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Error
                    );
                    return;
                }

                module.RunReleaseProcess();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    "An unexpected error occurred while starting the release process:\n" + ex.Message,
                    "ClineTools – Release to MFG",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error
                );
            }
        }

        public int EnableReleaseToMfg()
        {
            return (_swApp != null && _swApp.ActiveDoc != null && _clineToolsEnabled) ? 1 : 0;
        }

        public void ReleaseLegacyProject()
        {
            System.Windows.Forms.MessageBox.Show(
                "Release Legacy Project has not been implemented.",
                "ClineTools"
            );
        }

        public int EnableReleaseLegacyProject()
        {
            return (_swApp != null && _swApp.ActiveDoc != null) ? 1 : 0;
        }

        #endregion

        #region Toggle ClineTools

        // Global toggle button callback
        public void ToggleClineTools()
        {
            DebugTrace.DumpSnapshot("Toggle Cline Tools clicked");

            if (_clineToolsEnabled)
            {
                if (_modulesLoaded && _moduleManager != null)
                {
                    _moduleManager.UnloadModules();
                    _modulesLoaded = false;
                }

                _clineToolsEnabled = false;

                System.Windows.Forms.MessageBox.Show(
                    "ClineTools Add-In disabled.",
                    "ClineTools – Global Toggle",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
            }
            else
            {
                if (!_modulesLoaded && _moduleManager != null && _swApp != null)
                {
                    _moduleManager.LoadModules();
                    _modulesLoaded = true;
                }

                _clineToolsEnabled = true;

                System.Windows.Forms.MessageBox.Show(
                    "ClineTools Add-In enabled.",
                    "ClineTools – Global Toggle",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
            }
        }

        public int EnableToggleClineTools()
        {
            return 1;
        }

        #endregion

        #region Point Detail

        public int EnableInsertPointDetail()
        {
            if (_swApp == null || !_clineToolsEnabled) return 0;

            var model = _swApp.ActiveDoc as ModelDoc2;
            if (model == null) return 0;

            return (model.GetType() == (int)swDocumentTypes_e.swDocDRAWING) ? 1 : 0;
        }

        public void InsertPointDetail()
        {
            var model = _swApp?.ActiveDoc as ModelDoc2;
            if (model == null || model.GetType() != (int)swDocumentTypes_e.swDocDRAWING)
                return;

            if (!Modules.PointDetail.PointDetailWizard.TryGetRequest(null, out var req))
                return;

            string gDrillBlockPath = @"F:\Engineer\_STANDARD LIBRARY\CLINE DESIGN LIBRARY\BLOCKS\G DRILL.SLDBLK";
            string spiral2FluteBlockPath = @"F:\Engineer\_STANDARD LIBRARY\CLINE DESIGN LIBRARY\BLOCKS\2 FLUTE SPIRAL DRILL.SLDBLK";

            Modules.PointDetail.PointDetailResult res;
            string blockPath;
            Dictionary<string, string> values;

            if (req.Type.Equals("G-DRILL", StringComparison.OrdinalIgnoreCase))
            {
                res = Modules.PointDetail.PointDetailCalculator.Calculate(req.DiameterValue, req.Unit);
                blockPath = gDrillBlockPath;

                values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["PDIAMETER"] = res.PDIAMETER,
                    ["PtMargin"] = res.PtMargin,
                    ["PtAoC"] = res.PtAoC,
                    ["PtHone"] = res.PtHone,
                    ["PtClearanceDia"] = res.PtClearanceDia,
                    ["PtConeRelief"] = res.PtConeRelief,
                    ["PtGashRadius"] = res.PtGashRadius,
                    ["PtChislePointThck"] = res.PtChislePointThck,
                    ["PtFluteRadius"] = res.PtFluteRadius,
                    ["PtBackTaper"] = res.PtBackTaper,
                    ["PtSecondaryRAngle"] = res.PtSecondaryRAngle,
                };
            }
            else if (req.Type.Equals("2 FLUTE SPIRAL DRILL", StringComparison.OrdinalIgnoreCase))
            {
                res = Modules.PointDetail.PointDetailCalculator.Calculate2FluteSpiral(req.DiameterValue, req.Unit);
                blockPath = spiral2FluteBlockPath;

                values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["PDIAMETER"] = res.PDIAMETER,
                    ["PtGashRadius"] = res.PtGashRadius,
                    ["PtAoC"] = res.PtAoC,
                    ["PtKLand"] = res.PtKLand,
                };
            }
            else
            {
                System.Windows.Forms.MessageBox.Show(
                    $"Unknown point detail type selected:\n{req.Type}",
                    "Insert Point Detail",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            Modules.PointDetail.PointDetailPropertyWriter.WriteDrawingProps(model, req, res);

            if (!Modules.PointDetail.PointDetailAnchorProps.TryGetAnchorInches(model, out double xIn, out double yIn))
            {
                System.Windows.Forms.MessageBox.Show(
                    "Point Detail anchor is not configured.\n\n" +
                    "Add these drawing custom properties to your drawing TEMPLATE (.drwdot):\n" +
                    "  CT_POINTDETAIL_X_IN\n" +
                    "  CT_POINTDETAIL_Y_IN\n\n" +
                    "Values must be numeric inches (example: 0.310768).",
                    "Insert Point Detail",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error
                );
                return;
            }

            var inst = Modules.PointDetail.PointDetailBlockInserter
                .InsertAtStoredAnchor(_swApp, model, blockPath, xIn, yIn);

            if (inst == null)
            {
                System.Windows.Forms.MessageBox.Show(
                    "Block insertion failed (instance is null).",
                    "Insert Point Detail",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }

            Modules.PointDetail.PointDetailBlockInserter.PopulateAttributes(inst, values);

            model.ForceRebuild3(false);
            model.GraphicsRedraw2();
        }

        #endregion

        #region Where Used

        public void WhereUsed()
        {
            if (!_clineToolsEnabled)
                return;

            try
            {
                var module = _moduleManager.GetModule<ClineTools.Modules.WhereUsed.WhereUsedModule>();
                if (module == null)
                {
                    System.Windows.Forms.MessageBox.Show(
                        "WhereUsed module not loaded.",
                        "ClineTools – Where Used",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Error);
                    return;
                }

                module.RunWhereUsed();
            }
            catch (Exception ex)
            {
                DebugTrace.DumpOnError(ex, "WhereUsed");
                System.Windows.Forms.MessageBox.Show(
                    "Where Used failed:\n\n" + ex,
                    "ClineTools – Where Used",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        public int EnableWhereUsed()
        {
            if (_swApp == null || !_clineToolsEnabled)
                return 0;

            var doc = _swApp.IActiveDoc2;
            if (doc == null)
                return 0;

            // PART ONLY (per your requirement)
            return doc.GetType() == (int)swDocumentTypes_e.swDocPART ? 1 : 0;
        }

        #endregion

        #endregion

        #region Event Methods
        public bool AttachEventHandlers()
        {
            AttachSwEvents();
            AttachEventsToAllDocuments();
            return true;
        }

        private bool AttachSwEvents()
        {
            try
            {
                _swEventPtr.ActiveDocChangeNotify += new DSldWorksEvents_ActiveDocChangeNotifyEventHandler(OnDocChange);
                _swEventPtr.DocumentLoadNotify2 += new DSldWorksEvents_DocumentLoadNotify2EventHandler(OnDocLoad);
                _swEventPtr.FileNewNotify2 += new DSldWorksEvents_FileNewNotify2EventHandler(OnFileNew);
                _swEventPtr.ActiveModelDocChangeNotify += new DSldWorksEvents_ActiveModelDocChangeNotifyEventHandler(OnModelChange);
                _swEventPtr.FileOpenPostNotify += new DSldWorksEvents_FileOpenPostNotifyEventHandler(FileOpenPostNotify);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        private bool DetachSwEvents()
        {
            try
            {
                _swEventPtr.ActiveDocChangeNotify -= new DSldWorksEvents_ActiveDocChangeNotifyEventHandler(OnDocChange);
                _swEventPtr.DocumentLoadNotify2 -= new DSldWorksEvents_DocumentLoadNotify2EventHandler(OnDocLoad);
                _swEventPtr.FileNewNotify2 -= new DSldWorksEvents_FileNewNotify2EventHandler(OnFileNew);
                _swEventPtr.ActiveModelDocChangeNotify -= new DSldWorksEvents_ActiveModelDocChangeNotifyEventHandler(OnModelChange);
                _swEventPtr.FileOpenPostNotify -= new DSldWorksEvents_FileOpenPostNotifyEventHandler(FileOpenPostNotify);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        public void AttachEventsToAllDocuments()
        {
            ModelDoc2 modDoc = (ModelDoc2)_swApp.GetFirstDocument();
            while (modDoc != null)
            {
                if (!_openDocs.Contains(modDoc))
                {
                    AttachModelDocEventHandler(modDoc);
                }
                modDoc = (ModelDoc2)modDoc.GetNext();
            }
        }

        public bool AttachModelDocEventHandler(ModelDoc2 modDoc)
        {
            if (modDoc == null)
                return false;

            DocumentEventHandler docHandler = null;

            if (!_openDocs.Contains(modDoc))
            {
                switch (modDoc.GetType())
                {
                    case (int)swDocumentTypes_e.swDocPART:
                        docHandler = new PartEventHandler(modDoc, this);
                        break;

                    case (int)swDocumentTypes_e.swDocASSEMBLY:
                        docHandler = new AssemblyEventHandler(modDoc, this);
                        break;

                    case (int)swDocumentTypes_e.swDocDRAWING:
                        docHandler = new DrawingEventHandler(modDoc, this);
                        break;

                    default:
                        return false;
                }

                docHandler.AttachEventHandlers();
                _openDocs.Add(modDoc, docHandler);
            }

            return true;
        }

        public bool DetachModelEventHandler(ModelDoc2 modDoc)
        {
            DocumentEventHandler docHandler = (DocumentEventHandler)_openDocs[modDoc];
            _openDocs.Remove(modDoc);
            modDoc = null;
            docHandler = null;
            return true;
        }

        public bool DetachEventHandlers()
        {
            DetachSwEvents();

            DocumentEventHandler docHandler;
            int numKeys = _openDocs.Count;
            object[] keys = new object[numKeys];

            _openDocs.Keys.CopyTo(keys, 0);
            foreach (ModelDoc2 key in keys)
            {
                docHandler = (DocumentEventHandler)_openDocs[key];
                docHandler.DetachEventHandlers();
                docHandler = null;
            }

            return true;
        }
        #endregion

        #region Event Handlers
        public int OnDocChange() => 0;
        public int OnDocLoad(string docTitle, string docPath) => 0;

        int FileOpenPostNotify(string fileName)
        {
            AttachEventsToAllDocuments();
            return 0;
        }

        public int OnFileNew(object newDoc, int docType, string templateName)
        {
            AttachEventsToAllDocuments();
            return 0;
        }

        public int OnModelChange() => 0;
        #endregion
    }
}