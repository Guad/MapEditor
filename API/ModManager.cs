using System;
using System.Collections.Generic;
using GTA;
using NativeUI;

namespace MapEditor.API
{
	public delegate void MapSavedEvent(Map currentMap);

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
				UI.Notify("~b~~h~Map Editor~h~~n~Mod ~h~" + tmpMod.Name + "~h~ has been connected.");
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

		public string Name;
		public string Description;
		public string ButtonString;

		internal void MapSavedInvoker(Map map)
		{
			OnMapSaved?.Invoke(map);
		}
	}
}