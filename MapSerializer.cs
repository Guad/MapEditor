using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using GTA;
using GTA.Math;

namespace MapEditor
{
	public class MapSerializer
	{
		public enum Format
		{
			NormalXml,
			SimpleTrainer,
			CSharpCode,
			Raw,
		}

		internal Map Deserialize(string path, Format format)
		{
			string tip = "";
			switch (format)
			{
				case Format.NormalXml:
					XmlSerializer reader = new XmlSerializer(typeof (Map));
					var file = new StreamReader(path);
					var map = (Map) reader.Deserialize(file);
					file.Close();
					return map;
				case Format.SimpleTrainer:
					var tmpMap = new Map();
					string currentSection = "";
					string oldSection = "";
					Dictionary<string, string> tmpData = new Dictionary<string, string>();
					foreach (string line in File.ReadAllLines(path))
					{
						if (line.StartsWith("[") && line.EndsWith("]"))
						{
							oldSection = currentSection;
							currentSection = line;
							tip = currentSection;
							if (oldSection == "" || oldSection == "[Player]") continue;
							Vector3 pos = new Vector3(float.Parse(tmpData["x"], CultureInfo.InvariantCulture),
								float.Parse(tmpData["y"], CultureInfo.InvariantCulture),
								float.Parse(tmpData["z"], CultureInfo.InvariantCulture));
							Vector3 rot = new Vector3(float.Parse(tmpData["qz"]), float.Parse(tmpData["qw"]), float.Parse(tmpData["h"]));
							Quaternion q = new Quaternion()
							{
								X = float.Parse(tmpData["qx"]),
								Y = float.Parse(tmpData["qy"]),
								Z = float.Parse(tmpData["qz"]),
								W = float.Parse(tmpData["qw"]),
							};
							int mod = Convert.ToInt32(tmpData["Model"], CultureInfo.InvariantCulture);
							int dyn = Convert.ToInt32(tmpData["Dynamic"], CultureInfo.InvariantCulture);
							tmpMap.Objects.Add(new MapObject()
							{
								Hash = mod,
								Position = pos,
								Rotation = rot,
								Dynamic = dyn == 1,
								Quaternion = q
							});
							tmpData = new Dictionary<string, string>();
							continue;
						}
						if (currentSection == "[Player]") continue;
						string[] spl = line.Split('=');
						tmpData.Add(spl[0], spl[1]);
					}
					Vector3 lastPos = new Vector3(float.Parse(tmpData["x"], CultureInfo.InvariantCulture),
						float.Parse(tmpData["y"], CultureInfo.InvariantCulture), float.Parse(tmpData["z"], CultureInfo.InvariantCulture));
					Vector3 lastRot = new Vector3(float.Parse(tmpData["qz"]), float.Parse(tmpData["qw"]), float.Parse(tmpData["h"]));
					Quaternion lastQ = new Quaternion()
					{
						X = float.Parse(tmpData["qx"]),
						Y = float.Parse(tmpData["qy"]),
						Z = float.Parse(tmpData["qz"]),
						W = float.Parse(tmpData["qw"]),
					};
					int lastMod = Convert.ToInt32(tmpData["Model"], CultureInfo.InvariantCulture);
					int lastDyn = Convert.ToInt32(tmpData["Dynamic"], CultureInfo.InvariantCulture);
					tmpMap.Objects.Add(new MapObject()
					{
						Hash = lastMod,
						Position = lastPos,
						Rotation = lastRot,
						Dynamic = lastDyn == 1,
						Quaternion = lastQ
					});
					return tmpMap;
				default:
					throw new NotImplementedException("This is not implemented yet.");
			}
		}

