using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using GTA;
using GTA.Native;
using NativeUI;

namespace MapEditor
{
    public static class ObjectDatabase
    {
        public static Dictionary<string, int> MainDb;
		
	    public static List<int> InvalidHashes = new List<int>();

	    public static Dictionary<string, int> VehicleDb;

		public static Dictionary<string, int> PedDb;

	    public static void LoadEnumDatabases()
	    {
			VehicleDb = new Dictionary<string, int>();
			PedDb = new Dictionary<string, int>();

		    // Vehicles.
		    foreach (string veh in Enum.GetNames(typeof(VehicleHash)))
		    {
			    VehicleHash hash;
			    Enum.TryParse(veh, out hash);
				if(VehicleDb.ContainsKey(veh)) continue;
				VehicleDb.Add(veh, (int)hash);
		    }

			// Peds
			foreach (string ped in Enum.GetNames(typeof(PedHash)))
			{
				PedHash hash;
				Enum.TryParse(ped, out hash);
				if (PedDb.ContainsKey(ped)) continue;
				PedDb.Add(ped, (int)hash);
			}
		}

		public static void LoadFromFile(string path)
        {
            MainDb = new Dictionary<string, int>();
            string[] lines = File.ReadAllLines(path);
            foreach (string line in lines)
            {
                string[] s = line.Split('=');
                if(MainDb.ContainsKey(s[0])) continue;
                //int val = unchecked((int)Convert.ToUInt32(s[1], 10));
                int val = Convert.ToInt32(s[1]);
                MainDb.Add(s[0], val);
            }
        }

	    public static void LoadInvalidHashes()
	    {
			if(!File.Exists("scripts\\InvalidObjects.ini")) return;
		    string[] lines = File.ReadAllLines("scripts\\InvalidObjects.ini");
		    foreach (string line in lines)
		    {
			    int val = Convert.ToInt32(line);
				InvalidHashes.Add(val);
		    }
	    }

	    public static void SaveInvalidHashes()
	    {
		    string output = InvalidHashes.Aggregate("", (current, hash) => current + (hash + "\r\n"));
		    File.WriteAllText("scripts\\InvalidObjects.ini", output);
	    }
    }
}