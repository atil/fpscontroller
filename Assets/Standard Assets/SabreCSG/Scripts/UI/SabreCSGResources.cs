#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Sabresaurus.SabreCSG
{
	public static class SabreCSGResources
	{
		//	    private static Material marqueeBorderMaterial = null;
		//	    private static Material marqueeFillMaterial = null;
		private static Material selectedBrushMaterial = null;
		private static Material selectedBrushDashedMaterial = null;
		private static Material gizmoMaterial = null;
		//	    private static Material gizmoSelectedMaterial = null;
		private static Material vertexMaterial = null;
		private static Material circleMaterial = null;
		private static Material circleOutlineMaterial = null;
		private static Material planeMaterial = null;
		private static Material previewMaterial = null;

		private static Texture2D addIconTexture = null;
		private static Texture2D subtractIconTexture = null;
		private static Texture2D noCSGIconTexture = null;

		private static Texture2D dialogOverlayTexture = null;
		private static Texture2D dialogOverlayRetinaTexture = null;

		private static Texture2D clearTexture = null;
		private static Texture2D halfWhiteTexture = null;
		private static Texture2D halfBlackTexture = null;

		private static Texture2D buttonCubeTexture = null;
		private static Texture2D buttonCylinderTexture = null;
		private static Texture2D buttonPrismTexture = null;
		private static Texture2D buttonSphereTexture = null;

		private static Texture2D circleTexture = null;
		private static Texture2D circleOutlineTexture = null;

		private static Material excludedMaterial = null;
		private static Texture2D excludedTexture = null;

		public static Texture2D AddIconTexture 
		{
			get 
			{
				if(addIconTexture == null)
				{
					addIconTexture = UnityEditor.AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Gizmos/Add.png") as Texture2D;
				}
				return addIconTexture;
			}
		}

		public static Texture2D SubtractIconTexture 
		{
			get 
			{
				if(subtractIconTexture == null)
				{
					subtractIconTexture = UnityEditor.AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Gizmos/Subtract.png") as Texture2D;
				}
				return subtractIconTexture;
			}
		}

		public static Texture2D NoCSGIconTexture 
		{
			get 
			{
				if(noCSGIconTexture == null)
				{
					noCSGIconTexture = UnityEditor.AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Gizmos/NoCSG.png") as Texture2D;
				}
				return noCSGIconTexture;
			}
		}

		public static Texture2D DialogOverlayTexture 
		{
			get 
			{
				if(dialogOverlayTexture == null)
				{
					dialogOverlayTexture = UnityEditor.AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Gizmos/DialogOverlay75.png") as Texture2D;
				}
				return dialogOverlayTexture;
			}
		}

		public static Texture2D DialogOverlayRetinaTexture 
		{
			get 
			{
				if(dialogOverlayRetinaTexture == null)
				{
					dialogOverlayRetinaTexture = UnityEditor.AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Gizmos/DialogOverlay75@2x.png") as Texture2D;
				}
				return dialogOverlayRetinaTexture;
			}
		}


		public static Texture2D ClearTexture
		{
			get
			{
				if(clearTexture == null)
				{
					clearTexture = new Texture2D(2,2, TextureFormat.RGBA32, false);
					for (int x = 0; x < clearTexture.width; x++) 
					{
						for (int y = 0; y < clearTexture.height; y++) 
						{
							clearTexture.SetPixel(x,y,Color.clear);
						}	
					}
					clearTexture.Apply();
				}
				return clearTexture;
			}
		}

		public static Texture2D HalfWhiteTexture
		{
			get
			{
				if(halfWhiteTexture == null)
				{
					halfWhiteTexture = new Texture2D(2,2, TextureFormat.RGBA32, false);
					for (int x = 0; x < halfWhiteTexture.width; x++) 
					{
						for (int y = 0; y < halfWhiteTexture.height; y++) 
						{
							halfWhiteTexture.SetPixel(x,y,new Color(1,1,1,0.5f));
						}	
					}
					halfWhiteTexture.Apply();
				}
				return halfWhiteTexture;
			}
		}

		public static Texture2D HalfBlackTexture
		{
			get
			{
				if(halfBlackTexture == null)
				{
					halfBlackTexture = new Texture2D(2,2, TextureFormat.RGBA32, false);
					for (int x = 0; x < halfBlackTexture.width; x++) 
					{
						for (int y = 0; y < halfBlackTexture.height; y++) 
						{
							halfBlackTexture.SetPixel(x,y,new Color(0,0,0,0.5f));
						}	
					}
					halfBlackTexture.Apply();
				}
				return halfBlackTexture;
			}
		}

		public static Texture2D ButtonCubeTexture 
		{
			get 
			{
				if(buttonCubeTexture == null)
				{
					buttonCubeTexture = UnityEditor.AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Gizmos/ButtonCube.png") as Texture2D;
				}
				return buttonCubeTexture;
			}
		}

		public static Texture2D ButtonPrismTexture 
		{
			get 
			{
				if(buttonPrismTexture == null)
				{
					buttonPrismTexture = UnityEditor.AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Gizmos/ButtonPrism.png") as Texture2D;
				}
				return buttonPrismTexture;
			}
		}

		public static Texture2D ButtonCylinderTexture 
		{
			get 
			{
				if(buttonCylinderTexture == null)
				{
					buttonCylinderTexture = UnityEditor.AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Gizmos/ButtonCylinder.png") as Texture2D;
				}
				return buttonCylinderTexture;
			}
		}

		public static Texture2D ButtonSphereTexture 
		{
			get 
			{
				if(buttonSphereTexture == null)
				{
					buttonSphereTexture = UnityEditor.AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Gizmos/ButtonSphere.png") as Texture2D;
				}
				return buttonSphereTexture;
			}
		}

		public static Texture2D CircleTexture 
		{
			get 
			{
				if(circleTexture == null)
				{
					circleTexture = UnityEditor.AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Gizmos/Circle.png") as Texture2D;
				}
				return circleTexture;
			}
		}

		public static Texture2D CircleOutlineTexture 
		{
			get 
			{
				if(circleOutlineTexture == null)
				{
					circleOutlineTexture = UnityEditor.AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Gizmos/CircleOutline.png") as Texture2D;
				}
				return circleOutlineTexture;
			}
		}

		//	    public static Material GetMarqueeBorderMaterial()
		//	    {
		//	        if (marqueeBorderMaterial == null)
		//	        {
		//	            marqueeBorderMaterial = new Material(Shader.Find("Transparent/Diffuse"));
		//	        }
		//	        return marqueeBorderMaterial;
		//	    }
		//
		//	    public static Material GetMarqueeFillMaterial()
		//	    {
		//	        if (marqueeFillMaterial == null)
		//	        {
		//	            marqueeFillMaterial = new Material(Shader.Find("Transparent/Diffuse"));
		//	        }
		//	        return marqueeFillMaterial;
		//	    }

		public static Material GetExcludedMaterial()
		{
			if (excludedTexture == null)
			{
				excludedTexture = UnityEditor.AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Internal/Excluded.png") as Texture2D;

			}
			if (excludedMaterial == null)
			{
				excludedMaterial = new Material(Shader.Find("SabreCSG/SeeExcluded"));
				excludedMaterial.hideFlags = HideFlags.HideAndDontSave;
				excludedMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
				excludedMaterial.mainTexture = excludedTexture;
			}
			return excludedMaterial;
		}

		public static Material GetSelectedBrushMaterial()
		{
			if (selectedBrushMaterial == null)
			{
				selectedBrushMaterial = new Material(Shader.Find("SabreCSG/Line"));
				selectedBrushMaterial.hideFlags = HideFlags.HideAndDontSave;
				selectedBrushMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
			}
			return selectedBrushMaterial;
		}

		public static Material GetSelectedBrushDashedMaterial()
		{
			if (selectedBrushDashedMaterial == null)
			{
				selectedBrushDashedMaterial = new Material(Shader.Find("SabreCSG/Line Dashed"));
				selectedBrushDashedMaterial.hideFlags = HideFlags.HideAndDontSave;
				selectedBrushDashedMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
			}
			return selectedBrushDashedMaterial;
		}

		public static Material GetGizmoMaterial()
		{
			if (gizmoMaterial == null)
			{
				Shader shader = Shader.Find("SabreCSG/Handle");
				gizmoMaterial = new Material(shader);
				gizmoMaterial.hideFlags = HideFlags.HideAndDontSave;
				gizmoMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
				gizmoMaterial.mainTexture = UnityEditor.AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Gizmos/SquareGizmo8x8.png") as Texture;
			}
			return gizmoMaterial;
		}

		public static Material GetVertexMaterial()
		{
			if (vertexMaterial == null)
			{
				Shader shader = Shader.Find("SabreCSG/Handle");
				vertexMaterial = new Material(shader);
				vertexMaterial.hideFlags = HideFlags.HideAndDontSave;
				vertexMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
				vertexMaterial.mainTexture = UnityEditor.AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Gizmos/CircleGizmo8x8.png") as Texture;
			}
			return vertexMaterial;
		}

		public static Material GetCircleMaterial()
		{
			if (circleMaterial == null)
			{
				Shader shader = Shader.Find("SabreCSG/Handle");
				circleMaterial = new Material(shader);
				circleMaterial.hideFlags = HideFlags.HideAndDontSave;
				circleMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
				circleMaterial.mainTexture = CircleTexture;
			}
			return circleMaterial;
		}

		public static Material GetCircleOutlineMaterial()
		{
			if (circleOutlineMaterial == null)
			{
				Shader shader = Shader.Find("SabreCSG/Handle");
				circleOutlineMaterial = new Material(shader);
				circleOutlineMaterial.hideFlags = HideFlags.HideAndDontSave;
				circleOutlineMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
				circleOutlineMaterial.mainTexture = CircleOutlineTexture;
			}
			return circleOutlineMaterial;
		}

		public static Material GetPreviewMaterial()
		{
			if(previewMaterial == null)
			{
				Shader shader = Shader.Find("SabreCSG/Preview");

				previewMaterial = new Material(shader);
				previewMaterial.hideFlags = HideFlags.HideAndDontSave;
				previewMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
			}

			return previewMaterial;
		}

		public static Material GetPlaneMaterial()
		{
			if (planeMaterial == null)
			{
				planeMaterial = new Material(Shader.Find("SabreCSG/Plane"));
				planeMaterial.hideFlags = HideFlags.HideAndDontSave;
				planeMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
			}
			return planeMaterial;
		}
	}
}
#endif