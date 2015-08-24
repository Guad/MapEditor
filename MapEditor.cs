using System;
using System.Collections.Generic;
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
using System.Security;
using System.Xml.Serialization;
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
        
        private Camera _mainCamera;
        private Camera _objectPreviewCamera;

        private readonly Vector3 _objectPreviewPos = new Vector3(1200.133f, 4000.958f, 85.9f);

        private bool _zAxis = true;
	    private bool _controlsRotate;
		
	    private readonly string _crosshairPath;
	    private readonly string _crosshairBluePath;
	    private readonly string _crosshairYellowPath;

	    private bool _savingMap;
	    private bool _hasLoaded;
	    private int _mapObjCounter = 0;
	    

	    private ObjectTypes _currentObjectType;
		
	    private Settings _settings;

	    public enum Marker
	    {
		    Crosshair,
			Orb,
			None,
	    }
		
        public MapEditor()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;

			_scaleform = new Scaleform(0);
			_scaleform.Load("instructional_buttons");

			_objectInfoMenu = new UIMenu("", "~b~PROPERTIES", new Point(0, -107));
			_objectInfoMenu.ResetKey(UIMenu.MenuControls.Back);
			_objectInfoMenu.DisableInstructionalButtons(true);
			_objectInfoMenu.SetBannerType(new UIResRectangle(new Point(), new Size()));
			_menuPool.Add(_objectInfoMenu);

			LoadSettings();

			_objectsMenu = new UIMenu("Map Editor", "~b~PLACE OBJECT");

            ObjectDatabase.LoadFromFile("scripts\\ObjectList.ini");
			ObjectDatabase.LoadInvalidHashes();
			ObjectDatabase.LoadEnumDatabases();
			
			_crosshairPath = Sprite.WriteFileFromResources(Assembly.GetExecutingAssembly(), "MapEditor.crosshair.png");
			_crosshairBluePath = Sprite.WriteFileFromResources(Assembly.GetExecutingAssembly(), "MapEditor.crosshair_blue.png");
			_crosshairYellowPath = Sprite.WriteFileFromResources(Assembly.GetExecutingAssembly(), "MapEditor.crosshair_yellow.png");


			RedrawObjectsMenu();
            _objectsMenu.OnItemSelect += OnObjectSelect;
            _objectsMenu.OnIndexChange += OnIndexChange;
            _menuPool.Add(_objectsMenu);

			_objectsMenu.ResetKey(UIMenu.MenuControls.Back);
            _objectsMenu.AddInstructionalButton(new InstructionalButton(Control.SelectWeapon, "Change Axis"));
            _objectsMenu.AddInstructionalButton(new InstructionalButton(Control.MoveUpDown, "Zoom"));
            _objectsMenu.AddInstructionalButton(new InstructionalButton(Control.Jump, "Search"));

            _mainMenu = new UIMenu("Map Editor", "~b~MAIN MENU");
            _mainMenu.AddItem(new UIMenuItem("Enter/Exit Map Editor"));
            _mainMenu.AddItem(new UIMenuItem("New Map", "Remove all current objects and start a new map."));
            _mainMenu.AddItem(new UIMenuItem("Save Map", "Save all current objects to a file."));
			_mainMenu.AddItem(new UIMenuItem("Load Map", "Load objects from a file and add them to the world."));
			

	        var validate = new UIMenuItem("Validate Object Database",
		        "This will update the current object database, removing any invalid objects. The changes will take effect after you restart the script." +
		        " It will take a couple of minutes.");
	        validate.Activated += (men, item) => ValidateDatabase();
			
			_mainMenu.AddItem(validate);
			_mainMenu.RefreshIndex();
			_mainMenu.DisableInstructionalButtons(true);
            _menuPool.Add(_mainMenu);

			_formatMenu = new UIMenu("Map Editor", "~b~SELECT FORMAT");
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
                        if (!_isInFreecam) return;
                        World.DestroyAllCameras();
                        _mainCamera = World.CreateCamera(GameplayCamera.Position, GameplayCamera.Rotation, 60f);
						_objectPreviewCamera = World.CreateCamera(new Vector3(1200.016f, 3980.998f, 86.05062f), new Vector3(0f, 0f, 0f), 60f);
						World.RenderingCamera = _mainCamera;
                        break;
                    case 1:
						PropStreamer.RemoveAll();
						_currentObjectsMenu.Clear();
		                foreach (MapObject o in PropStreamer.RemovedObjects)
		                {
			                var t = World.CreateProp(o.Hash, o.Position, o.Rotation, true, false);
			                t.Position = o.Position;
		                }
						PropStreamer.RemovedObjects.Clear();
						UI.Notify("~b~~h~Map Editor~h~~w~~n~Loaded new map.");
						break;
					case 2:
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
							SaveMap(filename, MapSerializer.Format.CSharpCode);
							break;
						case 3: // Raw
							SaveMap(filename, MapSerializer.Format.Raw);
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
			        }
					LoadMap(filename, tmpFor);
				}
		        _formatMenu.Visible = false;
	        };

			_settingsMenu = new UIMenu("Map Editor", "~b~SETTINGS");


			var checkem = new UIMenuListItem("Marker", new List<dynamic>(Enum.GetNames(typeof(Marker))), Enum.GetNames(typeof(Marker)).ToList().FindIndex(x => x == _settings.Marker.ToString()));
			checkem.OnListChanged += (i, indx) =>
			{
				Marker outHash;
				Enum.TryParse(i.IndexToItem(indx).ToString(), out outHash);
				_settings.Marker = outHash;
				SaveSettings();
			};
			List<dynamic> senslist = new List<dynamic>();
			for (int i = 1; i < 60; i++)
			{
				senslist.Add(i);
			}
			var gamboy = new UIMenuListItem("Camera Sensitivity", senslist, _settings.CameraSensivity - 1);
			gamboy.OnListChanged += (item, index) =>
			{
				_settings.CameraSensivity = index + 1;
				SaveSettings();
			};
			var butts = new UIMenuCheckboxItem("Instructional Buttons", _settings.InstructionalButtons);
			butts.CheckboxEvent += (i, checkd) =>
			{
				_settings.InstructionalButtons = checkd;
				SaveSettings();
			};
	        var gamepadItem = new UIMenuCheckboxItem("Use Gamepad", _settings.Gamepad);
	        gamepadItem.CheckboxEvent += (i, checkd) =>
	        {
		        _settings.CameraSensivity = checkd ? 5 : 30;
		        _settings.Gamepad = checkd;
		        gamboy.Index = _settings.CameraSensivity - 1;
				SaveSettings();
	        };

	        var counterItem = new UIMenuCheckboxItem("Entity Counter", _settings.PropCounterDisplay);
	        counterItem.CheckboxEvent += (i, checkd) =>
	        {
		        _settings.PropCounterDisplay = checkd;
				SaveSettings();
	        };

	        var snapper = new UIMenuCheckboxItem("Follow Object With Camera", _settings.SnapCameraToSelectedObject);
	        snapper.CheckboxEvent += (i, checkd) =>
	        {
		        _settings.SnapCameraToSelectedObject = checkd;
				SaveSettings();
	        };
			
			_settingsMenu.AddItem(gamepadItem);
			_settingsMenu.AddItem(checkem);
			_settingsMenu.AddItem(gamboy);
			_settingsMenu.AddItem(butts);
			_settingsMenu.AddItem(counterItem);
	        _settingsMenu.AddItem(snapper);
			_settingsMenu.RefreshIndex();
			_settingsMenu.DisableInstructionalButtons(true);
			_menuPool.Add(_settingsMenu);


			_currentObjectsMenu = new UIMenu("Map Editor", "~b~CURRENT ENTITES");
	        _currentObjectsMenu.OnItemSelect += OnEntityTeleport;
			_currentObjectsMenu.DisableInstructionalButtons(true);
            _menuPool.Add(_currentObjectsMenu);


	        var binder = new UIMenuItem("Settings");
	        _currentEntitiesItem = new UIMenuItem("Current Entities");

			_mainMenu.AddItem(_currentEntitiesItem);
            _mainMenu.AddItem(binder);

			_mainMenu.BindMenuToItem(_settingsMenu, binder);
			_mainMenu.BindMenuToItem(_currentObjectsMenu, _currentEntitiesItem);
			_mainMenu.RefreshIndex();
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
		    }
		    else
		    {
			    _settings = new Settings()
			    {
				    CameraSensivity = 30,
					Gamepad = true,
					InstructionalButtons = true,
					Marker = Marker.Crosshair,
					PropCounterDisplay = true,
					SnapCameraToSelectedObject = true,
					ActivationKey = Keys.F7,
			    };
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
					
				UI.Notify("~b~~h~Map Editor~h~~w~~n~The filename was empty or the file does not exist!");
				return;
			}
			var des = new MapSerializer();
		    try
		    {
			    var map2Load = des.Deserialize(filename, format);
			    if (map2Load == null) return;
			    foreach (MapObject o in map2Load.Objects)
			    {
				    if (o.Type == ObjectTypes.Prop)
					    AddItemToEntityMenu(PropStreamer.CreateProp(ObjectPreview.LoadObject(o.Hash), o.Position, o.Rotation, o.Dynamic,
						    o.Quaternion == new Quaternion() {X = 0, Y = 0, Z = 0, W = 0} ? null : o.Quaternion));
				    else if (o.Type == ObjectTypes.Vehicle)
					    AddItemToEntityMenu(PropStreamer.CreateVehicle(ObjectPreview.LoadObject(o.Hash), o.Position, o.Rotation.Z,
						    o.Dynamic));
				    else if (o.Type == ObjectTypes.Ped)
					    AddItemToEntityMenu(PropStreamer.CreatePed(ObjectPreview.LoadObject(o.Hash),
						    o.Position - new Vector3(0f, 0f, 1f), o.Rotation.Z, o.Dynamic));
			    }
			    foreach (MapObject o in map2Load.RemoveFromWorld)
			    {
				    PropStreamer.RemovedObjects.Add(o);
				    Prop returnedProp = Function.Call<Prop>(Hash.GET_CLOSEST_OBJECT_OF_TYPE, o.Position.X, o.Position.Y,
					    o.Position.Z, 1f, o.Hash, 0);
				    if (returnedProp == null || returnedProp.Handle == 0) continue;
				    returnedProp.Delete();
			    }
			    UI.Notify("~b~~h~Map Editor~h~~w~~n~Loaded map ~h~" + filename + "~h~.");
		    }
		    catch (Exception e)
		    {
				UI.Notify("~r~~h~Map Editor~h~~w~~n~Map failed to load, see error below.");
				UI.Notify(e.Message);
			}
	    }

	    private void SaveMap(string filename, MapSerializer.Format format)
	    {
			if (String.IsNullOrWhiteSpace(filename))
			{
				UI.Notify("~b~~h~Map Editor~h~~w~~n~The filename was empty!");
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
				ser.Serialize(filename, tmpmap, format);
				UI.Notify("~b~~h~Map Editor~h~~w~~n~Saved current map as ~h~" + filename + "~h~.");
			}
			catch (Exception e)
			{
				UI.Notify("~r~~h~Map Editor~h~~w~~n~Map failed to save, see error below.");
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

			if (PropStreamer.EntityCount > 0 || PropStreamer.RemovedObjects.Count > 0)
			{
				_currentEntitiesItem.Enabled = true;
				_currentEntitiesItem.Description = "";
			}
			else
			{
				_currentEntitiesItem.Enabled = false;
				_currentEntitiesItem.Description = "There are no current entities.";
			}

			if (Game.IsControlPressed(0, Control.LookBehind) && Game.IsControlJustPressed(0, Control.FrontendLb) && !_menuPool.IsAnyMenuOpen() && _settings.Gamepad)
			{
				_mainMenu.Visible = !_mainMenu.Visible;
			}

            if (!_isInFreecam) return;
			if(_settings.InstructionalButtons && !_objectsMenu.Visible)
				_scaleform.Render2D();
			Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.CharacterWheel);
			Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.SelectWeapon);
			Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.FrontendPause);
			Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.NextCamera);

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
                _objectsMenu.Visible = true;
                OnIndexChange(_objectsMenu, _objectsMenu.CurrentSelection);
				_objectsMenu.Subtitle.Caption = "~b~PLACE " + _currentObjectType.ToString().ToUpper();
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
				_objectsMenu.Visible = true;
				OnIndexChange(_objectsMenu, _objectsMenu.CurrentSelection);
				_objectsMenu.Subtitle.Caption = "~b~PLACE " + _currentObjectType.ToString().ToUpper();
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
				_objectsMenu.Visible = true;
				OnIndexChange(_objectsMenu, _objectsMenu.CurrentSelection);
				_objectsMenu.Subtitle.Caption = "~b~PLACE " + _currentObjectType.ToString().ToUpper();
			}

			if (_isChoosingObject)
            {
                if (_previewProp != null)
                {
                    _previewProp.Rotation = _previewProp.Rotation + (_zAxis ? new Vector3(0f, 0f, 2.5f) : new Vector3(2.5f, 0f, 0f));
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
                World.RenderingCamera = _objectPreviewCamera;

                if (Game.IsControlJustPressed(0, Control.PhoneCancel) && !_searchResultsOn)
                {
                    _isChoosingObject = false;
                    _objectsMenu.Visible = false;
                    _previewProp?.Delete();
                }

                if (Game.IsControlJustPressed(0, Control.PhoneCancel) && _searchResultsOn)
                {
                    RedrawObjectsMenu(type: _currentObjectType);
                    OnIndexChange(_objectsMenu, 0);
                    _searchResultsOn = false;
                    _objectsMenu.Subtitle.Caption = "~b~PLACE " + _currentObjectType.ToString().ToUpper();
                }

                if (Game.IsControlJustPressed(0, Control.Jump))
                {
                    string query = Game.GetUserInput(255);
                    RedrawObjectsMenu(query, _currentObjectType);
                    if(_objectsMenu.Size != 0)
                        OnIndexChange(_objectsMenu, 0);
                    _objectsMenu.Subtitle.Caption = "~b~SEARCH RESULTS FOR \"" + query.ToUpper() + "\"";
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

				new UIResText("WORLD", new Point(Convert.ToInt32(res.Width) - safe.X - 180, Convert.ToInt32(res.Height) - safe.Y - (90 + (3 * interval))), 0.3f, Color.White).Draw();
				new UIResText(PropStreamer.RemovedObjects.Count.ToString(), new Point(Convert.ToInt32(res.Width) - safe.X - 20, Convert.ToInt32(res.Height) - safe.Y - (102 + (3 * interval))), 0.5f, Color.White, Font.ChaletLondon, UIResText.Alignment.Right).Draw();
				new Sprite("timerbars", "all_black_bg", new Point(Convert.ToInt32(res.Width) - safe.X - 248, Convert.ToInt32(res.Height) - safe.Y - (100 + (3 * interval))), new Size(250, 37)).Draw();

				new UIResText("PROPS", new Point(Convert.ToInt32(res.Width) - safe.X - 180, Convert.ToInt32(res.Height) - safe.Y - (90 + (2*interval))), 0.3f, Color.White).Draw();
				new UIResText(PropStreamer.PropCount.ToString(), new Point(Convert.ToInt32(res.Width) - safe.X - 20, Convert.ToInt32(res.Height) - safe.Y - (102 + (2 * interval))), 0.5f, Color.White, Font.ChaletLondon, UIResText.Alignment.Right).Draw();
				new Sprite("timerbars", "all_black_bg", new Point(Convert.ToInt32(res.Width) - safe.X - 248, Convert.ToInt32(res.Height) - safe.Y - (100 + (2 * interval))), new Size(250, 37)).Draw();

				new UIResText("VEHICLES", new Point(Convert.ToInt32(res.Width) - safe.X - 180, Convert.ToInt32(res.Height) - safe.Y - (90 + interval)), 0.3f, Color.White).Draw();
				new UIResText(PropStreamer.Vehicles.Count.ToString(), new Point(Convert.ToInt32(res.Width) - safe.X - 20, Convert.ToInt32(res.Height) - safe.Y - (102 + interval)), 0.5f, Color.White, Font.ChaletLondon, UIResText.Alignment.Right).Draw();
				new Sprite("timerbars", "all_black_bg", new Point(Convert.ToInt32(res.Width) - safe.X - 248, Convert.ToInt32(res.Height) - safe.Y - (100 + interval)), new Size(250, 37)).Draw();
				
				new UIResText("PEDS", new Point(Convert.ToInt32(res.Width) - safe.X - 180, Convert.ToInt32(res.Height) - safe.Y - 90), 0.3f, Color.White).Draw();
				new UIResText(PropStreamer.Peds.Count.ToString(), new Point(Convert.ToInt32(res.Width) - safe.X - 20, Convert.ToInt32(res.Height) - safe.Y - 102), 0.5f, Color.White, Font.ChaletLondon, UIResText.Alignment.Right).Draw();
				new Sprite("timerbars", "all_black_bg", new Point(Convert.ToInt32(res.Width) - safe.X - 248, Convert.ToInt32(res.Height) - safe.Y - 100), new Size(250, 37)).Draw();

				
			}

			int wi = Convert.ToInt32(res.Width*0.5);
			int he = Convert.ToInt32(res.Height * 0.5);
			
			Entity hitEnt = VectorExtensions.RaycastEntity(new Vector2(0f, 0f), _mainCamera.Position, _mainCamera.Rotation);

			if (_settings.Marker == Marker.Crosshair)
			{
				var crossColor = _crosshairPath;
				if (hitEnt != null && hitEnt.Handle != 0 && !PropStreamer.GetAllHandles().Contains(hitEnt.Handle))
					crossColor = _crosshairBluePath;
				else if (hitEnt != null && hitEnt.Handle != 0 && PropStreamer.GetAllHandles().Contains(hitEnt.Handle))
					crossColor = _crosshairYellowPath;
				Sprite.DrawTexture(crossColor, new Point(wi - 15, he - 15), new Size(30, 30));
			}

			Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.CharacterWheel);
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.SelectWeapon);
			Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.FrontendPause);

			var mouseX = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)Control.LookLeftRight);
			var mouseY = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)Control.LookUpDown);


			mouseX *= -1;
			mouseY *= -1;

            mouseX *= _settings.CameraSensivity;
            mouseY *= _settings.CameraSensivity;
            

            float modifier = 1f;
            if (Game.IsControlPressed(0, Control.Sprint))
                modifier = 5f;
            else if (Game.IsControlPressed(0, Control.CharacterWheel))
                modifier = 0.3f;


			if (_selectedProp == null)
            {
                _mainCamera.Rotation = new Vector3(_mainCamera.Rotation.X + mouseY, _mainCamera.Rotation.Y, _mainCamera.Rotation.Z + mouseX);
				
	            var dir = VectorExtensions.RotationToDirection(_mainCamera.Rotation);
				var rotLeft = _mainCamera.Rotation + new Vector3(0, 0, -10);
				var rotRight = _mainCamera.Rotation + new Vector3(0, 0, 10);
				var right = VectorExtensions.RotationToDirection(rotRight) - VectorExtensions.RotationToDirection(rotLeft);

				if (Game.IsControlPressed(0, Control.MoveUpOnly))
                {
                    _mainCamera.Position += dir*modifier;
                }
                if (Game.IsControlPressed(0, Control.MoveDownOnly))
                {
                    _mainCamera.Position -= dir*modifier;
                }
                if (Game.IsControlPressed(0, Control.MoveLeftOnly))
                {
                    _mainCamera.Position += right*modifier;
                }
                if (Game.IsControlPressed(0, Control.MoveRightOnly))
                {
                    _mainCamera.Position -= right*modifier;
                }
                Game.Player.Character.Position = _mainCamera.Position - dir*8f;

                if (_snappedProp != null)
                {
                    _snappedProp.Position = VectorExtensions.RaycastEverything(new Vector2(0f, 0f), _mainCamera.Position, _mainCamera.Rotation, _snappedProp);
                    if (Game.IsControlPressed(0, Control.CursorScrollUp) || Game.IsControlPressed(0, Control.FrontendRb))
                    {
                        _snappedProp.Rotation = _snappedProp.Rotation + new Vector3(0f, 0f, modifier);
                    }

                    if (Game.IsControlPressed(0, Control.CursorScrollDown) || Game.IsControlPressed(0, Control.FrontendLb))
                    {
                        _snappedProp.Rotation = _snappedProp.Rotation - new Vector3(0f, 0f, modifier);
                    }

					if (Game.IsControlJustPressed(0, Control.CreatorDelete))
					{
						RemoveItemFromEntityMenu(_snappedProp);
						PropStreamer.RemoveEntity(_snappedProp.Handle);
						_snappedProp = null;
					}

					if (Game.IsControlJustPressed(0, Control.Attack))
                    {
                        _snappedProp = null;
                    }
					InstructionalButtonsStart();
					InstructionalButtonsSnapped();
					InstructionalButtonsEnd();
				}
                else
                {
	                if (_settings.Marker == Marker.Orb)
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
							if (Function.Call<bool>(Hash.IS_ENTITY_AN_OBJECT, hitEnt.Handle))
								_snappedProp = new Prop(hitEnt.Handle);
							else if (Function.Call<bool>(Hash.IS_ENTITY_A_VEHICLE, hitEnt.Handle))
								_snappedProp = new Vehicle(hitEnt.Handle);
							else if (Function.Call<bool>(Hash.IS_ENTITY_A_PED, hitEnt.Handle))
								_snappedProp = new Ped(hitEnt.Handle);
						}
                    }

                    if (Game.IsControlJustPressed(0, Control.Attack))
                    {
                        if (hitEnt != null && PropStreamer.GetAllHandles().Contains(hitEnt.Handle))
                        {
							if (Function.Call<bool>(Hash.IS_ENTITY_AN_OBJECT, hitEnt.Handle))
								_selectedProp = new Prop(hitEnt.Handle);
							else if (Function.Call<bool>(Hash.IS_ENTITY_A_VEHICLE, hitEnt.Handle))
								_selectedProp = new Vehicle(hitEnt.Handle);
							else if (Function.Call<bool>(Hash.IS_ENTITY_A_PED, hitEnt.Handle))
								_selectedProp = new Ped(hitEnt.Handle);
							RedrawObjectInfoMenu(_selectedProp);
							_menuPool.CloseAllMenus();
							_objectInfoMenu.Visible = true;
							if(_settings.SnapCameraToSelectedObject)
								_mainCamera.PointAt(_selectedProp);
						}
                    }

	                if (Game.IsControlJustReleased(0, Control.LookBehind))
	                {
						if (hitEnt != null)
						{
							if (Function.Call<bool>(Hash.IS_ENTITY_AN_OBJECT, hitEnt.Handle))
								AddItemToEntityMenu(_snappedProp = PropStreamer.CreateProp(hitEnt.Model, hitEnt.Position, hitEnt.Rotation, !PropStreamer.StaticProps.Contains(hitEnt.Handle), force: true));
							else if (Function.Call<bool>(Hash.IS_ENTITY_A_VEHICLE, hitEnt.Handle))
								AddItemToEntityMenu(_snappedProp = PropStreamer.CreateVehicle(hitEnt.Model, hitEnt.Position, hitEnt.Rotation.Z, !PropStreamer.StaticProps.Contains(hitEnt.Handle)));
							else if (Function.Call<bool>(Hash.IS_ENTITY_A_PED, hitEnt.Handle))
							{
								AddItemToEntityMenu(_snappedProp = Function.Call<Ped>(Hash.CLONE_PED, ((Ped)hitEnt).Handle, hitEnt.Rotation.Z, 1, 1));
								if(_snappedProp != null)
									PropStreamer.Peds.Add(_snappedProp.Handle);
							}
						}
					}

					if (Game.IsControlJustPressed(0, Control.CreatorDelete))
					{
						if (hitEnt != null && PropStreamer.GetAllHandles().Contains(hitEnt.Handle))
						{
							RemoveItemFromEntityMenu(hitEnt);
							PropStreamer.RemoveEntity(hitEnt.Handle);
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
								Id = _mapObjCounter,
							};
							_mapObjCounter++;
							PropStreamer.RemovedObjects.Add(tmpObj);
							AddItemToEntityMenu(tmpObj);
							hitEnt.Delete();
						}
					}
					InstructionalButtonsStart();
					InstructionalButtonsFreelook();
					InstructionalButtonsEnd();
				}
            }
            else //_selectedProp isnt null
            {
	            var tmp = _controlsRotate ? Color.FromArgb(200, 200, 20, 20) : Color.FromArgb(200, 200, 200, 10);
                Function.Call(Hash.DRAW_MARKER, 0, _selectedProp.Position.X, _selectedProp.Position.Y, _selectedProp.Position.Z + 5f, 0f, 0f, 0f, 0f, 0f, 0f, 2f, 2f, 2f, tmp.R, tmp.G, tmp.B, tmp.A, 1, 0, 2, 2, 0, 0, 0);
	            if (Game.IsControlJustReleased(0, Control.Duck))
	            {
		            _controlsRotate = !_controlsRotate;
	            }
                if (Game.IsControlPressed(0, Control.FrontendRb))
                {
	                float pedMod = 0f;
	                if (_selectedProp is Ped)
		                pedMod = -1f;
					if(!_controlsRotate)
						_selectedProp.Position += new Vector3(0f, 0f, (modifier/4) + pedMod);
					else
						_selectedProp.Rotation += new Vector3(0f, 0f, modifier);
				}
                if (Game.IsControlPressed(0, Control.FrontendLb))
                {
					float pedMod = 0f;
					if (_selectedProp is Ped)
						pedMod = 1f;
					if (!_controlsRotate)
		                _selectedProp.Position -= new Vector3(0f, 0f, (modifier/4) + pedMod);
	                else
		                _selectedProp.Rotation -= new Vector3(0f, 0f, modifier);
				}
				
                if (Game.IsControlPressed(0, Control.MoveUpOnly))
                {
					float pedMod = 0f;
	                if (_selectedProp is Ped)
		                pedMod = -1f;
	                if (!_controlsRotate)
	                {
						var dir = VectorExtensions.RotationToDirection(_mainCamera.Rotation) * (modifier/4);
						_selectedProp.Position += new Vector3(dir.X, dir.Y, pedMod);
	                }
	                else
		                _selectedProp.Rotation += new Vector3(modifier, 0f, 0f);
                }
                if (Game.IsControlPressed(0, Control.MoveDownOnly))
                {
					float pedMod = 0f;
					if (_selectedProp is Ped)
						pedMod = 1f;
					if (!_controlsRotate)
					{
						var dir = VectorExtensions.RotationToDirection(_mainCamera.Rotation) * (modifier / 4);
						_selectedProp.Position -= new Vector3(dir.X, dir.Y, pedMod);
					}
					else
						_selectedProp.Rotation -= new Vector3(modifier, 0f, 0f);
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
						var right = (VectorExtensions.RotationToDirection(rotRight) - VectorExtensions.RotationToDirection(rotLeft)) * (modifier/2);
						_selectedProp.Position += new Vector3(right.X, right.Y, pedMod);
					}
					else
		                _selectedProp.Rotation += new Vector3(0f, modifier, 0f);
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
						var right = (VectorExtensions.RotationToDirection(rotRight) - VectorExtensions.RotationToDirection(rotLeft)) * (modifier/2);
						_selectedProp.Position -= new Vector3(right.X, right.Y, pedMod);
					}
					else
						_selectedProp.Rotation -= new Vector3(0f, modifier, 0f);
				}

	            if (Game.IsControlJustReleased(0, Control.MoveLeft) ||
					Game.IsControlJustReleased(0, Control.MoveRight) ||
                    Game.IsControlJustReleased(0, Control.MoveUp) ||
					Game.IsControlJustReleased(0, Control.MoveDown) ||
                    Game.IsControlJustReleased(0, Control.FrontendLb) ||
	                Game.IsControlJustReleased(0, Control.FrontendRb))
	            {
					RedrawObjectInfoMenu(_selectedProp);
				}

				if (Game.IsControlJustReleased(0, Control.LookBehind))
				{
					Entity mainProp = new Prop(0);
					if (_selectedProp is Prop)
						AddItemToEntityMenu(mainProp = PropStreamer.CreateProp(_selectedProp.Model, _selectedProp.Position, _selectedProp.Rotation, !PropStreamer.StaticProps.Contains(_selectedProp.Handle), force: true));
					else if (_selectedProp is Vehicle)
						AddItemToEntityMenu(mainProp = PropStreamer.CreateVehicle(_selectedProp.Model, _selectedProp.Position, _selectedProp.Rotation.Z, !PropStreamer.StaticProps.Contains(_selectedProp.Handle)));
					else if (_selectedProp is Ped)
					{
						AddItemToEntityMenu(mainProp = Function.Call<Ped>(Hash.CLONE_PED, ((Ped) _selectedProp).Handle, _selectedProp.Rotation.Z, 1, 1));
						PropStreamer.Peds.Add(mainProp.Handle);
					}

					_selectedProp = mainProp;
					if(_settings.SnapCameraToSelectedObject)
						_mainCamera.PointAt(_selectedProp);
					if(_selectedProp != null) RedrawObjectInfoMenu(_selectedProp);
				}

				if (Game.IsControlJustPressed(0, Control.CreatorDelete))
				{
					RemoveItemFromEntityMenu(_selectedProp);
					PropStreamer.RemoveEntity(_selectedProp.Handle);
					_selectedProp = null;
					_objectInfoMenu.Visible = false;
					_mainCamera.StopPointing();
				}

				if (Game.IsControlJustPressed(0, Control.PhoneCancel) || Game.IsControlJustPressed(0, Control.Attack))
                {
                    _selectedProp = null;
					_objectInfoMenu.Visible = false;
					_mainCamera.StopPointing();
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
				if(_previewProp != null) _previewProp.FreezePosition = true;
            }
        }

        private void OnObjectSelect(UIMenu sender, UIMenuItem item, int index)
        {
	        int objectHash;
	        switch (_currentObjectType)
	        {
			    case ObjectTypes.Prop:
					objectHash = ObjectDatabase.MainDb[_objectsMenu.MenuItems[_objectsMenu.CurrentSelection].Text];
					AddItemToEntityMenu(_snappedProp = PropStreamer.CreateProp(ObjectPreview.LoadObject(objectHash), VectorExtensions.RaycastEverything(new Vector2(0f, 0f)), new Vector3(0, 0, 0), false, force: true));
					break;
				case ObjectTypes.Vehicle:
			        objectHash = ObjectDatabase.VehicleDb[_objectsMenu.MenuItems[_objectsMenu.CurrentSelection].Text];
			        AddItemToEntityMenu(_snappedProp = PropStreamer.CreateVehicle(ObjectPreview.LoadObject(objectHash), VectorExtensions.RaycastEverything(new Vector2(0f, 0f)), 0f, true));
					break;
				case ObjectTypes.Ped:
					objectHash = ObjectDatabase.PedDb[_objectsMenu.MenuItems[_objectsMenu.CurrentSelection].Text];
			        AddItemToEntityMenu(_snappedProp = PropStreamer.CreatePed(ObjectPreview.LoadObject(objectHash), VectorExtensions.RaycastEverything(new Vector2(0f, 0f)), 0f, true));
					break;
	        }
            _isChoosingObject = false;
            _objectsMenu.Visible = false;
			_previewProp?.Delete();
        }

        private void RedrawObjectsMenu(string searchQuery = null, ObjectTypes type = ObjectTypes.Prop)
        {
            _objectsMenu.Clear();
            if (searchQuery == null)
            {
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
            else
            {
	            switch (type)
	            {
			        case ObjectTypes.Prop:
							foreach (var u in ObjectDatabase.MainDb.Where(pair => CultureInfo.InvariantCulture.CompareInfo.IndexOf(pair.Key, searchQuery, CompareOptions.IgnoreCase) >= 0))
							{
								var object1 = new UIMenuItem(u.Key);
								if (ObjectDatabase.InvalidHashes.Contains(u.Value))
									object1.SetRightLabel("~r~Invalid");
								_objectsMenu.AddItem(object1);
							}
			            break;
					case ObjectTypes.Vehicle:
						foreach (var u in ObjectDatabase.VehicleDb.Where(pair => CultureInfo.InvariantCulture.CompareInfo.IndexOf(pair.Key, searchQuery, CompareOptions.IgnoreCase) >= 0))
						{
							var object1 = new UIMenuItem(u.Key);
							_objectsMenu.AddItem(object1);
						}
						break;
					case ObjectTypes.Ped:
						foreach (var u in ObjectDatabase.PedDb.Where(pair => CultureInfo.InvariantCulture.CompareInfo.IndexOf(pair.Key, searchQuery, CompareOptions.IgnoreCase) >= 0))
						{
							var object1 = new UIMenuItem(u.Key);
							_objectsMenu.AddItem(object1);
						}
						break;
				}
                _objectsMenu.RefreshIndex();
            }
        }

	    private void RedrawFormatMenu()
	    {
			_formatMenu.Clear();
			_formatMenu.AddItem(new UIMenuItem("XML", "Default format for Map Editor. Choose this one if you have no idea. This saves props, vehicles and peds."));
		    _formatMenu.AddItem(new UIMenuItem("Simple Trainer",
				"Format used in Simple Trainer mod (objects.ini). Only saves props and has a prop limit of 250."));
		    if (_savingMap)
		    {
			    _formatMenu.AddItem(new UIMenuItem("C# Code",
				    "Directly outputs to C# code to spawn your entities. Saves props, vehicles and peds."));
			    _formatMenu.AddItem(new UIMenuItem("Raw",
				"Writes the entity and their position and rotation. Useful for taking coordinates."));
		    }
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
			_scaleform.CallFunction("SET_DATA_SLOT", 0, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.Enter, 0), "Spawn Prop");
			_scaleform.CallFunction("SET_DATA_SLOT", 1, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.FrontendPause, 0), "Spawn Ped");
			_scaleform.CallFunction("SET_DATA_SLOT", 2, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.NextCamera, 0), "Spawn Vehicle");
			_scaleform.CallFunction("SET_DATA_SLOT", 3, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.Aim, 0), "Move Entity");
			_scaleform.CallFunction("SET_DATA_SLOT", 4, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.Attack, 0), "Select Entity");
			_scaleform.CallFunction("SET_DATA_SLOT", 5, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.LookBehind, 0), "Copy Entity");
			_scaleform.CallFunction("SET_DATA_SLOT", 6, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.CreatorDelete, 0), "Delete Entity");
		}

		private void InstructionalButtonsSelected()
		{
			if (!_settings.InstructionalButtons) return;
			_scaleform.CallFunction("SET_DATA_SLOT", 0, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.MoveLeftRight, 0), "");
			_scaleform.CallFunction("SET_DATA_SLOT", 1, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.MoveUpDown, 0), "");
			_scaleform.CallFunction("SET_DATA_SLOT", 2, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.FrontendRb, 0), "");
			_scaleform.CallFunction("SET_DATA_SLOT", 3, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.FrontendLb, 0), "Move Entity");
			_scaleform.CallFunction("SET_DATA_SLOT", 4, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.Duck, 0), "Switch to Rotation");
			_scaleform.CallFunction("SET_DATA_SLOT", 5, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.LookBehind, 0), "Copy Entity");
			_scaleform.CallFunction("SET_DATA_SLOT", 6, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.CreatorDelete, 0), "Delete Entity");
			_scaleform.CallFunction("SET_DATA_SLOT", 7, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.Attack, 0), "Accept");
		}

		private void InstructionalButtonsSnapped()
		{
			if (!_settings.InstructionalButtons) return;
			_scaleform.CallFunction("SET_DATA_SLOT", 0, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.FrontendRb, 0), "");
			_scaleform.CallFunction("SET_DATA_SLOT", 1, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.FrontendLb, 0), "Rotate Entity");
			_scaleform.CallFunction("SET_DATA_SLOT", 2, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.CreatorDelete, 0), "Delete Entity");
			_scaleform.CallFunction("SET_DATA_SLOT", 3, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.Attack, 0), "Accept");
		}

		private void InstructionalButtonsEnd()
		{
			if (!_settings.InstructionalButtons) return;
			_scaleform.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", -1);
		}

	    private void RedrawObjectInfoMenu(Entity ent)
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
			List<dynamic> possiblePositions = new List<dynamic>();
		    for (int i = -500000; i <= 500000; i++)
		    {
			    possiblePositions.Add(i * 0.01);
		    }
		    var posXitem = new UIMenuListItem("Position X", possiblePositions, Convert.ToInt32(Math.Round((ent.Position.X * 100) + 500000)));
			var posYitem = new UIMenuListItem("Position Y", possiblePositions, Convert.ToInt32(Math.Round((ent.Position.Y * 100) + 500000)));
			var posZitem = new UIMenuListItem("Position Z", possiblePositions, Convert.ToInt32(Math.Round((ent.Position.Z * 100) + 500000)));

			var rotXitem = new UIMenuListItem("Rotation X", possiblePositions, Convert.ToInt32(Math.Round((ent.Rotation.X * 100) + 500000)));
			var rotYitem = new UIMenuListItem("Rotation Y", possiblePositions, Convert.ToInt32(Math.Round((ent.Rotation.Y * 100) + 500000)));
			var rotZitem = new UIMenuListItem("Rotation Z", possiblePositions, Convert.ToInt32(Math.Round((ent.Rotation.Z * 100) + 500000)));

		    var dynamic = new UIMenuCheckboxItem("Dynamic", !PropStreamer.StaticProps.Contains(ent.Handle));
		    dynamic.CheckboxEvent += (ite, checkd) =>
		    {
			    if (checkd && PropStreamer.StaticProps.Contains(ent.Handle)) PropStreamer.StaticProps.Remove(ent.Handle);
				else if (!checkd && !PropStreamer.StaticProps.Contains(ent.Handle)) PropStreamer.StaticProps.Add(ent.Handle);

			    ent.FreezePosition = PropStreamer.StaticProps.Contains(ent.Handle);
		    };

			_objectInfoMenu.AddItem(posXitem);
			_objectInfoMenu.AddItem(posYitem);
			_objectInfoMenu.AddItem(posZitem);
			_objectInfoMenu.AddItem(rotXitem);
			_objectInfoMenu.AddItem(rotYitem);
			_objectInfoMenu.AddItem(rotZitem);
			_objectInfoMenu.AddItem(dynamic);
			

		    posXitem.OnListChanged += (item, index) => ent.Position = new Vector3((float)item.IndexToItem(index), ent.Position.Y, ent.Position.Z);
			posYitem.OnListChanged += (item, index) => ent.Position = new Vector3(ent.Position.X, (float)item.IndexToItem(index), ent.Position.Z);
			posZitem.OnListChanged += (item, index) => ent.Position = new Vector3(ent.Position.X, ent.Position.Y, (float)item.IndexToItem(index));

			rotXitem.OnListChanged += (item, index) => ent.Rotation = new Vector3((float)item.IndexToItem(index), ent.Rotation.Y, ent.Rotation.Z);
			rotYitem.OnListChanged += (item, index) => ent.Rotation = new Vector3(ent.Rotation.X, (float)item.IndexToItem(index), ent.Rotation.Z);
			rotZitem.OnListChanged += (item, index) => ent.Rotation = new Vector3(ent.Rotation.X, ent.Rotation.Y, (float)item.IndexToItem(index));
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
			    new UIResText((counter) + "/" + ObjectDatabase.MainDb.Count + " done. (" + (counter/(float) ObjectDatabase.MainDb.Count)*100 +
				    "%)\nValid objects: " + tmpDict.Count, new Point(200, 200), 0.5f).Draw();
			    Yield();
			    if (!new Model(pair.Value).IsValid || !new Model(pair.Value).IsInCdImage) continue;
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
			_currentObjectsMenu.RefreshIndex(); //TODO: fix this, selected item remains after refresh.
	    }

	    public void RemoveItemFromEntityMenu(Entity ent)
	    {
		    var found = _currentObjectsMenu.MenuItems.FirstOrDefault(item => item.Description == ent.Handle.ToString());
			if(found == null) return;
			_currentObjectsMenu.RemoveItemAt(_currentObjectsMenu.MenuItems.IndexOf(found));
			if (_currentObjectsMenu.Size != 0)
				_currentObjectsMenu.RefreshIndex(); //TODO: fix this, selected item remains after refresh.
		}

	    public void RemoveItemFromEntityMenu(int id)
	    {
		    var found = _currentObjectsMenu.MenuItems.FirstOrDefault(item => item.Description == id.ToString());
			if(found == null) return;
			_currentObjectsMenu.RemoveItemAt(_currentObjectsMenu.MenuItems.IndexOf(found));
		    if (_currentObjectsMenu.Size != 0)
			    _currentObjectsMenu.RefreshIndex();
		    else
			    _currentObjectsMenu.Visible = false;
	    }

	    public void OnEntityTeleport(UIMenu menu, UIMenuItem item, int index)
	    {
		    if (item.Text.StartsWith("~h~[WORLD]~h~ "))
		    {
			    var mapObj = PropStreamer.RemovedObjects.FirstOrDefault(obj => obj.Id == int.Parse(item.Description, CultureInfo.InvariantCulture));
				if(mapObj == null) return;
			    var t = World.CreateProp(mapObj.Hash, mapObj.Position, mapObj.Rotation, true, false);
			    t.Position = mapObj.Position;
				RemoveItemFromEntityMenu(mapObj.Id);
			    PropStreamer.RemovedObjects.Remove(mapObj);
				_menuPool.CloseAllMenus();
			    return;
		    }
		    var prop = new Prop(int.Parse(item.Description, CultureInfo.InvariantCulture));
			if(!prop.Exists()) return;
		    _mainCamera.Position = prop.Position + new Vector3(5f, 5f, 10f);
			if(_settings.SnapCameraToSelectedObject)
				_mainCamera.PointAt(prop);
			_menuPool.CloseAllMenus();
			_selectedProp = prop;
			RedrawObjectInfoMenu(_selectedProp);
			_objectInfoMenu.Visible = true;
	    }
    }
}
