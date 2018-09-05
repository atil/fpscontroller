#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Sabresaurus.SabreCSG
{
	/// <summary>
	/// Provides helper methods for determining polygon adjacency
	/// </summary>
	public static class AdjacencyHelper
	{
		/// <summary>
		/// Delegate that takes an edge and specifies whether it is relevant to an adjacency test
		/// </summary>
		public delegate bool IsEdgeRelevant(Edge edgeToTest);
		/// <summary>
		/// Delegate that takes a polygon and specifies whether it is relevant to an adjacency test
		/// </summary>
		public delegate bool IsPolygonRelevant(Polygon polygonToTest);

		/// <summary>
		/// Finds the polygons in allBuiltPolygons that share a vertical edge with one of the source polygons
		/// </summary>
		/// <returns>The adjacent polygon Unique Indexes.</returns>
		/// <param name="allBuiltPolygons">All possible polygons.</param>
		/// <param name="selectedSourcePolygons">Selected source polygons.</param>
		public static List<int> FindAdjacentWalls(List<Polygon> allBuiltPolygons, List<Polygon> selectedSourcePolygons)
		{
			return FindAdjacentGeometry(allBuiltPolygons, selectedSourcePolygons, IsEdgeVertical, null);
		}

		/// <summary>
		/// Finds the polygons in allBuiltPolygons that share a horizontal edge with one of the source polygons and point up
		/// </summary>
		/// <returns>The adjacent polygon Unique Indexes.</returns>
		/// <param name="allBuiltPolygons">All possible polygons.</param>
		/// <param name="selectedSourcePolygons">Selected source polygons.</param>
		public static List<int> FindAdjacentFloors(List<Polygon> allBuiltPolygons, List<Polygon> selectedSourcePolygons)
		{
			return FindAdjacentGeometry(allBuiltPolygons, selectedSourcePolygons, IsEdgeHorizontal, IsPolygonFloor);
		}

		/// <summary>
		/// Finds the polygons in allBuiltPolygons that share a horizontal edge with one of the source polygons and point down
		/// </summary>
		/// <returns>The adjacent polygon Unique Indexes.</returns>
		/// <param name="allBuiltPolygons">All possible polygons.</param>
		/// <param name="selectedSourcePolygons">Selected source polygons.</param>
		public static List<int> FindAdjacentCeilings(List<Polygon> allBuiltPolygons, List<Polygon> selectedSourcePolygons)
		{
			return FindAdjacentGeometry(allBuiltPolygons, selectedSourcePolygons, IsEdgeHorizontal, IsPolygonCeiling);
		}

		/// <summary>
		/// Finds the polygons in allBuiltPolygons that share any edge with one of the source polygons
		/// </summary>
		/// <returns>The adjacent polygon Unique Indexes.</returns>
		/// <param name="allBuiltPolygons">All possible polygons.</param>
		/// <param name="selectedSourcePolygons">Selected source polygons.</param>
		public static List<int> FindAdjacentAll(List<Polygon> allBuiltPolygons, List<Polygon> selectedSourcePolygons)
		{
			return FindAdjacentGeometry(allBuiltPolygons, selectedSourcePolygons, null, null);
		}

		/// <summary>
		/// Given possible polygons and specific source polygons, this takes two delegates for if an edge and a polygon is relevant and finds all the polygons that match its criteria (see the other FindAdjacent* methods in this file for examples)
		/// </summary>
		/// <param name="allBuiltPolygons">All possible polygons.</param>
		/// <param name="selectedSourcePolygons">Selected source polygons.</param>
		/// <param name="isEdgeRelevant">Is edge relevant.</param>
		/// <param name="isPolygonRelevant">Is polygon relevant.</param>
		public static List<int> FindAdjacentGeometry(List<Polygon> allBuiltPolygons, 
			List<Polygon> selectedSourcePolygons, 
			IsEdgeRelevant isEdgeRelevant,
			IsPolygonRelevant isPolygonRelevant)
		{
			List<Polygon> unselectedPolygons = new List<Polygon>(allBuiltPolygons);
			List<Polygon> selectedBuiltPolygons = new List<Polygon>();

			// Sort the built polygons into those that are selected and those that aren't
			// This starts off with all the polygons in the unselected list then picks out all those that are actually
			// selected.
			IEqualityComparer<Polygon> comparer = new Polygon.PolygonUIDComparer();
			for (int i = 0; i < unselectedPolygons.Count; i++) 
			{
				if(selectedSourcePolygons.Contains(unselectedPolygons[i], comparer))
				{
					selectedBuiltPolygons.Add(unselectedPolygons[i]);

					unselectedPolygons.RemoveAt(i);
					i--;
				}
			}

			// Now extract all the target edges of the selected polygons that satisfy the edge relevant requirement
			List<Edge> targetEdges = new List<Edge>();
			for (int polygonIndex = 0; polygonIndex < selectedBuiltPolygons.Count; polygonIndex++) 
			{
				Edge[] edges = selectedBuiltPolygons[polygonIndex].GetEdges();
				for (int edgeIndex = 0; edgeIndex < edges.Length; edgeIndex++) 
				{
					if(isEdgeRelevant == null || isEdgeRelevant(edges[edgeIndex]))
					{
						targetEdges.Add(edges[edgeIndex]);
					}
				}
			}

			// Walk through all the unselected polygons and see if any match the target edges (if a isPolygonRelevant
			// delegate is also supplied then the unselected polygon must also be considered relevant)
			for (int polygonIndex = 0; polygonIndex < unselectedPolygons.Count; polygonIndex++) 
			{
				if(isPolygonRelevant != null && !isPolygonRelevant(unselectedPolygons[polygonIndex]))
				{
					continue;
				}

				// Grab all the edges of the polygon
				Edge[] edges = unselectedPolygons[polygonIndex].GetEdges();

				// Test each edge to see if it matches the target edges
				for (int edgeIndex = 0; edgeIndex < edges.Length; edgeIndex++) 
				{
					bool matched = false;
					for (int targetEdgeIndex = 0; targetEdgeIndex < targetEdges.Count; targetEdgeIndex++) 
					{
						if(edges[edgeIndex].Intersects(targetEdges[targetEdgeIndex]))
						{
							matched = true;
							selectedBuiltPolygons.Add(unselectedPolygons[polygonIndex]);
							break;
						}
					}
					if(matched)
					{
						break;
					}
				}
			}

			// Return all the unique set of polygon IDs
			return selectedBuiltPolygons.Select(polygon => polygon.UniqueIndex).Distinct().ToList();
		}



		static bool IsEdgeVertical(Edge edge)
		{
			Vector3 direction = edge.Vertex2.Position - edge.Vertex1.Position;
			direction.Normalize();

			// Vertical edges should have a dot product of about 1 or -1 with a vertical vector
			if(Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.99f)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		static bool IsEdgeHorizontal(Edge edge)
		{
			Vector3 direction = edge.Vertex2.Position - edge.Vertex1.Position;
			direction.Normalize();

			// Horizontal edges should have a dot product of about 0 with a vertical vector
			if(Mathf.Abs(Vector3.Dot(direction, Vector3.up)) < 0.01f)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		static bool IsPolygonFloor(Polygon polygon)
		{
			return Vector3.Dot(polygon.Plane.normal, Vector3.up) > 0.99f;
		}

		static bool IsPolygonCeiling(Polygon polygon)
		{
			return Vector3.Dot(polygon.Plane.normal, Vector3.up) < -0.99f;
		}

		/// <summary>
		/// Given an ordered series of vertices, a specific vertex and one of its neighbours, return the other neighbour
		/// </summary>
		/// <returns>The vertex adjacent to <c>sourceVertex</c> in the opposite direction to <c>neighbourToExclude</c>.</returns>
		/// <param name="vertices">Order vertices.</param>
		/// <param name="sourceVertex">Source vertex.</param>
		/// <param name="neighbourToExclude">Neighbour to exclude.</param>
		public static Vertex FindAdjacentVertex(Vertex[] vertices, Vertex sourceVertex, Vertex neighbourToExclude)
		{
			Vertex edge1AdjacentVertex = null;
			for (int i = 0; i < vertices.Length; i++) 
			{
				if(vertices[i] == sourceVertex)
				{
					int lastIndex = i-1;
					if(lastIndex < 0)
					{
						lastIndex = vertices.Length-1;
					}
					int nextIndex = i+1;
					if(nextIndex >= vertices.Length)
					{
						nextIndex = 0;
					}
					if(vertices[lastIndex] == neighbourToExclude)
					{
						edge1AdjacentVertex = vertices[nextIndex];
					}
					else
					{
						edge1AdjacentVertex = vertices[nextIndex];
					}
				}
			}
			return edge1AdjacentVertex;
		}
	}
}
#endif