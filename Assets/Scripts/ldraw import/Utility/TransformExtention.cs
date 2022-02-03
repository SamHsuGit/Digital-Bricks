using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LDraw
{
	public static class TransformExtention
	{
		public static void ApplyLocalTRS(this Transform tr, Matrix4x4 trs)
		{
			tr.localPosition = trs.ExtractPosition();
			tr.localRotation = trs.ExtractRotation();
			//tr.localScale = new Vector3(Mathf.Abs(trs.lossyScale.x), Mathf.Abs(trs.lossyScale.y), Mathf.Abs(trs.lossyScale.z)); // use Mathf.Abs to ensure scales are positive for box colliders
			tr.localScale = trs.lossyScale; // Mathf.Abs was causing import errors, reverted and box colliders appear correct...
		}
		
		public static Matrix4x4 ExtractLocalTRS(this Transform tr)
		{
			return Matrix4x4.TRS(tr.localPosition, tr.localRotation, tr.localScale);
		}

		public static void LocalReflect(this Transform tr, Vector3 planeNormal)
		{
			var trs = tr.ExtractLocalTRS();
			var reflected = trs.HouseholderReflection(planeNormal);
			tr.ApplyLocalTRS(reflected);
		}
	}
}