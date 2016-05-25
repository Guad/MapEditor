using System;
using System.Collections.Generic;
using GTA;
using NativeUI;

namespace MapEditor.API
{
	public delegate void MapSavedEvent(Map currentMap, string filename);
	public delegate void ModSelectedEvent();

	public delegate void ModDisconnectedEvent();

	public static class ModManager
	{
		internal static List<ModListener> Mods = new List<ModListener>();
		internal static ModListener CurrentMod;
	    internal static UIMenu ModMenu;

		internal static void InitMenu()
		{
		    ModMenu = new UIMenu("Map Editor", "~b~" + Translation.Translate("EXTERNAL MODS"));

            ModMenu.DisableInstructionalButtons(true);
			ModMenu.OnItemSelect += (menu, item, index) =>
			{
				var tmpMod = Mods[index];
				if (CurrentMod == tmpMod)
				{
					UI.Notify("~b~~h~Map Editor~h~~n~~w~Mod ~h~" + tmpMod.Name + "~h~ " + Translation.Translate("has been disconnected."));
					CurrentMod.ModDisconnectInvoker();
					CurrentMod = null;
				}
				else
				{
					UI.Notify("~b~~h~Map Editor~h~~n~~w~Mod ~h~" + tmpMod.Name + "~h~ " + Translation.Translate("has been connected."));
					tmpMod.ModSelectInvoker();
					CurrentMod = tmpMod;
				}
			};
		}

		public static void SuscribeMod(ModListener mod)
		{
			Mods.Add(mod);
			ModMenu.AddItem(new UIMenuItem(mod.ButtonString, mod.Description));
		}
	}

	public class ModListener
	{
		public event MapSavedEvent OnMapSaved;
		public event ModSelectedEvent OnModSelect;
		public event ModDisconnectedEvent OnModDisconnect;

		public string Name;
		public string Description;
		public string ButtonString;

		internal void MapSavedInvoker(Map map, string filename)
		{
			OnMapSaved?.Invoke(map, filename);
		}

		internal void ModSelectInvoker()
		{
			OnModSelect?.Invoke();
		}

		internal void ModDisconnectInvoker()
		{
			OnModDisconnect?.Invoke();
		}
	}
}