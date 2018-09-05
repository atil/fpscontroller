#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sabresaurus.SabreCSG
{
	/// <summary>
	/// Generates polygons for certain situations
	/// </summary>
	public static class PolygonFactory
	{
		/// <summary>
		/// Split brush polygons by a supplied plane, outputing the polygons on either side and capping the two halves on the plane
		/// </summary>
		/// <returns><c>true</c>, if splitting actually took place, <c>false</c> otherwise.</returns>
		/// <param name="polygons">Source polygons.</param>
		/// <param name="splitPlane">Split plane.</param>
		/// <param name="excludeNewPolygons">If set to <c>true</c> the cap polygons will be marked as excludeFromBuild.</param>
		/// <param name="polygonsFront">Generated polygons in front of the plane.</param>
		/// <param name="polygonsBack">Generated polygons behind the plane.</param>
		public static bool SplitPolygonsByPlane(List<Polygon> polygons, // Source polygons that will be split
		                                        Plane splitPlane, 
		                                        bool excludeNewPolygons, // Whether new polygons should be marked as excludeFromBuild
		                                        out List<Polygon> polygonsFront, 
		                                        out List<Polygon> polygonsBack)
		{
			polygonsFront = new List<Polygon>();
			polygonsBack = new List<Polygon>();

			// First of all make sure splitting actually needs to occur (we'll get bad issues if
			// we try splitting geometry when we don't need to)
			if(!GeometryHelper.PolygonsIntersectPlane(polygons, splitPlane))
			{
				return false;
			}

			Material newMaterial = polygons[0].Material;
			
			// These are the vertices that will be used in the new caps
			List<Vertex> newVertices = new List<Vertex>();
			
			for (int polygonIndex = 0; polygonIndex < polygons.Count; polygonIndex++) 
			{
				Polygon.PolygonPlaneRelation planeRelation = Polygon.TestPolygonAgainstPlane(polygons[polygonIndex], splitPlane);
				
				// Polygon has been found to span both sides of the plane, attempt to split into two pieces
				if(planeRelation == Polygon.PolygonPlaneRelation.Spanning)
				{
					Polygon frontPolygon;
					Polygon backPolygon;
					Vertex newVertex1;
					Vertex newVertex2;
					
					// Attempt to split the polygon
					if(Polygon.SplitPolygon(polygons[polygonIndex], out frontPolygon, out backPolygon, out newVertex1, out newVertex2, splitPlane))
					{
						// If the split algorithm was successful (produced two valid polygons) then add each polygon to 
						// their respective points and track the intersection points
						polygonsFront.Add(frontPolygon);
						polygonsBack.Add(backPolygon);
						
						newVertices.Add(newVertex1);
						newVertices.Add(newVertex2);

						newMaterial = polygons[polygonIndex].Material;
					}
					else
					{
						// Two valid polygons weren't generated, so use the valid one
						if(frontPolygon != null)
						{
							planeRelation = Polygon.PolygonPlaneRelation.InFront;
						}
						else if(backPolygon != null)
						{
							planeRelation = Polygon.PolygonPlaneRelation.Behind;
						}
						else
						{
							Debug.LogError("Polygon splitting has resulted in two zero area polygons. This is unhandled.");
							//							Polygon.PolygonPlaneRelation secondplaneRelation = Polygon.TestPolygonAgainstPlane(polygons[polygonIndex], splitPlane);
						}
					}
				}
				
				// If the polygon is on one side of the plane or the other
				if(planeRelation != Polygon.PolygonPlaneRelation.Spanning)
				{
					// Make sure any points that are coplanar on non-straddling polygons are still used in polygon 
					// construction
					for (int vertexIndex = 0; vertexIndex < polygons[polygonIndex].Vertices.Length; vertexIndex++) 
					{
						if(Polygon.ComparePointToPlane(polygons[polygonIndex].Vertices[vertexIndex].Position, splitPlane) == Polygon.PointPlaneRelation.On)
						{
							newVertices.Add(polygons[polygonIndex].Vertices[vertexIndex]);
						}
					}
					
					if(planeRelation == Polygon.PolygonPlaneRelation.Behind)
					{
						polygonsBack.Add(polygons[polygonIndex]);
					}
					else 
					{
						polygonsFront.Add(polygons[polygonIndex]);
					}
				}
			}
			
			// If any splits occured or coplanar vertices are found. (For example if you're splitting a sphere at the
			// equator then no polygons will be split but there will be a bunch of coplanar vertices!)
			if(newVertices.Count > 0)
			{
				// HACK: This code is awful, because we end up with lots of duplicate vertices
				List<Vector3> positions = newVertices.Select(item => item.Position).ToList ();
				
				Polygon newPolygon = PolygonFactory.ConstructPolygon(positions, true);
				
				// Assuming it was possible to create a polygon
				if(newPolygon != null)
				{
					if(!MathHelper.PlaneEqualsLooser(newPolygon.Plane, splitPlane))
					{
						// Polygons are sometimes constructed facing the wrong way, possibly due to a winding order
						// mismatch. If the two normals are opposite, flip the new polygon
						if(Vector3.Dot(newPolygon.Plane.normal, splitPlane.normal) < -0.9f)
						{
							newPolygon.Flip();
						}
					}
					
					newPolygon.ExcludeFromFinal = excludeNewPolygons;
					newPolygon.Material = newMaterial;
					
					polygonsFront.Add(newPolygon);
					
					newPolygon = newPolygon.DeepCopy();
					newPolygon.Flip();
					
					newPolygon.ExcludeFromFinal = excludeNewPolygons;
					newPolygon.Material = newMaterial;
					
					
					if(newPolygon.Plane.normal == Vector3.zero)
					{
						Debug.LogError("Invalid Normal! Shouldn't be zero. This is unexpected since extraneous positions should have been removed!");
						//						Polygon fooNewPolygon = PolygonFactory.ConstructPolygon(positions, true);
					}
					
					polygonsBack.Add(newPolygon);
				}
				return true;
			}
			else
			{
				// It wasn't possible to create the polygon, for example the constructed polygon was too small
				// This could happen if you attempt to clip the tip off a long but thin brush, the plane-polyhedron test
				// would say they intersect but in reality the resulting polygon would be near zero area
				return false;
			}
		}
		
		/// <summary>
		/// Constructs a polygon from an unordered coplanar set of positions
		/// </summary>
		/// <returns>The polygon or <c>null</c> if it wasn't possible to create one.</returns>
		/// <param name="sourcePositions">Source positions, these are not required to be in order and if <c>removeExtraPositions</c> is set to <c>true</c>can include overlapping positions.</param>
		/// <param name="removeExtraPositions">If set to <c>true</c> extraneous positions (overlapping) are removed.</param>
		public static Polygon ConstructPolygon(List<Vector3> sourcePositions, bool removeExtraPositions)
		{
			List<Vector3> positions;
			
			if(removeExtraPositions)
			{
				Polygon.Vector3ComparerEpsilon equalityComparer = new Polygon.Vector3ComparerEpsilon();
				positions = sourcePositions.Distinct(equalityComparer).ToList();
			}
			else
			{
				positions = sourcePositions;
			}
			
			// If positions is smaller than 3 then we can't construct a polygon. This could happen if you try to cut the
			// tip off a very, very thin brush. While the plane and the brushes would intersect, the actual
			// cross-sectional area is near zero and too small to create a valid polygon. In this case simply return
			// null to indicate polygon creation was impossible
			if(positions.Count < 3)
			{
				return null;
			}
			
			// Find center point, so we can sort the positions around it
			Vector3 center = positions[0];
			
			for (int i = 1; i < positions.Count; i++)
			{
				center += positions[i];
			}
			
			center *= 1f / positions.Count;
			
			if(positions.Count < 3)
			{
				Debug.LogError("Position count is below 3, this is probably unhandled");
			}
			
			// Find the plane
			UnityEngine.Plane plane = new UnityEngine.Plane(positions[0], positions[1], positions[2]);
			
			
			
			// Rotation to go from the polygon's plane to XY plane (for sorting)
			Quaternion cancellingRotation = Quaternion.Inverse(Quaternion.LookRotation(plane.normal));
			
			// Rotate the center point onto the plane too
			Vector3 rotatedCenter = cancellingRotation * center;
			
			// Sort the positions, passing the rotation to put the positions on XY plane and the rotated center point
			IComparer<Vector3> comparer = new SortVectorsClockwise(cancellingRotation, rotatedCenter);
			positions.Sort(comparer);
			
			
			// Create the vertices from the positions
			Vertex[] newPolygonVertices = new Vertex[positions.Count];
			for (int i = 0; i < positions.Count; i++)
			{
				newPolygonVertices[i] = new Vertex(positions[i], -plane.normal, (cancellingRotation * positions[i]) * 0.5f);
			}
			Polygon newPolygon = new Polygon(newPolygonVertices, null, false, false);
			
			if(newPolygon.Plane.normal == Vector3.zero)
			{
				Debug.LogError("Zero normal found, this leads to invalid polyhedron-point tests");
				
				// hacky
				//				if(removeExtraPositions)
				//				{
				//					Polygon.Vector3ComparerEpsilon equalityComparer = new Polygon.Vector3ComparerEpsilon();
				//					List<Vector3> testFoo = newPolygonVertices.Select(item => item.Position).Distinct(equalityComparer).ToList();
				//				}
			}
			return newPolygon;
		}

		/// <summary>
		/// Used to sort a collection of Vectors in a clockwise direction
		/// </summary>
		internal class SortVectorsClockwise : IComparer<Vector3>
		{
			Quaternion cancellingRotation; // Used to transform the positions from an arbitrary plane to the XY plane
			Vector3 rotatedCenter; // Transformed center point, used as the center point to find the angles around
			
			public SortVectorsClockwise(Quaternion cancellingRotation, Vector3 rotatedCenter)
			{
				this.cancellingRotation = cancellingRotation;
				this.rotatedCenter = rotatedCenter;
			}
			
			public int Compare(Vector3 position1, Vector3 position2)
			{
				// Rotate the positions and subtract the center, so they become vectors from the center point on the plane
				Vector3 vector1 = (cancellingRotation * position1) - rotatedCenter;
				Vector3 vector2 = (cancellingRotation * position2) - rotatedCenter;
				
				// Find the angle of each vector on the plane
				float angle1 = Mathf.Atan2(vector1.x, vector1.y);
				float angle2 = Mathf.Atan2(vector2.x, vector2.y);
				
				// Compare the angles
				return angle1.CompareTo(angle2);
			}
		}
	}
}
#endif