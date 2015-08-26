using System;
using GTA;
using GTA.Math;
using GTA.Native;

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

		// Ped stuff
		public string Action;
		public string Relationship;
		public WeaponHash? Weapon;

		// Vehicle stuff
		public bool SirensActive;

		[NonSerialized]
		public int Id = -1;
	}

	public enum ObjectTypes
	{
		Prop,
		Vehicle,
		Ped,
		Marker,
	}
}