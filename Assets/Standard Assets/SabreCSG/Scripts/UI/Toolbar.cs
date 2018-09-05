#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System;
using System.Linq;

namespace Sabresaurus.SabreCSG
{
    public static class Toolbar
    {
        const int BOTTOM_TOOLBAR_HEIGHT = 40;

        static CSGModel csgModel;

		static string warningMessage = "Concave brushes detected";

		static Rect gridRect;

        public static CSGModel CSGModel
        {
            get
            {
                return csgModel;
            }
            set
            {
                csgModel = value;
            }
        }

		public static string WarningMessage {
			get {
				return warningMessage;
			}
			set {
				warningMessage = value;
			}
		}

		public static void OnSceneGUI (SceneView sceneView, Event e)
		{
			if (e.type == EventType.Repaint || e.type == EventType.Layout)
			{
				OnRepaint(sceneView, e);
			}
		}

		private static void OnRepaint(SceneView sceneView, Event e)
        {
            Rect rectangle = new Rect(0, sceneView.position.height - BOTTOM_TOOLBAR_HEIGHT, sceneView.position.width, BOTTOM_TOOLBAR_HEIGHT);

            GUIStyle style = new GUIStyle(EditorStyles.toolbar);

            style.fixedHeight = BOTTOM_TOOLBAR_HEIGHT;
			GUILayout.Window(140003, rectangle, OnBottomToolbarGUI, "", style);//, EditorStyles.textField);

			style = new GUIStyle(EditorStyles.toolbar);

			style.normal.background = SabreCSGResources.ClearTexture;
			rectangle = new Rect(0, 20, 320, 50);
			GUILayout.Window(140004, rectangle, OnTopToolbarGUI, "", style);

			if(!string.IsNullOrEmpty(warningMessage))
			{				
				style.fixedHeight = 70;
				rectangle = new Rect(0, sceneView.position.height - BOTTOM_TOOLBAR_HEIGHT - style.fixedHeight, sceneView.position.width, style.fixedHeight);
				GUILayout.Window(140005, rectangle, OnWarningToolbar, "", style);
			}
            
        }

        private static void OnTopToolbarGUI(int windowID)
        {
			EditorGUILayout.BeginHorizontal();
//			csgModel.SetCurrentMode(SabreGUILayout.DrawEnumGrid(CurrentSettings.CurrentMode, GUILayout.Width(67)));
			csgModel.SetCurrentMode(SabreGUILayout.DrawEnumGrid(CurrentSettings.CurrentMode, GUILayout.Width(50)));

			/*
			bool isClipMode = (CurrentSettings.OverrideMode == OverrideMode.Clip);
			if(SabreGUILayout.Toggle(isClipMode, "Clip"))
			{
				csgModel.SetOverrideMode(OverrideMode.Clip);
			}
			else
			{
				if(isClipMode)
				{
					csgModel.ExitOverrideMode();
				}
			}

			bool isDrawMode = (CurrentSettings.OverrideMode == OverrideMode.Draw);

			if(SabreGUILayout.Toggle(isDrawMode, "Draw"))
			{
				csgModel.SetOverrideMode(OverrideMode.Draw);
			}
			else
			{
				if(isDrawMode)
				{
					csgModel.ExitOverrideMode();
				}
			}
			*/
			
			EditorGUILayout.EndHorizontal();
        }

		private static void OnWarningToolbar(int windowID)
		{
			GUIStyle style = SabreGUILayout.GetOverlayStyle();
			Vector2 size = style.CalcSize(new GUIContent(warningMessage));

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Box(warningMessage, style, GUILayout.Width(size.x));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}

