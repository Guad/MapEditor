using System.Drawing;
using GTA;
using GTA.Math;

namespace MapEditor
{
	public class Marker
	{
		public MarkerType Type;
		public Vector3 Position;
		public Vector3 Rotation;
		public Vector3 Scale;
	    public Vector3? TeleportTarget;
		public int Red;
		public int Green;
		public int Blue;
		public int Alpha;
		public bool BobUpAndDown;
		public bool RotateToCamera;
	    public bool OnlyVisibleInEditor;
		public int Id;
	}
}