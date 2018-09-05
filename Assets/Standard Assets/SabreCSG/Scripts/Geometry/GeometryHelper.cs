#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Sabresaurus.SabreCSG
{
	/// <summary>
	/// Holds information on a Ray to Polygon series raycast result
	/// </summary>
	public struct PolygonRaycastHit
	{
		public Vector3 Point; // Point at which the ray hit the polygon
		public Vector3 Normal; // Surface normal of the hit polygon
		public float Distance; // Distance along the ray at which the hit occurred
		public GameObject GameObject; // Brush that the polygon exists on (or <c>null</c> if not relevant)
		public Polygon Polygon; // Hit polygon
	}

	/// <summary>
	/// Provides general helper methods for dealing with geometry
	/// </summary>
	public static class GeometryHelper
	{
		private const float CONVEX_EPSILON = 0.001f;

		/// <summary>
		/// Determines if a set of polygons represent a convex brush with planar polygons
		/// </summary>
		/// <returns><c>true</c> if is brush is convex and all polygons are planar; otherwise, <c>false</c>.</returns>
		/// <param name="polygons">Source polygons.</param>
		public static bool IsBrushConvex(Polygon[] polygons)
		{
			for (int n = 0; n < polygons.Length; n++) 
			{
				for (int k = 0; k < polygons[n].Vertices.Length; k++) 
				{
					// Test every vertex against every plane, if the vertex is front of the plane then the brush is concave
					for (int i = 0; i < polygons.Length; i++) 
					{
						Polygon polygon = polygons[i];
						for (int z = 2; z < polygon.Vertices.Length; z++) 
						{
							Plane polygonPlane = new Plane(polygon.Vertices[0].Position, 
								polygon.Vertices[z-1].Position, 
								polygon.Vertices[z].Position);


							float dot = Vector3.Dot(polygonPlane.normal, polygons[n].Vertices[k].Position) + polygonPlane.distance;

							if(dot > CONVEX_EPSILON)
							{
								return false;
							}
						}

						for (int z = 0; z < polygon.Vertices.Length; z++) 
						{
							Plane polygonPlane = new Plane(polygon.Vertices[z].Position, 
								polygon.Vertices[(z+1)%polygon.Vertices.Length].Position, 
								polygon.Vertices[(z+2)%polygon.Vertices.Length].Position);


							float dot = Vector3.Dot(polygonPlane.normal, polygons[n].Vertices[k].Position) + polygonPlane.distance;

							if(dot > CONVEX_EPSILON)
							{
								return false;
							}
						}
					}

				}
			}

			return true;
		}

		/// <summary>
		/// Raycasts a series of polygons, returning the hit polygon or <c>null</c>
		/// </summary>
		/// <returns>The hit polygon or <c>null</c> if no polygon was hit.</returns>
		/// <param name="polygons">Source polygons to raycast against.</param>
		/// <param name="ray">Ray.</param>
		/// <param name="hitDistance">If a hit ocurred, this is how far along the ray the hit was.</param>
		/// <param name="polygonSkin">Optional polygon skin that allows the polygon to be made slightly larger by displacing its vertices.</param>
		public static Polygon RaycastPolygons(List<Polygon> polygons, Ray ray, out float hitDistance, float polygonSkin = 0)
		{
			Polygon closestPolygon = null;
			float closestSquareDistance = float.PositiveInfinity;
			hitDistance = 0;

			if(polygons != null)
			{
				// 
				for (int i = 0; i < polygons.Count; i++) 
				{
					if(polygons[i].ExcludeFromFinal)
					{
						continue;
					}

					// Skip any polygons that are facing away from the ray
					if(Vector3.Dot(polygons[i].Plane.normal, ray.direction) > 0)
					{
						continue;
					}

					if(GeometryHelper.RaycastPolygon(polygons[i], ray, polygonSkin))
					{
						// Get the real hit point by testing the ray against the polygon's plane
						Plane plane = polygons[i].Plane;

						float rayDistance;
						plane.Raycast(ray, out rayDistance);
						Vector3 hitPoint = ray.GetPoint(rayDistance);

						// Find the square distance from the camera to the hit point (squares used for speed)
						float squareDistance = (ray.origin - hitPoint).sqrMagnitude;
						// If the distance is closer than the previous closest polygon, use this one.
						if(squareDistance < closestSquareDistance)
						{
							closestPolygon = polygons[i];
							closestSquareDistance = squareDistance;
							hitDistance = rayDistance;
						}
					}
				}
			}

			return closestPolygon;
		}

		/// <summary>
		/// Raycasts the polygon.
		/// </summary>
		/// <returns><c>true</c>, if polygon was raycasted, <c>false</c> otherwise.</returns>
		/// <param name="polygon">Polygon.</param>
		/// <param name="ray">Ray.</param>
		/// <param name="polygonSkin">Polygon skin.</param>
		/// 
		/// <summary>
		/// Raycasts a polygons, returning <c>true</c> if a hit occurred; otherwise <c>false</c>
		/// </summary>
		/// <returns><c>true</c> if a hit occurred; otherwise <c>false</c></returns>
		/// <param name="polygons">Source polygon to raycast against.</param>
		/// <param name="ray">Ray.</param>
		/// <param name="hitDistance">If a hit ocurred, this is how far along the ray the hit was.</param>
		/// <param name="polygonSkin">Optional polygon skin that allows the polygon to be made slightly larger by displacing its vertices.</param>
		public static bool RaycastPolygon(Polygon polygon, Ray ray, float polygonSkin = 0)
		{
			// TODO: This probably won't work if the ray and polygon are coplanar, but right now that's not a usecase
//			polygon.CalculatePlane();
			Plane plane = polygon.Plane;
			float distance = 0;

			// First of all find if and where the ray hit's the polygon's plane
			if(plane.Raycast(ray, out distance))
			{
				Vector3 hitPoint = ray.GetPoint(distance);

				// Now find out if the point on the polygon plane is behind each polygon edge
				for (int i = 0; i < polygon.Vertices.Length; i++) 
				{
					Vector3 point1 = polygon.Vertices[i].Position;
					Vector3 point2 = polygon.Vertices[(i+1)%polygon.Vertices.Length].Position;

					Vector3 edge = point2 - point1; // Direction from a vertex to the next
					Vector3 polygonNormal = plane.normal;

					// Cross product of the edge with the polygon's normal gives the edge's normal
					Vector3 edgeNormal = Vector3.Cross(edge, polygonNormal);

					Vector3 edgeCenter = (point1+point2) * 0.5f;

					if(polygonSkin != 0)
					{
						edgeCenter += edgeNormal.normalized * polygonSkin;
					}

					Vector3 pointToEdgeCentroid = edgeCenter - hitPoint;

					// If the point is outside an edge this will return a negative value
					if(Vector3.Dot(edgeNormal, pointToEdgeCentroid) < 0)
					{
						return false;
					}
				}

				return true;
			}
			else
			{
				return false;
			}
		}

		const float TEST_EPSILON = 0.003f;


 

		/// <summary>
		/// Determine if any vertices from a series of polygons are on opposite sides of a plane. This basically tests against a really thick plane to see if some of the points are on each side of the thick plane. This makes sure we only split if we definitely need to (protecting against issues related to splitting very small polygons breaking other code).
		/// </summary>
		/// <returns><c>true</c>, if intersection was found, <c>false</c> otherwise.</returns>
		/// <param name="polygons">Source Polygons.</param>
		/// <param name="testPlane">Test plane.</param>
		public static bool PolygonsIntersectPlane (List<Polygon> polygons, Plane testPlane)
		{
			int numberInFront = 0;
			int numberBehind = 0;

			float distanceInFront = 0f;
			float distanceBehind = 0f;

			for (int polygonIndex = 0; polygonIndex < polygons.Count; polygonIndex++) 
			{
				for (int vertexIndex = 0; vertexIndex < polygons[polygonIndex].Vertices.Length; vertexIndex++) 
				{
					Vector3 point = polygons[polygonIndex].Vertices[vertexIndex].Position;

					float distance = testPlane.GetDistanceToPoint(point);

					if (distance < -TEST_EPSILON)
					{
						numberInFront++;

						distanceInFront = Mathf.Min(distanceInFront, distance);
					}
					else if (distance > TEST_EPSILON)
					{
						numberBehind++;

						distanceBehind = Mathf.Max(distanceBehind, distance);
					}
				}
			}

			if(numberInFront > 0 && numberBehind > 0)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Finds the UV for a supplied position on a polygon, note this internally handles situations where vertices overlap or are colinear which the other version of this method does not
		/// </summary>
		/// <returns>The UV for the supplied position.</returns>
		/// <param name="polygon">Polygon.</param>
		/// <param name="newPosition">Position to find the UV for.</param>
		public static Vector2 GetUVForPosition(Polygon polygon, Vector3 newPosition)
		{
			int vertexIndex1 = 0;
			int vertexIndex2 = 0;
			int vertexIndex3 = 0;

			// Account for overlapping vertices
			for (int i = vertexIndex1+1; i < polygon.Vertices.Length; i++) 
			{
				if(!polygon.Vertices[i].Position.EqualsWithEpsilon(polygon.Vertices[vertexIndex1].Position))
				{
					vertexIndex2 = i;
					break;
				}
			}

			for (int i = vertexIndex2+1; i < polygon.Vertices.Length; i++) 
			{
				if(!polygon.Vertices[i].Position.EqualsWithEpsilon(polygon.Vertices[vertexIndex2].Position))
				{
					vertexIndex3 = i;
					break;
				}
			}

			// Now account for the fact that the picked three vertices might be collinear
			Vector3 pos1 = polygon.Vertices[vertexIndex1].Position;
			Vector3 pos2 = polygon.Vertices[vertexIndex2].Position;
			Vector3 pos3 = polygon.Vertices[vertexIndex3].Position;

			Plane plane = new Plane(pos1,pos2,pos3);
			if(plane.normal == Vector3.zero)
			{
				for (int i = 2; i < polygon.Vertices.Length; i++) 
				{
					vertexIndex3 = i;

					pos3 = polygon.Vertices[vertexIndex3].Position;

					Plane tempPlane = new Plane(pos1,pos2,pos3);

					if(tempPlane.normal != Vector3.zero)
					{
						break;
					}
				}
				plane = new Plane(pos1,pos2,pos3);
			}

			// Should now have a good set of positions, so continue

			Vector3 planePoint = MathHelper.ClosestPointOnPlane(newPosition, plane);

			Vector2 uv1 = polygon.Vertices[vertexIndex1].UV;
			Vector2 uv2 = polygon.Vertices[vertexIndex2].UV;
			Vector2 uv3 = polygon.Vertices[vertexIndex3].UV;

			// calculate vectors from point f to vertices p1, p2 and p3:
			Vector3 f1 = pos1-planePoint;
			Vector3 f2 = pos2-planePoint;
			Vector3 f3 = pos3-planePoint;

			// calculate the areas (parameters order is essential in this case):
			Vector3 va = Vector3.Cross(pos1-pos2, pos1-pos3); // main triangle cross product
			Vector3 va1 = Vector3.Cross(f2, f3); // p1's triangle cross product
			Vector3 va2 = Vector3.Cross(f3, f1); // p2's triangle cross product
			Vector3 va3 = Vector3.Cross(f1, f2); // p3's triangle cross product

			float a = va.magnitude; // main triangle area

			// calculate barycentric coordinates with sign:
			float a1 = va1.magnitude/a * Mathf.Sign(Vector3.Dot(va, va1));
			float a2 = va2.magnitude/a * Mathf.Sign(Vector3.Dot(va, va2));
			float a3 = va3.magnitude/a * Mathf.Sign(Vector3.Dot(va, va3));

			// find the uv corresponding to point f (uv1/uv2/uv3 are associated to p1/p2/p3):
			Vector2 uv = uv1 * a1 + uv2 * a2 + uv3 * a3;

			return uv;
		}

		/// <summary>
		/// Given three position/UVs that can be used to reliably describe the UV find the UV for the specified position. Note if you do not have three vertices that will definitely provide reliable results you should use the other overload of this method which takes a polygon.
		/// </summary>
		/// <returns>The UV for the supplied position.</returns>
		/// <param name="pos1">Pos 1.</param>
		/// <param name="pos2">Pos 2.</param>
		/// <param name="pos3">Pos 3.</param>
		/// <param name="uv1">UV 1 (corresponding to Pos1).</param>
		/// <param name="uv2">UV 2 (corresponding to Pos2).</param>
		/// <param name="uv3">UV 3 (corresponding to Pos3).</param>
		/// <param name="newPosition">Position to find the UV for.</param>
		public static Vector2 GetUVForPosition(Vector3 pos1, Vector3 pos2, Vector3 pos3, 
			Vector2 uv1, Vector2 uv2, Vector2 uv3, 
			Vector3 newPosition)
		{
			Plane plane = new Plane(pos1,pos2,pos3);
			Vector3 planePoint = MathHelper.ClosestPointOnPlane(newPosition, plane);

			// calculate vectors from point f to vertices p1, p2 and p3:
			Vector3 f1 = pos1-planePoint;
			Vector3 f2 = pos2-planePoint;
			Vector3 f3 = pos3-planePoint;

			// calculate the areas (parameters order is essential in this case):
			Vector3 va = Vector3.Cross(pos1-pos2, pos1-pos3); // main triangle cross product
			Vector3 va1 = Vector3.Cross(f2, f3); // p1's triangle cross product
			Vector3 va2 = Vector3.Cross(f3, f1); // p2's triangle cross product
			Vector3 va3 = Vector3.Cross(f1, f2); // p3's triangle cross product

			float a = va.magnitude; // main triangle area

			// calculate barycentric coordinates with sign:
			float a1 = va1.magnitude/a * Mathf.Sign(Vector3.Dot(va, va1));
			float a2 = va2.magnitude/a * Mathf.Sign(Vector3.Dot(va, va2));
			float a3 = va3.magnitude/a * Mathf.Sign(Vector3.Dot(va, va3));

			// find the uv corresponding to point f (uv1/uv2/uv3 are associated to p1/p2/p3):
			Vector2 uv = uv1 * a1 + uv2 * a2 + uv3 * a3;

			return uv;
		}
	}
}
#endif