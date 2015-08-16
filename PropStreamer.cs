using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;

namespace MapEditor
{
	/// <summary>
	/// Only create the first 200 objects that are in proximity to the player.
	/// </summary>
	public static class PropStreamer
	{
		 public static List<MapObject> MemoryObjects = new List<MapObject>();

		 public static List<int> StreamedInHandles = new List<int>();

		public static List<int> StaticProps = new List<int>();

		public static List<int> Vehicles = new List<int>(); // Not streamed in.

		public static List<int> Peds = new List<int>(); // Not streamed in.

		public static Prop CreateProp(Model model, Vector3 position, Vector3 rotation, bool dynamic, Quaternion q = null)
		{
			if (StreamedInHandles.Count >= 200)
			{
				MemoryObjects.Add(new MapObject() {Dynamic = dynamic, Hash = model.Hash, Position = position, Rotation = rotation, Type = ObjectTypes.Prop, Quaternion = q});
				return null;
			}
			var prop = World.CreateProp(model, position, rotation, dynamic, false);
			StreamedInHandles.Add(prop.Handle);
			if (!dynamic)
			{
				StaticProps.Add(prop.Handle);
				prop.FreezePosition = true;
			}
			if (q != null)
				Quaternion.SetEntityQuaternion(prop, q);
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
			if (Peds.Contains(handle)) Peds.Remove(handle);
			if (Vehicles.Contains(handle)) Vehicles.Remove(handle);
			if (StreamedInHandles.Contains(handle)) StreamedInHandles.Remove(handle);
			Function.Call(Hash.DELETE_ENTITY, handle);
		}

		public static void AddProp(Prop prop, bool dynamic)
		{
			if (StreamedInHandles.Count >= 200)
			{
				MemoryObjects.Add(new MapObject() {Dynamic = dynamic, Hash = prop.Model.Hash, Position = prop.Position, Quaternion = Quaternion.GetEntityQuaternion(prop), Rotation = prop.Rotation, Type = ObjectTypes.Prop});
				prop.Delete();
				return;
			}
			StreamedInHandles.Add(prop.Handle);
			if(!dynamic)
				StaticProps.Add(prop.Handle);
		}

		public static void RemoveProp(Prop prop, bool dynamic)
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


		public static void Tick()
		{
			List<MapObject> tmpProps = new List<MapObject>();
			StreamedInHandles.ForEach(i => tmpProps.Add(new MapObject() { Dynamic = !StaticProps.Contains(i), Hash = new Prop(i).Model.Hash, Position = new Prop(i).Position, Quaternion = Quaternion.GetEntityQuaternion(new Prop(i)), Rotation = new Prop(i).Rotation, Type = ObjectTypes.Prop }));
			MemoryObjects.ForEach(m => tmpProps.Add(m));

			List<MapObject> streamedProps = tmpProps.OrderBy(prop => (prop.Position - Game.Player.Character.Position).Length()).Take(200).ToList();
			
			for (int i = StreamedInHandles.Count - 1; i >= 0; i--)
			{
				if(streamedProps.Contains(new MapObject() { Dynamic = !StaticProps.Contains(StreamedInHandles[i]), Hash = new Prop(StreamedInHandles[i]).Model.Hash, Position = new Prop(StreamedInHandles[i]).Position, Quaternion = Quaternion.GetEntityQuaternion(new Prop(StreamedInHandles[i])), Rotation = new Prop(StreamedInHandles[i]).Rotation, Type = ObjectTypes.Prop })) continue;
				Prop tmp = new Prop(StreamedInHandles[i]);
				MemoryObjects.Add(new MapObject() { Dynamic = !StaticProps.Contains(StreamedInHandles[i]), Hash = tmp.Model.Hash, Position = tmp.Position, Quaternion = Quaternion.GetEntityQuaternion(tmp), Rotation = tmp.Rotation, Type = ObjectTypes.Prop });
				StreamedInHandles.RemoveAt(i);
				tmp.Delete();
			}

			foreach (MapObject prop in streamedProps.Where(mp => MemoryObjects.Contains(mp))) // Have to spawn it in
			{
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
	}
}