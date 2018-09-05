using UnityEngine;
using System.Collections;
using UnityEditor;
//using System;
using System.Collections.Generic;
using System.Linq;

namespace Sabresaurus.SabreCSG
{
	[CanEditMultipleObjects]
    [CustomEditor(typeof(PrimitiveBrush))]
    public class PrimitiveBrushInspector : Editor
    {
		string rescaleString = "1";
		string resizeString = "1";

		Mesh sourceMesh = null;

		SerializedProperty prismSideCountProp;
		SerializedProperty cylinderSideCountProp;
		SerializedProperty sphereSideCountProp;

		PrimitiveBrushType? overridenBrushType = null;

		void OnEnable () 
		{
			// Setup the SerializedProperties.
			prismSideCountProp = serializedObject.FindProperty ("prismSideCount");
			cylinderSideCountProp = serializedObject.FindProperty ("cylinderSideCount");
			sphereSideCountProp = serializedObject.FindProperty ("sphereSideCount");
		}

		PrimitiveBrush BrushTarget
		{
			get
			{
				return (PrimitiveBrush)target;
			}
		}

		PrimitiveBrush[] BrushTargets
		{
			get
			{
				return System.Array.ConvertAll(targets, item => (PrimitiveBrush)item);
			}
		}

		public void DrawBrushTypeField()
		{
			GUILayout.BeginHorizontal();
			PrimitiveBrushType[] selectedTypes = BrushTargets.Select(item => item.BrushType).ToArray();

			if(overridenBrushType.HasValue)
			{
				selectedTypes = new PrimitiveBrushType[] { overridenBrushType.Value };
			}

			PrimitiveBrushType? newType = SabreGUILayout.EnumPopupMixed("Brush Type", selectedTypes);

			if(newType.HasValue)
			{
				overridenBrushType = newType;

				if(newType.Value == PrimitiveBrushType.Prism)
				{
					GUILayout.Label("Sides", SabreGUILayout.GetForeStyle(), GUILayout.Width(30));
					EditorGUILayout.PropertyField(prismSideCountProp, new GUIContent(""));
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
				}
				else if(newType.Value == PrimitiveBrushType.Cylinder)
				{
					GUILayout.Label("Sides", SabreGUILayout.GetForeStyle(), GUILayout.Width(30));
					EditorGUILayout.PropertyField(cylinderSideCountProp, new GUIContent(""));
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
				}

				else if(newType.Value == PrimitiveBrushType.Sphere)
				{
					GUILayout.Label("Sides", SabreGUILayout.GetForeStyle(), GUILayout.Width(30));
					EditorGUILayout.PropertyField(sphereSideCountProp, new GUIContent(""));
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
				}
			}

			if (GUILayout.Button("Reset Polygons"))
			{
				Undo.RecordObjects(targets, "Reset Polygons");
				foreach (var thisBrush in targets) 
				{
					if(overridenBrushType.HasValue)
					{
						((PrimitiveBrush)thisBrush).BrushType = overridenBrushType.Value;
					}
					((PrimitiveBrush)thisBrush).ResetPolygons();
					((PrimitiveBrush)thisBrush).Invalidate(true);
				}

				overridenBrushType = null;
			}
			GUILayout.EndHorizontal();

			if (GUILayout.Button("Shell"))
			{
				List<GameObject> newSelection = new List<GameObject>();
				foreach (var thisBrush in targets) 
				{
					GameObject newObject = ((PrimitiveBrush)thisBrush).Duplicate();
					Polygon[] polygons = newObject.GetComponent<PrimitiveBrush>().GetPolygons();
					VertexUtility.DisplacePolygons(polygons, -CurrentSettings.PositionSnapDistance);
					Bounds newBounds = newObject.GetComponent<PrimitiveBrush>().GetBounds();
					// Verify the new geometry
					if(GeometryHelper.IsBrushConvex(polygons) 
						&& newBounds.GetSmallestExtent() > 0)
					{
						Undo.RegisterCreatedObjectUndo(newObject, "Shell");
						newSelection.Add(newObject);
					}
					else
					{
						// Produced a concave brush, delete it and pretend nothing happened
						GameObject.DestroyImmediate(newObject);
						Debug.LogWarning("Could not shell " + thisBrush.name + " as shelled geometry would not be valid. Try lowering Pos Snapping and attempt Shell again.");
					}
				}

				if(newSelection.Count > 0)
				{
					Selection.objects = newSelection.ToArray();
				}
			}
		}