		static void CreateBrush(PrimitiveBrushType brushType)
		{
			GameObject newBrushObject = csgModel.CreateBrush(brushType, Vector3.zero);

			if(SceneView.lastActiveSceneView != null)
			{
				Transform cameraTransform = SceneView.lastActiveSceneView.camera.transform;
				Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
				List<PolygonRaycastHit> hits = csgModel.RaycastBrushesAll(ray);
				if(hits.Count > 0)
				{
					Vector3 newPosition = hits[0].Point;
					// Back a unit, since the brush is around 2 units in each dimensions
					newPosition += hits[0].Normal;
					if(CurrentSettings.PositionSnappingEnabled)
					{
						float snapDistance = CurrentSettings.PositionSnapDistance;
						newPosition = MathHelper.RoundVector3(newPosition, snapDistance);

						newBrushObject.transform.position = newPosition;
					}
				}
				else
				{
					Vector3 newPosition = SceneView.lastActiveSceneView.pivot;
					if(CurrentSettings.PositionSnappingEnabled)
					{
						float snapDistance = CurrentSettings.PositionSnapDistance;
						newPosition = MathHelper.RoundVector3(newPosition, snapDistance);

						newBrushObject.transform.position = newPosition;
					}
				}
			}

			newBrushObject.GetComponent<Brush>().Invalidate(true);

			// Set the selection to the new object
			Selection.activeGameObject = newBrushObject;

			Undo.RegisterCreatedObjectUndo(newBrushObject, "Create Brush");
		}

		static void OnSelectedGridOption(object userData)
		{
			if(userData.GetType() == typeof(GridMode))
			{
				CurrentSettings.GridMode = (GridMode)userData;
				GridManager.UpdateGrid();
			}
		}

