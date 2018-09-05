#if UNITY_EDITOR
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    public enum ResizeType { Corner, EdgeMid, FaceMid };

	// Used by ResizeEditor to describe two handles (e.g. X axis resize handles)
    public struct ResizeHandlePair
    {
        public Vector3 point1;
        public Vector3 point2;
        ResizeType resizeType;

        public ResizeType ResizeType
        {
            get
            {
                return resizeType;
            }
        }

        public ResizeHandlePair(Vector3 point1)
        {
            this.point1 = point1;
            this.point2 = -1 * point1;

            if (point1.sqrMagnitude == 1)
            {
                resizeType = ResizeType.FaceMid;
            }
            else if (point1.sqrMagnitude == 2)
            {
                resizeType = ResizeType.EdgeMid;
            }
            else
            {
                resizeType = ResizeType.Corner;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is ResizeHandlePair)
            {
                return this == (ResizeHandlePair)obj;
            }
            else
            {
                return false;
            }
        }

        public static bool operator ==(ResizeHandlePair lhs, ResizeHandlePair rhs)
        {
            return lhs.point1 == rhs.point1 && lhs.point2 == rhs.point2;
        }

        public static bool operator !=(ResizeHandlePair lhs, ResizeHandlePair rhs)
        {
            return lhs.point1 != rhs.point1 || lhs.point2 != rhs.point2;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
#endif