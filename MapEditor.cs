using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using MapEditor.API;
using Control = GTA.Control;
using Font = GTA.Font;

namespace MapEditor
{
    public class MapEditor : Script
    {
        private bool _isInFreecam;
        private bool _isChoosingObject;
        private bool _searchResultsOn;

        private readonly UIMenu _objectsMenu;
        private readonly UIMenu _searchMenu;
        private readonly UIMenu _mainMenu;
	    private readonly UIMenu _formatMenu;
	    private readonly UIMenu _objectInfoMenu;
	    private readonly UIMenu _settingsMenu;
	    private readonly UIMenu _currentObjectsMenu;

	    private UIMenuItem _currentEntitiesItem;

        private readonly MenuPool _menuPool = new MenuPool();

        private Entity _previewProp;
        private Entity _snappedProp;
        private Entity _selectedProp;
        private Entity _snappedRemovable;

        private Marker _snappedMarker;
	    private Marker _selectedMarker;
        
        private Camera _mainCamera;
        private Camera _objectPreviewCamera;

        private readonly Vector3 _objectPreviewPos = new Vector3(1200.133f, 4000.958f, 85.9f);

        private bool _zAxis = true;
	    private bool _controlsRotate;
        private bool _quitWithSearchVisible;

		
	    private readonly string _crosshairPath;
	    private readonly string _crosshairBluePath;
	    private readonly string _crosshairYellowPath;

	    private bool _savingMap;
	    private bool _hasLoaded;
	    private int _mapObjCounter = 0;
	    private int _markerCounter = 0;

	    private const Relationship DefaultRelationship = Relationship.Companion;
	    

	    private ObjectTypes _currentObjectType;
		
	    private Settings _settings;

	    private readonly string[] _markersTypes = Enum.GetNames(typeof(MarkerType)).ToArray();

	    public enum CrosshairType
	    {
		    Crosshair,
			Orb,
			None,
	    }

		private readonly List<dynamic> _possiblePositions = new List<dynamic>();
		private readonly List<dynamic> _possibleRoll = new List<dynamic>();
        

        // Autosaving
        private int _loadedEntities = 0;
        private int _changesMade = 0;
        private DateTime _lastAutosave = DateTime.Now;

		public MapEditor()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;

            if (!Directory.Exists("scripts\\MapEditor"))
                Directory.CreateDirectory("scripts\\MapEditor");

            ObjectDatabase.SetupRelationships();
			LoadSettings();

            try
		    {
		        Translation.Load("scripts\\MapEditor", _settings.Translation);
		    }
		    catch (Exception e) 
		    {
		        UI.Notify("~b~~h~Map Editor~h~~w~~n~Failed to load translations. Falling back to English.");
		        UI.Notify(e.Message);
		    }

		    _scaleform = new Scaleform(0);
			_scaleform.Load("instructional_buttons");

			_objectInfoMenu = new UIMenu("", "~b~" + Translation.Translate("PROPERTIES"), new Point(0, -107));
			_objectInfoMenu.ResetKey(UIMenu.MenuControls.Back);
			_objectInfoMenu.DisableInstructionalButtons(true);
			_objectInfoMenu.SetBannerType(new UIResRectangle(new Point(), new Size()));
			_menuPool.Add(_objectInfoMenu);


			ModManager.InitMenu();

			_objectsMenu = new UIMenu("Map Editor", "~b~" + Translation.Translate("PLACE OBJECT"));
		    
            ObjectDatabase.LoadFromFile("scripts\\ObjectList.ini", ref ObjectDatabase.MainDb);
			ObjectDatabase.LoadInvalidHashes();
			ObjectDatabase.LoadFromFile("scripts\\PedList.ini", ref ObjectDatabase.PedDb);
            ObjectDatabase.LoadFromFile("scripts\\VehicleList.ini", ref ObjectDatabase.VehicleDb);


		    _crosshairPath = Path.GetFullPath("scripts\\MapEditor\\crosshair.png");
            _crosshairBluePath = Path.GetFullPath("scripts\\MapEditor\\crosshair_blue.png");
            _crosshairYellowPath = Path.GetFullPath("scripts\\MapEditor\\crosshair_yellow.png");

            if (!File.Exists("scripts\\MapEditor\\crosshair.png"))
			    _crosshairPath = Sprite.WriteFileFromResources(Assembly.GetExecutingAssembly(), "MapEditor.crosshair.png", "scripts\\MapEditor\\crosshair.png");
            if (!File.Exists("scripts\\MapEditor\\crosshair_blue.png"))
                _crosshairBluePath = Sprite.WriteFileFromResources(Assembly.GetExecutingAssembly(), "MapEditor.crosshair_blue.png", "scripts\\MapEditor\\crosshair_blue.png");
            if (!File.Exists("scripts\\MapEditor\\crosshair_yellow.png"))
                _crosshairYellowPath = Sprite.WriteFileFromResources(Assembly.GetExecutingAssembly(), "MapEditor.crosshair_yellow.png", "scripts\\MapEditor\\crosshair_yellow.png");


			RedrawObjectsMenu();
            _objectsMenu.OnItemSelect += OnObjectSelect;
            _objectsMenu.OnIndexChange += OnIndexChange;
            _menuPool.Add(_objectsMenu);

			_objectsMenu.ResetKey(UIMenu.MenuControls.Back);
            _objectsMenu.AddInstructionalButton(new InstructionalButton(Control.SelectWeapon, Translation.Translate("Change Axis")));
            _objectsMenu.AddInstructionalButton(new InstructionalButton(Control.MoveUpDown, Translation.Translate("Zoom")));
            _objectsMenu.AddInstructionalButton(new InstructionalButton(Control.Jump, Translation.Translate("Search")));

            _searchMenu = new UIMenu("Map Editor", "~b~" + Translation.Translate("PLACE OBJECT"));
            _searchMenu.OnItemSelect += OnObjectSelect;
            _searchMenu.OnIndexChange += OnIndexChange;
            _menuPool.Add(_searchMenu);

            _searchMenu.ResetKey(UIMenu.MenuControls.Back);
            _searchMenu.AddInstructionalButton(new InstructionalButton(Control.SelectWeapon, Translation.Translate("Change Axis")));
            _searchMenu.AddInstructionalButton(new InstructionalButton(Control.MoveUpDown, Translation.Translate("Zoom")));
            _searchMenu.AddInstructionalButton(new InstructionalButton(Control.Jump, Translation.Translate("Search")));



            _mainMenu = new UIMenu("Map Editor", "~b~" + Translation.Translate("MAIN MENU"));
            _mainMenu.AddItem(new UIMenuItem(Translation.Translate("Enter/Exit Map Editor")));
            _mainMenu.AddItem(new UIMenuItem(Translation.Translate("New Map"), Translation.Translate("Remove all current objects and start a new map.")));
            _mainMenu.AddItem(new UIMenuItem(Translation.Translate("Save Map"), Translation.Translate("Save all current objects to a file.")));
			_mainMenu.AddItem(new UIMenuItem(Translation.Translate("Load Map"), Translation.Translate("Load objects from a file and add them to the world.")));
			_mainMenu.RefreshIndex();
			_mainMenu.DisableInstructionalButtons(true);
            _menuPool.Add(_mainMenu);

			_formatMenu = new UIMenu("Map Editor", "~b~" + Translation.Translate("SELECT FORMAT"));
			_formatMenu.DisableInstructionalButtons(true);
			_formatMenu.ParentMenu = _mainMenu;
	        RedrawFormatMenu();
			_menuPool.Add(_formatMenu);

            _mainMenu.OnItemSelect += (m, it, i) =>
            {
                switch (i)
                {
                    case 0:
                        _isInFreecam = !_isInFreecam;
                        Game.Player.Character.FreezePosition = _isInFreecam;
		                Game.Player.Character.IsVisible = !_isInFreecam;
                        World.RenderingCamera = null;
		                if (!_isInFreecam)
		                {
							Game.Player.Character.Position -= new Vector3(0f, 0f, Game.Player.Character.HeightAboveGround - 1f);
			                return;
		                }
                        World.DestroyAllCameras();
                        _mainCamera = World.CreateCamera(GameplayCamera.Position, GameplayCamera.Rotation, 60f);
						_objectPreviewCamera = World.CreateCamera(new Vector3(1200.016f, 3980.998f, 86.05062f), new Vector3(0f, 0f, 0f), 60f);
						World.RenderingCamera = _mainCamera;
                        break;
                    case 1:
                        JavascriptHook.StopAllScripts();
						PropStreamer.RemoveAll();
						PropStreamer.Markers.Clear();
						_currentObjectsMenu.Clear();
                        PropStreamer.Identifications.Clear();
						PropStreamer.ActiveScenarios.Clear();
						PropStreamer.ActiveRelationships.Clear();
						PropStreamer.ActiveWeapons.Clear();
                        PropStreamer.Doors.Clear();
						ModManager.CurrentMod?.ModDisconnectInvoker();
						ModManager.CurrentMod = null;
		                foreach (MapObject o in PropStreamer.RemovedObjects)
		                {
			                var t = World.CreateProp(o.Hash, o.Position, o.Rotation, true, false);
			                t.PositionNoOffset = o.Position;
		                }
						PropStreamer.RemovedObjects.Clear();
                        _loadedEntities = 0;
                        _changesMade = 0;
                        _lastAutosave = DateTime.Now;
						UI.Notify("~b~~h~Map Editor~h~~w~~n~" + Translation.Translate("Loaded new map."));
						break;
					case 2:
		                if (ModManager.CurrentMod != null)
		                {
			                string filename = Game.GetUserInput(255);
			                if (String.IsNullOrWhiteSpace(filename))
			                {
				                UI.Notify("~r~~h~Map Editor~h~~n~~w~" + Translation.Translate("The filename was empty."));
								return;
			                }
							Map tmpMap = new Map();
							tmpMap.Objects.AddRange(PropStreamer.GetAllEntities());
							tmpMap.RemoveFromWorld.AddRange(PropStreamer.RemovedObjects);
							tmpMap.Markers.AddRange(PropStreamer.Markers);
			                UI.Notify("~b~~h~Map Editor~h~~n~~w~" + Translation.Translate("Map sent to external mod for saving."));
							ModManager.CurrentMod.MapSavedInvoker(tmpMap, filename);
			                return;
		                }
		                _savingMap = true;
		                _mainMenu.Visible = false;
						RedrawFormatMenu();
		                _formatMenu.Visible = true;
		                break;
					case 3:
						_savingMap = false;
						_mainMenu.Visible = false;
						RedrawFormatMenu();
						_formatMenu.Visible = true;
						break;
                }
            };

	        _formatMenu.OnItemSelect += (m, item, indx) =>
	        {
		        if (_savingMap)
		        {
					string filename = Game.GetUserInput(255);
			        switch (indx)
			        {
						case 0: // XML
					        if (!filename.EndsWith(".xml")) filename += ".xml";
							SaveMap(filename, MapSerializer.Format.NormalXml);
					        break;
						case 1: // Objects.ini
							if (!filename.EndsWith(".ini")) filename += ".ini";
							SaveMap(filename, MapSerializer.Format.SimpleTrainer);
							break;
						case 2: // C#
                            if (!filename.EndsWith(".cs")) filename += ".cs";
                            SaveMap(filename, MapSerializer.Format.CSharpCode);
							break;
						case 3: // Raw
                            if (!filename.EndsWith(".txt")) filename += ".txt";
                            SaveMap(filename, MapSerializer.Format.Raw);
							break;
                        case 4: // SpoonerLegacy
                            if (!filename.EndsWith(".SP00N")) filename += ".SP00N";
                            SaveMap(filename, MapSerializer.Format.SpoonerLegacy);
			                break;
                        case 5: // Menyoo
                            if (!filename.EndsWith(".xml")) filename += ".xml";
                            SaveMap(filename, MapSerializer.Format.Menyoo);
                            break;
                    }
				}
		        else
		        {
					string filename = Game.GetUserInput(255);
			        MapSerializer.Format tmpFor = MapSerializer.Format.NormalXml;
			        switch (indx)
			        {
						case 0: // XML
							tmpFor = MapSerializer.Format.NormalXml;
					        break;
						case 1: // Objects.ini
							tmpFor = MapSerializer.Format.SimpleTrainer;
					        break;
                        case 2: // Spooner
                            tmpFor = MapSerializer.Format.SpoonerLegacy;
			                break;
                        case 3: // Spooner
                            tmpFor = MapSerializer.Format.Menyoo;
                            break;
                    }
					LoadMap(filename, tmpFor);
				}
		        _formatMenu.Visible = false;
	        };

			_settingsMenu = new UIMenu("Map Editor", "~b~" + Translation.Translate("SETTINGS"));

			for (int i = -500000; i <= 500000; i++)
			{
				_possiblePositions.Add(i * 0.01);
			}
			
			for (int i = -36000; i <= 36000; i++)
			{
				_possibleRoll.Add(i * 0.01);
			}

		    var possibleLangauges = new List<string>
		    {
                "Auto"
		    };
            possibleLangauges.AddRange(Translation.Translations.Select(t => t.Language).ToList());

		    var language = new UIMenuListItem(Translation.Translate("Language"),
		        possibleLangauges.Select(t => (dynamic) t).ToList(), possibleLangauges.IndexOf(_settings.Translation));

            language.OnListChanged += (sender, index) =>
		    {
		        var newLanguage = sender.IndexToItem(index).ToString();
                Translation.SetLanguage(newLanguage);
		        _settings.Translation = newLanguage;
                SaveSettings();
		        if (newLanguage == "Auto")
		        {
		            language.Description = "Use your game's language settings.";
		            return;
		        }
		        var descFile = Translation.Translations.FirstOrDefault(t => t.Language == newLanguage);
		        if (descFile == null) return;
		        language.Description = "~h~" + Translation.Translate("Translator") + ":~h~ " + descFile.Translator;
		    };

			var checkem = new UIMenuListItem(Translation.Translate("Marker"), new List<dynamic>(Enum.GetNames(typeof(CrosshairType))), Enum.GetNames(typeof(CrosshairType)).ToList().FindIndex(x => x == _settings.CrosshairType.ToString()));
			checkem.OnListChanged += (i, indx) =>
			{
				CrosshairType outHash;
				Enum.TryParse(i.IndexToItem(indx).ToString(), out outHash);
				_settings.CrosshairType = outHash;
				SaveSettings();
			};

            List<dynamic> autosaveList = new List<dynamic> {Translation.Translate("Disable")};
            for (int i = 5; i <= 60; i += 5)
            {
                autosaveList.Add(i);
            }
		    int aIndex = autosaveList.IndexOf(_settings.AutosaveInterval);
		    if (aIndex == -1)
                aIndex = 0;

            var autosaveItem = new UIMenuListItem(Translation.Translate("Autosave Interval"), autosaveList, aIndex, Translation.Translate("Interval in minutes between automatic autosaves."));
            autosaveItem.OnListChanged += (item, index) =>
            {
                var sel = item.IndexToItem(index);
                _settings.AutosaveInterval = (sel as string) == Translation.Translate("Disable") ? -1 : Convert.ToInt32(item.IndexToItem(index));
                SaveSettings();
            };

		    List<dynamic> possibleDrawDistances = new List<dynamic> {Translation.Translate("Default"), 50, 75};
		    for (int i = 100; i <= 3000; i += 100)
            {
                possibleDrawDistances.Add(i);
            }
            int dIndex = possibleDrawDistances.IndexOf(_settings.DrawDistance);
            if (dIndex == -1)
                dIndex = 0;
            var drawDistanceItem = new UIMenuListItem(Translation.Translate("Draw Distance"), possibleDrawDistances, dIndex, Translation.Translate("Draw distance for props, vehicles and peds. Reload the map for changes to take effect."));
            drawDistanceItem.OnListChanged += (item, index) =>
            {
                var sel = item.IndexToItem(index);
                _settings.DrawDistance = (sel as string) == Translation.Translate("Default") ? -1 : Convert.ToInt32(item.IndexToItem(index));
                SaveSettings();
            };

            List<dynamic> senslist = new List<dynamic>();
			for (int i = 1; i < 60; i++)
			{
				senslist.Add(i);
			}
			var gamboy = new UIMenuListItem(Translation.Translate("Mouse Camera Sensitivity"), senslist, _settings.CameraSensivity - 1);
			gamboy.OnListChanged += (item, index) =>
			{
				_settings.CameraSensivity = index + 1;
				SaveSettings();
			};
            var gampadSens = new UIMenuListItem(Translation.Translate("Gamepad Camera Sensitivity"), senslist, _settings.GamepadCameraSensitivity - 1);
            gampadSens.OnListChanged += (item, index) =>
            {
                _settings.GamepadCameraSensitivity = index + 1;
                SaveSettings();
            };

