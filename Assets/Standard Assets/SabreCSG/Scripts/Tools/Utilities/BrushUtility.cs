#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Sabresaurus.SabreCSG
{
	/// <summary>
	/// Provides utility methods for manipulating brushes
	/// </summary>
	public static class BrushUtility
	{
		/// <summary>
		/// Scales the brush by a local Vector3 scale from its pivot
		/// </summary>
		/// <param name="brush">The brush to be rescaled</param>
		/// <param name="rescaleValue">Local scale to apply</param>
		public static void Rescale (PrimitiveBrush brush, Vector3 rescaleValue)
		{
			Polygon[] polygons = brush.GetPolygons();

			for (int i = 0; i < polygons.Length; i++) 
			{
				Polygon polygon = polygons[i];

				polygons[i].CalculatePlane();
				Vector3 previousPlaneNormal = polygons[i].Plane.normal;

				int vertexCount = polygon.Vertices.Length;

				Vector3[] newPositions = new Vector3[vertexCount];
				Vector2[] newUV = new Vector2[vertexCount];

				for (int j = 0; j < vertexCount; j++) 
				{
					newPositions[j] = polygon.Vertices[j].Position;
					newUV[j] = polygon.Vertices[j].UV;
				}

				for (int j = 0; j < vertexCount; j++) 
				{
					Vertex vertex = polygon.Vertices[j];

					Vector3 newPosition = vertex.Position.Multiply(rescaleValue);
					newPositions[j] = newPosition;

					newUV[j] = GeometryHelper.GetUVForPosition(polygon, newPosition);
				}

				// Apply all the changes to the polygon
				for (int j = 0; j < vertexCount; j++) 
				{
					Vertex vertex = polygon.Vertices[j];
					vertex.Position = newPositions[j];
					vertex.UV = newUV[j];
				}

				// Polygon geometry has changed, inform the polygon that it needs to recalculate its cached plane
				polygons[i].CalculatePlane();

				Vector3 newPlaneNormal = polygons[i].Plane.normal;

				// Find the rotation from the original polygon plane to the new polygon plane
				Quaternion normalRotation = Quaternion.FromToRotation(previousPlaneNormal, newPlaneNormal);

				// Rotate all the vertex normals by the new rotation
				for (int j = 0; j < vertexCount; j++) 
				{
					Vertex vertex = polygon.Vertices[j];
					vertex.Normal = normalRotation * vertex.Normal;
				}
			}
#if UNITY_EDITOR
			EditorHelper.SetDirty(brush);
#endif
			brush.Invalidate(true);
		}

		/// <summary>
		/// Resizes the brush so that it's local bounds match the specified extents
		/// </summary>
		/// <param name="brush">The brush to be resized</param>
		/// <param name="rescaleValue">The extents to match</param>
		public static void Resize (PrimitiveBrush brush, Vector3 resizeValue)
		{
			Bounds bounds = brush.GetBounds();
			// Calculate the rescale vector required to change the bounds to the resize vector
			Vector3 rescaleVector3 = resizeValue.Divide(bounds.size);
			Rescale(brush, rescaleVector3);
		}
	}
}
#endif