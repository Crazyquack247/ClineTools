using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System.Collections.Generic;

namespace ClineTools
{
    public class DocumentEventHandler
    {
        protected readonly ModelDoc2 document;
        protected readonly SwAddin userAddin;

        private readonly Dictionary<IModelView, DocView> _openModelViews;

        public DocumentEventHandler(ModelDoc2 modDoc, SwAddin addin)
        {
            document = modDoc;
            userAddin = addin;
            _openModelViews = new Dictionary<IModelView, DocView>();
        }

        public virtual bool AttachEventHandlers()
        {
            return true;
        }

        public virtual bool DetachEventHandlers()
        {
            return true;
        }

        public bool ConnectModelViews()
        {
            IModelView mView = (IModelView)document.GetFirstModelView();
            while (mView != null)
            {
                if (!_openModelViews.ContainsKey(mView))
                {
                    var dView = new DocView(userAddin, mView, this);
                    dView.AttachEventHandlers();
                    _openModelViews.Add(mView, dView);
                }

                mView = (IModelView)mView.GetNext();
            }

            return true;
        }

        public bool DisconnectModelViews()
        {
            if (_openModelViews.Count == 0)
                return true;

            // Copy keys because DetachEventHandlers will remove items
            var keys = new List<IModelView>(_openModelViews.Keys);

            foreach (IModelView key in keys)
            {
                if (_openModelViews.TryGetValue(key, out DocView dView))
                {
                    dView.DetachEventHandlers();
                    _openModelViews.Remove(key);
                }
            }

            return true;
        }

        public bool DetachModelViewEventHandler(ModelView mView)
        {
            if (mView != null && _openModelViews.ContainsKey(mView))
                _openModelViews.Remove(mView);

            return true;
        }
    }

    public class PartEventHandler : DocumentEventHandler
    {
        private readonly PartDoc doc;

        public PartEventHandler(ModelDoc2 modDoc, SwAddin addin)
            : base(modDoc, addin)
        {
            doc = (PartDoc)document;
        }

        public override bool AttachEventHandlers()
        {
            doc.DestroyNotify += new DPartDocEvents_DestroyNotifyEventHandler(OnDestroy);
            doc.NewSelectionNotify += new DPartDocEvents_NewSelectionNotifyEventHandler(OnNewSelection);

            ConnectModelViews();
            return true;
        }

        public override bool DetachEventHandlers()
        {
            doc.DestroyNotify -= new DPartDocEvents_DestroyNotifyEventHandler(OnDestroy);
            doc.NewSelectionNotify -= new DPartDocEvents_NewSelectionNotifyEventHandler(OnNewSelection);

            DisconnectModelViews();

            userAddin.DetachModelEventHandler(document);
            return true;
        }

        // Event Handlers
        public int OnDestroy()
        {
            DetachEventHandlers();
            return 0;
        }

        public int OnNewSelection()
        {
            return 0;
        }
    }

    public class AssemblyEventHandler : DocumentEventHandler
    {
        private readonly AssemblyDoc doc;

        public AssemblyEventHandler(ModelDoc2 modDoc, SwAddin addin)
            : base(modDoc, addin)
        {
            doc = (AssemblyDoc)document;
        }

        public override bool AttachEventHandlers()
        {
            doc.DestroyNotify += new DAssemblyDocEvents_DestroyNotifyEventHandler(OnDestroy);
            doc.NewSelectionNotify += new DAssemblyDocEvents_NewSelectionNotifyEventHandler(OnNewSelection);

            doc.ComponentStateChangeNotify2 += new DAssemblyDocEvents_ComponentStateChangeNotify2EventHandler(ComponentStateChangeNotify2);
            doc.ComponentStateChangeNotify += new DAssemblyDocEvents_ComponentStateChangeNotifyEventHandler(ComponentStateChangeNotify);
            doc.ComponentVisualPropertiesChangeNotify += new DAssemblyDocEvents_ComponentVisualPropertiesChangeNotifyEventHandler(ComponentVisualPropertiesChangeNotify);
            doc.ComponentDisplayStateChangeNotify += new DAssemblyDocEvents_ComponentDisplayStateChangeNotifyEventHandler(ComponentDisplayStateChangeNotify);

            ConnectModelViews();
            return true;
        }

