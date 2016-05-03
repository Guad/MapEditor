using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using GTA;
using GTA.Math;
using GTA.Native;

namespace MapEditor
{
	public class MapSerializer
	{
		public enum Format
		{
			NormalXml,
			SimpleTrainer,
			CSharpCode,
            SpoonerLegacy,
            Menyoo,
			Raw,
		}

	    public float Parse(string floatVal)
	    {
	        return float.Parse(floatVal, CultureInfo.InvariantCulture);
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
                case Format.Menyoo:
                    var spReader = new XmlSerializer(typeof(MenyooCompatibility.SpoonerPlacements));
                    var spFile = new StreamReader(path);
			        var spMap = (MenyooCompatibility.SpoonerPlacements) spReader.Deserialize(spFile);
                    spFile.Close();

                    var outputMap = new Map();

			        foreach (var placement in spMap.Placement)
			        {
                        var obj = new MapObject();
                        switch (placement.Type)
			            {
                            case 3: // Props
			                    {
                                    obj.Type = ObjectTypes.Prop;
                                }
			                    break;
                            case 1: // Peds
                                {
                                    
                                    obj.Type = ObjectTypes.Ped;
                                }
                                break;
                            case 2: // Vehicles
                                {
                                    obj.Type = ObjectTypes.Vehicle;
                                }
                                break;
                        }
                        obj.Dynamic = placement.Dynamic;
                        obj.Hash = Convert.ToInt32(placement.ModelHash, 16);
                        obj.Position = new Vector3(placement.PositionRotation.X, placement.PositionRotation.Y, placement.PositionRotation.Z);
                        obj.Rotation = new Vector3(placement.PositionRotation.Pitch, placement.PositionRotation.Roll, placement.PositionRotation.Yaw);
                        outputMap.Objects.Add(obj);
                    }
			        return outputMap;
				case Format.SimpleTrainer:
			    {
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
			                    X = Parse(tmpData["qx"]),
			                    Y = Parse(tmpData["qy"]),
			                    Z = Parse(tmpData["qz"]),
			                    W = Parse(tmpData["qw"]),
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
			            X = Parse(tmpData["qx"]),
			            Y = Parse(tmpData["qy"]),
			            Z = Parse(tmpData["qz"]),
			            W = Parse(tmpData["qw"]),
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
			    }
                case Format.SpoonerLegacy:
			    {
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
                                if (!tmpData.ContainsKey("Type")) continue;
                                Vector3 pos = new Vector3(float.Parse(tmpData["X"], CultureInfo.InvariantCulture),
                                    float.Parse(tmpData["Y"], CultureInfo.InvariantCulture),
                                    float.Parse(tmpData["Z"], CultureInfo.InvariantCulture));
                                Vector3 rot = new Vector3(float.Parse(tmpData["Pitch"]), float.Parse(tmpData["Roll"]), float.Parse(tmpData["Yaw"]));
                                
                                int mod = Convert.ToInt32("0x" + tmpData["Hash"], 16);
                                tmpMap.Objects.Add(new MapObject()
                                {
                                    Hash = mod,
                                    Position = pos,
                                    Rotation = rot,
                                    Type = tmpData["Type"] == "1" ? ObjectTypes.Ped : tmpData["Type"] == "2" ? ObjectTypes.Vehicle : ObjectTypes.Prop,
                                });
                                tmpData = new Dictionary<string, string>();
                                continue;
                            }
                            string[] spl = line.Split('=');
                            if (spl.Length >= 2)
                                tmpData.Add(spl[0].Trim(), spl[1].Trim());
                        }
                        return tmpMap;
                    }
			        break;
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
                case Format.SpoonerLegacy:
			        string main = "";
                    int c = 1;
                    for (int i = 0; i < map.Objects.Count; i++)
                    {
                        if (map.Objects[i].Position == new Vector3(0, 0, 0)) continue;
                        main += "[" + c + "]\r\n";
                        main += "Type = " + (map.Objects[i].Type == ObjectTypes.Ped ? 1 : map.Objects[i].Type == ObjectTypes.Vehicle ? 2 : 3) + "\r\n";
                        main += "Hash = " + map.Objects[i].Hash.ToString("x8") + "\r\n";
                        main += "X = " + map.Objects[i].Position.X.ToString(CultureInfo.InvariantCulture) + "\r\n";
                        main += "Y = " + map.Objects[i].Position.Y.ToString(CultureInfo.InvariantCulture) + "\r\n";
                        main += "Z = " + map.Objects[i].Position.Z.ToString(CultureInfo.InvariantCulture) + "\r\n";
                        main += "Pitch = " + map.Objects[i].Rotation.X.ToString(CultureInfo.InvariantCulture) + "\r\n";
                        main += "Roll = " + map.Objects[i].Rotation.Y.ToString(CultureInfo.InvariantCulture) + "\r\n";
                        main += "Yaw = " + map.Objects[i].Rotation.Z.ToString(CultureInfo.InvariantCulture) + "\r\n";
                        main += "Opacity = 0x000000ff\r\n";
                        c++;
                    }
                    File.WriteAllText(path, main);
                    break;
				case Format.CSharpCode:
			        string vehicles = "";
			        string peds = "";
			        string props = "";
			        string markers = "";
			        string pickups = "";
			        string removedFromWorld = "            Prop returnedProp;\r\n";