            var keymovesens = new UIMenuListItem(Translation.Translate("Keyboard Movement Sensitivity"), senslist, _settings.KeyboardMovementSensitivity - 1);
            keymovesens.OnListChanged += (item, index) =>
            {
                _settings.KeyboardMovementSensitivity = index + 1;
                SaveSettings();
            };

            var gammovesens = new UIMenuListItem(Translation.Translate("Gamepad Movement Sensitivity"), senslist, _settings.GamepadMovementSensitivity - 1);
            gammovesens.OnListChanged += (item, index) =>
            {
                _settings.GamepadMovementSensitivity = index + 1;
                SaveSettings();
            };

            var butts = new UIMenuCheckboxItem(Translation.Translate("Instructional Buttons"), _settings.InstructionalButtons);
			butts.CheckboxEvent += (i, checkd) =>
			{
				_settings.InstructionalButtons = checkd;
				SaveSettings();
			};
	        var gamepadItem = new UIMenuCheckboxItem(Translation.Translate("Enable Gamepad Shortcut"), _settings.Gamepad);
	        gamepadItem.CheckboxEvent += (i, checkd) =>
	        {
		        _settings.Gamepad = checkd;
				SaveSettings();
	        };

	        var counterItem = new UIMenuCheckboxItem(Translation.Translate("Entity Counter"), _settings.PropCounterDisplay);
	        counterItem.CheckboxEvent += (i, checkd) =>
	        {
		        _settings.PropCounterDisplay = checkd;
				SaveSettings();
	        };

	        var snapper = new UIMenuCheckboxItem(Translation.Translate("Follow Object With Camera"), _settings.SnapCameraToSelectedObject);
	        snapper.CheckboxEvent += (i, checkd) =>
	        {
		        _settings.SnapCameraToSelectedObject = checkd;
				SaveSettings();
	        };

            var boundItem = new UIMenuCheckboxItem(Translation.Translate("Bounding Box"), _settings.BoundingBox.GetValueOrDefault(false));
            boundItem.CheckboxEvent += (i, checkd) =>
            {
                _settings.BoundingBox = checkd;
                SaveSettings();
            };

            var scriptItem = new UIMenuCheckboxItem(Translation.Translate("Execute Scripts"), _settings.LoadScripts);
            scriptItem.CheckboxEvent += (i, checkd) =>
            {
                _settings.LoadScripts = checkd;
                SaveSettings();
            };

            var validate = new UIMenuItem(Translation.Translate("Validate Object Database"),Translation.Translate(
				"This will update the current object database, removing any invalid objects. The changes will take effect after you restart the script." +
				" It will take a couple of minutes."));
			validate.Activated += (men, item) => ValidateDatabase();

			var resetGrps = new UIMenuItem(Translation.Translate("Reset Active Relationship Groups"),
				Translation.Translate("This will set all ped's relationship groups to Companion."));
			resetGrps.Activated += (men, item) =>
			{
				PropStreamer.Peds.ForEach(ped => ObjectDatabase.SetPedRelationshipGroup(new Ped(ped), "Companion"));
			};

            _settingsMenu.AddItem(language);
			_settingsMenu.AddItem(gamepadItem);
            _settingsMenu.AddItem(drawDistanceItem);
            _settingsMenu.AddItem(autosaveItem);
			_settingsMenu.AddItem(checkem);
            _settingsMenu.AddItem(boundItem);
			_settingsMenu.AddItem(gamboy);
            _settingsMenu.AddItem(gampadSens);
            _settingsMenu.AddItem(keymovesens);
            _settingsMenu.AddItem(gammovesens);
            _settingsMenu.AddItem(butts);
			_settingsMenu.AddItem(counterItem);
	        _settingsMenu.AddItem(snapper);
            _settingsMenu.AddItem(scriptItem);
			_settingsMenu.AddItem(validate);
			_settingsMenu.AddItem(resetGrps);
			_settingsMenu.RefreshIndex();
			_settingsMenu.DisableInstructionalButtons(true);
			_menuPool.Add(_settingsMenu);


			_currentObjectsMenu = new UIMenu("Map Editor", "~b~" + Translation.Translate("CURRENT ENTITES"));
	        _currentObjectsMenu.OnItemSelect += OnEntityTeleport;
			_currentObjectsMenu.DisableInstructionalButtons(true);
            _menuPool.Add(_currentObjectsMenu);


	        var binder = new UIMenuItem(Translation.Translate("Settings"));
	        _currentEntitiesItem = new UIMenuItem(Translation.Translate("Current Entities"));

	        var binder2 = new UIMenuItem(Translation.Translate("Create Map for External Mod"));

			_mainMenu.AddItem(_currentEntitiesItem);
            _mainMenu.AddItem(binder);
			_mainMenu.AddItem(binder2);

			_mainMenu.BindMenuToItem(_settingsMenu, binder);
			_mainMenu.BindMenuToItem(_currentObjectsMenu, _currentEntitiesItem);
			_mainMenu.BindMenuToItem(ModManager.ModMenu, binder2);
			_mainMenu.RefreshIndex();
			_menuPool.Add(ModManager.ModMenu);
        }

	    private void LoadSettings()
	    {
		    if (File.Exists("scripts\\MapEditor.xml"))
		    {
			    XmlSerializer serializer = new XmlSerializer(typeof(Settings));
			    var file = new StreamReader("scripts\\MapEditor.xml");
			    _settings = (Settings) serializer.Deserialize(file);
				file.Close();
			    if (_settings.ActivationKey == Keys.None)
			    {
				    _settings.ActivationKey = Keys.F7;
					SaveSettings();
			    }

		        if (_settings.GamepadCameraSensitivity == 0)
		            _settings.GamepadCameraSensitivity = 5;
                if (_settings.KeyboardMovementSensitivity == 0)
                    _settings.KeyboardMovementSensitivity = 30;
                if (_settings.GamepadMovementSensitivity == 0)
                    _settings.GamepadMovementSensitivity = 30;
		        if (_settings.AutosaveInterval == 0)
		            _settings.AutosaveInterval = 5;
		        if (_settings.DrawDistance == 0)
		            _settings.DrawDistance = -1;
		        if (!_settings.BoundingBox.HasValue)
		            _settings.BoundingBox = true;
		    }
		    else
		    {
		        _settings = new Settings();
				SaveSettings();
		    }
	    }

	    private void SaveSettings()
	    {
		    XmlSerializer serializer = new XmlSerializer(typeof(Settings));
		    var file = new StreamWriter("scripts\\MapEditor.xml");
			serializer.Serialize(file, _settings);
			file.Close();
	    }

	    private void AutoloadMaps()
	    {
		    if(!Directory.Exists("scripts\\AutoloadMaps")) return;
		    foreach (string file in Directory.GetFiles("scripts\\AutoloadMaps", "*.xml"))
		    {
			    LoadMap(file, MapSerializer.Format.NormalXml);
		    }
			foreach (string file in Directory.GetFiles("scripts\\AutoloadMaps", "*.ini"))
			{
				LoadMap(file, MapSerializer.Format.SimpleTrainer);
			}
		}

