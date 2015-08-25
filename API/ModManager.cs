using System;
using System.Collections.Generic;
using GTA;
using NativeUI;

namespace MapEditor.API
{
	public delegate void MapSavedEvent(Map currentMap, string filename);
	public delegate void ModSelectedEvent();

	public static class ModManager
	{
		internal static List<ModListener> Mods = new List<ModListener>();
		internal static ModListener CurrentMod;
		internal static UIMenu ModMenu = new UIMenu("Map Editor", "~b~EXTERNAL MODS");

		internal static void InitMenu()
		{
			ModMenu.DisableInstructionalButtons(true);
			ModMenu.OnItemSelect += (menu, item, index) =>
			{
				var tmpMod = Mods[index];
				UI.Notify("~b~~h~Map Editor~h~~n~~w~Mod ~h~" + tmpMod.Name + "~h~ has been connected.");
				tmpMod.ModSelectInvoker();
				CurrentMod = tmpMod;
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
	}
}