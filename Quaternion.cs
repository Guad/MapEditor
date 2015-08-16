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
	}
}