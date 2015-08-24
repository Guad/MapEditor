using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using Font = GTA.Font;

namespace MapEditor
{
	/// <summary>
	/// Only create the first 200 objects that are in proximity to the player.
	/// </summary>
	public static class PropStreamer
	{
		public static int MAX_OBJECTS = 2048;

		public static List<MapObject> MemoryObjects = new List<MapObject>();

		public static List<int> StreamedInHandles = new List<int>();

		public static List<int> StaticProps = new List<int>();

		public static List<int> Vehicles = new List<int>(); // Not streamed in.

		public static List<int> Peds = new List<int>(); // Not streamed in.

		public static int PropCount => StreamedInHandles.Count + MemoryObjects.Count;

		public static int EntityCount => StreamedInHandles.Count + MemoryObjects.Count + Vehicles.Count + Peds.Count;

		public static List<MapObject> RemovedObjects = new List<MapObject>();

		public static Prop CreateProp(Model model, Vector3 position, Vector3 rotation, bool dynamic, Quaternion q = null, bool force = false)
		{
			
			if (StreamedInHandles.Count >= MAX_OBJECTS)
			{
				UI.Notify("~r~~h~Map Editor~h~~w~\nYou have reached the prop limit. You cannot place any more props.");
				return null;
			} // */
			var prop = World.CreateProp(model, position, rotation, dynamic, false);
			if (prop == null) return null;
			StreamedInHandles.Add(prop.Handle);
			if (!dynamic)
			{
				StaticProps.Add(prop.Handle);
				prop.FreezePosition = true;
			}
			if (q != null)
				Quaternion.SetEntityQuaternion(prop, q);
			prop.Position = position;
			return prop;
		}

		public static Vehicle CreateVehicle(Model model, Vector3 position, float heading, bool dynamic, Quaternion q = null)
		{
			var veh = World.CreateVehicle(model, position, heading);
			Vehicles.Add(veh.Handle);
			if (!dynamic)
			{
				StaticProps.Add(veh.Handle);
				veh.FreezePosition = true;
			}
			if(q != null)
				Quaternion.SetEntityQuaternion(veh, q);
			return veh;
		}

		public static Ped CreatePed(Model model, Vector3 position, float heading, bool dynamic, Quaternion q = null)
		{
			var veh = World.CreatePed(model, position, heading);
			Peds.Add(veh.Handle);
			if (!dynamic)
			{
				StaticProps.Add(veh.Handle);
				veh.FreezePosition = true;
			}
			if (q != null)
				Quaternion.SetEntityQuaternion(veh, q);
			return veh;
		}

		public static void RemoveVehicle(int handle)
		{
			new Vehicle(handle).Delete();
			if (Vehicles.Contains(handle)) Vehicles.Remove(handle);
			if (StaticProps.Contains(handle)) StaticProps.Remove(handle);
		}

		public static void RemovePed(int handle)
		{
			new Ped(handle).Delete();
			if (Peds.Contains(handle)) Peds.Remove(handle);
			if (StaticProps.Contains(handle)) StaticProps.Remove(handle);
		}

		public static void RemoveEntity(int handle)
		{
			new Prop(handle).Delete();
			if (Peds.Contains(handle)) Peds.Remove(handle);
			if (Vehicles.Contains(handle)) Vehicles.Remove(handle);
			if (StreamedInHandles.Contains(handle)) StreamedInHandles.Remove(handle);
		}

		internal static void AddProp(Prop prop, bool dynamic)
		{
			if (StreamedInHandles.Count > MAX_OBJECTS)
			{
				MemoryObjects.Add(new MapObject() {Dynamic = dynamic, Hash = prop.Model.Hash, Position = prop.Position, Quaternion = Quaternion.GetEntityQuaternion(prop), Rotation = prop.Rotation, Type = ObjectTypes.Prop});
				prop.Delete();
				return;
			}
			StreamedInHandles.Add(prop.Handle);
			if(!dynamic)
				StaticProps.Add(prop.Handle);
		}

		internal static void RemoveProp(Prop prop, bool dynamic)
		{
			if(StreamedInHandles.Contains(prop.Handle)) StreamedInHandles.Remove(prop.Handle);
			if(StaticProps.Contains(prop.Handle)) StaticProps.Remove(prop.Handle);
			if(MemoryObjects.Contains(new MapObject() { Dynamic = dynamic, Hash = prop.Model.Hash, Position = prop.Position, Quaternion = Quaternion.GetEntityQuaternion(prop), Rotation = prop.Rotation, Type = ObjectTypes.Prop })) 
				MemoryObjects.Remove(new MapObject() { Dynamic = dynamic, Hash = prop.Model.Hash, Position = prop.Position, Quaternion = Quaternion.GetEntityQuaternion(prop), Rotation = prop.Rotation, Type = ObjectTypes.Prop });
		}

		public static void RemoveAll()
		{
			StreamedInHandles.ForEach(i => new Prop(i).Delete());
			StreamedInHandles.Clear();
			MemoryObjects.Clear();
			StaticProps.Clear();
			Vehicles.ForEach(v => new Vehicle(v).Delete());
			Peds.ForEach(v => new Ped(v).Delete());
			Vehicles.Clear();
			Peds.Clear();
		}