	    private void LoadMap(string filename, MapSerializer.Format format)
	    {
			if (String.IsNullOrWhiteSpace(filename) || !File.Exists(filename))
			{
				if (File.Exists(filename + ".xml") && format == MapSerializer.Format.NormalXml)
				{
					LoadMap(filename + ".xml", MapSerializer.Format.NormalXml);
					return;
				}

				if(File.Exists(filename + ".ini") && format == MapSerializer.Format.SimpleTrainer)
				{
					LoadMap(filename + ".ini", MapSerializer.Format.SimpleTrainer);
					return;
				}

                if (File.Exists(filename + ".xml") && format == MapSerializer.Format.Menyoo)
                {
                    LoadMap(filename + ".xml", MapSerializer.Format.Menyoo);
                    return;
                }

                if (File.Exists(filename + ".SP00N") && format == MapSerializer.Format.SpoonerLegacy)
                {
                    LoadMap(filename + ".SP00N", MapSerializer.Format.SpoonerLegacy);
                    return;
                }

                UI.Notify("~b~~h~Map Editor~h~~w~~n~" + Translation.Translate("The filename was empty or the file does not exist!"));
				return;
			}
            var handles = new List<int>();
			var des = new MapSerializer();
		    try
		    {
			    var map2Load = des.Deserialize(filename, format);
			    if (map2Load == null) return;
			    foreach (MapObject o in map2Load.Objects)
			    {
				    if(o == null) continue;
			        _loadedEntities++;
				    switch (o.Type)
				    {
					    case ObjectTypes.Prop:
				            var newProp = PropStreamer.CreateProp(ObjectPreview.LoadObject(o.Hash), o.Position, o.Rotation,
				                o.Dynamic && !o.Door, o.Quaternion == new Quaternion() {X = 0, Y = 0, Z = 0, W = 0} ? null : o.Quaternion,
				                drawDistance: _settings.DrawDistance);
                            AddItemToEntityMenu(newProp);

				            if (o.Door)
				            {
				                PropStreamer.Doors.Add(newProp.Handle);
				                newProp.FreezePosition = false;
				            }

				            if (o.Id != null && !PropStreamer.Identifications.ContainsKey(newProp.Handle))
				            {
				                PropStreamer.Identifications.Add(newProp.Handle, o.Id);
                                handles.Add(newProp.Handle);
				            }
						    break;
					    case ObjectTypes.Vehicle:
						    Vehicle tmpVeh;
						    AddItemToEntityMenu(tmpVeh = PropStreamer.CreateVehicle(ObjectPreview.LoadObject(o.Hash), o.Position, o.Rotation.Z, o.Dynamic, drawDistance: _settings.DrawDistance));
				            if (o.Id != null && !PropStreamer.Identifications.ContainsKey(tmpVeh.Handle))
				            {
				                PropStreamer.Identifications.Add(tmpVeh.Handle, o.Id);
				                handles.Add(tmpVeh.Handle);
				            }
                            if (o.SirensActive)
						    {
							    PropStreamer.ActiveSirens.Add(tmpVeh.Handle);
							    tmpVeh.SirenActive = true;
						    }
						    break;
					    case ObjectTypes.Ped:
						    Ped pedid;
						    AddItemToEntityMenu(pedid = PropStreamer.CreatePed(ObjectPreview.LoadObject(o.Hash), o.Position - new Vector3(0f, 0f, 1f), o.Rotation.Z, o.Dynamic, drawDistance: _settings.DrawDistance));
							if((o.Action == null || o.Action == "None") && !PropStreamer.ActiveScenarios.ContainsKey(pedid.Handle))
								PropStreamer.ActiveScenarios.Add(pedid.Handle, "None");
							else if (o.Action != null && o.Action != "None" && !PropStreamer.ActiveScenarios.ContainsKey(pedid.Handle))
							{
								PropStreamer.ActiveScenarios.Add(pedid.Handle, o.Action);
								if (o.Action == "Any" || o.Action == "Any - Walk")
									Function.Call(Hash.TASK_USE_NEAREST_SCENARIO_TO_COORD, pedid.Handle, pedid.Position.X, pedid.Position.Y,
										pedid.Position.Z, 100f, -1);
								else if(o.Action == "Any - Warp")
									Function.Call(Hash.TASK_USE_NEAREST_SCENARIO_TO_COORD_WARP, pedid.Handle, pedid.Position.X, pedid.Position.Y,
											pedid.Position.Z, 100f, -1);
								else
								{
									Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, pedid.Handle, ObjectDatabase.ScrenarioDatabase[o.Action], 0, 0);
								}
							}

				            if (o.Id != null && !PropStreamer.Identifications.ContainsKey(pedid.Handle))
				            {
				                PropStreamer.Identifications.Add(pedid.Handle, o.Id);
				                handles.Add(pedid.Handle);
				            }

                            if (o.Relationship == null)
							    PropStreamer.ActiveRelationships.Add(pedid.Handle, DefaultRelationship.ToString());
						    else
						    {
							    PropStreamer.ActiveRelationships.Add(pedid.Handle, o.Relationship);
							    if (o.Relationship != DefaultRelationship.ToString())
							    {
								    ObjectDatabase.SetPedRelationshipGroup(pedid, o.Relationship);
							    }
						    }

						    if (o.Weapon == null)
							    PropStreamer.ActiveWeapons.Add(pedid.Handle, WeaponHash.Unarmed);
						    else
						    {
							    PropStreamer.ActiveWeapons.Add(pedid.Handle, o.Weapon.Value);
							    if (o.Weapon != WeaponHash.Unarmed)
							    {
								    pedid.Weapons.Give(o.Weapon.Value, 999, true, true);
							    }
						    }
						    break;
                        case ObjectTypes.Pickup:
				            var newPickup = PropStreamer.CreatePickup(o.Hash, o.Position, o.Rotation.Z, o.Amount, o.Dynamic, o.Quaternion);
				            newPickup.Timeout = o.RespawnTimer;
                            AddItemToEntityMenu(newPickup);
                            if (o.Id != null && !PropStreamer.Identifications.ContainsKey(newPickup.ObjectHandle))
                            {
                                PropStreamer.Identifications.Add(newPickup.ObjectHandle, o.Id);
                                handles.Add(newPickup.ObjectHandle);
                            }
                            break;
				    }
			    }
			    foreach (MapObject o in map2Load.RemoveFromWorld)
			    {
					if(o == null) continue;
				    PropStreamer.RemovedObjects.Add(o);
				    Prop returnedProp = Function.Call<Prop>(Hash.GET_CLOSEST_OBJECT_OF_TYPE, o.Position.X, o.Position.Y,
					    o.Position.Z, 1f, o.Hash, 0);
				    if (returnedProp == null || returnedProp.Handle == 0) continue;
                    MapObject tmpObj = new MapObject()
                    {
                        Hash = returnedProp.Model.Hash,
                        Position = returnedProp.Position,
                        Rotation = returnedProp.Rotation,
                        Quaternion = Quaternion.GetEntityQuaternion(returnedProp),
                        Type = ObjectTypes.Prop,
                        Id = _mapObjCounter.ToString(),
                    };
                    _mapObjCounter++;
                    AddItemToEntityMenu(tmpObj);
                    returnedProp.Delete();
			    }
			    foreach (Marker marker in map2Load.Markers)
			    {
				    if(marker == null) continue;
			        _markerCounter++;
			        marker.Id = _markerCounter;
					PropStreamer.Markers.Add(marker);
					AddItemToEntityMenu(marker);
			    }

		        if (_settings.LoadScripts && format == MapSerializer.Format.NormalXml &&
		            File.Exists(new FileInfo(filename).Directory.FullName + "\\" + Path.GetFileNameWithoutExtension(filename) + ".js"))
		        {
                    JavascriptHook.StartScript(File.ReadAllText(new FileInfo(filename).Directory.FullName + "\\" + Path.GetFileNameWithoutExtension(filename) + ".js"), handles);
		        }
                
			    UI.Notify("~b~~h~Map Editor~h~~w~~n~" + Translation.Translate("Loaded map") + " ~h~" + filename + "~h~.");
		    }
		    catch (Exception e)
		    {
				UI.Notify("~r~~h~Map Editor~h~~w~~n~" + Translation.Translate("Map failed to load, see error below."));
				UI.Notify(e.Message);
			}
	    }

	    private void SaveMap(string filename, MapSerializer.Format format)
	    {
			if (String.IsNullOrWhiteSpace(filename))
			{
				UI.Notify("~b~~h~Map Editor~h~~w~~n~" + Translation.Translate("The filename was empty!"));
				return;
			}

			var ser = new MapSerializer();
			var tmpmap = new Map();
			try
			{
				tmpmap.Objects.AddRange(format == MapSerializer.Format.SimpleTrainer
					? PropStreamer.GetAllEntities().Where(p => p.Type == ObjectTypes.Prop)
					: PropStreamer.GetAllEntities());
				tmpmap.RemoveFromWorld.AddRange(PropStreamer.RemovedObjects);
				tmpmap.Markers.AddRange(PropStreamer.Markers);
				ser.Serialize(filename, tmpmap, format);
				UI.Notify("~b~~h~Map Editor~h~~w~~n~" + Translation.Translate("Saved current map as") + " ~h~" + filename + "~h~.");
			    _changesMade = 0;
			}
			catch (Exception e)
			{
				UI.Notify("~r~~h~Map Editor~h~~w~~n~" + Translation.Translate("Map failed to save, see error below."));
				UI.Notify(e.Message);
			}
		}
		
		public void OnTick(object sender, EventArgs e)
		{
			// Load maps from "AutoloadMaps"
			if (!_hasLoaded)
			{
				AutoloadMaps();
				_hasLoaded = true;
			}
			_menuPool.ProcessMenus();
			PropStreamer.Tick();

			if (PropStreamer.EntityCount > 0 || PropStreamer.RemovedObjects.Count > 0 || PropStreamer.Markers.Count > 0 || PropStreamer.Pickups.Count > 0)
			{
				_currentEntitiesItem.Enabled = true;
				_currentEntitiesItem.Description = "";
			}
			else
			{
				_currentEntitiesItem.Enabled = false;
				_currentEntitiesItem.Description = Translation.Translate("There are no current entities.");
			}

			if (Game.IsControlPressed(0, Control.LookBehind) && Game.IsControlJustPressed(0, Control.FrontendLb) && !_menuPool.IsAnyMenuOpen() && _settings.Gamepad)
			{
				_mainMenu.Visible = !_mainMenu.Visible;
			}
		    
		    if (_settings.AutosaveInterval != -1 && DateTime.Now.Subtract(_lastAutosave).Minutes >= _settings.AutosaveInterval && PropStreamer.EntityCount > 0 && _changesMade > 0 && PropStreamer.EntityCount != _loadedEntities)
		    {
                SaveMap("Autosave.xml", MapSerializer.Format.NormalXml);
		        _lastAutosave = DateTime.Now;
		    }

		    if (_currentObjectsMenu.Visible)
		    {
                if (Game.IsControlJustPressed(0, Control.PhoneLeft))
                {
                    if (_currentObjectsMenu.CurrentSelection <= 100)
                    {
                        _currentObjectsMenu.CurrentSelection = 0;
                    }
                    else
                    {
                        _currentObjectsMenu.CurrentSelection -= 100;
                    }
                }

                if (Game.IsControlJustPressed(0, Control.PhoneRight))
                {
                    if (_currentObjectsMenu.CurrentSelection >= _currentObjectsMenu.Size - 101)
                    {
                        _currentObjectsMenu.CurrentSelection = _currentObjectsMenu.Size - 1;
                    }
                    else
                    {
                        _currentObjectsMenu.CurrentSelection += 100;
                    }
                }
            }

            // 
            // BELOW ONLY WHEN MAP EDITOR IS ACTIVE
            //

            if (!_isInFreecam) return;
			if(_settings.InstructionalButtons && !_objectsMenu.Visible)
                Function.Call(Hash._0x0DF606929C105BE1, _scaleform.Handle, 255, 255, 255, 255, 0);
            
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.CharacterWheel);
			Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.SelectWeapon);
			Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.FrontendPause);
			Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.NextCamera);
			Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.Phone);
			Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);

			if (Game.IsControlJustPressed(0, Control.Enter) && !_isChoosingObject)
            {
	            var oldType = _currentObjectType;
				_currentObjectType = ObjectTypes.Prop;
				if(oldType != _currentObjectType)
					RedrawObjectsMenu(type: _currentObjectType);
                World.CurrentDayTime = new TimeSpan(14, 0, 0);
                _isChoosingObject = true;
	            _snappedProp = null;
	            _selectedProp = null;
				_menuPool.CloseAllMenus();

                if (_quitWithSearchVisible && oldType == _currentObjectType)
                {
                    _searchMenu.Visible = true;
                    OnIndexChange(_searchMenu, _searchMenu.CurrentSelection);
                }
                else
                {
                    _objectsMenu.Visible = true;
                    OnIndexChange(_objectsMenu, _objectsMenu.CurrentSelection);
                }

                
				_objectsMenu.Subtitle.Caption = "~b~" + Translation.Translate("PLACE") + " " + _currentObjectType.ToString().ToUpper();
			}

			if (Game.IsControlJustPressed(0, Control.NextCamera) && !_isChoosingObject)
			{
				var oldType = _currentObjectType;
				_currentObjectType = ObjectTypes.Vehicle;
				if (oldType != _currentObjectType)
					RedrawObjectsMenu(type: _currentObjectType);
				World.CurrentDayTime = new TimeSpan(14, 0, 0);
				_isChoosingObject = true;
				_snappedProp = null;
				_selectedProp = null;
				_menuPool.CloseAllMenus();

			    if (_quitWithSearchVisible && oldType == _currentObjectType)
                {
                    _searchMenu.Visible = true;
                    OnIndexChange(_searchMenu, _searchMenu.CurrentSelection);
                }
                else
                {
                    _objectsMenu.Visible = true;
                    OnIndexChange(_objectsMenu, _objectsMenu.CurrentSelection);
                }
                _objectsMenu.Subtitle.Caption = "~b~" + Translation.Translate("PLACE") + " " + _currentObjectType.ToString().ToUpper();
			}
            
            if (Game.IsControlJustPressed(0, Control.FrontendPause) && !_isChoosingObject)
			{
				var oldType = _currentObjectType;
				_currentObjectType = ObjectTypes.Ped;
				if (oldType != _currentObjectType)
					RedrawObjectsMenu(type: _currentObjectType);
				World.CurrentDayTime = new TimeSpan(14, 0, 0);
				_isChoosingObject = true;
				_snappedProp = null;
				_selectedProp = null;
				_menuPool.CloseAllMenus();

			    if (_quitWithSearchVisible && oldType == _currentObjectType)
                {
                    _searchMenu.Visible = true;
                    OnIndexChange(_searchMenu, _searchMenu.CurrentSelection);
                }
                else
                {
                    _objectsMenu.Visible = true;
                    OnIndexChange(_objectsMenu, _objectsMenu.CurrentSelection);
                }
                _objectsMenu.Subtitle.Caption = "~b~" + Translation.Translate("PLACE") + " " + _currentObjectType.ToString().ToUpper();
			}

			if (Game.IsControlJustPressed(0, Control.Phone) && !_isChoosingObject && !_menuPool.IsAnyMenuOpen())
			{
				_snappedProp = null;
				_selectedProp = null;
				_snappedMarker = null;
				_selectedMarker = null;

				var tmpMark = new Marker()
				{
					Red = Color.Yellow.R,
					Green = Color.Yellow.G,
					Blue = Color.Yellow.B,
					Alpha = Color.Yellow.A,
					Scale = new Vector3(0.75f, 0.75f, 0.75f),
					Type =  MarkerType.UpsideDownCone,
					Position = VectorExtensions.RaycastEverything(new Vector2(0f, 0f), _mainCamera.Position, _mainCamera.Rotation, Game.Player.Character),
					Id = _markerCounter,
				};
				PropStreamer.Markers.Add(tmpMark);
				_snappedMarker = tmpMark;
				_markerCounter++;
			    _changesMade++;
				AddItemToEntityMenu(_snappedMarker);
			}

            if (Game.IsControlJustPressed(0, Control.ThrowGrenade) && !_isChoosingObject && !_menuPool.IsAnyMenuOpen())
            {
                _snappedProp = null;
                _selectedProp = null;
                _snappedMarker = null;
                _selectedMarker = null;

                var pickup = PropStreamer.CreatePickup(new Model((int) ObjectDatabase.PickupHash.Parachute),
                    VectorExtensions.RaycastEverything(new Vector2(0f, 0f), _mainCamera.Position, _mainCamera.Rotation,
                        Game.Player.Character), 0f, 100, false);

                _changesMade++;
                AddItemToEntityMenu(pickup);
                _snappedProp = new Prop(pickup.ObjectHandle);
            }

            if (_isChoosingObject)
            {
                if (_previewProp != null)
                {
                    _previewProp.Rotation = _previewProp.Rotation + (_zAxis ? new Vector3(0f, 0f, 2.5f) : new Vector3(2.5f, 0f, 0f));
                    if (_zAxis && IsPed(_previewProp))
                        _previewProp.Heading = _previewProp.Rotation.Z;
                    DrawEntityBox(_previewProp, Color.White);
                }

                if (Game.IsControlJustPressed(0, Control.SelectWeapon))
                    _zAxis = !_zAxis;

                if (_objectPreviewCamera == null)
                {
                    _objectPreviewCamera = World.CreateCamera(new Vector3(1200.016f, 4000.998f, 86.05062f), new Vector3(0f, 0f, 0f), 60f);
                    _objectPreviewCamera.PointAt(_objectPreviewPos);
                }

                if (Game.IsControlPressed(0, Control.MoveDownOnly))
                {
                    _objectPreviewCamera.Position -= new Vector3(0f, 0.5f, 0f);
                }

                if (Game.IsControlPressed(0, Control.MoveUpOnly))
                {
                    _objectPreviewCamera.Position += new Vector3(0f, 0.5f, 0f);
                }

                if (Game.IsControlJustPressed(0, Control.PhoneLeft))
                {
                    if (_objectsMenu.CurrentSelection <= 100)
                    {
                        _objectsMenu.CurrentSelection = 0;
                        OnIndexChange(_objectsMenu, 0);
                    }
                    else
                    {
                        _objectsMenu.CurrentSelection -= 100;
                        OnIndexChange(_objectsMenu, _objectsMenu.CurrentSelection);
                    }
                }

                if (Game.IsControlJustPressed(0, Control.PhoneRight))
                {
                    if (_objectsMenu.CurrentSelection >= _objectsMenu.Size - 101)
                    {
                        _objectsMenu.CurrentSelection = _objectsMenu.Size - 1;
                        OnIndexChange(_objectsMenu, _objectsMenu.CurrentSelection);
                    }
                    else
                    {
                        _objectsMenu.CurrentSelection += 100;
                        OnIndexChange(_objectsMenu, _objectsMenu.CurrentSelection);
                    }
                }

                World.RenderingCamera = _objectPreviewCamera;

                if (Game.IsControlJustPressed(0, Control.PhoneCancel) && !_searchResultsOn)
                {
                    _isChoosingObject = false;
                    _objectsMenu.Visible = false;
                    _previewProp?.Delete();
                }

                if (Game.IsControlJustPressed(0, Control.PhoneCancel) && _searchResultsOn)
                {
                    //RedrawObjectsMenu(type: _currentObjectType);
                    _searchMenu.Visible = false;
                    _objectsMenu.Visible = true;
                    OnIndexChange(_objectsMenu, 0);
                    _searchResultsOn = false;
                    _objectsMenu.Subtitle.Caption = "~b~" + Translation.Translate("PLACE") + " " + _currentObjectType.ToString().ToUpper();
                }

                if (Game.IsControlJustPressed(0, Control.Jump))
                {
                    string query = Game.GetUserInput(255);
                    if(String.IsNullOrWhiteSpace(query)) return;
                    if (query[0] == ' ')
                        query = query.Remove(0, 1);
                    _objectsMenu.Visible = false;
                    RedrawSearchMenu(query, _currentObjectType);
                    if(_searchMenu.Size != 0)
                        OnIndexChange(_searchMenu, 0);
                    _searchMenu.Subtitle.Caption = "~b~" + Translation.Translate("SEARCH RESULTS FOR") + " \"" + query.ToUpper() + "\"";
                    _searchMenu.Visible = true;
                    _searchResultsOn = true;
                }
                return;
            }
            World.RenderingCamera = _mainCamera;

	        var res = UIMenu.GetScreenResolutionMantainRatio();
			var safe = UIMenu.GetSafezoneBounds();

			if (_settings.PropCounterDisplay)
			{
				const int interval = 45;

                new UIResText(Translation.Translate("PICKUPS"), new Point(Convert.ToInt32(res.Width) - safe.X - 90, Convert.ToInt32(res.Height) - safe.Y - (90 + (5 * interval))), 0.3f, Color.White, Font.ChaletLondon, UIResText.Alignment.Right).Draw();
                new UIResText(PropStreamer.Pickups.Count.ToString(), new Point(Convert.ToInt32(res.Width) - safe.X - 20, Convert.ToInt32(res.Height) - safe.Y - (102 + (5 * interval))), 0.5f, Color.White, Font.ChaletLondon, UIResText.Alignment.Right).Draw();
                new Sprite("timerbars", "all_black_bg", new Point(Convert.ToInt32(res.Width) - safe.X - 248, Convert.ToInt32(res.Height) - safe.Y - (100 + (5 * interval))), new Size(250, 37), 0f, Color.FromArgb(180, 255, 255, 255)).Draw();

                new UIResText(Translation.Translate("MARKERS"), new Point(Convert.ToInt32(res.Width) - safe.X - 90, Convert.ToInt32(res.Height) - safe.Y - (90 + (4 * interval))), 0.3f, Color.White, Font.ChaletLondon, UIResText.Alignment.Right).Draw();
				new UIResText(PropStreamer.Markers.Count.ToString(), new Point(Convert.ToInt32(res.Width) - safe.X - 20, Convert.ToInt32(res.Height) - safe.Y - (102 + (4 * interval))), 0.5f, Color.White, Font.ChaletLondon, UIResText.Alignment.Right).Draw();
				new Sprite("timerbars", "all_black_bg", new Point(Convert.ToInt32(res.Width) - safe.X - 248, Convert.ToInt32(res.Height) - safe.Y - (100 + (4 * interval))), new Size(250, 37), 0f, Color.FromArgb(180, 255, 255, 255)).Draw();

				new UIResText(Translation.Translate("WORLD"), new Point(Convert.ToInt32(res.Width) - safe.X - 90, Convert.ToInt32(res.Height) - safe.Y - (90 + (3 * interval))), 0.3f, Color.White, Font.ChaletLondon, UIResText.Alignment.Right).Draw();
				new UIResText(PropStreamer.RemovedObjects.Count.ToString(), new Point(Convert.ToInt32(res.Width) - safe.X - 20, Convert.ToInt32(res.Height) - safe.Y - (102 + (3 * interval))), 0.5f, Color.White, Font.ChaletLondon, UIResText.Alignment.Right).Draw();
				new Sprite("timerbars", "all_black_bg", new Point(Convert.ToInt32(res.Width) - safe.X - 248, Convert.ToInt32(res.Height) - safe.Y - (100 + (3 * interval))), new Size(250, 37), 0f, Color.FromArgb(180, 255, 255, 255)).Draw();

				new UIResText(Translation.Translate("PROPS"), new Point(Convert.ToInt32(res.Width) - safe.X - 90, Convert.ToInt32(res.Height) - safe.Y - (90 + (2*interval))), 0.3f, Color.White, Font.ChaletLondon, UIResText.Alignment.Right).Draw();
				new UIResText(PropStreamer.PropCount.ToString(), new Point(Convert.ToInt32(res.Width) - safe.X - 20, Convert.ToInt32(res.Height) - safe.Y - (102 + (2 * interval))), 0.5f, Color.White, Font.ChaletLondon, UIResText.Alignment.Right).Draw();
				new Sprite("timerbars", "all_black_bg", new Point(Convert.ToInt32(res.Width) - safe.X - 248, Convert.ToInt32(res.Height) - safe.Y - (100 + (2 * interval))), new Size(250, 37), 0f, Color.FromArgb(180, 255, 255, 255)).Draw();

				new UIResText(Translation.Translate("VEHICLES"), new Point(Convert.ToInt32(res.Width) - safe.X - 90, Convert.ToInt32(res.Height) - safe.Y - (90 + interval)), 0.3f, Color.White, Font.ChaletLondon, UIResText.Alignment.Right).Draw();
				new UIResText(PropStreamer.Vehicles.Count.ToString(), new Point(Convert.ToInt32(res.Width) - safe.X - 20, Convert.ToInt32(res.Height) - safe.Y - (102 + interval)), 0.5f, Color.White, Font.ChaletLondon, UIResText.Alignment.Right).Draw();
				new Sprite("timerbars", "all_black_bg", new Point(Convert.ToInt32(res.Width) - safe.X - 248, Convert.ToInt32(res.Height) - safe.Y - (100 + interval)), new Size(250, 37), 0f, Color.FromArgb(180, 255, 255, 255)).Draw();
				
				new UIResText(Translation.Translate("PEDS"), new Point(Convert.ToInt32(res.Width) - safe.X - 90, Convert.ToInt32(res.Height) - safe.Y - 90), 0.3f, Color.White, Font.ChaletLondon, UIResText.Alignment.Right).Draw();
				new UIResText(PropStreamer.Peds.Count.ToString(), new Point(Convert.ToInt32(res.Width) - safe.X - 20, Convert.ToInt32(res.Height) - safe.Y - 102), 0.5f, Color.White, Font.ChaletLondon, UIResText.Alignment.Right).Draw();
				new Sprite("timerbars", "all_black_bg", new Point(Convert.ToInt32(res.Width) - safe.X - 248, Convert.ToInt32(res.Height) - safe.Y - 100), new Size(250, 37), 0f, Color.FromArgb(180, 255, 255, 255)).Draw();
			}

			int wi = Convert.ToInt32(res.Width*0.5);
			int he = Convert.ToInt32(res.Height * 0.5);
			
			Entity hitEnt = VectorExtensions.RaycastEntity(new Vector2(0f, 0f), _mainCamera.Position, _mainCamera.Rotation);

			if (_settings.CrosshairType == CrosshairType.Crosshair)
			{
				var crossColor = _crosshairPath;
				if (hitEnt != null && hitEnt.Handle != 0 && !PropStreamer.GetAllHandles().Contains(hitEnt.Handle))
					crossColor = _crosshairBluePath;
				else if (hitEnt != null && hitEnt.Handle != 0 && PropStreamer.GetAllHandles().Contains(hitEnt.Handle))
					crossColor = _crosshairYellowPath;
				Sprite.DrawTexture(crossColor, new Point(wi - 15, he - 15), new Size(30, 30));
			}

			//Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.CharacterWheel);
            //Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.SelectWeapon);
			//Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.FrontendPause);
            Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS, 0);
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.LookLeftRight);
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.LookUpDown);
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.CursorX);
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.CursorY);
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.FrontendPauseAlternate);

            var mouseX = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)Control.LookLeftRight);
			var mouseY = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)Control.LookUpDown);


			mouseX *= -1;
			mouseY *= -1;

		    switch (Game.CurrentInputMode)
		    {
		        case InputMode.MouseAndKeyboard:
		            mouseX *= _settings.CameraSensivity;
		            mouseY *= _settings.CameraSensivity;
		            break;
		        case InputMode.GamePad:
		            mouseX *= _settings.GamepadCameraSensitivity;
		            mouseY *= _settings.GamepadCameraSensitivity;
		            break;
		    }


            float movementModifier = 1f;
            if (Game.IsControlPressed(0, Control.Sprint))
                movementModifier = 5f;
            else if (Game.IsControlPressed(0, Control.CharacterWheel))
                movementModifier = 0.3f;

		    switch (Game.CurrentInputMode)
		    {
                case InputMode.MouseAndKeyboard:
		            float baseSpeed = _settings.KeyboardMovementSensitivity / 30f; // 1 - 60, baseSpeed = 0.03 - 2
		            movementModifier *= baseSpeed;
		            break;
                case InputMode.GamePad:
                    float gamepadSpeed = _settings.GamepadMovementSensitivity / 30f; // 1 - 60, baseSpeed = 0.03 - 2
                    movementModifier *= gamepadSpeed;
                    break;
		    }

            float modifier = 1f;
            if (Game.IsControlPressed(0, Control.Sprint))
                modifier = 5f;
            else if (Game.IsControlPressed(0, Control.CharacterWheel))
                modifier = 0.3f;
            

			if (_selectedProp == null && _selectedMarker == null)
            {
                if(!_menuPool.IsAnyMenuOpen() || Game.CurrentInputMode == InputMode.GamePad)
                    _mainCamera.Rotation = new Vector3(_mainCamera.Rotation.X + mouseY, _mainCamera.Rotation.Y, _mainCamera.Rotation.Z + mouseX);
				
	            var dir = VectorExtensions.RotationToDirection(_mainCamera.Rotation);
				var rotLeft = _mainCamera.Rotation + new Vector3(0, 0, -10);
				var rotRight = _mainCamera.Rotation + new Vector3(0, 0, 10);
				var right = VectorExtensions.RotationToDirection(rotRight) - VectorExtensions.RotationToDirection(rotLeft);


                var newPos = _mainCamera.Position;
				if (Game.IsControlPressed(0, Control.MoveUpOnly))
                {
                    newPos += dir* movementModifier;
                }
                if (Game.IsControlPressed(0, Control.MoveDownOnly))
                {
                    newPos -= dir* movementModifier;
                }
                if (Game.IsControlPressed(0, Control.MoveLeftOnly))
                {
                    newPos += right* movementModifier;
                }
                if (Game.IsControlPressed(0, Control.MoveRightOnly))
                {
                    newPos -= right* movementModifier;
                }
                _mainCamera.Position = newPos;
                Game.Player.Character.PositionNoOffset = _mainCamera.Position - dir*8f;

                if (_snappedProp != null)
                {
                    if (!IsProp(_snappedProp))
                        _snappedProp.Position = VectorExtensions.RaycastEverything(new Vector2(0f, 0f), _mainCamera.Position, _mainCamera.Rotation, _snappedProp);
                    else
                        _snappedProp.PositionNoOffset = VectorExtensions.RaycastEverything(new Vector2(0f, 0f), _mainCamera.Position, _mainCamera.Rotation, _snappedProp);
                    if (Game.IsControlPressed(0, Control.CursorScrollUp) || Game.IsControlPressed(0, Control.FrontendRb))
                    {
                        _snappedProp.Rotation = _snappedProp.Rotation - new Vector3(0f, 0f, modifier);
                        if (IsPed(_snappedProp))
                            _snappedProp.Heading = _snappedProp.Rotation.Z;
                    }

                    if (Game.IsControlPressed(0, Control.CursorScrollDown) || Game.IsControlPressed(0, Control.FrontendLb))
                    {
                        _snappedProp.Rotation = _snappedProp.Rotation + new Vector3(0f, 0f, modifier);
                        if (IsPed(_snappedProp))
                            _snappedProp.Heading = _snappedProp.Rotation.Z;
                    }

					if (Game.IsControlJustPressed(0, Control.CreatorDelete))
					{
						RemoveItemFromEntityMenu(_snappedProp);
					    if (PropStreamer.IsPickup(_snappedProp.Handle))
					    {
                            PropStreamer.RemovePickup(_snappedProp.Handle);
					    }
					    else
					    {
					        PropStreamer.RemoveEntity(_snappedProp.Handle);
					        if (PropStreamer.Identifications.ContainsKey(_snappedProp.Handle))
					            PropStreamer.Identifications.Remove(_snappedProp.Handle);
					    }
					    _snappedProp = null;
                        _changesMade++;
                    }

					if (Game.IsControlJustPressed(0, Control.Attack))
                    {
                        if (PropStreamer.IsPickup(_snappedProp.Handle))
                        {
                            PropStreamer.GetPickup(_snappedProp.Handle).UpdatePos();
                        }
                        _snappedProp = null;
                        _changesMade++;
                    }
					InstructionalButtonsStart();
					InstructionalButtonsSnapped();
					InstructionalButtonsEnd();
				}
				else if (_snappedMarker != null)
				{
					_snappedMarker.Position = VectorExtensions.RaycastEverything(new Vector2(0f, 0f), _mainCamera.Position, _mainCamera.Rotation, Game.Player.Character);
					if (Game.IsControlPressed(0, Control.CursorScrollUp) || Game.IsControlPressed(0, Control.FrontendRb))
					{
						_snappedMarker.Rotation = _snappedMarker.Rotation - new Vector3(0f, 0f, modifier);
					}

					if (Game.IsControlPressed(0, Control.CursorScrollDown) || Game.IsControlPressed(0, Control.FrontendLb))
					{
						_snappedMarker.Rotation = _snappedMarker.Rotation + new Vector3(0f, 0f, modifier);
					}

					if (Game.IsControlJustPressed(0, Control.CreatorDelete))
					{
						RemoveMarkerFromEntityMenu(_snappedMarker.Id); 
						PropStreamer.Markers.Remove(_snappedMarker);
						_snappedMarker = null;
                        _changesMade++;
                    }

					if (Game.IsControlJustPressed(0, Control.Attack))
					{
						_snappedMarker = null;
                        _changesMade++;
                    }

					InstructionalButtonsStart();
					InstructionalButtonsSnapped();
					InstructionalButtonsEnd();
				}
                else if(_snappedProp == null && _snappedMarker == null)
                {
	                if (_settings.CrosshairType == CrosshairType.Orb)
	                {
		                var pos = VectorExtensions.RaycastEverything(new Vector2(0f, 0f), _mainCamera.Position, _mainCamera.Rotation, Game.Player.Character);
		                var color = Color.FromArgb(255, 200, 20, 20);
		                if (hitEnt != null && hitEnt.Handle != 0 && !PropStreamer.GetAllHandles().Contains(hitEnt.Handle))
			                color = Color.FromArgb(255, 20, 20, 255);
						else if (hitEnt != null && hitEnt.Handle != 0 && PropStreamer.GetAllHandles().Contains(hitEnt.Handle))
							color = Color.FromArgb(255, 200, 200, 20);
						Function.Call(Hash.DRAW_MARKER, 28, pos.X, pos.Y, pos.Z, 0f, 0f, 0f, 0f, 0f, 0f, 0.20f, 0.20f, 0.20f, color.R, color.G, color.B, color.A, false, true, 2, false, false, false, false);
	                }

	                if (Game.IsControlJustPressed(0, Control.Aim))
                    {
                        if (hitEnt != null && PropStreamer.GetAllHandles().Contains(hitEnt.Handle))
                        {
                            if (PropStreamer.IsPickup(hitEnt.Handle))
                                _snappedProp = new Prop(hitEnt.Handle);
							else if (Function.Call<bool>(Hash.IS_ENTITY_AN_OBJECT, hitEnt.Handle))
								_snappedProp = new Prop(hitEnt.Handle);
							else if (Function.Call<bool>(Hash.IS_ENTITY_A_VEHICLE, hitEnt.Handle))
								_snappedProp = new Vehicle(hitEnt.Handle);
							else if (Function.Call<bool>(Hash.IS_ENTITY_A_PED, hitEnt.Handle))
								_snappedProp = new Ped(hitEnt.Handle);
                            _changesMade++;
                        }
                        else
                        {
							var pos = VectorExtensions.RaycastEverything(new Vector2(0f, 0f), _mainCamera.Position, _mainCamera.Rotation, Game.Player.Character);
	                        Marker mark = PropStreamer.Markers.FirstOrDefault(m => (m.Position - pos).Length() < 2f);
	                        if (mark != null)
	                        {
		                        _snappedMarker = mark;
                                _changesMade++;
                            }
                        }
                    }

                    if (Game.IsControlJustPressed(0, Control.Attack))
                    {
                        if (hitEnt != null && PropStreamer.GetAllHandles().Contains(hitEnt.Handle))
                        {
                            if (PropStreamer.IsPickup(hitEnt.Handle))
                                _selectedProp = new Prop(hitEnt.Handle);
							else if (Function.Call<bool>(Hash.IS_ENTITY_AN_OBJECT, hitEnt.Handle))
								_selectedProp = new Prop(hitEnt.Handle);
							else if (Function.Call<bool>(Hash.IS_ENTITY_A_VEHICLE, hitEnt.Handle))
								_selectedProp = new Vehicle(hitEnt.Handle);
							else if (Function.Call<bool>(Hash.IS_ENTITY_A_PED, hitEnt.Handle))
								_selectedProp = new Ped(hitEnt.Handle);
							RedrawObjectInfoMenu(_selectedProp, true);
							_menuPool.CloseAllMenus();
							_objectInfoMenu.Visible = true;
							if(_settings.SnapCameraToSelectedObject)
								_mainCamera.PointAt(_selectedProp);
                            _changesMade++;
                        }
						else
						{
							var pos = VectorExtensions.RaycastEverything(new Vector2(0f, 0f), _mainCamera.Position, _mainCamera.Rotation, Game.Player.Character);
							Marker mark = PropStreamer.Markers.FirstOrDefault(m => (m.Position - pos).Length() < 2f);
							if (mark != null)
							{
								_selectedMarker = mark;
								RedrawObjectInfoMenu(_selectedMarker, true);
								_menuPool.CloseAllMenus();
								_objectInfoMenu.Visible = true;
                                _changesMade++;
                            }
						}
					}

	                if (Game.IsControlJustReleased(0, Control.LookBehind))
	                {
						if (hitEnt != null)
						{
						    if (PropStreamer.IsPickup(hitEnt.Handle))
						    {
						        var oldPickup = PropStreamer.GetPickup(hitEnt.Handle);
						        var newPickup = PropStreamer.CreatePickup(new Model(oldPickup.PickupHash), oldPickup.Position,
						            new Prop(oldPickup.ObjectHandle).Rotation.Z, oldPickup.Amount, oldPickup.Dynamic);
						        AddItemToEntityMenu(newPickup);
						        _snappedProp = new Prop(newPickup.ObjectHandle);
						    }
							else if (Function.Call<bool>(Hash.IS_ENTITY_AN_OBJECT, hitEnt.Handle))
							{
							    var isDoor = PropStreamer.Doors.Contains(hitEnt.Handle);
                                AddItemToEntityMenu(_snappedProp = PropStreamer.CreateProp(hitEnt.Model, hitEnt.Position, hitEnt.Rotation, (!PropStreamer.StaticProps.Contains(hitEnt.Handle) && !isDoor), force: true, drawDistance: _settings.DrawDistance));
							    if (isDoor)
							    {
							        _snappedProp.FreezePosition = false;
                                    PropStreamer.Doors.Add(_snappedProp.Handle);
							    }
							}
							else if (Function.Call<bool>(Hash.IS_ENTITY_A_VEHICLE, hitEnt.Handle))
								AddItemToEntityMenu(_snappedProp = PropStreamer.CreateVehicle(hitEnt.Model, hitEnt.Position, hitEnt.Rotation.Z, !PropStreamer.StaticProps.Contains(hitEnt.Handle), drawDistance: _settings.DrawDistance));
							else if (Function.Call<bool>(Hash.IS_ENTITY_A_PED, hitEnt.Handle))
							{
								AddItemToEntityMenu(_snappedProp = Function.Call<Ped>(Hash.CLONE_PED, ((Ped)hitEnt).Handle, hitEnt.Rotation.Z, 1, 1));
								if(_snappedProp != null)
									PropStreamer.Peds.Add(_snappedProp.Handle);

							    if (_settings.DrawDistance != -1)
							        _snappedProp.LodDistance = _settings.DrawDistance;

							    if (PropStreamer.StaticProps.Contains(hitEnt.Handle))
							    {
							        _snappedProp.FreezePosition = true;
                                    PropStreamer.StaticProps.Add(_snappedProp.Handle);
							    }

								if(!PropStreamer.ActiveScenarios.ContainsKey(_snappedProp.Handle))
									PropStreamer.ActiveScenarios.Add(_snappedProp.Handle, "None");

								if(PropStreamer.ActiveRelationships.ContainsKey(hitEnt.Handle))
									PropStreamer.ActiveRelationships.Add(_snappedProp.Handle, PropStreamer.ActiveRelationships[hitEnt.Handle]);
								else if (!PropStreamer.ActiveRelationships.ContainsKey(_snappedProp.Handle))
									PropStreamer.ActiveRelationships.Add(_snappedProp.Handle, DefaultRelationship.ToString());

								if(PropStreamer.ActiveWeapons.ContainsKey(hitEnt.Handle))
									PropStreamer.ActiveWeapons.Add(_snappedProp.Handle, PropStreamer.ActiveWeapons[hitEnt.Handle]);
								else if(!PropStreamer.ActiveWeapons.ContainsKey(_snappedProp.Handle))
									PropStreamer.ActiveWeapons.Add(_snappedProp.Handle, WeaponHash.Unarmed);
							}
							_changesMade++;
						}
						else
						{
							var pos = VectorExtensions.RaycastEverything(new Vector2(0f, 0f), _mainCamera.Position, _mainCamera.Rotation, Game.Player.Character);
							Marker mark = PropStreamer.Markers.FirstOrDefault(m => (m.Position - pos).Length() < 2f);
							if (mark != null)
							{
								var tmpMark = new Marker()
								{
									BobUpAndDown = mark.BobUpAndDown,
									Red = mark.Red,
									Green = mark.Green,
									Blue = mark.Blue,
									Alpha = mark.Alpha,
									Position = mark.Position,
									RotateToCamera = mark.RotateToCamera,
									Rotation = mark.Rotation,
									Scale = mark.Scale,
									Type = mark.Type,
									Id = _markerCounter,
								};
								_markerCounter++;
								AddItemToEntityMenu(tmpMark);
								PropStreamer.Markers.Add(tmpMark);
								_snappedMarker = tmpMark;
                                _changesMade++;
                            }
						}
					}

					if (Game.IsControlJustPressed(0, Control.CreatorDelete))
					{
						if (hitEnt != null && PropStreamer.GetAllHandles().Contains(hitEnt.Handle))
						{
							RemoveItemFromEntityMenu(hitEnt);
                            if (PropStreamer.Identifications.ContainsKey(hitEnt.Handle))
                                PropStreamer.Identifications.Remove(hitEnt.Handle);
                            if (PropStreamer.ActiveScenarios.ContainsKey(hitEnt.Handle))
								PropStreamer.ActiveScenarios.Remove(hitEnt.Handle);
							if (PropStreamer.ActiveRelationships.ContainsKey(hitEnt.Handle))
								PropStreamer.ActiveRelationships.Remove(hitEnt.Handle);
							if (PropStreamer.ActiveWeapons.ContainsKey(hitEnt.Handle))
								PropStreamer.ActiveWeapons.Remove(hitEnt.Handle);
							PropStreamer.RemoveEntity(hitEnt.Handle);
                            _changesMade++;
                        }
						else if(hitEnt != null && !PropStreamer.GetAllHandles().Contains(hitEnt.Handle) && Function.Call<bool>(Hash.IS_ENTITY_AN_OBJECT, hitEnt.Handle))
						{
							MapObject tmpObj = new MapObject()
							{
								Hash = hitEnt.Model.Hash,
								Position = hitEnt.Position,
								Rotation = hitEnt.Rotation,
								Quaternion = Quaternion.GetEntityQuaternion(hitEnt),
								Type = ObjectTypes.Prop,
								Id = _mapObjCounter.ToString(),
							};
							_mapObjCounter++;
							PropStreamer.RemovedObjects.Add(tmpObj);
							AddItemToEntityMenu(tmpObj);
							hitEnt.Delete();
                            _changesMade++;
                        }
						else
						{
							var pos = VectorExtensions.RaycastEverything(new Vector2(0f, 0f), _mainCamera.Position, _mainCamera.Rotation, Game.Player.Character);
							Marker mark = PropStreamer.Markers.FirstOrDefault(m => (m.Position - pos).Length() < 2f);
							if (mark != null)
							{
								PropStreamer.Markers.Remove(mark);
								RemoveMarkerFromEntityMenu(mark.Id);
                                _changesMade++;
                            }
						}
					}
					InstructionalButtonsStart();
					InstructionalButtonsFreelook();
					InstructionalButtonsEnd();
				}
            }
            else if(_selectedProp != null)//_selectedProp isnt null
            {
	            var tmp = _controlsRotate ? Color.FromArgb(200, 200, 20, 20) : Color.FromArgb(200, 200, 200, 10);
                var modelDims = _selectedProp.Model.GetDimensions();
                Function.Call(Hash.DRAW_MARKER, 0, _selectedProp.Position.X, _selectedProp.Position.Y, _selectedProp.Position.Z + modelDims.Z + 2f, 0f, 0f, 0f, 0f, 0f, 0f, 2f, 2f, 2f, tmp.R, tmp.G, tmp.B, tmp.A, 1, 0, 2, 2, 0, 0, 0);
                
                DrawEntityBox(_selectedProp, tmp);

	            if (Game.IsControlJustReleased(0, Control.Duck))
	            {
		            _controlsRotate = !_controlsRotate;
	            }
                if (Game.IsControlPressed(0, Control.FrontendRb))
                {
	                float pedMod = 0f;
	                if (_selectedProp is Ped)
		                pedMod = -1f;
                    if (!_controlsRotate)
                    {
                        if (!IsProp(_selectedProp))
                            _selectedProp.Position = _selectedProp.Position + new Vector3(0f, 0f, (modifier / 4) + pedMod);
                        else
                            _selectedProp.PositionNoOffset = _selectedProp.Position + new Vector3(0f, 0f, (modifier/4) + pedMod);
                    }
	                else
	                {
						_selectedProp.Quaternion = new Vector3(_selectedProp.Rotation.X, _selectedProp.Rotation.Y, _selectedProp.Rotation.Z - (modifier / 4)).ToQuaternion();
                        if (IsPed(_selectedProp))
                            _selectedProp.Heading = _selectedProp.Rotation.Z;
                    }

                    if (PropStreamer.IsPickup(_selectedProp.Handle))
                    {
                        PropStreamer.GetPickup(_selectedProp.Handle).UpdatePos();
                    }
                    _changesMade++;
                }
                if (Game.IsControlPressed(0, Control.FrontendLb))
                {
					float pedMod = 0f;
					if (_selectedProp is Ped)
						pedMod = 1f;
                    if (!_controlsRotate)
                    {
                        if (!IsProp(_selectedProp))
                            _selectedProp.Position = _selectedProp.Position - new Vector3(0f, 0f, (modifier/4) + pedMod);
                        else
                            _selectedProp.PositionNoOffset = _selectedProp.Position - new Vector3(0f, 0f, (modifier / 4) + pedMod);
                    }
	                else
	                {
                        _selectedProp.Quaternion = new Vector3(_selectedProp.Rotation.X, _selectedProp.Rotation.Y, _selectedProp.Rotation.Z + (modifier / 4)).ToQuaternion();
                        if (IsPed(_selectedProp))
	                        _selectedProp.Heading = _selectedProp.Rotation.Z;
	                }
                    _changesMade++;

                    if (PropStreamer.IsPickup(_selectedProp.Handle))
                    {
                        PropStreamer.GetPickup(_selectedProp.Handle).UpdatePos();
                    }
                }
				
                if (Game.IsControlPressed(0, Control.MoveUpOnly))
                {
					float pedMod = 0f;
	                if (IsPed(_selectedProp))
		                pedMod = -1f;
	                if (!_controlsRotate)
	                {
		                var dir = VectorExtensions.RotationToDirection(_mainCamera.Rotation)*(modifier/4);
                            
                        if (!IsProp(_selectedProp) )
                            _selectedProp.Position = _selectedProp.Position + new Vector3(dir.X, dir.Y, pedMod);
                        else
                            _selectedProp.PositionNoOffset = _selectedProp.Position + new Vector3(dir.X, dir.Y, pedMod);
                    }
	                else
	                {
                        _selectedProp.Quaternion = new Vector3(_selectedProp.Rotation.X + (modifier / 4), _selectedProp.Rotation.Y, _selectedProp.Rotation.Z).ToQuaternion();
                    }
                    _changesMade++;

                    if (PropStreamer.IsPickup(_selectedProp.Handle))
                    {
                        PropStreamer.GetPickup(_selectedProp.Handle).UpdatePos();
                    }
                }
                if (Game.IsControlPressed(0, Control.MoveDownOnly))
                {
					float pedMod = 0f;
					if (_selectedProp is Ped)
						pedMod = 1f;
	                if (!_controlsRotate)
	                {
		                var dir = VectorExtensions.RotationToDirection(_mainCamera.Rotation)*(modifier/4);
                        if (!IsProp(_selectedProp) )
                            _selectedProp.Position = _selectedProp.Position - new Vector3(dir.X, dir.Y, pedMod);
                        else
                            _selectedProp.PositionNoOffset = _selectedProp.Position - new Vector3(dir.X, dir.Y, pedMod);
                    }
	                else
	                {
                        _selectedProp.Quaternion = new Vector3(_selectedProp.Rotation.X - (modifier / 4), _selectedProp.Rotation.Y, _selectedProp.Rotation.Z).ToQuaternion();
                    }
                    _changesMade++;

                    if (PropStreamer.IsPickup(_selectedProp.Handle))
                    {
                        PropStreamer.GetPickup(_selectedProp.Handle).UpdatePos();
                    }
                }

                if (Game.IsControlPressed(0, Control.MoveLeftOnly))
                {
					float pedMod = 0f;
					if (_selectedProp is Ped)
						pedMod = -1f;
	                if (!_controlsRotate)
	                {
		                var rotLeft = _mainCamera.Rotation + new Vector3(0, 0, -10);
		                var rotRight = _mainCamera.Rotation + new Vector3(0, 0, 10);
		                var right = (VectorExtensions.RotationToDirection(rotRight) -
		                             VectorExtensions.RotationToDirection(rotLeft))*(modifier/2);
                        if (!IsProp(_selectedProp) )
                            _selectedProp.Position = _selectedProp.Position + new Vector3(right.X, right.Y, pedMod);
                        else
                            _selectedProp.PositionNoOffset = _selectedProp.Position + new Vector3(right.X, right.Y, pedMod);
                    }
	                else
	                {
                        _selectedProp.Quaternion = new Vector3(_selectedProp.Rotation.X, _selectedProp.Rotation.Y + (modifier / 4), _selectedProp.Rotation.Z).ToQuaternion();
                    }
                    _changesMade++;

                    if (PropStreamer.IsPickup(_selectedProp.Handle))
                    {
                        PropStreamer.GetPickup(_selectedProp.Handle).UpdatePos();
                    }
                }
                if (Game.IsControlPressed(0, Control.MoveRightOnly))
                {
					float pedMod = 0f;
					if (_selectedProp is Ped)
						pedMod = 1f;
	                if (!_controlsRotate)
	                {
		                var rotLeft = _mainCamera.Rotation + new Vector3(0, 0, -10);
		                var rotRight = _mainCamera.Rotation + new Vector3(0, 0, 10);
		                var right = (VectorExtensions.RotationToDirection(rotRight) -
		                             VectorExtensions.RotationToDirection(rotLeft))*(modifier/2);
                        if (!IsProp(_selectedProp) )
                            _selectedProp.Position = _selectedProp.Position - new Vector3(right.X, right.Y, pedMod);
                        else
                            _selectedProp.PositionNoOffset = _selectedProp.Position - new Vector3(right.X, right.Y, pedMod);
                    }
	                else
	                {
                        _selectedProp.Quaternion = new Vector3(_selectedProp.Rotation.X, _selectedProp.Rotation.Y - (modifier / 4), _selectedProp.Rotation.Z).ToQuaternion();
                    }
                    _changesMade++;

                    if (PropStreamer.IsPickup(_selectedProp.Handle))
                    {
                        PropStreamer.GetPickup(_selectedProp.Handle).UpdatePos();
                    }
                }

	            if (Game.IsControlJustReleased(0, Control.MoveLeftOnly) ||
					Game.IsControlJustReleased(0, Control.MoveRightOnly) ||
                    Game.IsControlJustReleased(0, Control.MoveUpOnly) ||
					Game.IsControlJustReleased(0, Control.MoveDownOnly) ||
                    Game.IsControlJustReleased(0, Control.FrontendLb) ||
	                Game.IsControlJustReleased(0, Control.FrontendRb))
	            {
					RedrawObjectInfoMenu(_selectedProp, false);
				}

				if (Game.IsControlJustReleased(0, Control.LookBehind))
				{
					Entity mainProp = new Prop(0);
				    if (PropStreamer.IsPickup(_selectedProp.Handle))
				    {
                        var oldPickup = PropStreamer.GetPickup(_selectedProp.Handle);
                        var newPickup = PropStreamer.CreatePickup(new Model(oldPickup.PickupHash), oldPickup.Position,
                            new Prop(oldPickup.ObjectHandle).Rotation.Z, oldPickup.Amount, oldPickup.Dynamic);
                        AddItemToEntityMenu(newPickup);
                        mainProp = new Prop(newPickup.ObjectHandle);
                    }
					else if (_selectedProp is Prop)
					{
                        var isDoor = PropStreamer.Doors.Contains(_selectedProp.Handle);
                        AddItemToEntityMenu(mainProp = PropStreamer.CreateProp(_selectedProp.Model, _selectedProp.Position, _selectedProp.Rotation, (!PropStreamer.StaticProps.Contains(_selectedProp.Handle) && !isDoor), force: true, drawDistance: _settings.DrawDistance));
                        if (isDoor)
                        {
                            mainProp.FreezePosition = false;
                            PropStreamer.Doors.Add(mainProp.Handle);
                        }
                    }
					else if (_selectedProp is Vehicle)
						AddItemToEntityMenu(mainProp = PropStreamer.CreateVehicle(_selectedProp.Model, _selectedProp.Position, _selectedProp.Rotation.Z, !PropStreamer.StaticProps.Contains(_selectedProp.Handle), drawDistance: _settings.DrawDistance));
					else if (_selectedProp is Ped)
					{
						AddItemToEntityMenu(mainProp = Function.Call<Ped>(Hash.CLONE_PED, ((Ped) _selectedProp).Handle, _selectedProp.Rotation.Z, 1, 1));
						PropStreamer.Peds.Add(mainProp.Handle);
						if(!PropStreamer.ActiveScenarios.ContainsKey(mainProp.Handle))
							PropStreamer.ActiveScenarios.Add(mainProp.Handle, "None");

					    if (_settings.DrawDistance != -1)
					        mainProp.LodDistance = _settings.DrawDistance;

						if (PropStreamer.ActiveRelationships.ContainsKey(_selectedProp.Handle))
							PropStreamer.ActiveRelationships.Add(mainProp.Handle, PropStreamer.ActiveRelationships[_selectedProp.Handle]);
						else if (!PropStreamer.ActiveRelationships.ContainsKey(mainProp.Handle))
							PropStreamer.ActiveRelationships.Add(mainProp.Handle, DefaultRelationship.ToString());

						if(PropStreamer.ActiveWeapons.ContainsKey(_selectedProp.Handle))
							PropStreamer.ActiveWeapons.Add(mainProp.Handle, PropStreamer.ActiveWeapons[_selectedProp.Handle]);
						else if(!PropStreamer.ActiveRelationships.ContainsKey(mainProp.Handle))
							PropStreamer.ActiveWeapons.Add(mainProp.Handle, WeaponHash.Unarmed);
					}

                    _changesMade++;
					_selectedProp = mainProp;
					if(_settings.SnapCameraToSelectedObject)
						_mainCamera.PointAt(_selectedProp);
					if(_selectedProp != null) RedrawObjectInfoMenu(_selectedProp, true);
                }

				if (Game.IsControlJustPressed(0, Control.CreatorDelete))
				{
                    if (PropStreamer.Identifications.ContainsKey(_selectedProp.Handle))
                        PropStreamer.Identifications.Remove(_selectedProp.Handle);
                    if (PropStreamer.ActiveScenarios.ContainsKey(_selectedProp.Handle))
						PropStreamer.ActiveScenarios.Remove(_selectedProp.Handle);
					if (PropStreamer.ActiveRelationships.ContainsKey(_selectedProp.Handle))
						PropStreamer.ActiveRelationships.Remove(_selectedProp.Handle);
					if (PropStreamer.ActiveWeapons.ContainsKey(_selectedProp.Handle))
						PropStreamer.ActiveWeapons.Remove(_selectedProp.Handle);
					RemoveItemFromEntityMenu(_selectedProp);
					PropStreamer.RemoveEntity(_selectedProp.Handle);
					_selectedProp = null;
					_objectInfoMenu.Visible = false;
					_mainCamera.StopPointing();
                    _changesMade++;
                }

				if (Game.IsControlJustPressed(0, Control.PhoneCancel) || Game.IsControlJustPressed(0, Control.Attack))
                {
                    if (PropStreamer.IsPickup(_selectedProp.Handle))
                    {
                        PropStreamer.GetPickup(_selectedProp.Handle).UpdatePos();
                    }

                    _selectedProp = null;
					_objectInfoMenu.Visible = false;
					_mainCamera.StopPointing();
                    _changesMade++;
                }
				InstructionalButtonsStart();
				InstructionalButtonsSelected();
				InstructionalButtonsEnd();
			}
			else if (_selectedMarker != null) // marker isn't null
			{
				if (Game.IsControlJustReleased(0, Control.Duck))
				{
					_controlsRotate = !_controlsRotate;
				}
				if (Game.IsControlPressed(0, Control.FrontendRb))
				{
					if (!_controlsRotate)
						_selectedMarker.Position += new Vector3(0f, 0f, (modifier / 4));
					else
						_selectedMarker.Rotation += new Vector3(0f, 0f, modifier);
                    _changesMade++;
                }
				if (Game.IsControlPressed(0, Control.FrontendLb))
				{
					if (!_controlsRotate)
						_selectedMarker.Position -= new Vector3(0f, 0f, (modifier / 4));
					else
						_selectedMarker.Rotation -= new Vector3(0f, 0f, modifier);
                    _changesMade++;
                }

				if (Game.IsControlPressed(0, Control.MoveUpOnly))
				{
					if (!_controlsRotate)
					{
						var dir = VectorExtensions.RotationToDirection(_mainCamera.Rotation) * (modifier / 4);
						_selectedMarker.Position += new Vector3(dir.X, dir.Y, 0f);
					}
					else
						_selectedMarker.Rotation += new Vector3(modifier, 0f, 0f);
                    _changesMade++;
                }
				if (Game.IsControlPressed(0, Control.MoveDownOnly))
				{
					if (!_controlsRotate)
					{
						var dir = VectorExtensions.RotationToDirection(_mainCamera.Rotation) * (modifier / 4);
						_selectedMarker.Position -= new Vector3(dir.X, dir.Y, 0f);
					}
					else
						_selectedMarker.Rotation -= new Vector3(modifier, 0f, 0f);
                    _changesMade++;
                }

				if (Game.IsControlPressed(0, Control.MoveLeftOnly))
				{
					if (!_controlsRotate)
					{
						var rotLeft = _mainCamera.Rotation + new Vector3(0, 0, -10);
						var rotRight = _mainCamera.Rotation + new Vector3(0, 0, 10);
						var right = (VectorExtensions.RotationToDirection(rotRight) - VectorExtensions.RotationToDirection(rotLeft)) * (modifier / 2);
						_selectedMarker.Position += new Vector3(right.X, right.Y, 0f);
					}
					else
						_selectedMarker.Rotation += new Vector3(0f, modifier, 0f);
                    _changesMade++;
                }
				if (Game.IsControlPressed(0, Control.MoveRightOnly))
				{
					if (!_controlsRotate)
					{
						var rotLeft = _mainCamera.Rotation + new Vector3(0, 0, -10);
						var rotRight = _mainCamera.Rotation + new Vector3(0, 0, 10);
						var right = (VectorExtensions.RotationToDirection(rotRight) - VectorExtensions.RotationToDirection(rotLeft)) * (modifier / 2);
						_selectedMarker.Position -= new Vector3(right.X, right.Y, 0f);
					}
					else
						_selectedMarker.Rotation -= new Vector3(0f, modifier, 0f);
                    _changesMade++;
                }

				if (Game.IsControlJustReleased(0, Control.MoveLeftOnly) ||
					Game.IsControlJustReleased(0, Control.MoveRightOnly) ||
					Game.IsControlJustReleased(0, Control.MoveUpOnly) ||
					Game.IsControlJustReleased(0, Control.MoveDownOnly) ||
					Game.IsControlJustReleased(0, Control.FrontendLb) ||
					Game.IsControlJustReleased(0, Control.FrontendRb))
				{
					RedrawObjectInfoMenu(_selectedMarker, false);
				}

				if (Game.IsControlJustReleased(0, Control.LookBehind))
				{
					var tmpMark = new Marker()
					{
						BobUpAndDown = _selectedMarker.BobUpAndDown,
						Red = _selectedMarker.Red,
						Green = _selectedMarker.Green,
						Blue = _selectedMarker.Blue,
						Alpha =  _selectedMarker.Alpha,
						Position = _selectedMarker.Position,
						RotateToCamera = _selectedMarker.RotateToCamera,
						Rotation = _selectedMarker.Rotation,
						Scale = _selectedMarker.Scale,
						Type = _selectedMarker.Type,
						Id = _markerCounter,
					};
					_markerCounter++;
					PropStreamer.Markers.Add(tmpMark);
					AddItemToEntityMenu(tmpMark);
					_selectedMarker = tmpMark;
					RedrawObjectInfoMenu(_selectedMarker, true);
                    _changesMade++;
                }

				if (Game.IsControlJustPressed(0, Control.CreatorDelete))
				{
					PropStreamer.Markers.Remove(_selectedMarker);
					RemoveMarkerFromEntityMenu(_selectedMarker.Id);
					_selectedMarker = null;
					_objectInfoMenu.Visible = false;
					_mainCamera.StopPointing();
                    _changesMade++;
                }

				if (Game.IsControlJustPressed(0, Control.PhoneCancel) || Game.IsControlJustPressed(0, Control.Attack))
				{
					_selectedMarker = null;
					_objectInfoMenu.Visible = false;
					_mainCamera.StopPointing();
                    _changesMade++;
                }
				InstructionalButtonsStart();
				InstructionalButtonsSelected();
				InstructionalButtonsEnd();
			}

        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == _settings.ActivationKey && !_menuPool.IsAnyMenuOpen())
            {
                _mainMenu.Visible = !_mainMenu.Visible;
            }
        }

        private void DrawEntityBox(Entity ent, Color color)
        {
            if(ent == null || (_settings.BoundingBox.HasValue && !_settings.BoundingBox.Value)) return;
            var pos = ent.Position;
            var modelSize = ent.Model.GetDimensions();

            modelSize = new Vector3(modelSize.X/2, modelSize.Y/2, modelSize.Z/2);

            var b1 = GetEntityOffset(ent, new Vector3(-modelSize.X, -modelSize.Y, -modelSize.Z * 0));
            var b2 = GetEntityOffset(ent, new Vector3(-modelSize.X, modelSize.Y, -modelSize.Z * 0));
            var b3 = GetEntityOffset(ent, new Vector3(modelSize.X, -modelSize.Y, -modelSize.Z * 0));
            var b4 = GetEntityOffset(ent, new Vector3(modelSize.X, modelSize.Y, -modelSize.Z * 0));

            var a1 = GetEntityOffset(ent, new Vector3(-modelSize.X, -modelSize.Y, modelSize.Z * 2));
            var a2 = GetEntityOffset(ent, new Vector3(-modelSize.X, modelSize.Y, modelSize.Z * 2));
            var a3 = GetEntityOffset(ent, new Vector3(modelSize.X, -modelSize.Y, modelSize.Z * 2));
            var a4 = GetEntityOffset(ent, new Vector3(modelSize.X, modelSize.Y, modelSize.Z * 2));

            DrawLine(a1, a2, color);
            DrawLine(a2, a4, color);
            DrawLine(a4, a3, color);
            DrawLine(a3, a1, color);

            DrawLine(b1, b2, color);
            DrawLine(b2, b4, color);
            DrawLine(b4, b3, color);
            DrawLine(b3, b1, color);

            DrawLine(a1, b1, color);
            DrawLine(a2, b2, color);
            DrawLine(a3, b3, color);
            DrawLine(a4, b4, color);
        }

        private void DrawLine(Vector3 start, Vector3 end, Color color)
        {
            Function.Call(Hash.DRAW_LINE, start.X, start.Y, start.Z, end.X, end.Y, end.Z, color.R, color.G, color.B, color.A);
        }

        private Vector3 GetEntityOffset(Entity ent, Vector3 offset)
        {
            return Function.Call<Vector3>(Hash.GET_OFFSET_FROM_ENTITY_IN_WORLD_COORDS, ent.Handle, offset.X, offset.Y, offset.Z); ;
        }

        private void OnIndexChange(UIMenu sender, int index)
        {
	        int requestedHash = 0;
	        switch (_currentObjectType)
	        {
		        case ObjectTypes.Prop:
					requestedHash = ObjectDatabase.MainDb[sender.MenuItems[index].Text];
			        break;
				case ObjectTypes.Vehicle:
					requestedHash = ObjectDatabase.VehicleDb[sender.MenuItems[index].Text];
			        break;
				case ObjectTypes.Ped:
					requestedHash = ObjectDatabase.PedDb[sender.MenuItems[index].Text];
			        break;
	        }
            if ((_previewProp == null || _previewProp.Model.Hash != requestedHash) && !ObjectDatabase.InvalidHashes.Contains(requestedHash))
            {
				_previewProp?.Delete();
                Model tmpModel = ObjectPreview.LoadObject(requestedHash);
				if(tmpModel == null)
					sender.MenuItems[index].SetRightLabel("~r~Invalid");
	            switch (_currentObjectType)
	            {
                    case ObjectTypes.Prop:
						_previewProp = World.CreateProp(tmpModel, _objectPreviewPos, false, false);
						break;
					case ObjectTypes.Vehicle:
			            _previewProp = World.CreateVehicle(tmpModel, _objectPreviewPos);
			            break;
					case ObjectTypes.Ped:
			            _previewProp = World.CreatePed(tmpModel, _objectPreviewPos);
			            break;
	            }
                if (_previewProp != null)
                {
                    _previewProp.FreezePosition = true;
                    _previewProp.Rotation = new Vector3(0, 0, 180f);
                    if (_previewProp.Model.IsPed)
                        _previewProp.Heading = 180f;
                }
            }
        }

        private void OnObjectSelect(UIMenu sender, UIMenuItem item, int index)
        {
	        int objectHash;
            if (PropStreamer.EntityCount == 0)
                _lastAutosave = DateTime.Now;

            _quitWithSearchVisible = _searchMenu.Visible;

	        switch (_currentObjectType)
	        {
			    case ObjectTypes.Prop:
					objectHash = _searchMenu.Visible ? ObjectDatabase.MainDb[_searchMenu.MenuItems[_searchMenu.CurrentSelection].Text] : ObjectDatabase.MainDb[_objectsMenu.MenuItems[_objectsMenu.CurrentSelection].Text];
                    AddItemToEntityMenu(_snappedProp = PropStreamer.CreateProp(ObjectPreview.LoadObject(objectHash), VectorExtensions.RaycastEverything(new Vector2(0f, 0f)), new Vector3(0, 0, 0), false, force: true, drawDistance: _settings.DrawDistance));
					break;
				case ObjectTypes.Vehicle:
                    objectHash = _searchMenu.Visible ? ObjectDatabase.VehicleDb[_searchMenu.MenuItems[_searchMenu.CurrentSelection].Text] : ObjectDatabase.VehicleDb[_objectsMenu.MenuItems[_objectsMenu.CurrentSelection].Text];
			        AddItemToEntityMenu(_snappedProp = PropStreamer.CreateVehicle(ObjectPreview.LoadObject(objectHash), VectorExtensions.RaycastEverything(new Vector2(0f, 0f)), 0f, true, drawDistance: _settings.DrawDistance));
					break;
				case ObjectTypes.Ped:
                    objectHash = _searchMenu.Visible ? ObjectDatabase.PedDb[_searchMenu.MenuItems[_searchMenu.CurrentSelection].Text] : ObjectDatabase.PedDb[_objectsMenu.MenuItems[_objectsMenu.CurrentSelection].Text];
                    AddItemToEntityMenu(_snappedProp = PropStreamer.CreatePed(ObjectPreview.LoadObject(objectHash), VectorExtensions.RaycastEverything(new Vector2(0f, 0f)), 0f, true, drawDistance: _settings.DrawDistance));
					PropStreamer.ActiveScenarios.Add(_snappedProp.Handle, "None");
					PropStreamer.ActiveRelationships.Add(_snappedProp.Handle, DefaultRelationship.ToString());
					PropStreamer.ActiveWeapons.Add(_snappedProp.Handle, WeaponHash.Unarmed);
					break;
	        }
            _isChoosingObject = false;
            _objectsMenu.Visible = false;
            _searchMenu.Visible = false;
			_previewProp?.Delete();
            _changesMade++;
        }

        private void RedrawObjectsMenu(ObjectTypes type = ObjectTypes.Prop)
        {
            _objectsMenu.Clear();
	        switch (type)
	        {
				case ObjectTypes.Prop:
					foreach (var u in ObjectDatabase.MainDb)
					{
						var object1 = new UIMenuItem(u.Key);
						if(ObjectDatabase.InvalidHashes.Contains(u.Value))
							object1.SetRightLabel("~r~Invalid");
						_objectsMenu.AddItem(object1);
					}
			        break;
				case ObjectTypes.Vehicle:
					foreach (var u in ObjectDatabase.VehicleDb)
					{
						var object1 = new UIMenuItem(u.Key);
						_objectsMenu.AddItem(object1);
					}
					break;
				case ObjectTypes.Ped:
					foreach (var u in ObjectDatabase.PedDb)
					{
						var object1 = new UIMenuItem(u.Key);
						_objectsMenu.AddItem(object1);
					}
					break;
			}
            _objectsMenu.RefreshIndex();
        }

        private void RedrawSearchMenu(string searchQuery, ObjectTypes type = ObjectTypes.Prop)
        {
            _searchMenu.Clear();
            switch (type)
            {
                case ObjectTypes.Prop:
                    foreach (var u in ObjectDatabase.MainDb.Where(pair => CultureInfo.InvariantCulture.CompareInfo.IndexOf(pair.Key, searchQuery, CompareOptions.IgnoreCase) >= 0))
                    {
                        var object1 = new UIMenuItem(u.Key);
                        if (ObjectDatabase.InvalidHashes.Contains(u.Value))
                            object1.SetRightLabel("~r~Invalid");
                        _searchMenu.AddItem(object1);
                    }
                    break;
                case ObjectTypes.Vehicle:
                    foreach (var u in ObjectDatabase.VehicleDb.Where(pair => CultureInfo.InvariantCulture.CompareInfo.IndexOf(pair.Key, searchQuery, CompareOptions.IgnoreCase) >= 0))
                    {
                        var object1 = new UIMenuItem(u.Key);
                        _searchMenu.AddItem(object1);
                    }
                    break;
                case ObjectTypes.Ped:
                    foreach (var u in ObjectDatabase.PedDb.Where(pair => CultureInfo.InvariantCulture.CompareInfo.IndexOf(pair.Key, searchQuery, CompareOptions.IgnoreCase) >= 0))
                    {
                        var object1 = new UIMenuItem(u.Key);
                        _searchMenu.AddItem(object1);
                    }
                    break;
            }
            _searchMenu.RefreshIndex();
        }

        private void RedrawFormatMenu()
	    {
			_formatMenu.Clear();
			_formatMenu.AddItem(new UIMenuItem("XML", Translation.Translate("Default format for Map Editor. Choose this one if you have no idea. This saves props, vehicles and peds.")));
		    _formatMenu.AddItem(new UIMenuItem("Simple Trainer",
				Translation.Translate("Format used in Simple Trainer mod (objects.ini). Only saves props.")));
		    if (_savingMap)
		    {
			    _formatMenu.AddItem(new UIMenuItem(Translation.Translate("C# Code"),
				    Translation.Translate("Directly outputs to C# code to spawn your entities. Saves props, vehicles and peds.")));
			    _formatMenu.AddItem(new UIMenuItem(Translation.Translate("Raw"),
				Translation.Translate("Writes the entity and their position and rotation. Useful for taking coordinates.")));
		    }
            _formatMenu.AddItem(new UIMenuItem("Spooner (Legacy)",
                Translation.Translate("Format used in Object Spooner mod (.SP00N).")));
            _formatMenu.AddItem(new UIMenuItem("Menyoo", Translation.Translate("Format used in Meynoo mod (.xml).")));
            _formatMenu.RefreshIndex();
		}

	    private readonly Scaleform _scaleform;
	    private void InstructionalButtonsStart()
	    {
		    if(!_settings.InstructionalButtons) return;
			_scaleform.CallFunction("CLEAR_ALL");
			_scaleform.CallFunction("TOGGLE_MOUSE_BUTTONS", 0);
			_scaleform.CallFunction("CREATE_CONTAINER");
		}

	    private void InstructionalButtonsFreelook()
	    {
			if (!_settings.InstructionalButtons) return;
			_scaleform.CallFunction("SET_DATA_SLOT", 0, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.Enter, 0), Translation.Translate("Spawn Prop"));
			_scaleform.CallFunction("SET_DATA_SLOT", 1, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.FrontendPause, 0), Translation.Translate("Spawn Ped"));
			_scaleform.CallFunction("SET_DATA_SLOT", 2, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.NextCamera, 0), Translation.Translate("Spawn Vehicle"));
			_scaleform.CallFunction("SET_DATA_SLOT", 3, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.Phone, 0), Translation.Translate("Spawn Marker"));
            _scaleform.CallFunction("SET_DATA_SLOT", 5, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.ThrowGrenade, 0), Translation.Translate("Spawn Pickup"));
            _scaleform.CallFunction("SET_DATA_SLOT", 6, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.Aim, 0), Translation.Translate("Move Entity"));
			_scaleform.CallFunction("SET_DATA_SLOT", 7, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.Attack, 0), Translation.Translate("Select Entity"));
			_scaleform.CallFunction("SET_DATA_SLOT", 8, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.LookBehind, 0), Translation.Translate("Copy Entity"));
			_scaleform.CallFunction("SET_DATA_SLOT", 9, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.CreatorDelete, 0), Translation.Translate("Delete Entity"));
		}

		private void InstructionalButtonsSelected()
		{
			if (!_settings.InstructionalButtons) return;
			_scaleform.CallFunction("SET_DATA_SLOT", 0, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.MoveLeftRight, 0), "");
			_scaleform.CallFunction("SET_DATA_SLOT", 1, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.MoveUpDown, 0), "");
			_scaleform.CallFunction("SET_DATA_SLOT", 2, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.FrontendRb, 0), "");
			_scaleform.CallFunction("SET_DATA_SLOT", 3, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.FrontendLb, 0), Translation.Translate("Move Entity"));
			_scaleform.CallFunction("SET_DATA_SLOT", 4, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.Duck, 0), Translation.Translate("Switch to Rotation"));
			_scaleform.CallFunction("SET_DATA_SLOT", 5, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.LookBehind, 0), Translation.Translate("Copy Entity"));
			_scaleform.CallFunction("SET_DATA_SLOT", 6, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.CreatorDelete, 0), Translation.Translate("Delete Entity"));
			_scaleform.CallFunction("SET_DATA_SLOT", 7, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.Attack, 0), Translation.Translate("Accept"));
		}

		private void InstructionalButtonsSnapped()
		{
			if (!_settings.InstructionalButtons) return;
			_scaleform.CallFunction("SET_DATA_SLOT", 0, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.FrontendRb, 0), "");
			_scaleform.CallFunction("SET_DATA_SLOT", 1, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.FrontendLb, 0), Translation.Translate("Rotate Entity"));
			_scaleform.CallFunction("SET_DATA_SLOT", 2, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.CreatorDelete, 0), Translation.Translate("Delete Entity"));
			_scaleform.CallFunction("SET_DATA_SLOT", 3, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.Attack, 0), Translation.Translate("Accept")); 
		}

		private void InstructionalButtonsEnd()
		{
			if (!_settings.InstructionalButtons) return;
			_scaleform.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", -1);
		}



	    private float oldRotX = 0f;
	    private float oldRotY = 0f;
	    private float oldRotZ = 0f;
	    private void RedrawObjectInfoMenu(Entity ent, bool refreshIndex)
	    {
			if(ent == null) return;
		    string name = "";

		    if (Function.Call<bool>(Hash.IS_ENTITY_AN_OBJECT, ent.Handle))
		    {
			    name = ObjectDatabase.MainDb.ContainsValue(ent.Model.Hash) ? ObjectDatabase.MainDb.First(x => x.Value == ent.Model.Hash).Key.ToUpper() : "Unknown Prop";
		    }
			if (Function.Call<bool>(Hash.IS_ENTITY_A_VEHICLE, ent.Handle))
				name = ObjectDatabase.VehicleDb.ContainsValue(ent.Model.Hash) ? ObjectDatabase.VehicleDb.First(x => x.Value == ent.Model.Hash).Key.ToUpper() : "Unknown Vehicle";
			if (Function.Call<bool>(Hash.IS_ENTITY_A_PED, ent.Handle))
				name = ObjectDatabase.PedDb.ContainsValue(ent.Model.Hash) ? ObjectDatabase.PedDb.First(x => x.Value == ent.Model.Hash).Key.ToUpper() : "Unknown Ped";

		    _objectInfoMenu.Subtitle.Caption = "~b~" + name;
			_objectInfoMenu.Clear();
			
			
			var posXitem = new UIMenuListItem(Translation.Translate("Position X"), _possiblePositions, Convert.ToInt32(Math.Round((ent.Position.X * 100) + 500000)));
			var posYitem = new UIMenuListItem(Translation.Translate("Position Y"), _possiblePositions, Convert.ToInt32(Math.Round((ent.Position.Y * 100) + 500000)));
			var posZitem = new UIMenuListItem(Translation.Translate("Position Z"), _possiblePositions, Convert.ToInt32(Math.Round((ent.Position.Z * 100) + 500000)));

	        var itemRot = ent.Quaternion.ToEuler();

			var rotXitem = new UIMenuListItem(Translation.Translate("Pitch"), _possibleRoll, (int)Math.Round((itemRot.Y * 100 * -1) + 36000));
			var rotYitem = new UIMenuListItem(Translation.Translate("Roll"), _possibleRoll, (int)Math.Round((itemRot.X * 100) + 36000));
			var rotZitem = new UIMenuListItem(Translation.Translate("Yaw"), _possibleRoll, (int)Math.Round((itemRot.Z * 100) + 36000));

			

		    var dynamic = new UIMenuCheckboxItem(Translation.Translate("Dynamic"), !PropStreamer.StaticProps.Contains(ent.Handle));
		    dynamic.CheckboxEvent += (ite, checkd) =>
		    {
			    if (checkd && PropStreamer.StaticProps.Contains(ent.Handle)) { PropStreamer.StaticProps.Remove(ent.Handle);}
				else if (!checkd && !PropStreamer.StaticProps.Contains(ent.Handle)) PropStreamer.StaticProps.Add(ent.Handle);

			    ent.FreezePosition = PropStreamer.StaticProps.Contains(ent.Handle);
		    };

            var ident = new UIMenuItem("Identification", "Optional identification for easier access during scripting.");
            if (PropStreamer.Identifications.ContainsKey(ent.Handle))
                ident.SetRightLabel(PropStreamer.Identifications[ent.Handle]);

	        ident.Activated += (sender, item) =>
	        {
	            if (PropStreamer.Identifications.ContainsKey(ent.Handle))
	            {
                    var newLabel = Game.GetUserInput(PropStreamer.Identifications[ent.Handle], 20);
	                if (PropStreamer.Identifications.ContainsValue(newLabel))
	                {
	                    UI.Notify(Translation.Translate("~r~~h~Map Editor~h~~w~~n~The identification must be unique!"));
                        return;
	                }

	                if (newLabel.Length > 0 && (Regex.IsMatch(newLabel, @"^\d") || newLabel.StartsWith(".") || newLabel.StartsWith(",") || newLabel.StartsWith("\\")))
	                {
                        UI.Notify(Translation.Translate("~r~~h~Map Editor~h~~w~~n~This identification is invalid!"));
                        return;
                    }

	                PropStreamer.Identifications[ent.Handle] = newLabel;
                    ident.SetRightLabel(newLabel);
                }
	            else
	            {
	                var newLabel = Game.GetUserInput(20);
                    if (PropStreamer.Identifications.ContainsValue(newLabel))
                    {
                        UI.Notify(Translation.Translate("~r~~h~Map Editor~h~~w~~n~The identification must be unique!"));
                        return;
                    }
                    if (newLabel.Length > 0 && (Regex.IsMatch(newLabel, @"^\d") || newLabel.StartsWith(".") || newLabel.StartsWith(",") || newLabel.StartsWith("\\")))
                    {
                        UI.Notify(Translation.Translate("~r~~h~Map Editor~h~~w~~n~This identification is invalid!"));
                        return;
                    }
                    PropStreamer.Identifications.Add(ent.Handle, newLabel);
                    ident.SetRightLabel(newLabel);
	            }
	        };


			_objectInfoMenu.AddItem(posXitem);
			_objectInfoMenu.AddItem(posYitem);
			_objectInfoMenu.AddItem(posZitem);
			_objectInfoMenu.AddItem(rotXitem);
			_objectInfoMenu.AddItem(rotYitem);
			_objectInfoMenu.AddItem(rotZitem);
			_objectInfoMenu.AddItem(dynamic);
            _objectInfoMenu.AddItem(ident);

	        if (Function.Call<bool>(Hash.IS_ENTITY_AN_OBJECT, ent.Handle))
	        {
	            var doorItem = new UIMenuCheckboxItem("Door", PropStreamer.Doors.Contains(ent.Handle), Translation.Translate("This option overrides the \"Dynamic\" setting."));
	            doorItem.CheckboxEvent += (sender, @checked) =>
	            {
	                if (@checked)
	                {
	                    PropStreamer.Doors.Add(ent.Handle);
	                    Function.Call(Hash.SET_ENTITY_DYNAMIC, ent.Handle, false);
	                    ent.FreezePosition = false;
	                }
	                else
	                {
	                    PropStreamer.Doors.Remove(ent.Handle);
	                    var isDynamic = !PropStreamer.StaticProps.Contains(ent.Handle);
                        Function.Call(Hash.SET_ENTITY_DYNAMIC, ent.Handle, isDynamic);
	                    ent.FreezePosition = !isDynamic;
	                }
	            };
                _objectInfoMenu.AddItem(doorItem);
	        }

            if (Function.Call<bool>(Hash.IS_ENTITY_A_PED, ent.Handle))
		    {
				List<dynamic> actions = new List<dynamic> {"None", "Any - Walk", "Any - Warp"};
				actions.AddRange(ObjectDatabase.ScrenarioDatabase.Keys);
			    var scenarioItem = new UIMenuListItem(Translation.Translate("Idle Action"), actions, actions.IndexOf(PropStreamer.ActiveScenarios[ent.Handle]));
			    scenarioItem.OnListChanged += (item, index) =>
			    {
			        PropStreamer.ActiveScenarios[ent.Handle] = item.IndexToItem(index).ToString();
                    _changesMade++;
                };
			    scenarioItem.Activated += (item, index) =>
			    {
				    if (PropStreamer.ActiveScenarios[ent.Handle] == "None")
				    {
					    ((Ped)ent).Task.ClearAll();
					    return;
				    }
					if (PropStreamer.ActiveScenarios[ent.Handle] == "Any - Walk" || PropStreamer.ActiveScenarios[ent.Handle] == "Any")
					{
						Function.Call(Hash.TASK_USE_NEAREST_SCENARIO_TO_COORD, ent.Handle, ent.Position.X, ent.Position.Y, ent.Position.Z, 100f, -1);
						return;
				    }
					if (PropStreamer.ActiveScenarios[ent.Handle] == "Any - Warp")
					{
						Function.Call(Hash.TASK_USE_NEAREST_SCENARIO_TO_COORD_WARP, ent.Handle, ent.Position.X, ent.Position.Y, ent.Position.Z, 100f, -1);
						return;
					}
					string scenario = ObjectDatabase.ScrenarioDatabase[PropStreamer.ActiveScenarios[ent.Handle]];
				    if (Function.Call<bool>(Hash.IS_PED_USING_SCENARIO, ent.Handle, scenario))
					    ((Ped) ent).Task.ClearAll();
				    else
				    {
						Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, ent.Handle, scenario, 0, 0);
				    }
                };
				_objectInfoMenu.AddItem(scenarioItem);

				List<dynamic> rels = new List<dynamic> { "Ballas", "Grove"};
				Enum.GetNames(typeof(Relationship)).ToList().ForEach(rel => rels.Add(rel));
			    var relItem = new UIMenuListItem(Translation.Translate("Relationship"), rels, rels.IndexOf(PropStreamer.ActiveRelationships[ent.Handle]));
			    relItem.OnListChanged += (item, index) =>
			    {
			        PropStreamer.ActiveRelationships[ent.Handle] = item.IndexToItem(index).ToString();
                    _changesMade++;
                };
			    relItem.Activated += (item, index) =>
			    {
				    ObjectDatabase.SetPedRelationshipGroup((Ped) ent, PropStreamer.ActiveRelationships[ent.Handle]);
			    };
				_objectInfoMenu.AddItem(relItem);


				List<dynamic> weps = new List<dynamic>();
				Enum.GetNames(typeof(WeaponHash)).ToList().ForEach(rel => weps.Add(rel));
				var wepItem = new UIMenuListItem(Translation.Translate("Weapon"), weps, weps.IndexOf(PropStreamer.ActiveWeapons[ent.Handle].ToString()));
				wepItem.OnListChanged += (item, index) =>
				{
					PropStreamer.ActiveWeapons[ent.Handle] = Enum.Parse(typeof(WeaponHash), item.IndexToItem(index).ToString());
                    _changesMade++;
                };
				wepItem.Activated += (item, index) =>
				{
					((Ped)ent).Weapons.RemoveAll();
					if(PropStreamer.ActiveWeapons[ent.Handle] == WeaponHash.Unarmed) return;
					((Ped) ent).Weapons.Give(PropStreamer.ActiveWeapons[ent.Handle], 999, true, true);
				};
				_objectInfoMenu.AddItem(wepItem);
			}

		    if (Function.Call<bool>(Hash.IS_ENTITY_A_VEHICLE, ent.Handle))
		    {
			    var sirentBool = new UIMenuCheckboxItem(Translation.Translate("Siren"), PropStreamer.ActiveSirens.Contains(ent.Handle));
			    sirentBool.CheckboxEvent += (item, check) =>
			    {
				    if (check && !PropStreamer.ActiveSirens.Contains(ent.Handle)) PropStreamer.ActiveSirens.Add(ent.Handle);
				    else if (!check && PropStreamer.ActiveSirens.Contains(ent.Handle)) PropStreamer.ActiveSirens.Remove(ent.Handle);
				    ((Vehicle) ent).SirenActive = check;
                    _changesMade++;
                };
				_objectInfoMenu.AddItem(sirentBool);
		    }

	        if (PropStreamer.IsPickup(ent.Handle))
	        {
	            var pickup = PropStreamer.GetPickup(ent.Handle);
	            var amountList = new UIMenuItem(Translation.Translate("Amount"));
                amountList.SetRightLabel(pickup.Amount.ToString());
	            amountList.Activated += (sender, item) =>
	            {
                    string playerInput = Game.GetUserInput(10);
                    int newValue;
                    if (!int.TryParse(playerInput, out newValue) || newValue < -1)
                    {
                        UI.Notify("~r~~h~Map Editor~h~~n~~w~" +
                                  Translation.Translate("Input was not in the correct format."));
                        return;
                    }
	                pickup.SetAmount(newValue);
	                amountList.SetRightLabel(pickup.Amount.ToString());
                    _selectedProp = new Prop(pickup.ObjectHandle);
                    if (_settings.SnapCameraToSelectedObject)
                        _mainCamera.PointAt(_selectedProp);
                };
                _objectInfoMenu.AddItem(amountList);

	            var pickupTypesList =
	                Enum.GetValues(typeof (ObjectDatabase.PickupHash)).Cast<ObjectDatabase.PickupHash>().ToList();
	            var itemIndex = pickupTypesList.IndexOf((ObjectDatabase.PickupHash) pickup.PickupHash);

                var pickupTypeItem = new UIMenuListItem("Type", pickupTypesList.Select(s => (dynamic)(s.ToString())).ToList(), itemIndex);
	            pickupTypeItem.OnListChanged += (sender, index) =>
	            {
                    pickup.SetPickupHash((int)pickupTypesList[index]);
	                _selectedProp = new Prop(pickup.ObjectHandle);
                    if (_settings.SnapCameraToSelectedObject)
                        _mainCamera.PointAt(_selectedProp);
	            };
                _objectInfoMenu.AddItem(pickupTypeItem);

                var timeoutTime = new UIMenuItem("Regeneration Time");
                timeoutTime.SetRightLabel(pickup.Timeout.ToString());

	            timeoutTime.Activated += (sender, item) =>
	            {
	                string playerInput = Game.GetUserInput(10);
	                int newValue;
	                if (!int.TryParse(playerInput, out newValue) || newValue < 0)
	                {
	                    UI.Notify("~r~~h~Map Editor~h~~n~~w~" +
	                              Translation.Translate("Input was not in the correct format."));
                        return;
	                }
	                pickup.Timeout = newValue;
                    timeoutTime.SetRightLabel((newValue).ToString());
	            };

                _objectInfoMenu.AddItem(timeoutTime);
	        }


	        posXitem.OnListChanged += (item, index) =>
	        {
                if (!IsProp(ent) )
                    ent.Position = new Vector3((float) item.IndexToItem(index), ent.Position.Y, ent.Position.Z);
                else
                    ent.PositionNoOffset = new Vector3((float)item.IndexToItem(index), ent.Position.Y, ent.Position.Z);

	            if (PropStreamer.IsPickup(ent.Handle))
	            {
	                PropStreamer.GetPickup(ent.Handle).UpdatePos();
	            }

                _changesMade++;
	        };
			posYitem.OnListChanged += (item, index) =>
			{
                if (!IsProp(ent))
                    ent.Position = new Vector3(ent.Position.X, (float) item.IndexToItem(index), ent.Position.Z);
                else
                    ent.PositionNoOffset = new Vector3(ent.Position.X, (float)item.IndexToItem(index), ent.Position.Z);

                if (PropStreamer.IsPickup(ent.Handle))
                {
                    PropStreamer.GetPickup(ent.Handle).UpdatePos();
                }
                _changesMade++;
            };
			posZitem.OnListChanged += (item, index) =>
			{
                if (!IsProp(ent) )
                    ent.Position = new Vector3(ent.Position.X, ent.Position.Y, (float)item.IndexToItem(index));
                else
                    ent.PositionNoOffset = new Vector3(ent.Position.X, ent.Position.Y, (float)item.IndexToItem(index));

                if (PropStreamer.IsPickup(ent.Handle))
                {
                    PropStreamer.GetPickup(ent.Handle).UpdatePos();
                }
                _changesMade++;
            };

		    rotYitem.OnListChanged += (item, index) =>
		    {
			    var change = (float)item.IndexToItem(index);
				ent.Quaternion = new Vector3(change, ent.Rotation.Y, ent.Rotation.Z).ToQuaternion();
                _changesMade++;
            };

		    rotZitem.OnListChanged += (item, index) =>
		    {
		        var change = (float) item.IndexToItem(index);
                ent.Quaternion = new Vector3(ent.Rotation.X, ent.Rotation.Y, change).ToQuaternion();
                _changesMade++;
            };
			
			rotXitem.OnListChanged += (item, index) =>
			{
				var change = (float)item.IndexToItem(index);
                ent.Quaternion = new Vector3(ent.Rotation.X, change, ent.Rotation.Z).ToQuaternion();
                _changesMade++;
            };

            if (refreshIndex)
                _objectInfoMenu.RefreshIndex();

        }

		private void RedrawObjectInfoMenu(Marker ent, bool refreshIndex)
		{
			if (ent == null) return;
			string name = ent.Type.ToString() + " #" + ent.Id;

			_objectInfoMenu.Subtitle.Caption = "~b~" + name;
			_objectInfoMenu.Clear();

			List<dynamic> possbileScale = new List<dynamic>();
			for (int i = 0; i <= 1000; i++)
			{
				possbileScale.Add(i * 0.01);
			}

			List<dynamic> possibleColors = new List<dynamic>();
			for (int i = 0; i <= 255; i++)
			{
				possibleColors.Add(i);
			}

			var posXitem = new UIMenuListItem(Translation.Translate("Position X"), _possiblePositions, Convert.ToInt32(Math.Round((ent.Position.X * 100) + 500000)));
			var posYitem = new UIMenuListItem(Translation.Translate("Position Y"), _possiblePositions, Convert.ToInt32(Math.Round((ent.Position.Y * 100) + 500000)));
			var posZitem = new UIMenuListItem(Translation.Translate("Position Z"), _possiblePositions, Convert.ToInt32(Math.Round((ent.Position.Z * 100) + 500000)));

			var rotXitem = new UIMenuListItem(Translation.Translate("Rotation X"), _possiblePositions, Convert.ToInt32(Math.Round((ent.Rotation.X * 100) + 500000)));
			var rotYitem = new UIMenuListItem(Translation.Translate("Rotation Y"), _possiblePositions, Convert.ToInt32(Math.Round((ent.Rotation.Y * 100) + 500000)));
			var rotZitem = new UIMenuListItem(Translation.Translate("Rotation Z"), _possiblePositions, Convert.ToInt32(Math.Round((ent.Rotation.Z * 100) + 500000)));

			var dynamic = new UIMenuCheckboxItem(Translation.Translate("Bop Up And Down"), ent.BobUpAndDown);
			dynamic.CheckboxEvent += (ite, checkd) =>
			{
				ent.BobUpAndDown = checkd;
			};

			var faceCam = new UIMenuCheckboxItem(Translation.Translate("Face Camera"), ent.RotateToCamera);
			dynamic.CheckboxEvent += (ite, checkd) =>
			{
				ent.RotateToCamera = checkd;
			};

			var type = new UIMenuListItem(Translation.Translate("Type"), new List<dynamic>(_markersTypes), _markersTypes.ToList().IndexOf(ent.Type.ToString()));
			type.OnListChanged += (ite, index) =>
			{
				MarkerType hash;
				Enum.TryParse(ite.IndexToItem(index), out hash);
				ent.Type = hash;
			};

			var scaleXitem = new UIMenuListItem(Translation.Translate("Scale X"), possbileScale, Convert.ToInt32(Math.Round((ent.Scale.X * 100))));
			var scaleYitem = new UIMenuListItem(Translation.Translate("Scale Y"), possbileScale, Convert.ToInt32(Math.Round((ent.Scale.Y * 100))));
			var scaleZitem = new UIMenuListItem(Translation.Translate("Scale Z"), possbileScale, Convert.ToInt32(Math.Round((ent.Scale.Z * 100))));

			var colorR = new UIMenuListItem(Translation.Translate("Red Color"), possibleColors, ent.Red);
			var colorG = new UIMenuListItem(Translation.Translate("Green Color"), possibleColors, ent.Green);
			var colorB = new UIMenuListItem(Translation.Translate("Blue Color"), possibleColors, ent.Blue);
			var colorA = new UIMenuListItem(Translation.Translate("Transparency"), possibleColors, ent.Alpha);

		    var targetId = 0;

		    if (ent.TeleportTarget.HasValue)
		    {
		        var ourMarkers =
		            PropStreamer.Markers.Where(m => (m.Position - ent.TeleportTarget.Value).Length() < 1f)
		                .OrderBy(m => (m.Position - ent.TeleportTarget.Value).Length());
		        if (ourMarkers.Any())
		            targetId = ourMarkers.First().Id + 1;
		    }

		    var targetPos = new UIMenuListItem(Translation.Translate("Teleport Marker Target"),
		        Enumerable.Range(-1, _markerCounter + 1).Select(n => (dynamic) n).ToList(), targetId);
		    targetPos.OnListChanged += (sender, index) =>
		    {
		        if (index == 0)
		        {
		            ent.TeleportTarget = null;
                    return;
		        }

		        ent.TeleportTarget = PropStreamer.Markers.FirstOrDefault(n => n.Id == index-1)?.Position;
		    };

			_objectInfoMenu.AddItem(type);
			_objectInfoMenu.AddItem(posXitem);
			_objectInfoMenu.AddItem(posYitem);
			_objectInfoMenu.AddItem(posZitem);
			_objectInfoMenu.AddItem(rotXitem);
			_objectInfoMenu.AddItem(rotYitem);
			_objectInfoMenu.AddItem(rotZitem);
			_objectInfoMenu.AddItem(scaleXitem);
			_objectInfoMenu.AddItem(scaleYitem);
			_objectInfoMenu.AddItem(scaleZitem);
			_objectInfoMenu.AddItem(colorR);
			_objectInfoMenu.AddItem(colorG);
			_objectInfoMenu.AddItem(colorB);
			_objectInfoMenu.AddItem(colorA);
			_objectInfoMenu.AddItem(dynamic);
			_objectInfoMenu.AddItem(faceCam);
            _objectInfoMenu.AddItem(targetPos);


            posXitem.OnListChanged += (item, index) => ent.Position = new Vector3((float)item.IndexToItem(index), ent.Position.Y, ent.Position.Z);
			posYitem.OnListChanged += (item, index) => ent.Position = new Vector3(ent.Position.X, (float)item.IndexToItem(index), ent.Position.Z);
			posZitem.OnListChanged += (item, index) => ent.Position = new Vector3(ent.Position.X, ent.Position.Y, (float)item.IndexToItem(index));

			rotXitem.OnListChanged += (item, index) => ent.Rotation = new Vector3((float)item.IndexToItem(index), ent.Rotation.Y, ent.Rotation.Z);
			rotYitem.OnListChanged += (item, index) => ent.Rotation = new Vector3(ent.Rotation.X, (float)item.IndexToItem(index), ent.Rotation.Z);
			rotZitem.OnListChanged += (item, index) => ent.Rotation = new Vector3(ent.Rotation.X, ent.Rotation.Y, (float)item.IndexToItem(index));

			scaleXitem.OnListChanged += (item, index) => ent.Scale = new Vector3((float)item.IndexToItem(index), ent.Scale.Y, ent.Scale.Z);
			scaleXitem.OnListChanged += (item, index) => ent.Scale = new Vector3(ent.Scale.X, (float)item.IndexToItem(index), ent.Scale.Z);
			scaleXitem.OnListChanged += (item, index) => ent.Scale = new Vector3(ent.Scale.X, ent.Scale.Y, (float)item.IndexToItem(index));

			colorR.OnListChanged += (item, index) => ent.Red = index;
			colorG.OnListChanged += (item, index) => ent.Green = index;
			colorB.OnListChanged += (item, index) => ent.Blue = index;
			colorA.OnListChanged += (item, index) => ent.Alpha = index;

            if (refreshIndex)
                _objectInfoMenu.RefreshIndex();
        }

        public static bool IsPed(Entity ent)
        {
            return Function.Call<bool>(Hash.IS_ENTITY_A_PED, ent);
        }

        public static bool IsVehicle(Entity ent)
        {
            return Function.Call<bool>(Hash.IS_ENTITY_A_VEHICLE, ent);
        }

        public static bool IsProp(Entity ent)
        {
            return Function.Call<bool>(Hash.IS_ENTITY_AN_OBJECT, ent);
        }

        public void ValidateDatabase()
	    {
		    // Validate object list.
		    Dictionary<string, int> tmpDict = new Dictionary<string, int>();
		    int counter = 0;
		    while (counter < ObjectDatabase.MainDb.Count)
		    {
			    var pair = ObjectDatabase.MainDb.ElementAt(counter);
			    counter++;
		        UI.ShowSubtitle((counter) + "/" + ObjectDatabase.MainDb.Count + " done. (" +
		            (counter/(float) ObjectDatabase.MainDb.Count)*100 +
		            "%)\nValid objects: " + tmpDict.Count, 2000);
                Yield();
			    
		        var model = new Model(pair.Value);
		        model.Request(100);
		        if (!model.IsLoaded)
		        {
                    model.MarkAsNoLongerNeeded();
		            continue;
		        }
                model.MarkAsNoLongerNeeded();
                if (!tmpDict.ContainsKey(pair.Key))
				    tmpDict.Add(pair.Key, pair.Value);
		    }
			string output = tmpDict.Aggregate("", (current, pair) => current + (pair.Key + "=" + pair.Value + "\r\n"));
		    File.WriteAllText("scripts\\ObjectList.ini", output);
	    }

	    public void AddItemToEntityMenu(MapObject obj)
	    {
		    if(obj == null) return;
			var name = ObjectDatabase.MainDb.ContainsValue(obj.Hash) ? ObjectDatabase.MainDb.First(pair => pair.Value == obj.Hash).Key : "Unknown World Prop";
			_currentObjectsMenu.AddItem(new UIMenuItem("~h~[WORLD]~h~ " + name, obj.Id.ToString()));
			_currentObjectsMenu.RefreshIndex();
		}

		public void AddItemToEntityMenu(Marker mark)
		{
			if (mark == null) return;
			var name = mark.Type.ToString();
			_currentObjectsMenu.AddItem(new UIMenuItem("~h~[MARK]~h~ " + name, "marker-" + mark.Id));
			_currentObjectsMenu.RefreshIndex();
		}

        public void AddItemToEntityMenu(DynamicPickup pickup)
        {
            if (pickup == null) return;
            var name = pickup.PickupName;
            _currentObjectsMenu.AddItem(new UIMenuItem("~h~[PICKUP]~h~ " + name, "pickup-" + pickup.UID));
            _currentObjectsMenu.RefreshIndex();
        }

        public void AddItemToEntityMenu(Entity ent)
	    {
			if(ent == null) return;
		    var name = "";
		    var type = "";
		    if (ent is Prop)
		    {
			    name = ObjectDatabase.MainDb.ContainsValue(ent.Model.Hash) ? ObjectDatabase.MainDb.First(pair => pair.Value == ent.Model.Hash).Key : "Unknown Prop";
			    type = "~h~[PROP]~h~ ";
		    }
		    else if (ent is Vehicle)
			{
				name = ObjectDatabase.VehicleDb.ContainsValue(ent.Model.Hash) ? ObjectDatabase.VehicleDb.First(x => x.Value == ent.Model.Hash).Key.ToUpper() : "Unknown Vehicle";
				type = "~h~[VEH]~h~ ";
			}
			else if (ent is Ped)
			{
				name = ObjectDatabase.PedDb.ContainsValue(ent.Model.Hash) ? ObjectDatabase.PedDb.First(x => x.Value == ent.Model.Hash).Key.ToUpper() : "Unknown Ped";
				type = "~h~[PED]~h~ ";
			}
			_currentObjectsMenu.AddItem(new UIMenuItem(type + name, ent.Handle.ToString()));
			_currentObjectsMenu.RefreshIndex();
	    }

	    public void RemoveItemFromEntityMenu(Entity ent)
	    {
	        if (PropStreamer.IsPickup(ent.Handle))
	        {
                var found = _currentObjectsMenu.MenuItems.FirstOrDefault(item => item.Description == "pickup-" + PropStreamer.GetPickup(ent.Handle).UID);
                if (found == null) return;
                _currentObjectsMenu.RemoveItemAt(_currentObjectsMenu.MenuItems.IndexOf(found));
                if (_currentObjectsMenu.Size != 0)
                    _currentObjectsMenu.RefreshIndex();
            }
	        else
	        {
	            var found = _currentObjectsMenu.MenuItems.FirstOrDefault(item => item.Description == ent.Handle.ToString());
	            if (found == null) return;
	            _currentObjectsMenu.RemoveItemAt(_currentObjectsMenu.MenuItems.IndexOf(found));
	            if (_currentObjectsMenu.Size != 0)
	                _currentObjectsMenu.RefreshIndex(); //TODO: fix this, selected item remains after refresh.
	        }
	    }

	    public void RemoveItemFromEntityMenu(string id)
	    {
		    var found = _currentObjectsMenu.MenuItems.FirstOrDefault(item => item.Description == id);
			if(found == null) return;
			_currentObjectsMenu.RemoveItemAt(_currentObjectsMenu.MenuItems.IndexOf(found));
		    if (_currentObjectsMenu.MenuItems.Count > 0)
			    _currentObjectsMenu.RefreshIndex();
		    else
			    _currentObjectsMenu.Visible = false;
	    }

		public void RemoveMarkerFromEntityMenu(int id)
		{
			var found = _currentObjectsMenu.MenuItems.FirstOrDefault(item => item.Description == "marker-" + id);
			if (found == null) return;
			_currentObjectsMenu.RemoveItemAt(_currentObjectsMenu.MenuItems.IndexOf(found));
			if (_currentObjectsMenu.Size != 0)
				_currentObjectsMenu.RefreshIndex();
			else
				_currentObjectsMenu.Visible = false;
		}

		public void OnEntityTeleport(UIMenu menu, UIMenuItem item, int index)
	    {
            if (!_isInFreecam) return;
		    if (item.Text.StartsWith("~h~[PICKUP]~h~"))
		    {
		        var uid = int.Parse(item.Description.Substring(7));
		        var pickup = PropStreamer.GetPickupByUID(uid);
                if (_settings.SnapCameraToSelectedObject)
                {
                    _mainCamera.Position = pickup.RealPosition + new Vector3(5f, 5f, 10f);
                    _mainCamera.PointAt(pickup.RealPosition);
                }
                _menuPool.CloseAllMenus();
                Script.Wait(300);
                _selectedProp = new Prop(pickup.ObjectHandle);
                RedrawObjectInfoMenu(_selectedProp, true);
                _objectInfoMenu.Visible = true;
		        return;
		    }

		    if (item.Text.StartsWith("~h~[WORLD]~h~ "))
		    {
			    var mapObj = PropStreamer.RemovedObjects.FirstOrDefault(obj => obj.Id == item.Description);
				if(mapObj == null) return;
			    var t = World.CreateProp(mapObj.Hash, mapObj.Position, mapObj.Rotation, true, false);
			    t.PositionNoOffset = mapObj.Position;
				_menuPool.CloseAllMenus();
				RemoveItemFromEntityMenu(mapObj.Id);
			    PropStreamer.RemovedObjects.Remove(mapObj);
			    return;
		    }
			if (item.Text.StartsWith("~h~[MARK]~h~ "))
			{
				Marker tmpM = PropStreamer.Markers.FirstOrDefault(m => item.Description == "marker-" + m.Id);
				if(tmpM == null) return;
				_mainCamera.Position = tmpM.Position + new Vector3(5f, 5f, 10f);
				if(_settings.SnapCameraToSelectedObject)
					_mainCamera.PointAt(tmpM.Position);
				_menuPool.CloseAllMenus();
				_selectedMarker = tmpM;
				RedrawObjectInfoMenu(_selectedMarker, true);
				_objectInfoMenu.Visible = true;
				return;
			}
		    var prop = new Prop(int.Parse(item.Description, CultureInfo.InvariantCulture));
			if(!prop.Exists()) return;
		    if (_settings.SnapCameraToSelectedObject)
		    {
		        _mainCamera.Position = prop.Position + new Vector3(5f, 5f, 10f);
				_mainCamera.PointAt(prop);
		    }
			_menuPool.CloseAllMenus();
			_selectedProp = prop;
			RedrawObjectInfoMenu(_selectedProp, true);
			_objectInfoMenu.Visible = true;
	    }
	}
}
