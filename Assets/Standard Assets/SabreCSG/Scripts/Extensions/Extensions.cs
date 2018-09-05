using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	public static class Extensions
	{
		const float EPSILON = 1e-5f;
		const float EPSILON_LOWER = 1e-4f;
		const float EPSILON_LOWER_2 = 1e-3f;

		public static Vector3 Abs(this Vector3 a)
	    {
	        return new Vector3(Mathf.Abs(a.x), Mathf.Abs(a.y), Mathf.Abs(a.z));
	    }

	    public static Vector3 Multiply(this Vector3 a, Vector3 b)
	    {
	        return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
	    }

		public static Vector3 Divide(this Vector3 a, Vector3 b)
		{
			return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
		}

		public static Vector2 Multiply(this Vector2 a, Vector2 b)
		{
			return new Vector2(a.x * b.x, a.y * b.y);
		}
		
		public static Vector2 Divide(this Vector2 a, Vector2 b)
		{
			return new Vector2(a.x / b.x, a.y / b.y);
		}

		public static bool HasComponent<T>(this MonoBehaviour behaviour) where T : Component
	    {
	        return (behaviour.GetComponent<T>() != null);
	    }

	    public static bool HasComponent<T>(this GameObject gameObject) where T : Component
	    {
	        return (gameObject.GetComponent<T>() != null);
	    }

		public static T AddOrGetComponent<T>(this MonoBehaviour behaviour) where T : Component
		{
			T component = behaviour.GetComponent<T>();
			if(component != null)
			{
				return component;
			}
			else
			{
				return behaviour.gameObject.AddComponent<T>();
			}
		}

		public static T AddOrGetComponent<T>(this GameObject gameObject) where T : Component
		{
			T component = gameObject.GetComponent<T>();
			if(component != null)
			{
				return component;
			}
			else
			{
				return gameObject.AddComponent<T>();
			}
		}

		public static Vector2 Rotate(this Vector2 vector, float angle)
		{
			angle *= Mathf.Deg2Rad;
			return new Vector2( vector.x * Mathf.Cos(angle) - vector.y * Mathf.Sin(angle),
								vector.x * Mathf.Sin(angle) + vector.y * Mathf.Cos(angle));
		}

//		public static GameObject Duplicate(this GameObject sourceObject)
//		{
//			GameObject duplicate = GameObject.Instantiate(sourceObject) as GameObject;
//			duplicate.transform.parent = sourceObject.transform.parent;
//			duplicate.name = sourceObject.name;
//			return duplicate;
//		}

		public static float GetSmallestExtent(this Bounds bounds)
		{
			if(bounds.extents.x < bounds.extents.y && bounds.extents.x < bounds.extents.z)
			{
				return bounds.extents.x;
			}
			else if(bounds.extents.y < bounds.extents.x && bounds.extents.y < bounds.extents.z)
			{
				return bounds.extents.y;
			}
			else
			{
				return bounds.extents.z;
			}
		}

		public static float GetLargestExtent(this Bounds bounds)
		{
			if(bounds.extents.x > bounds.extents.y && bounds.extents.x > bounds.extents.z)
			{
				return bounds.extents.x;
			}
			else if(bounds.extents.y > bounds.extents.x && bounds.extents.y > bounds.extents.z)
			{
				return bounds.extents.y;
			}
			else
			{
				return bounds.extents.z;
			}
		}

		public static bool Equals(this Color32 color, Color32 other)
		{
			if(color.r == other.r && color.g == other.g && color.b == other.b && color.a == other.a)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public static bool NotEquals(this Color32 color, Color32 other)
		{
			if(color.r != other.r || color.g != other.g || color.b != other.b || color.a != other.a)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public static Transform AddChild(this Transform parentTransform, string name)
		{
			GameObject newObject = new GameObject(name);
			newObject.transform.parent = parentTransform;
			newObject.transform.localPosition = Vector3.zero;
			newObject.transform.localRotation = Quaternion.identity;
			newObject.transform.localScale = Vector3.one;
			return newObject.transform;
		}

		public static void DestroyChildrenImmediate(this Transform parentTransform)
		{
			int childCount = parentTransform.childCount;
			for (int i = 0; i < childCount; i++) 
			{
				GameObject.DestroyImmediate(parentTransform.GetChild(0).gameObject);
				
	//			GameObject.DestroyImmediate(parentTransform.GetChild(i).gameObject);
			}
		}

		public static bool IsParentOf(this Transform thisTransform, Transform otherTransform)
		{
			Transform parentTransform = otherTransform.parent;

			// Walk up the other transform's parents until we match this transform or hit null
			while(parentTransform != null)
			{
				if(parentTransform == thisTransform)
				{
					return true;
				}

				parentTransform = parentTransform.parent;
			}

			// Reached the top and didn't match. This transform is not a parent of the other transform
			return false;
		}

		public static void ForceRefreshSharedMesh(this MeshCollider meshCollider)
		{
			Mesh sharedMesh = meshCollider.sharedMesh;
			meshCollider.sharedMesh = null;
			meshCollider.sharedMesh = sharedMesh;
		}

		public static void ForceRefreshSharedMesh(this MeshFilter meshFilter)
		{
			Mesh sharedMesh = meshFilter.sharedMesh;
			Vector3[] vertices = sharedMesh.vertices;
			sharedMesh.vertices = vertices;
		}

		public static string ToStringLong(this Vector3 source)
		{
			return string.Format("{0},{1},{2}", source.x,source.y,source.z);
		}

		public static string ToStringLong(this Plane source)
		{
			return string.Format("{0}, {1}, {2} : {3}", source.normal.x, source.normal.y, source.normal.z, source.distance);
		}

		public static string ToStringWithSuffix(this int number, string suffixSingular, string suffixPlural)
		{
			if(number == 1)
			{
				return number + suffixSingular;
			}
			else
			{
				return number + suffixPlural;
			}
		}

		public static bool ContentsEquals<T>(this T[] array1, T[] array2)
		{
			// If array references are identical, it's the same object, must be equal
			if(ReferenceEquals(array1,array2))
			{
				return true;
			}

			// Null arrays will always be considered not equal, even if both are null
			if(array1 == null || array2 == null)
			{
				return false;
			}
			// If the arrays have different length's they're obviously not equal
			if(array1.Length != array2.Length)
			{
				return false;
			}

			// Walk through and compare each element in the two arrays, if any don't match, return false
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;

			for (int i = 0; i < array1.Length; i++) 
			{
				if(!comparer.Equals(array1[i], array2[i]))
				{
					return false;
				}
			}

			// Array contents are equal!
			return true;
		}

		public static bool ContentsEquals<T>(this List<T> list1, List<T> list2)
		{
			// If array references are identical, it's the same object, must be equal
			if(ReferenceEquals(list1, list2))
			{
				return true;
			}

			// Null arrays will always be considered not equal, even if both are null
			if(list1 == null || list2 == null)
			{
				return false;
			}
			// If the arrays have different length's they're obviously not equal
			if(list1.Count != list2.Count)
			{
				return false;
			}

			// Walk through and compare each element in the two arrays, if any don't match, return false
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;

			for (int i = 0; i < list1.Count; i++) 
			{
				if(!comparer.Equals(list1[i], list2[i]))
				{
					return false;
				}
			}

			// Array contents are equal!
			return true;
		}

		public static int[] GetTrianglesSafe(this Mesh mesh)
		{
			// Unfortunately in Unity 5.1 accessing .triangles on an empty mesh throws an error
			if(mesh.vertexCount == 0)
			{
				return new int[0];
			}
			else
			{
				return mesh.triangles;
			}
		}

		public static bool EqualsWithEpsilon(this float a, float b)
		{
			return Mathf.Abs(a - b) < EPSILON;
		}

		/// <summary>
		/// Determines whether two vector's are equal, allowing for floating point differences with an Epsilon value taken into account in per component comparisons
		/// </summary>
		public static bool EqualsWithEpsilon(this Vector3 a, Vector3 b)
		{
			return Mathf.Abs(a.x - b.x) < EPSILON && Mathf.Abs(a.y - b.y) < EPSILON && Mathf.Abs(a.z - b.z) < EPSILON;
		}

		public static bool EqualsWithEpsilonLower(this Vector3 a, Vector3 b)
		{
			return Mathf.Abs(a.x - b.x) < EPSILON_LOWER && Mathf.Abs(a.y - b.y) < EPSILON_LOWER && Mathf.Abs(a.z - b.z) < EPSILON_LOWER;
		}

		public static Rect ExpandFromCenter(this Rect rect, Vector2 expansion)
		{
			rect.size += expansion;
			rect.center -= expansion / 2f;
			return rect;
		}
	}
}