		internal void Serialize(string path, Map map, Format format)
		{
			if(path == null) return;
			switch (format)
			{
				case Format.NormalXml:
					XmlSerializer writer = new XmlSerializer(typeof(Map));
					map.Objects.RemoveAll(mo => mo.Position == new Vector3(0, 0, 0));
					var file = new StreamWriter(path);
					writer.Serialize(file, map);
					file.Close();
					break;
				case Format.SimpleTrainer:
					string mainOutput = "[Player]\r\n" +
					                    "Teleport=0\r\n" +
					                    "x=0\r\n" +
					                    "y=0\r\n" +
					                    "z=0\r\n";
					int count = 1;
					for (int i = 0; i < map.Objects.Count; i++)
					{
						if(map.Objects[i].Position == new Vector3(0, 0, 0)) continue;
						mainOutput += "[" + count + "]\r\n";
						mainOutput += "x=" + map.Objects[i].Position.X.ToString(CultureInfo.InvariantCulture) + "\r\n";
						mainOutput += "y=" + map.Objects[i].Position.Y.ToString(CultureInfo.InvariantCulture) + "\r\n";
						mainOutput += "z=" + map.Objects[i].Position.Z.ToString(CultureInfo.InvariantCulture) + "\r\n";
						mainOutput += "h=" + map.Objects[i].Rotation.Z.ToString(CultureInfo.InvariantCulture) + "\r\n";
						mainOutput += "Model=" + map.Objects[i].Hash + "\r\n";
						mainOutput += "qx=" + map.Objects[i].Quaternion.X.ToString(CultureInfo.InvariantCulture) + "\r\n";
						mainOutput += "qy=" + map.Objects[i].Quaternion.Y.ToString(CultureInfo.InvariantCulture) + "\r\n";
						mainOutput += "qz=" + map.Objects[i].Quaternion.Z.ToString(CultureInfo.InvariantCulture) + "\r\n";
						mainOutput += "qw=" + map.Objects[i].Quaternion.W.ToString(CultureInfo.InvariantCulture) + "\r\n";
						mainOutput += "offz=0\r\n";
						mainOutput += "Dynamic=" + (map.Objects[i].Dynamic ? "1\n" : "0\n");
						count++;
					}
					File.WriteAllText(path, mainOutput);
					break;
				case Format.CSharpCode:
					string output = "";
					foreach (var prop in map.Objects)
					{
						if (prop.Position == new Vector3(0, 0, 0)) continue;
						string cmd = "";

						if (prop.Type == ObjectTypes.Vehicle)
						{
							cmd = String.Format("GTA.World.CreateVehicle(new Model({0}) new GTA.Math.Vector3({1}f, {2}f, {3}f), {4}f);",
								prop.Hash,
								prop.Position.X,
								prop.Position.Y,
								prop.Position.Z,
								prop.Rotation.Z
							);
						}
						if (prop.Type == ObjectTypes.Ped)
						{
							cmd = String.Format("GTA.World.CreatePed(new Model({0}), new GTA.Math.Vector3({1}f, {2}f, {3}f), {4}f);",
								prop.Hash,
								prop.Position.X,
								prop.Position.Y,
								prop.Position.Z,
								prop.Rotation.Z
							);
						}
						if (prop.Type == ObjectTypes.Prop)
						{
							cmd = String.Format("GTA.World.CreateProp(new Model({0}), new GTA.Math.Vector3({1}f, {2}f, {3}f), new GTA.Math.Vector3({4}f, {5}f, {6}f), false, false);",
								prop.Hash,
								prop.Position.X,
								prop.Position.Y,
								prop.Position.Z,
								prop.Rotation.X,
								prop.Rotation.Y,
								prop.Rotation.Z
							);
						}
						output += cmd + "\r\n";
					}
					File.WriteAllText(path, output);
					break;
				case Format.Raw:
					string flush = "";
					foreach (var prop in map.Objects)
					{
						if (prop.Position == new Vector3(0, 0, 0)) continue;
						string name = "";
						ObjectTypes thisType = ObjectTypes.Prop;

						if (prop.Type == ObjectTypes.Vehicle)
						{
							name = ObjectDatabase.VehicleDb.First(pair => pair.Value == prop.Hash).Key;
							thisType = ObjectTypes.Vehicle;
						}
						if (prop.Type == ObjectTypes.Ped)
						{
							name = ObjectDatabase.PedDb.First(pair => pair.Value == prop.Hash).Key;
							thisType = ObjectTypes.Ped;
						}
						if (prop.Type == ObjectTypes.Prop)
						{
							name = ObjectDatabase.MainDb.First(pair => pair.Value == prop.Hash).Key;
							thisType = ObjectTypes.Prop;
						}
						flush += String.Format("{8} name = {0}, hash = {7}, x = {1}, y = {2}, z = {3}, rotationx = {4}, rotationy = {5}, rotationz = {6}\r\n",
							name,
							prop.Position.X,
							prop.Position.Y,
							prop.Position.Z,
							prop.Rotation.X,
							prop.Rotation.Y,
							prop.Rotation.Z,
							prop.Hash,
							thisType
						);
					}
					File.WriteAllText(path, flush);
					break;
			}
		}
	}
}