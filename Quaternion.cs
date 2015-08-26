using System;
using GTA;
using GTA.Native;

namespace MapEditor
{
	public class Quaternion
	{
		public float X;
		public float Y;
		public float Z;
		public float W;

		public static void SetEntityQuaternion(Entity ent, Quaternion q)
		{
			Function.Call(Hash.SET_ENTITY_QUATERNION, ent.Handle, q.X, q.Y, q.Z, q.W);
		}

		public static void SetEntityQuaternion(Entity ent, GTA.Math.Quaternion q)
		{
			Function.Call(Hash.SET_ENTITY_QUATERNION, ent.Handle, q.X, q.Y, q.Z, q.W);
		}

		public static Quaternion GetEntityQuaternion(Entity e)
		{
			var xg = new OutputArgument();
			var yg = new OutputArgument();
			var zg = new OutputArgument();
			var wg = new OutputArgument();

			Function.Call(Hash.GET_ENTITY_QUATERNION, e.Handle, xg, yg, zg, wg);
			return new Quaternion()
			{
				X = xg.GetResult<float>(),
				Y = yg.GetResult<float>(),
				Z = zg.GetResult<float>(),
				W = wg.GetResult<float>()
			};
		}

		public static Quaternion RotationYawPitchRoll(float pitch, float roll, float yaw)
		{
			Quaternion result = new Quaternion();

			pitch = (float)VectorExtensions.DegToRad(pitch);
			roll = (float)VectorExtensions.DegToRad(roll);
			yaw = (float)VectorExtensions.DegToRad(yaw);

			float halfRoll = roll*0.5f;
			float sinRoll = (float) Math.Sin((double) halfRoll);
			float cosRoll = (float) Math.Cos((double) halfRoll);

			float halfPitch = pitch*0.5f;
			float sinPitch = (float) Math.Sin((double) halfPitch);
			float cosPitch = (float) Math.Cos((double) halfPitch);

			float halfYaw = yaw*0.5f;
			float sinYaw = (float) Math.Sin((double) halfYaw);
			float cosYaw = (float) Math.Cos((double) halfYaw);

			result.X = (cosYaw*sinPitch*cosRoll) + (sinYaw*cosPitch*sinRoll);
			result.Y = (sinYaw*cosPitch*cosRoll) - (cosYaw*sinPitch*sinRoll);
			result.Z = (cosYaw*cosPitch*sinRoll) - (sinYaw*sinPitch*cosRoll);
			result.W = (cosYaw*cosPitch*cosRoll) + (sinYaw*sinPitch*sinRoll);

			return result;
		}
	}
}