					foreach (var prop in map.Objects)
					{
						if (prop.Position == new Vector3(0, 0, 0)) continue;
                        
						if (prop.Type == ObjectTypes.Vehicle)
						{
							vehicles += String.Format("               GTA.World.CreateVehicle(new Model({0}), new GTA.Math.Vector3({1}f, {2}f, {3}f), {4}f);\r\n",
								prop.Hash,
								prop.Position.X.ToString(CultureInfo.InvariantCulture),
								prop.Position.Y.ToString(CultureInfo.InvariantCulture),
								prop.Position.Z.ToString(CultureInfo.InvariantCulture),
								prop.Rotation.Z.ToString(CultureInfo.InvariantCulture)
                            );
						}
						else if (prop.Type == ObjectTypes.Ped)
						{
							peds += String.Format("                GTA.World.CreatePed(new Model({0}), new GTA.Math.Vector3({1}f, {2}f, {3}f), {4}f);\r\n",
								prop.Hash,
								prop.Position.X.ToString(CultureInfo.InvariantCulture),
								prop.Position.Y.ToString(CultureInfo.InvariantCulture),
								prop.Position.Z.ToString(CultureInfo.InvariantCulture),
								prop.Rotation.Z.ToString(CultureInfo.InvariantCulture)
                            );
						}
						else if (prop.Type == ObjectTypes.Prop)
						{
							props += String.Format("                Props.Add(createProp({0}, new GTA.Math.Vector3({1}f, {2}f, {3}f), new GTA.Math.Vector3({4}f, {5}f, {6}f), {7}));\r\n",
								prop.Hash,
								prop.Position.X.ToString(CultureInfo.InvariantCulture),
								prop.Position.Y.ToString(CultureInfo.InvariantCulture),
								prop.Position.Z.ToString(CultureInfo.InvariantCulture),
								prop.Rotation.X.ToString(CultureInfo.InvariantCulture),
								prop.Rotation.Y.ToString(CultureInfo.InvariantCulture),
								prop.Rotation.Z.ToString(CultureInfo.InvariantCulture),
                                prop.Dynamic.ToString().ToLower()
							);
						}
                        else if (prop.Type == ObjectTypes.Pickup)
                        {
                            pickups += string.Format("              Function.Call<int>(Hash.CREATE_PICKUP_ROTATE, {0}, {1}f, {2}f, {3}f, 0, 0, 0, 515, {4}, 0, false, 0);\r\n",
                                prop.Hash,
                                prop.Position.X.ToString(CultureInfo.InvariantCulture),
                                prop.Position.Y.ToString(CultureInfo.InvariantCulture),
                                prop.Position.Z.ToString(CultureInfo.InvariantCulture),
                                prop.Amount);
                        }
					}