        public override bool DetachEventHandlers()
        {
            doc.DestroyNotify -= new DAssemblyDocEvents_DestroyNotifyEventHandler(OnDestroy);
            doc.NewSelectionNotify -= new DAssemblyDocEvents_NewSelectionNotifyEventHandler(OnNewSelection);

            doc.ComponentStateChangeNotify2 -= new DAssemblyDocEvents_ComponentStateChangeNotify2EventHandler(ComponentStateChangeNotify2);
            doc.ComponentStateChangeNotify -= new DAssemblyDocEvents_ComponentStateChangeNotifyEventHandler(ComponentStateChangeNotify);
            doc.ComponentVisualPropertiesChangeNotify -= new DAssemblyDocEvents_ComponentVisualPropertiesChangeNotifyEventHandler(ComponentVisualPropertiesChangeNotify);
            doc.ComponentDisplayStateChangeNotify -= new DAssemblyDocEvents_ComponentDisplayStateChangeNotifyEventHandler(ComponentDisplayStateChangeNotify);

            DisconnectModelViews();

            userAddin.DetachModelEventHandler(document);
            return true;
        }

        // Event Handlers
        public int OnDestroy()
        {
            DetachEventHandlers();
            return 0;
        }

        public int OnNewSelection()
        {
            return 0;
        }

        // Attach events to a component if it becomes resolved
        protected int ComponentStateChange(object componentModel, short newCompState)
        {
            ModelDoc2 modDoc = (ModelDoc2)componentModel;
            swComponentSuppressionState_e newState = (swComponentSuppressionState_e)newCompState;

            switch (newState)
            {
                case swComponentSuppressionState_e.swComponentFullyResolved:
                case swComponentSuppressionState_e.swComponentResolved:
                    if (modDoc != null && !userAddin.OpenDocs.Contains(modDoc))
                        userAddin.AttachModelDocEventHandler(modDoc);
                    break;
            }

            return 0;
        }

        protected int ComponentStateChange(object componentModel)
        {
            ComponentStateChange(componentModel, (short)swComponentSuppressionState_e.swComponentResolved);
            return 0;
        }

        public int ComponentStateChangeNotify2(object componentModel, string compName, short oldCompState, short newCompState)
        {
            return ComponentStateChange(componentModel, newCompState);
        }

        private int ComponentStateChangeNotify(object componentModel, short oldCompState, short newCompState)
        {
            return ComponentStateChange(componentModel, newCompState);
        }

        private int ComponentDisplayStateChangeNotify(object swObject)
        {
            Component2 component = (Component2)swObject;
            ModelDoc2 modDoc = (ModelDoc2)component.GetModelDoc();

            return ComponentStateChange(modDoc);
        }

        private int ComponentVisualPropertiesChangeNotify(object swObject)
        {
            Component2 component = (Component2)swObject;
            ModelDoc2 modDoc = (ModelDoc2)component.GetModelDoc();

            return ComponentStateChange(modDoc);
        }
    }

    public class DrawingEventHandler : DocumentEventHandler
    {
        private readonly DrawingDoc doc;

        public DrawingEventHandler(ModelDoc2 modDoc, SwAddin addin)
            : base(modDoc, addin)
        {
            doc = (DrawingDoc)document;
        }

        public override bool AttachEventHandlers()
        {
            doc.DestroyNotify += new DDrawingDocEvents_DestroyNotifyEventHandler(OnDestroy);
            doc.NewSelectionNotify += new DDrawingDocEvents_NewSelectionNotifyEventHandler(OnNewSelection);

            ConnectModelViews();
            return true;
        }

        public override bool DetachEventHandlers()
        {
            doc.DestroyNotify -= new DDrawingDocEvents_DestroyNotifyEventHandler(OnDestroy);
            doc.NewSelectionNotify -= new DDrawingDocEvents_NewSelectionNotifyEventHandler(OnNewSelection);

            DisconnectModelViews();

            userAddin.DetachModelEventHandler(document);
            return true;
        }

        // Event Handlers
        public int OnDestroy()
        {
            DetachEventHandlers();
            return 0;
        }

        public int OnNewSelection()
        {
            return 0;
        }
    }

    public class DocView
    {
        private readonly SwAddin userAddin;
        private readonly ModelView mView;
        private readonly DocumentEventHandler parent;

        public DocView(SwAddin addin, IModelView mv, DocumentEventHandler doc)
        {
            userAddin = addin;
            mView = (ModelView)mv;
            parent = doc;
        }

        public bool AttachEventHandlers()
        {
            mView.DestroyNotify2 += new DModelViewEvents_DestroyNotify2EventHandler(OnDestroy);
            mView.RepaintNotify += new DModelViewEvents_RepaintNotifyEventHandler(OnRepaint);
            return true;
        }

        public bool DetachEventHandlers()
        {
            mView.DestroyNotify2 -= new DModelViewEvents_DestroyNotify2EventHandler(OnDestroy);
            mView.RepaintNotify -= new DModelViewEvents_RepaintNotifyEventHandler(OnRepaint);

            parent.DetachModelViewEventHandler(mView);
            return true;
        }

        // Event Handlers
        public int OnDestroy(int destroyType)
        {
            return 0;
        }

        public int OnRepaint(int repaintType)
        {
            return 0;
        }
    }
}