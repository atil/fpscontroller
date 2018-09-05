using UnityEngine;
using System.Collections;
using System;

namespace Sabresaurus.SabreCSG
{
	public static class EnumHelper
	{
	    public static bool IsFlagSet(Enum candidate, Enum test)
	    {
	        //		// TODO: Are these casts required?
	        if ((Convert.ToInt32(candidate) & Convert.ToInt32(test)) != 0)
	        {
	            return true;
	        }
	        else
	        {
	            return false;
	        }
	    }
	}
}