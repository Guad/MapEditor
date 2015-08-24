using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;

namespace MapEditor
{
	public class Map
	{
		 public List<MapObject> Objects = new List<MapObject>();
		 public List<MapObject> RemoveFromWorld = new List<MapObject>();
		 public List<Marker> Markers = new List<Marker>();
	}
}