		public static MapObject[] GetAllEntities()
		{
			List<MapObject> outList = StreamedInHandles.Select(handle => new MapObject() {Dynamic = !StaticProps.Contains(handle), Hash = new Prop(handle).Model.Hash, Position = new Prop(handle).Position, Quaternion = Quaternion.GetEntityQuaternion(new Prop(handle)), Rotation = new Prop(handle).Rotation, Type = ObjectTypes.Prop}).ToList();
			outList.AddRange(MemoryObjects);
			Vehicles.ForEach(v => outList.Add(new MapObject() { Dynamic = !StaticProps.Contains(v), Hash = new Vehicle(v).Model.Hash, Position = new Vehicle(v).Position, Quaternion = Quaternion.GetEntityQuaternion(new Vehicle(v)), Rotation = new Vehicle(v).Rotation, Type = ObjectTypes.Vehicle}));
			Peds.ForEach(v => outList.Add(new MapObject() { Dynamic = !StaticProps.Contains(v), Hash = new Ped(v).Model.Hash, Position = new Ped(v).Position, Quaternion = Quaternion.GetEntityQuaternion(new Ped(v)), Rotation = new Ped(v).Rotation, Type = ObjectTypes.Ped }));
			return outList.ToArray();
		}

		public static int[] GetAllHandles()
		{
			List<int> outHandles = new List<int>();
			outHandles.AddRange(StreamedInHandles);
			outHandles.AddRange(Vehicles);
			outHandles.AddRange(Peds);
			return outHandles.ToArray();
		}

		[Obsolete("Prop streaming has been disabled since the object limit is 2048.")]
		public static void MoveToMemory(Entity i)
		{
			var obj = new MapObject()
			{
				Dynamic = !StaticProps.Contains(i.Handle),
				Hash = i.Model.Hash,
				Position = i.Position,
				Quaternion = Quaternion.GetEntityQuaternion(i),
				Rotation = i.Rotation,
				Type = ObjectTypes.Prop,
			};
            MemoryObjects.Add(obj);
			StreamedInHandles.Remove(i.Handle);
			StaticProps.Remove(i.Handle);
			i.Delete();
		}

		[Obsolete("Prop streaming has been disabled since the object limit is 2048.")]
		public static void MoveFromMemory(MapObject obj)
		{
			var prop = obj;
			Prop newProp = World.CreateProp(new Model(prop.Hash), prop.Position, prop.Rotation, false, false);
			newProp.FreezePosition = !prop.Dynamic;
			StreamedInHandles.Add(newProp.Handle);
			if (!prop.Dynamic)
			{
				StaticProps.Add(newProp.Handle);
				newProp.FreezePosition = true;
			}
			if (prop.Quaternion != null)
				Quaternion.SetEntityQuaternion(newProp, prop.Quaternion);
			newProp.Position = prop.Position;
			MemoryObjects.Remove(prop);
		}

		
		//private static Vector3 _lastPos;
		public static void Tick()
		{
			foreach (MapObject o in RemovedObjects)
			{
				Prop returnedProp = Function.Call<Prop>(Hash.GET_CLOSEST_OBJECT_OF_TYPE, o.Position.X, o.Position.Y, o.Position.Z, 1f, o.Hash, 0);
				if (returnedProp == null || returnedProp.Handle == 0 || StreamedInHandles.Contains(returnedProp.Handle)) continue;
				returnedProp.Delete();
			}

			/*
			if(_lastPos == Game.Player.Character.Position)
				return;
			_lastPos = Game.Player.Character.Position;

			if (PropCount < MAX_OBJECTS)
			{
				if (MemoryObjects.Count != 0)
				{
					for (int i = MemoryObjects.Count - 1; i >= 0; i--)
					{
						var prop = MemoryObjects[i];
						Prop newProp = World.CreateProp(ObjectPreview.LoadObject(prop.Hash), prop.Position, prop.Rotation, false, false);
						newProp.FreezePosition = !prop.Dynamic;
						StreamedInHandles.Add(newProp.Handle);
						if (!prop.Dynamic)
						{
							StaticProps.Add(newProp.Handle);
							newProp.FreezePosition = true;
						}
						if (prop.Quaternion != null)
							Quaternion.SetEntityQuaternion(newProp, prop.Quaternion);
						MemoryObjects.Remove(prop);
					}
				}
				return;
			}
			
			MapObject[] propsToRemove = StreamedInHandles.Select(i => new MapObject()
			{
				Dynamic = !StaticProps.Contains(i), Hash = new Prop(i).Model.Hash, Position = new Prop(i).Position, Quaternion = Quaternion.GetEntityQuaternion(new Prop(i)), Rotation = new Prop(i).Rotation, Type = ObjectTypes.Prop, Id = i
			}).OrderBy(obj => (obj.Position - Game.Player.Character.Position).Length()).ToArray();

			MapObject[] propsToReAdd = MemoryObjects.OrderBy(obj => (obj.Position - Game.Player.Character.Position).Length()).ToArray();


			int lastPropToRemove = 0;
			int lastPropToReAdd = 0;
			for (int i = 0; i < MAX_OBJECTS; i++)
			{
				if (propsToReAdd.Length <= lastPropToReAdd)
				{
					lastPropToRemove = MAX_OBJECTS - lastPropToReAdd;
					break;
				}
				if (propsToRemove.Length <= lastPropToRemove)
				{
					lastPropToReAdd = MAX_OBJECTS - lastPropToRemove;
					break;
				}
				float readdLen = (propsToReAdd[lastPropToReAdd].Position - Game.Player.Character.Position).Length();
				float removeLen = (propsToRemove[lastPropToRemove].Position - Game.Player.Character.Position).Length();
				if (readdLen < removeLen)
					lastPropToReAdd++;
				else
					lastPropToRemove++;
			}

			for (var i = lastPropToRemove; i < propsToRemove.Length; i++)
			{
				MoveToMemory(new Prop(propsToRemove[i].Id));
			}
			
			for (int i = 0; i < lastPropToReAdd; i++) // Have to spawn it in
			{
				var prop = propsToReAdd[i];
				MoveFromMemory(prop);
			}
			// */
		}
	}
}