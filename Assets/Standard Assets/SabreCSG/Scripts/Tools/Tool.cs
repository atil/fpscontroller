#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Sabresaurus.SabreCSG
{
    public abstract class Tool
    {
        protected CSGModel csgModel;

        // Used so we can reset some of the tool if the selected brush changes
        protected PrimitiveBrush primaryTargetBrush;
		protected Transform primaryTargetBrushTransform;

		protected PrimitiveBrush[] targetBrushes;
		protected Transform[] targetBrushTransforms;

		bool panInProgress = false;

		public CSGModel CSGModel {
			get {
				return csgModel;
			}
			set {
				csgModel = value;
			}
		}

        public PrimitiveBrush PrimaryTargetBrush
        {
            get
            {
                return primaryTargetBrush;
            }
            set
            {

                if (primaryTargetBrush != value)
                {
					PrimarySelectionAboutToChange();

					primaryTargetBrush = value;

					ResetTool();

                    if (primaryTargetBrush != null)
                    {
                        
						primaryTargetBrushTransform = primaryTargetBrush.transform;
                    }
					else
					{
						primaryTargetBrushTransform = null;
					}
                }
            }
        }

		public PrimitiveBrush[] TargetBrushes
		{
			get
			{
				return targetBrushes;
			}
			set
			{
				if(!targetBrushes.ContentsEquals(value))
				{
					OnSelectionChanged();
					targetBrushes = value;
					targetBrushTransforms = targetBrushes.Select(brush => brush.transform).ToArray();
				}
			}
		}

		// Calculate the bounds for all selected brushes, respecting the current pivotRotation mode to produce 
		// bounds aligned to the first selected brush in Local mode, or bounds aligned to the absolute grid in Global
		// mode.
		public Bounds GetBounds()
		{
			Bounds bounds;

			if(Tools.pivotRotation == PivotRotation.Local)
			{
				bounds = primaryTargetBrush.GetBounds();

				for (int i = 0; i < targetBrushes.Length; i++) 
				{
					if(targetBrushes[i] != primaryTargetBrush)
					{
						bounds.Encapsulate(targetBrushes[i].GetBoundsLocalTo(primaryTargetBrush.transform));
					}
				}
			}
			else // Absolute/Global
			{
				bounds = primaryTargetBrush.GetBoundsTransformed();
				for (int i = 0; i < targetBrushes.Length; i++) 
				{
					if(targetBrushes[i] != primaryTargetBrush)
					{
						bounds.Encapsulate(targetBrushes[i].GetBoundsTransformed());
					}
				}
			}

			return bounds;
		}

		// Takes into account pivotRotation and the way Tool.GetBounds() works with absolute vs local modes
		public Vector3 TransformPoint(Vector3 point)
		{
			if(Tools.pivotRotation == PivotRotation.Local)
			{
				return primaryTargetBrushTransform.TransformPoint(point);	
			}
			else
			{
				return point;
			}
		}

		// Takes into account pivotRotation and the way Tool.GetBounds() works with absolute vs local modes
		public Vector3 InverseTransformDirection(Vector3 direction)
		{
			if(Tools.pivotRotation == PivotRotation.Local)
			{
				return primaryTargetBrushTransform.InverseTransformDirection(direction);	
			}
			else
			{
				return direction;
			}
		}

		// Takes into account pivotRotation and the way Tool.GetBounds() works with absolute vs local modes
		public Vector3 TransformDirection(Vector3 direction)
		{
			if(Tools.pivotRotation == PivotRotation.Local)
			{
				return primaryTargetBrushTransform.TransformDirection(direction);	
			}
			else
			{
				return direction;
			}
		}

		protected bool CameraPanInProgress
		{
			get
			{
				if(Tools.viewTool == ViewTool.Orbit)
				{
					return true;
				}
				else if(Tools.viewTool == ViewTool.FPS)
				{
					return true;
				}
				else if(Tools.viewTool == ViewTool.Zoom)
				{
					return true;
				}
				else if(Tools.viewTool == ViewTool.Pan)
				{
					// Unity will often report panning in progress even when it's not, so use a separate check
					return panInProgress;
				}
				else
				{
					return false;
				}
			}
		}

        public virtual void OnSceneGUI(SceneView sceneView, Event e)
		{
			if(e.type == EventType.MouseDown)
			{
				// You can use ctrl-alt and left drag on PC for pan. On OSX it's cmd-alt and left drag
				// Unfortunately even when you're not panning Unity will usually default to saying the viewTool is
				// still pan, so it's necessary to do a more substantial event driven check to see if panning is actually
				// in progress
#if UNITY_EDITOR_OSX
				if(e.command && e.alt)
				{
					panInProgress = true;
				}
#else
				if(e.control && e.alt)
				{
					panInProgress = true;
				}
#endif
			}
			else if(e.type == EventType.MouseUp)
			{
				panInProgress = false;
			}
		}

		// Called when the selected brush is about to change, to give the tool a last chance to cleanup anything it 
		// needs to on the previous brush
		public virtual void PrimarySelectionAboutToChange() {}

		// Called when the selected objects has changed
		public virtual void OnSelectionChanged() {}

		// Fired by the CSG Model on the active tool when Unity triggers Undo.undoRedoPerformed
		public virtual void OnUndoRedoPerformed() {}

		// Called when the selected brush changes
		public abstract void ResetTool();

		public abstract void Deactivated();

		public virtual bool BrushesHandleDrawing
		{
			get
			{
				return true;
			}
		}

		public virtual bool PreventBrushSelection
		{
			get
			{
				return false;
			}
		}
    }
}
#endif