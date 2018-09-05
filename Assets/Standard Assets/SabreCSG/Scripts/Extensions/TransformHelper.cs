using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Sabresaurus.SabreCSG
{
	public static class TransformHelper
	{
		public static List<Transform> GetRootSelectionOnly(Transform[] sourceTransforms)
		{
			List<Transform> rootTransforms = new List<Transform>(sourceTransforms);

			for (int i = 0; i < rootTransforms.Count; i++) 
			{
				for (int j = 0; j < rootTransforms.Count; j++) 
				{
					if(rootTransforms[i] != rootTransforms[j])
					{
						if(rootTransforms[j].IsParentOf(rootTransforms[i]))
						{
							rootTransforms.RemoveAt(i);
							i--;
							break;
						}
					}
				}
			}

			return rootTransforms;
		}


	}
}