using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;

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

		public static Prop CreateProp(Model model, Vector3 position, Vector3 rotation, bool dynamic)
		{
			if (StreamedInHandles.Count >= 200)
			{
				MemoryObjects.Add(new MapObject() {Dynamic = dynamic, Hash = model.Hash, Position = position, Rotation = rotation});
				return null;
			}
			var prop = World.CreateProp(model, position, rotation, dynamic, false);
			StreamedInHandles.Add(prop.Handle);
			if (!dynamic)
				StaticProps.Add(prop.Handle);
			return prop;
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

		public static IEnumerable<MapObject> GetAllProps()
		{
			List<MapObject> outList = StreamedInHandles.Select(handle => new MapObject() {Dynamic = !StaticProps.Contains(handle), Hash = new Prop(handle).Model.Hash, Position = new Prop(handle).Position, Quaternion = Quaternion.GetEntityQuaternion(new Prop(handle)), Rotation = new Prop(handle).Rotation, Type = ObjectTypes.Prop}).ToList();
			outList.AddRange(MemoryObjects);
			return (IEnumerable<MapObject>)outList;
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
				if(!prop.Dynamic) StaticProps.Add(newProp.Handle);
				MemoryObjects.Remove(prop);
			}
		}
	}
}