        private static void OnBottomToolbarGUI(int windowID)
        {
            GUILayout.BeginHorizontal();

			// For debugging frame rate
//			GUILayout.Label(((int)(1 / csgModel.CurrentFrameDelta)).ToString(), SabreGUILayout.GetLabelStyle());

			GUIStyle createBrushStyle = new GUIStyle(EditorStyles.toolbarButton);
			createBrushStyle.fixedHeight = 20;
			if(GUI.Button(new Rect(0,0, 30, createBrushStyle.fixedHeight), SabreCSGResources.ButtonCubeTexture, createBrushStyle))
			{
				CreateBrush(PrimitiveBrushType.Cube);
			}

			if(GUI.Button(new Rect(30,0, 30, createBrushStyle.fixedHeight), SabreCSGResources.ButtonPrismTexture, createBrushStyle))
			{
				CreateBrush(PrimitiveBrushType.Prism);
			}

			GUILayout.Space(62);


            if (SabreGUILayout.Button("Rebuild"))
            {
				csgModel.Build(false, false);
            }

			if (SabreGUILayout.Button("Force Rebuild"))
			{
				csgModel.Build(true, false);
			}

			GUI.color = Color.white;

			if(csgModel.AutoRebuild)
			{
				GUI.color = Color.green;
			}
			csgModel.AutoRebuild = SabreGUILayout.Toggle(csgModel.AutoRebuild, "Auto Rebuild");
			GUI.color = Color.white;

			GUILayout.Label(csgModel.BuildMetrics.BuildMetaData.ToString(), SabreGUILayout.GetForeStyle(), GUILayout.Width(140));

            bool lastBrushesHidden = CurrentSettings.BrushesHidden;
			if(lastBrushesHidden)
			{
				GUI.color = Color.red;
			}
            CurrentSettings.BrushesHidden = SabreGUILayout.Toggle(CurrentSettings.BrushesHidden, "Brushes Hidden");
            if (CurrentSettings.BrushesHidden != lastBrushesHidden)
            {
                // Has changed
                csgModel.UpdateBrushVisibility();
                SceneView.RepaintAll();
            }
			GUI.color = Color.white;


			bool lastMeshHidden = CurrentSettings.MeshHidden;
			if(lastMeshHidden)
			{
				GUI.color = Color.red;
			}
			CurrentSettings.MeshHidden = SabreGUILayout.Toggle(CurrentSettings.MeshHidden, "Mesh Hidden");
			if (CurrentSettings.MeshHidden != lastMeshHidden)
			{
				// Has changed
				csgModel.UpdateBrushVisibility();
				SceneView.RepaintAll();
			}

			GUI.color = Color.white;

			
			if(GUILayout.Button("Grid " + CurrentSettings.GridMode.ToString(), EditorStyles.toolbarDropDown, GUILayout.Width(90)))
			{
				GenericMenu menu = new GenericMenu ();
				
				string[] names = Enum.GetNames(typeof(GridMode));
				
				for (int i = 0; i < names.Length; i++) 
				{
					GridMode value = (GridMode)Enum.Parse(typeof(GridMode),names[i]);
					bool selected = false;
					if(CurrentSettings.GridMode == value)
					{
						selected = true;
					}
					menu.AddItem (new GUIContent (names[i]), selected, OnSelectedGridOption, value);
				}
				
				menu.DropDown(gridRect);
			}

			if (Event.current.type == EventType.Repaint)
			{
				gridRect = GUILayoutUtility.GetLastRect();
				gridRect.width = 100;
			}

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Line Two
            GUILayout.BeginHorizontal();

			if(GUI.Button(new Rect(0,createBrushStyle.fixedHeight, 30, createBrushStyle.fixedHeight), SabreCSGResources.ButtonCylinderTexture, createBrushStyle))
			{
				CreateBrush(PrimitiveBrushType.Cylinder);
			}

			if(GUI.Button(new Rect(30,createBrushStyle.fixedHeight, 30, createBrushStyle.fixedHeight), SabreCSGResources.ButtonSphereTexture, createBrushStyle))
			{
				CreateBrush(PrimitiveBrushType.Sphere);
			}

			GUILayout.Space(62);

			// Display brush count
			GUILayout.Label(csgModel.BrushCount.ToStringWithSuffix(" brush", " brushes"), SabreGUILayout.GetLabelStyle());
//			CurrentSettings.GridMode = (GridMode)EditorGUILayout.EnumPopup(CurrentSettings.GridMode, EditorStyles.toolbarPopup, GUILayout.Width(80));

            if (Selection.activeGameObject != null)
            {
				Brush primaryBrush = Selection.activeGameObject.GetComponent<Brush>();
				List<Brush> brushes = new List<Brush>();
				for (int i = 0; i < Selection.gameObjects.Length; i++) 
				{
					Brush brush = Selection.gameObjects[i].GetComponent<Brush>();
					if (brush != null)
					{
						brushes.Add(brush);
					}
				}
                if (primaryBrush != null)
                {
					CSGMode brushMode = (CSGMode)EditorGUILayout.EnumPopup(primaryBrush.Mode, EditorStyles.toolbarPopup, GUILayout.Width(80));
					if(brushMode != primaryBrush.Mode)
					{
						bool anyChanged = false;

						foreach (Brush brush in brushes) 
						{
							Undo.RecordObject(brush, "Change Brush To " + brushMode);
							brush.Mode = brushMode;
							anyChanged = true;
						}
						if(anyChanged)
						{
							// Need to update the icon for the csg mode in the hierarchy
							EditorApplication.RepaintHierarchyWindow();
						}
					}


					bool[] noCSGStates = brushes.Select(brush => brush.IsNoCSG).Distinct().ToArray();
					bool isNoCSG = (noCSGStates.Length == 1) ? noCSGStates[0] : false;

					bool newIsNoCSG = SabreGUILayout.ToggleMixed(noCSGStates, "NoCSG", GUILayout.Width(53));


					bool[] collisionStates = brushes.Select(item => item.HasCollision).Distinct().ToArray();
					bool hasCollision = (collisionStates.Length == 1) ? collisionStates[0] : false;

					bool newHasCollision = SabreGUILayout.ToggleMixed(collisionStates, "Collision", GUILayout.Width(53));


					bool[] visibleStates = brushes.Select(item => item.IsVisible).Distinct().ToArray();
					bool isVisible = (visibleStates.Length == 1) ? visibleStates[0] : false;

					bool newIsVisible = SabreGUILayout.ToggleMixed(visibleStates, "Visible", GUILayout.Width(53));

					if(newIsNoCSG != isNoCSG)
					{
						foreach (Brush brush in brushes) 
						{
							Undo.RecordObject(brush, "Change Brush NoCSG Mode");
							brush.IsNoCSG = newIsNoCSG;						
						}
						// Tell the brushes that they have changed and need to recalc intersections
						foreach (Brush brush in brushes) 
						{
							brush.Invalidate(true);
						}

						EditorApplication.RepaintHierarchyWindow();
					}
					if(newHasCollision != hasCollision)
					{
						foreach (Brush brush in brushes) 
						{
							Undo.RecordObject(brush, "Change Brush Collision Mode");
							brush.HasCollision = newHasCollision;
						}
						// Tell the brushes that they have changed and need to recalc intersections
						foreach (Brush brush in brushes) 
						{
							brush.Invalidate(true);
						}
					}
					if(newIsVisible != isVisible)
					{
						foreach (Brush brush in brushes) 
						{
							Undo.RecordObject(brush, "Change Brush Visible Mode");
							brush.IsVisible = newIsVisible;
						}
						// Tell the brushes that they have changed and need to recalc intersections
						foreach (Brush brush in brushes) 
						{
							brush.Invalidate(true);
						}
						if(newIsVisible == false)
						{
							csgModel.NotifyPolygonsRemoved();
						}
					}
                }
            }

			GUILayout.Space(10);

			// Position snapping UI
			CurrentSettings.PositionSnappingEnabled = SabreGUILayout.Toggle(CurrentSettings.PositionSnappingEnabled, "Pos Snapping");
			CurrentSettings.PositionSnapDistance = EditorGUILayout.FloatField(CurrentSettings.PositionSnapDistance, GUILayout.Width(50));
			
			if (SabreGUILayout.Button("-", EditorStyles.miniButtonLeft))
			{
				CurrentSettings.ChangePosSnapDistance(.5f);
			}
			if (SabreGUILayout.Button("+", EditorStyles.miniButtonRight))
			{
				CurrentSettings.ChangePosSnapDistance(2f);
			}

			// Rotation snapping UI
			CurrentSettings.AngleSnappingEnabled = SabreGUILayout.Toggle(CurrentSettings.AngleSnappingEnabled, "Ang Snapping");
			CurrentSettings.AngleSnapDistance = EditorGUILayout.FloatField(CurrentSettings.AngleSnapDistance, GUILayout.Width(50));

			if (SabreGUILayout.Button("-", EditorStyles.miniButtonLeft))
			{
				if(CurrentSettings.AngleSnapDistance > 15)
				{
					CurrentSettings.AngleSnapDistance -= 15;
				}
				else
				{
					CurrentSettings.AngleSnapDistance -= 5;
				}
			}
			if (SabreGUILayout.Button("+", EditorStyles.miniButtonRight))
			{
				if(CurrentSettings.AngleSnapDistance >= 15)
				{
					CurrentSettings.AngleSnapDistance += 15;
				}
				else
				{
					CurrentSettings.AngleSnapDistance += 5;
				}
			}

// Disabled test build options
//			CurrentSettings.RestoreOriginalPolygons = SabreGUILayout.Toggle(CurrentSettings.RestoreOriginalPolygons, "Restore Original Polygons", GUILayout.Width(153));
//			CurrentSettings.RemoveHiddenGeometry = SabreGUILayout.Toggle(CurrentSettings.RemoveHiddenGeometry, "Remove Hidden Geometry", GUILayout.Width(153));

			GUILayout.FlexibleSpace();

			if (SabreGUILayout.Button("Prefs"))
			{
				SabreCSGPreferences.CreateAndShow();
			}

			if (SabreGUILayout.Button("Disable"))
			{
				Selection.activeGameObject = null;
				csgModel.EditMode = false;
			}

            GUILayout.EndHorizontal();
        }
    }
}
#endif