			        foreach (var marker in map.Markers)
			        {
			            markers += string.Format("            Function.Call(Hash.DRAW_MARKER, {0}," +
			                                     "{1}f, {2}f, {3}f, " +
			                                     "0f, 0f, 0f, {4}f, {5}f, {6}f, " +
			                                     "{7}f, {8}f, {9}f, " +
			                                     "{10}, {11}, {12}, {13}, " +
			                                     "{14}, {15}, 2, false, false, false);\r\n",

			                (int)marker.Type, marker.Position.X.ToString(CultureInfo.InvariantCulture), marker.Position.Y.ToString(CultureInfo.InvariantCulture), marker.Position.Z.ToString(CultureInfo.InvariantCulture), marker.Rotation.X.ToString(CultureInfo.InvariantCulture),
			                marker.Rotation.Y.ToString(CultureInfo.InvariantCulture), marker.Rotation.Z.ToString(CultureInfo.InvariantCulture), marker.Scale.X.ToString(CultureInfo.InvariantCulture), marker.Scale.Y.ToString(CultureInfo.InvariantCulture), marker.Scale.Z.ToString(CultureInfo.InvariantCulture), marker.Red,
			                marker.Green, marker.Blue, marker.Alpha, marker.BobUpAndDown.ToString().ToLower(),
			                marker.RotateToCamera.ToString().ToLower());
			        }

			        foreach (var o in map.RemoveFromWorld)
			        {
			            removedFromWorld += string.Format(
                            @"            returnedProp = Function.Call<Prop>(Hash.GET_CLOSEST_OBJECT_OF_TYPE, {0}f, {1}f, {2}f, 1f, {3}, 0);
            if (returnedProp != null && returnedProp.Handle != 0 && !Props.Contains(returnedProp.Handle))
                returnedProp.Delete();" + "\r\n",
                            o.Position.X.ToString(CultureInfo.InvariantCulture), o.Position.Y.ToString(CultureInfo.InvariantCulture), o.Position.Z.ToString(CultureInfo.InvariantCulture), o.Hash);
			        }

                    

			        string finalOutput = string.Format(@"using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;


public class MapEditorGeneratedMap : GTA.Script
{{
    public MapEditorGeneratedMap()
    {{
        List<int> Props = new List<int>();
        int LodDistance = 3000;            

        Func<int, Vector3, Vector3, bool, int> createProp = new Func<int, Vector3, Vector3, bool, int>(delegate(int hash, Vector3 pos, Vector3 rot, bool dynamic)
	    {{
		    Model model = new Model(hash);
		    model.Request(10000);
		    Prop prop = GTA.World.CreateProp(model, pos, rot, dynamic, false);
		    prop.Position = pos;
            prop.LodDistance = LodDistance;
            if (!dynamic)
                prop.FreezePosition = true;
		    return prop.Handle;
	    }});

        bool Initialized = false;


        base.Tick += delegate (object sender, EventArgs args)
        {{
            if (!Initialized)
            {{
                /* PROPS */
{0}

                /* VEHICLES */
{1}

                /* PEDS */
{2}

                /* PICKUPS */
{4}
                Initialized = true;
            }}

            /* MARKERS */
{3}

            /* WORLD */
{5}
        }};
    }}
}}", props, vehicles, peds, markers, pickups, removedFromWorld);

                    using (var stream = new StreamWriter(new FileStream(path, File.Exists(path) ? FileMode.Truncate : FileMode.CreateNew)))
                    {
                        stream.Write(finalOutput);
                    }
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
                case Format.Menyoo:
                    XmlSerializer menSer = new XmlSerializer(typeof(MenyooCompatibility.SpoonerPlacements));
                    map.Objects.RemoveAll(mo => mo.Position == new Vector3(0, 0, 0));
                    var menObj = new MenyooCompatibility.SpoonerPlacements();

			        foreach (var o in map.Objects)
			        {
                        var pl = new MenyooCompatibility.Placement();
                        pl.Type = o.Type == ObjectTypes.Ped ? 1 : o.Type == ObjectTypes.Vehicle ? 2 : 3;
                        pl.Dynamic = o.Dynamic;
			            pl.ModelHash = "0x" + o.Hash.ToString("x8");
                        pl.PositionRotation = new MenyooCompatibility.PositionRotation()
                        {
                            X = o.Position.X,
                            Y = o.Position.Y,
                            Z = o.Position.Z,
                            Pitch = o.Rotation.X,
                            Roll = o.Rotation.Y,
                            Yaw = o.Rotation.Z,
                        };
                        menObj.Placement.Add(pl);
			        }

                    var menF = new StreamWriter(path);
                    menSer.Serialize(menF, menObj);
                    menF.Close();
                    break;
            }
		}
	}
}