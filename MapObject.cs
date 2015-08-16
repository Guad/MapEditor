using GTA.Math;

namespace MapEditor
{
	public class MapObject
	{
		public ObjectTypes Type;
		public Vector3 Position;
		public Vector3 Rotation;
		public int Hash;
		public bool Dynamic;

		public Quaternion Quaternion;
	}

	public enum ObjectTypes
	{
		Prop,
		Vehicle,
		Ped,
	}
}