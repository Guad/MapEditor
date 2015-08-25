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

		internal static void LoadEnumDatabases()
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

		internal static void LoadFromFile(string path)
        {
            MainDb = new Dictionary<string, int>();
            string[] lines = File.ReadAllLines(path);
            foreach (string line in lines)
            {
                string[] s = line.Split('=');
                if(MainDb.ContainsKey(s[0])) continue;
                //int val = unchecked((int)Convert.ToUInt32(s[1], 16));
                int val = Convert.ToInt32(s[1]);
                MainDb.Add(s[0], val);
            }
        }

		internal static void LoadInvalidHashes()
	    {
			if(!File.Exists("scripts\\InvalidObjects.ini")) return;
		    string[] lines = File.ReadAllLines("scripts\\InvalidObjects.ini");
		    foreach (string line in lines)
		    {
			    int val = Convert.ToInt32(line);
				InvalidHashes.Add(val);
		    }
	    }

		internal static void SaveInvalidHashes()
	    {
		    string output = InvalidHashes.Aggregate("", (current, hash) => current + (hash + "\r\n"));
		    File.WriteAllText("scripts\\InvalidObjects.ini", output);
	    }

		internal static Dictionary<string, string> ScrenarioDatabase = new Dictionary<string, string>
		{
			{"Drink Coffee",  "WORLD_HUMAN_AA_COFFEE"},
			{"Smoke", "WORLD_HUMAN_AA_SMOKE" },
			{"Smoke 2", "WORLD_HUMAN_SMOKING" },
			{"Binoculars",  "WORLD_HUMAN_BINOCULARS"},
			{"Bum", "WORLD_HUMAN_BUM_FREEWAY" },
			{"Cheering", "WORLD_HUMAN_CHEERING" },
			{"Clipboard", "WORLD_HUMAN_CLIPBOARD" },
			{"Drilling",  "WORLD_HUMAN_CONST_DRILL"},
			{"Drinking", "WORLD_HUMAN_DRINKING" },
			{"Drug Dealer", "WORLD_HUMAN_DRUG_DEALER"},
			{"Drug Dealer Hard", "WORLD_HUMAN_DRUG_DEALER_HARD" },
			{"Traffic Signaling",  "WORLD_HUMAN_CAR_PARK_ATTENDANT"},
			{"Filming", "WORLD_HUMAN_MOBILE_FILM_SHOCKING" },
			{"Leaf Blower", "WORLD_HUMAN_GARDENER_LEAF_BLOWER" },
			{"Golf Player", "WORLD_HUMAN_GOLF_PLAYER" },
			{"Guard Patrol", "WORLD_HUMAN_GUARD_PATROL" },
			{"Hammering", "WORLD_HUMAN_HAMMERING" },
			{"Janitor", "WORLD_HUMAN_JANITOR" },
			{"Musician", "WORLD_HUMAN_MUSICIAN" },
			{"Paparazzi", "WORLD_HUMAN_PAPARAZZI" },
			{"Party", "WORLD_HUMAN_PARTYING" },
			{"Picnic", "WORLD_HUMAN_PICNIC" },
			{"Push Ups", "WORLD_HUMAN_PUSH_UPS"},
			{"Shine Torch", "WORLD_HUMAN_SECURITY_SHINE_TORCH" },
			{"Sunbathe", "WORLD_HUMAN_SUNBATHE" },
			{"Sunbathe Back", "WORLD_HUMAN_SUNBATHE_BACK"},
			{"Tourist", "WORLD_HUMAN_TOURIST_MAP" },
			{"Mechanic", "WORLD_HUMAN_VEHICLE_MECHANIC" },
			{"Welding", "WORLD_HUMAN_WELDING" },
			{"Yoga", "WORLD_HUMAN_YOGA" },
		};
    }
}