        public override void OnInspectorGUI()
        {
//            DrawDefaultInspector();

			DrawBrushTypeField();

//			BrushOrder brushOrder = BrushTarget.GetBrushOrder();
//			string positionString = string.Join(",", brushOrder.Position.Select(item => item.ToString()).ToArray());
//            GUILayout.Label(positionString, EditorStyles.boldLabel);

//			List<BrushCache> intersections = BrushTarget.BrushCache.IntersectingVisualBrushCaches;
//
//			for (int i = 0; i < intersections.Count; i++) 
//			{
//				GUILayout.Label(intersections[i].Mode.ToString(), EditorStyles.boldLabel);
//			}

			GUILayout.BeginHorizontal();

			GUI.SetNextControlName("rescaleTextbox");			       

			rescaleString = EditorGUILayout.TextField(rescaleString);

			bool keyboardEnter = Event.current.isKey 
				&& Event.current.keyCode == KeyCode.Return 
				&& Event.current.type == EventType.KeyUp 
				&& GUI.GetNameOfFocusedControl() == "rescaleTextbox";
			
			if(GUILayout.Button("Rescale") || keyboardEnter)
			{
				// Try to parse a Vector3 scale from the input string
				Vector3 rescaleVector3;
				if(StringHelper.TryParseScale(rescaleString, out rescaleVector3))
				{
					// None of the scale components can be zero
					if(rescaleVector3.x != 0 && rescaleVector3.y != 0 && rescaleVector3.z != 0) 
					{
						// Rescale all the brushes
						Undo.RecordObjects(targets, "Rescale Polygons");
						foreach (var thisBrush in targets) 
						{
							BrushUtility.Rescale((PrimitiveBrush)thisBrush, rescaleVector3);
						}
					}
				}
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			GUI.SetNextControlName("resizeTextbox");			       

			resizeString = EditorGUILayout.TextField(resizeString);

			keyboardEnter = Event.current.isKey 
				&& Event.current.keyCode == KeyCode.Return 
				&& Event.current.type == EventType.KeyUp 
				&& GUI.GetNameOfFocusedControl() == "resizeTextbox";

			if(GUILayout.Button("Resize") || keyboardEnter)
			{
				// Try to parse a Vector3 scale from the input string
				Vector3 resizeVector3;
				if(StringHelper.TryParseScale(resizeString, out resizeVector3))
				{
					// None of the size components can be zero
					if(resizeVector3.x != 0 && resizeVector3.y != 0 && resizeVector3.z != 0) 
					{
						// Rescale all the brushes so that the local bounds is the same size as the resize vector
						Undo.RecordObjects(targets, "Resize Polygons");
						PrimitiveBrush[] brushes = BrushTargets;
						foreach (PrimitiveBrush brush in brushes) 
						{
							BrushUtility.Resize(brush, resizeVector3);
						}
					}
				}
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			sourceMesh = EditorGUILayout.ObjectField(sourceMesh, typeof(Mesh), false) as Mesh;

			if (GUILayout.Button("Import"))
			{
				if(sourceMesh != null)
				{
					Undo.RecordObjects(targets, "Import Polygons From Mesh");
					
					Polygon[] polygons = BrushFactory.GeneratePolygonsFromMesh(sourceMesh).ToArray();
					bool convex = GeometryHelper.IsBrushConvex(polygons);
					if(!convex)
					{
						Debug.LogError("Concavities detected in imported mesh. This may result in issues during CSG, please change the source geometry so that it is convex");
					}
					foreach (var thisBrush in targets) 
					{
						((PrimitiveBrush)thisBrush).SetPolygons(polygons, true);
					}
				}
			}

			GUILayout.EndHorizontal();

			List<PrimitiveBrush> orderedTargets = BrushTargets.ToList();
			orderedTargets.Sort((x,y) => x.transform.GetSiblingIndex().CompareTo(y.transform.GetSiblingIndex()));

			if (GUILayout.Button("Set As First"))
			{
				for (int i = 0; i < orderedTargets.Count; i++) 
				{
					// REVERSED
					PrimitiveBrush thisBrush = orderedTargets[orderedTargets.Count-1-i];

					Undo.SetTransformParent(thisBrush.transform, thisBrush.transform.parent, "Change Order");
					thisBrush.transform.SetAsFirstSibling();
				}

				// Force all the brushes to recalculate their intersections and get ready for rebuilding
				for (int i = 0; i < orderedTargets.Count; i++) 
				{
					orderedTargets[i].RecalculateIntersections();
					orderedTargets[i].BrushCache.SetUnbuilt();
				}
			}
			
			if (GUILayout.Button("Send Earlier"))
			{
				for (int i = 0; i < orderedTargets.Count; i++) 
				{
					PrimitiveBrush thisBrush = orderedTargets[i];

					int siblingIndex = thisBrush.transform.GetSiblingIndex();
					if(siblingIndex > 0)
					{
						Undo.SetTransformParent(thisBrush.transform, thisBrush.transform.parent, "Change Order");
						siblingIndex--;
						thisBrush.transform.SetSiblingIndex(siblingIndex);
					}
				}

				// Force all the brushes to recalculate their intersections and get ready for rebuilding
				for (int i = 0; i < orderedTargets.Count; i++) 
				{
					orderedTargets[i].RecalculateIntersections();
					orderedTargets[i].BrushCache.SetUnbuilt();
				}
			}

			if (GUILayout.Button("Send Later"))
			{
				for (int i = 0; i < orderedTargets.Count; i++) 
				{
					// REVERSED
					PrimitiveBrush thisBrush = orderedTargets[orderedTargets.Count-1-i];

					int siblingIndex = thisBrush.transform.GetSiblingIndex();
					Undo.SetTransformParent(thisBrush.transform, thisBrush.transform.parent, "Change Order");
					siblingIndex++;
					thisBrush.transform.SetSiblingIndex(siblingIndex);
				}

				// Force all the brushes to recalculate their intersections and get ready for rebuilding
				for (int i = 0; i < orderedTargets.Count; i++) 
				{
					orderedTargets[i].RecalculateIntersections();
					orderedTargets[i].BrushCache.SetUnbuilt();
				}
			}

			if (GUILayout.Button("Set As Last"))
			{
				for (int i = 0; i < orderedTargets.Count; i++) 
				{
					PrimitiveBrush thisBrush = orderedTargets[i];

					Undo.SetTransformParent(thisBrush.transform, thisBrush.transform.parent, "Change Order");
					thisBrush.transform.SetAsLastSibling();
				}

				// Force all the brushes to recalculate their intersections and get ready for rebuilding
				for (int i = 0; i < orderedTargets.Count; i++) 
				{
					orderedTargets[i].RecalculateIntersections();
					orderedTargets[i].BrushCache.SetUnbuilt();
				}
			}

			serializedObject.ApplyModifiedProperties ();



//            GUILayout.Label("UVs", EditorStyles.boldLabel);
//
//            if (GUILayout.Button("Flip XY"))
//            {
//                UVUtility.FlipUVsXY(thisBrush.Polygons);
//            }
//
//            GUILayout.BeginHorizontal();
//            if (GUILayout.Button("Flip X"))
//            {
//                UVUtility.FlipUVsX(thisBrush.Polygons);
//            }
//            if (GUILayout.Button("Flip Y"))
//            {
//                UVUtility.FlipUVsY(thisBrush.Polygons);
//            }
//            GUILayout.EndHorizontal();
//
//            GUILayout.BeginHorizontal();
//            if (GUILayout.Button("UVs x 2"))
//            {
//                UVUtility.ScaleUVs(thisBrush.Polygons, 2f);
//            }
//            if (GUILayout.Button("UVs / 2"))
//            {
//                UVUtility.ScaleUVs(thisBrush.Polygons, .5f);
//            }
//            GUILayout.EndHorizontal();
            // Ensure Edit Mode is on
//            csgModel.EditMode = true;
        }
    }
}