using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using System.IO;
using Control = GTA.Control;

namespace MapEditor
{
    public class MapEditor : Script
    {
        private bool _isInFreecam;
        private bool _isChoosingObject;
        private bool _searchResultsOn = false;

        private UIMenu _objectsMenu;
        private UIMenu _mainMenu;
	    private UIMenu _formatMenu;
	    private UIMenu _objectInfoMenu;
        private MenuPool _menuPool = new MenuPool();

        private Entity _previewProp;
        private Entity _snappedProp;
        private Entity _selectedProp;
        
        private Camera _mainCamera;
        private Camera _objectPreviewCamera;

        private Vector3 _objectPreviewPos = new Vector3(1200.133f, 4000.958f, 85.9f);

        private bool _zAxis = true;
	    private bool _controlsRotate = false;

        //List<int> _currentProps = new List<int>();
		
	    private string _crosshairPath;
	    private bool _showCrosshair = true;
	    private bool _savingMap;
	    private bool _drawInstructionalbuttons = true;

	    private ObjectTypes _currentObjectType;

	    private float _cameraSensitivity = 30f;

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
			

			_objectsMenu = new UIMenu("Map Editor", "~b~PLACE OBJECT");

            ObjectDatabase.LoadFromFile("scripts\\ObjectList.ini");
			ObjectDatabase.LoadInvalidHashes();
			ObjectDatabase.LoadEnumDatabases();
			
			_crosshairPath = Sprite.WriteFileFromResources(Assembly.GetExecutingAssembly(), "MapEditor.crosshair.png");

            RedrawObjectsMenu();
            _objectsMenu.OnItemSelect += OnObjectSelect;
            _objectsMenu.OnIndexChange += OnIndexChange;
            _menuPool.Add(_objectsMenu);

			_objectsMenu.ResetKey(UIMenu.MenuControls.Back);
            _objectsMenu.AddInstructionalButton(new InstructionalButton(GTA.Control.SelectWeapon, "Change Axis"));
            _objectsMenu.AddInstructionalButton(new InstructionalButton(GTA.Control.MoveUpOnly, ""));
            _objectsMenu.AddInstructionalButton(new InstructionalButton(GTA.Control.MoveDownOnly, "Zoom"));
            _objectsMenu.AddInstructionalButton(new InstructionalButton(GTA.Control.Jump, "Search"));

            _mainMenu = new UIMenu("Map Editor", "~b~MAIN MENU");
            _mainMenu.AddItem(new UIMenuItem("Enter/Exit Map Editor"));
            _mainMenu.AddItem(new UIMenuItem("New Map", "Remove all current objects and start a new map."));
            _mainMenu.AddItem(new UIMenuItem("Save Map", "Save all current objects to a file."));
			_mainMenu.AddItem(new UIMenuItem("Load Map", "Load objects from a file and add them to the world."));
	        var checkem = new UIMenuCheckboxItem("Show Crosshair", true);
	        checkem.CheckboxEvent += (i, checkd) =>
	        {
		        _showCrosshair = checkd;
	        };
			List<dynamic> senslist = new List<dynamic>();
	        for (int i = 1; i < 60; i++)
	        {
		        senslist.Add(i);
	        }
			var gamboy = new UIMenuListItem("Camera Sensitivity", senslist, 29);
	        gamboy.OnListChanged += (item, index) =>
	        {
		        _cameraSensitivity = index + 1;
	        };
	        var butts = new UIMenuCheckboxItem("Instructional Buttons", true);
	        butts.CheckboxEvent += (i, checkd) =>
	        {
		        _drawInstructionalbuttons = checkd;
	        };
			_mainMenu.AddItem(checkem);
			_mainMenu.AddItem(gamboy);
			_mainMenu.AddItem(butts);
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
						//_currentProps.ForEach(p => new Prop(p).Delete());
                        //_currentProps.Clear();
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
					if (String.IsNullOrWhiteSpace(filename))
					{
						UI.Notify("~b~~h~Map Editor~h~~w~~n~The filename was empty!");
						return;
					}
					var ser = new MapSerializer();
					var tmpmap = new Map();
			        try
			        {
				        switch (indx)
				        {
					        case 0: // XML
								//foreach (int prop in _currentProps)
								foreach (int prop in )
								{
									Entity tmpProp = new Prop(0);
									if (Function.Call<bool>(Hash.IS_ENTITY_A_VEHICLE, prop))
										tmpProp = new Vehicle(prop);
									else if (Function.Call<bool>(Hash.IS_ENTITY_AN_OBJECT, prop))
										tmpProp = new Prop(prop);
									else if (Function.Call<bool>(Hash.IS_ENTITY_A_PED, prop))
										tmpProp = new Ped(prop);

									tmpmap.Objects.Add(new MapObject()
									{
										Hash = tmpProp.Model.Hash,
										Position = tmpProp.Position,
										Rotation = tmpProp.Rotation,
										Type = (tmpProp is Vehicle ? ObjectTypes.Vehicle : tmpProp is Ped ? ObjectTypes.Ped : ObjectTypes.Prop)
									});
								}
								ser.Serialize(filename, tmpmap, MapSerializer.Format.NormalXml);
								UI.Notify("~b~~h~Map Editor~h~~w~~n~Saved current map as ~h~" + filename + "~h~.");
								break;
							case 1: // objects.ini
								foreach (int prop in _currentProps)
								{
									if (Function.Call<bool>(Hash.IS_ENTITY_A_VEHICLE, prop) || Function.Call<bool>(Hash.IS_ENTITY_A_PED, prop)) continue;
									var tmpProp = new Prop(prop);
									tmpmap.Objects.Add(new MapObject()
									{
										Hash = tmpProp.Model.Hash,
										Position = tmpProp.Position,
										Rotation = tmpProp.Rotation,
										Quaternion = Quaternion.GetEntityQuaternion(tmpProp)
									});
								}
								ser.Serialize(filename, tmpmap, MapSerializer.Format.SimpleTrainer);
								UI.Notify("~b~~h~Map Editor~h~~w~~n~Saved current map as ~h~" + filename + "~h~.");
								break;
							case 2: // C#
						        string output = "";
								foreach (int prop in _currentProps)
								{
									var p = new Prop(prop);
									if (p.Position == new Vector3(0, 0, 0)) continue;
									string cmd = "";
									
									if (Function.Call<bool>(Hash.IS_ENTITY_A_VEHICLE, prop))
									{
										cmd = String.Format("GTA.World.CreateVehicle(new Model({0}).Request(100), new GTA.Math.Vector3({1}f, {2}f, {3}f), {4}f);",
											p.Model.Hash,
											p.Position.X,
											p.Position.Y,
											p.Position.Z,
											p.Rotation.Z
                                        );
									}
									if (Function.Call<bool>(Hash.IS_ENTITY_A_PED, prop))
									{
										cmd = String.Format("GTA.World.CreatePed(new Model({0}).Request(100), new GTA.Math.Vector3({1}f, {2}f, {3}f), {4}f);",
											p.Model.Hash,
											p.Position.X,
											p.Position.Y,
											p.Position.Z,
											p.Rotation.Z
										);
									}
									if (Function.Call<bool>(Hash.IS_ENTITY_AN_OBJECT, prop))
									{
										cmd = String.Format("GTA.World.CreateProp(new Model({0}).Request(100), new GTA.Math.Vector3({1}f, {2}f, {3}f), new GTA.Math.Vector3({4}f, {5}f, {6}f), false, false);",
											p.Model.Hash,
											p.Position.X,
											p.Position.Y,
											p.Position.Z,
											p.Rotation.X,
											p.Rotation.Y,
											p.Rotation.Z
                                        );
									}
									output += cmd + "\r\n";
								}
								File.WriteAllText(filename, output);
								UI.Notify("~b~~h~Map Editor~h~~w~~n~Saved current map as ~h~" + filename + "~h~.");
								break;
							case 3: // Raw
								string flush = "";
								foreach (int prop in _currentProps)
								{
									var p = new Prop(prop);
									if(p.Position == new Vector3(0, 0, 0)) continue;
									string name = "";
									ObjectTypes thisType = ObjectTypes.Prop;

									if (Function.Call<bool>(Hash.IS_ENTITY_A_VEHICLE, prop))
									{
										name = ObjectDatabase.VehicleDb.First(pair => pair.Value == p.Model.Hash).Key;
										thisType = ObjectTypes.Vehicle;
									}
									if (Function.Call<bool>(Hash.IS_ENTITY_A_PED, prop))
									{
										name = ObjectDatabase.PedDb.First(pair => pair.Value == p.Model.Hash).Key;
										thisType = ObjectTypes.Ped;
									}
									if (Function.Call<bool>(Hash.IS_ENTITY_AN_OBJECT, prop))
									{
										name = ObjectDatabase.MainDb.First(pair => pair.Value == p.Model.Hash).Key;
										thisType = ObjectTypes.Prop;
									}
									flush += String.Format("{8} name = {0}, hash = {7}, x = {1}, y = {2}, z = {3}, rotationx = {4}, rotationy = {5}, rotationz = {6}\r\n",
										name,
										p.Position.X,
										p.Position.Y,
										p.Position.Z,
										p.Rotation.X,
										p.Rotation.Y,
										p.Rotation.Z,
										p.Model.Hash,
										thisType
									);
								}
								File.WriteAllText(filename, flush);
								UI.Notify("~b~~h~Map Editor~h~~w~~n~Saved current map as ~h~" + filename + "~h~.");
								break;
						}
					}
					catch (Exception e)
					{
						UI.Notify("~r~~h~Map Editor~h~~w~~n~Map failed to save, see error below.");
						UI.Notify(e.Message);
					}
				}
		        else
		        {
					string filename = Game.GetUserInput(255);
					if (String.IsNullOrWhiteSpace(filename) || !File.Exists(filename))
					{
						UI.Notify("~b~~h~Map Editor~h~~w~~n~The filename was empty or the file does not exist!");
						return;
					}
					var des = new MapSerializer();
			        try
			        {
				        switch (indx)
				        {
					        case 0: // XML
						        var map2Load = des.Deserialize(filename, MapSerializer.Format.NormalXml);
						        foreach (MapObject o in map2Load.Objects)
						        {
							        Entity prop = new Prop(0);
							        if (o.Type == ObjectTypes.Prop)
								        //PropStreamer.AddProp((Prop)(prop = World.CreateProp(ObjectPreview.LoadObject(o.Hash), o.Position, o.Rotation, o.Dynamic, false)));
								        prop = World.CreateProp(ObjectPreview.LoadObject(o.Hash), o.Position, o.Rotation, o.Dynamic, false);
                                    else if (o.Type == ObjectTypes.Vehicle)
								        prop = World.CreateVehicle(ObjectPreview.LoadObject(o.Hash), o.Position, o.Rotation.Z);
							        else if (o.Type == ObjectTypes.Ped)
								        prop = World.CreatePed(ObjectPreview.LoadObject(o.Hash), o.Position - new Vector3(0f, 0f, 1f),
									        o.Rotation.Z);
									if(prop == null) continue;
							        if (!o.Dynamic)
							        {
								        prop.FreezePosition = true;
										PropStreamer.StaticProps.Add(prop.Handle);
							        }
							        _currentProps.Add(prop.Handle);
						        }
						        UI.Notify("~b~~h~Map Editor~h~~w~~n~Loaded map ~h~" + filename + "~h~.");
						        break;
					        case 1: // objects.ini
						        var objectsinimap = des.Deserialize(filename, MapSerializer.Format.SimpleTrainer);
						        foreach (MapObject o in objectsinimap.Objects)
						        {
							        Entity prop = new Prop(0);
							        if (o.Type == ObjectTypes.Prop)
								        //PropStreamer.AddProp((Prop)(prop = World.CreateProp(ObjectPreview.LoadObject(o.Hash), o.Position, o.Rotation, o.Dynamic, false)));
								        prop = World.CreateProp(ObjectPreview.LoadObject(o.Hash), o.Position, o.Rotation, o.Dynamic, false);
                                    else if (o.Type == ObjectTypes.Vehicle)
								        prop = World.CreateVehicle(ObjectPreview.LoadObject(o.Hash), o.Position, o.Rotation.Z);
							        else if (o.Type == ObjectTypes.Ped)
								        prop = World.CreatePed(ObjectPreview.LoadObject(o.Hash), o.Position - new Vector3(0f, 0f, 1f),
									        o.Rotation.Z);
							        Quaternion.SetEntityQuaternion(prop, o.Quaternion);
							        if (!o.Dynamic)
							        {
								        prop.FreezePosition = true;
										PropStreamer.StaticProps.Add(prop.Handle);
									}
							        _currentProps.Add(prop.Handle);
						        }
						        UI.Notify("~b~~h~Map Editor~h~~w~~n~Loaded map ~h~" + filename + "~h~.");
						        break;
				        }
					}
					catch (Exception e)
					{
						UI.Notify("~r~~h~Map Editor~h~~w~~n~Map failed to load, see error below.");
						UI.Notify(e.Message);
					}
				}
		        _formatMenu.Visible = false;
	        };
        }

	    //private int counter = 0; 
		//private Dictionary<string, int> tmpDict = new Dictionary<string, int>();
	    //private bool written = false;
		
		public void OnTick(object sender, EventArgs e)
        {
			/* // Validate object list.
			if (counter < ObjectDatabase.MainDb.Count)
			{
				var pair = ObjectDatabase.MainDb.ElementAt(counter);
				counter++;
				new UIResText((counter) + "/" + ObjectDatabase.MainDb.Count + " done. (" + (counter/(float)ObjectDatabase.MainDb.Count)*100 + "%)\nValidDB size: " + tmpDict.Count, new Point(200, 200), 0.5f).Draw();
				if(!new Model(pair.Value).IsValid || !new Model(pair.Value).IsInCdImage) return;
				if(!tmpDict.ContainsKey(pair.Key))
					tmpDict.Add(pair.Key, pair.Value);
				return;
			}
			else if(!written)
			{
				string output = "";
				foreach (var pair in tmpDict)
				{
					output += pair.Key + "=" + pair.Value + "\r\n";
				}
				File.WriteAllText("scripts\\FixedObjects.ini", output);
				written = true;
			}
			// */
			_menuPool.ProcessMenus();
			//PropStreamer.Tick();

			if (Game.IsControlPressed(0, GTA.Control.LookBehind) && Game.IsControlJustPressed(0, GTA.Control.FrontendLb) && !_menuPool.IsAnyMenuOpen())
			{
				_mainMenu.Visible = !_mainMenu.Visible;
			}

            if (!_isInFreecam) return;
			if(_drawInstructionalbuttons)
				_scaleform.Render2D();
			Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)GTA.Control.CharacterWheel);
			Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)GTA.Control.SelectWeapon);
			Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)GTA.Control.FrontendPause);
			Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)GTA.Control.NextCamera);

			if (Game.IsControlJustPressed(0, GTA.Control.Enter) && !_isChoosingObject)
            {
	            var oldType = _currentObjectType;
				_currentObjectType = ObjectTypes.Prop;
				if(oldType != _currentObjectType)
					RedrawObjectsMenu(type: _currentObjectType);
                World.CurrentDayTime = new TimeSpan(14, 0, 0);
                _isChoosingObject = true;
                _objectsMenu.Visible = true;
                OnIndexChange(_objectsMenu, _objectsMenu.CurrentSelection);
				_objectsMenu.Subtitle.Caption = "~b~PLACE " + _currentObjectType.ToString().ToUpper();
			}

			if (Game.IsControlJustPressed(0, GTA.Control.NextCamera) && !_isChoosingObject)
			{
				var oldType = _currentObjectType;
				_currentObjectType = ObjectTypes.Vehicle;
				if (oldType != _currentObjectType)
					RedrawObjectsMenu(type: _currentObjectType);
				World.CurrentDayTime = new TimeSpan(14, 0, 0);
				_isChoosingObject = true;
				_objectsMenu.Visible = true;
				OnIndexChange(_objectsMenu, _objectsMenu.CurrentSelection);
				_objectsMenu.Subtitle.Caption = "~b~PLACE " + _currentObjectType.ToString().ToUpper();
			}

			if (Game.IsControlJustPressed(0, GTA.Control.FrontendPause) && !_isChoosingObject)
			{
				var oldType = _currentObjectType;
				_currentObjectType = ObjectTypes.Ped;
				if (oldType != _currentObjectType)
					RedrawObjectsMenu(type: _currentObjectType);
				World.CurrentDayTime = new TimeSpan(14, 0, 0);
				_isChoosingObject = true;
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

                if (Game.IsControlJustPressed(0, GTA.Control.SelectWeapon))
                    _zAxis = !_zAxis;

                if (_objectPreviewCamera == null)
                {
                    _objectPreviewCamera = World.CreateCamera(new Vector3(1200.016f, 4000.998f, 86.05062f), new Vector3(0f, 0f, 0f), 60f);
                    _objectPreviewCamera.PointAt(_objectPreviewPos);
                }

                if (Game.IsControlPressed(0, GTA.Control.MoveDownOnly))
                {
                    _objectPreviewCamera.Position -= new Vector3(0f, 0.5f, 0f);
                }

                if (Game.IsControlPressed(0, GTA.Control.MoveUpOnly))
                {
                    _objectPreviewCamera.Position += new Vector3(0f, 0.5f, 0f);
                }
                World.RenderingCamera = _objectPreviewCamera;

                if (Game.IsControlJustPressed(0, GTA.Control.PhoneCancel) && !_searchResultsOn)
                {
                    _isChoosingObject = false;
                    _objectsMenu.Visible = false;
                    _previewProp?.Delete();
                }

                if (Game.IsControlJustPressed(0, GTA.Control.PhoneCancel) && _searchResultsOn)
                {
                    RedrawObjectsMenu(type: _currentObjectType);
                    OnIndexChange(_objectsMenu, 0);
                    _searchResultsOn = false;
                    _objectsMenu.Subtitle.Caption = "~b~PLACE " + _currentObjectType.ToString().ToUpper();
                }

                if (Game.IsControlJustPressed(0, GTA.Control.Jump))
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

			int wi = Convert.ToInt32(res.Width*0.5);
			int he = Convert.ToInt32(res.Height * 0.5);

			if (_showCrosshair)
				Sprite.DrawTexture(_crosshairPath, new Point(wi - 15, he - 15), new Size(30, 30));

			Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)GTA.Control.CharacterWheel);
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)GTA.Control.SelectWeapon);
			Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)GTA.Control.FrontendPause);

			var mouseX = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)GTA.Control.LookLeftRight);
			var mouseY = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)GTA.Control.LookUpDown);


			mouseX *= -1;
			mouseY *= -1;

            mouseX *= _cameraSensitivity;
            mouseY *= _cameraSensitivity;
            

            float modifier = 1f;
            if (Game.IsControlPressed(0, GTA.Control.Sprint))
                modifier = 5f;
            else if (Game.IsControlPressed(0, GTA.Control.CharacterWheel))
                modifier = 0.3f;

            if (_selectedProp == null)
            {
                _mainCamera.Rotation = new Vector3(_mainCamera.Rotation.X + mouseY, _mainCamera.Rotation.Y, _mainCamera.Rotation.Z + mouseX);

                //Cursor.Position = new Point(Game.ScreenResolution.Width/2, Game.ScreenResolution.Height/2);
                Vector3 dir = (VectorExtensions.ScreenRelToWorld(_mainCamera.Position, _mainCamera.Rotation, new Vector2(0f, 0f)) -
                     _mainCamera.Position);
                dir.Normalize();
                Vector3 right = VectorExtensions.CrossWith(dir, new Vector3(0, 0, 1));
                if (Game.IsControlPressed(0, GTA.Control.MoveUpOnly))
                {
                    _mainCamera.Position += dir*modifier;
                }
                if (Game.IsControlPressed(0, GTA.Control.MoveDownOnly))
                {
                    _mainCamera.Position -= dir*modifier;
                }
                if (Game.IsControlPressed(0, GTA.Control.MoveLeftOnly))
                {
                    _mainCamera.Position -= right*modifier;
                }
                if (Game.IsControlPressed(0, GTA.Control.MoveRightOnly))
                {
                    _mainCamera.Position += right*modifier;
                }
                Game.Player.Character.Position = _mainCamera.Position - dir*8f;

                if (_snappedProp != null)
                {
                    _snappedProp.Position = VectorExtensions.RaycastEverything(new Vector2(0f, 0f), _mainCamera.Position, _mainCamera.Rotation, _snappedProp);
                    if (Game.IsControlPressed(0, GTA.Control.CursorScrollUp) || Game.IsControlPressed(0, GTA.Control.FrontendRb))
                    {
                        _snappedProp.Rotation = _snappedProp.Rotation + new Vector3(0f, 0f, modifier);
                    }

                    if (Game.IsControlPressed(0, GTA.Control.CursorScrollDown) || Game.IsControlPressed(0, GTA.Control.FrontendLb))
                    {
                        _snappedProp.Rotation = _snappedProp.Rotation - new Vector3(0f, 0f, modifier);
                    }
                    if (Game.IsControlJustPressed(0, GTA.Control.Attack))
                    {
                        _snappedProp = null;
                    }
					InstructionalButtonsStart();
					InstructionalButtonsSnapped();
					InstructionalButtonsEnd();
				}
                else
                {
                    if (Game.IsControlJustPressed(0, GTA.Control.Aim))
                    {
                        Entity hitEnt = VectorExtensions.RaycastEntity(new Vector2(0f, 0f), _mainCamera.Position, _mainCamera.Rotation);
                        if (hitEnt != null && _currentProps.Contains(hitEnt.Handle))
                        {
							if (Function.Call<bool>(Hash.IS_ENTITY_AN_OBJECT, hitEnt.Handle))
								_snappedProp = new Prop(hitEnt.Handle);
							else if (Function.Call<bool>(Hash.IS_ENTITY_A_VEHICLE, hitEnt.Handle))
								_snappedProp = new Vehicle(hitEnt.Handle);
							else if (Function.Call<bool>(Hash.IS_ENTITY_A_PED, hitEnt.Handle))
								_snappedProp = new Ped(hitEnt.Handle);
						}
                    }

                    if (Game.IsControlJustPressed(0, GTA.Control.Attack))
                    {
                        Entity hitEnt = VectorExtensions.RaycastEntity(new Vector2(0f, 0f), _mainCamera.Position, _mainCamera.Rotation);
                        if (hitEnt != null && _currentProps.Contains(hitEnt.Handle))
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
						}
                    }

	                if (Game.IsControlJustReleased(0, GTA.Control.LookBehind))
	                {
						Entity hitEnt = VectorExtensions.RaycastEntity(new Vector2(0f, 0f), _mainCamera.Position, _mainCamera.Rotation);
						if (hitEnt != null && _currentProps.Contains(hitEnt.Handle))
						{
							Entity tmpProp = new Prop(0);
							if (Function.Call<bool>(Hash.IS_ENTITY_AN_OBJECT, hitEnt.Handle))
								tmpProp = PropStreamer.CreateProp(hitEnt.Model, hitEnt.Position, hitEnt.Rotation, !PropStreamer.StaticProps.Contains(hitEnt.Handle));
							//tmpProp = World.CreateProp(hitEnt.Model, hitEnt.Position, hitEnt.Rotation, false, false);
							else if (Function.Call<bool>(Hash.IS_ENTITY_A_VEHICLE, hitEnt.Handle))
								tmpProp = World.CreateVehicle(hitEnt.Model, hitEnt.Position, hitEnt.Rotation.Z);
							else if (Function.Call<bool>(Hash.IS_ENTITY_A_PED, hitEnt.Handle))
								tmpProp = Function.Call<Ped>(Hash.CLONE_PED, ((Ped)hitEnt).Handle, hitEnt.Rotation.Z, 1, 1);
							//tmpProp = World.CreatePed(hitEnt.Model, hitEnt.Position, hitEnt.Rotation.Z);
							_currentProps.Add(tmpProp.Handle);
							if (PropStreamer.StaticProps.Contains(hitEnt.Handle))
							{
								PropStreamer.StaticProps.Add(tmpProp.Handle);
								tmpProp.FreezePosition = true;
							}
							_snappedProp = tmpProp;
						}
					}

					if (Game.IsControlJustPressed(0, GTA.Control.CreatorDelete))
					{
						Entity hitEnt = VectorExtensions.RaycastEntity(new Vector2(0f, 0f), _mainCamera.Position, _mainCamera.Rotation);
						if (hitEnt != null && _currentProps.Contains(hitEnt.Handle))
						{
							_currentProps.Remove(hitEnt.Handle);
							if(PropStreamer.StaticProps.Contains(hitEnt.Handle)) PropStreamer.StaticProps.Remove(hitEnt.Handle);
							//if (Function.Call<bool>(Hash.IS_ENTITY_AN_OBJECT, hitEnt))
								//PropStreamer.RemoveProp((Prop)hitEnt);
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
	            Color tmp;
	            tmp = _controlsRotate ? Color.FromArgb(200, 200, 20, 20) : Color.FromArgb(200, 200, 200, 10);
                Function.Call(Hash.DRAW_MARKER, 0, _selectedProp.Position.X, _selectedProp.Position.Y, _selectedProp.Position.Z + 5f, 0f, 0f, 0f, 0f, 0f, 0f, 2f, 2f, 2f, tmp.R, tmp.G, tmp.B, tmp.A, 1, 0, 2, 2, 0, 0, 0);
	            if (Game.IsControlJustReleased(0, GTA.Control.Duck))
	            {
		            _controlsRotate = !_controlsRotate;
	            }
                if (Game.IsControlPressed(0, GTA.Control.FrontendRb))
                {
	                float pedMod = 0f;
	                if (_selectedProp is Ped)
		                pedMod = -1f;
					if(!_controlsRotate)
						_selectedProp.Position += new Vector3(0f, 0f, (modifier/4) + pedMod);
					else
						_selectedProp.Rotation += new Vector3(0f, 0f, modifier);
				}
                if (Game.IsControlPressed(0, GTA.Control.FrontendLb))
                {
					float pedMod = 0f;
					if (_selectedProp is Ped)
						pedMod = 1f;
					if (!_controlsRotate)
		                _selectedProp.Position -= new Vector3(0f, 0f, (modifier/4) + pedMod);
	                else
		                _selectedProp.Rotation -= new Vector3(0f, 0f, modifier);
				}

                if (Game.IsControlPressed(0, GTA.Control.MoveUpOnly))
                {
	                float pedMod = 0f;
	                if (_selectedProp is Ped)
		                pedMod = -1f;
					if(!_controlsRotate)
						_selectedProp.Position += new Vector3(modifier/4, 0f, pedMod);
					else
						_selectedProp.Rotation += new Vector3(modifier, 0f, 0f);
				}
                if (Game.IsControlPressed(0, GTA.Control.MoveDownOnly))
                {
					float pedMod = 0f;
					if (_selectedProp is Ped)
						pedMod = 1f;
					if (!_controlsRotate)
						_selectedProp.Position -= new Vector3(modifier/4, 0f, pedMod);
					else
						_selectedProp.Rotation -= new Vector3(modifier, 0f, 0f);
				}

                if (Game.IsControlPressed(0, GTA.Control.MoveLeftOnly))
                {
					float pedMod = 0f;
					if (_selectedProp is Ped)
						pedMod = -1f;
					if (!_controlsRotate)
						_selectedProp.Position += new Vector3(0f, modifier/4, pedMod);
					else
						_selectedProp.Rotation += new Vector3(0f, modifier, 0f);
				}
                if (Game.IsControlPressed(0, GTA.Control.MoveRightOnly))
                {
					float pedMod = 0f;
					if (_selectedProp is Ped)
						pedMod = 1f;
					if (!_controlsRotate)
						_selectedProp.Position -= new Vector3(0f, modifier / 4, pedMod);
					else
						_selectedProp.Rotation -= new Vector3(0f, modifier, 0f);
				}

	            if (Game.IsControlJustReleased(0, GTA.Control.MoveLeftRight) ||
	                Game.IsControlJustReleased(0, GTA.Control.MoveUpDown) ||
	                Game.IsControlJustReleased(0, GTA.Control.FrontendLb) ||
	                Game.IsControlJustReleased(0, GTA.Control.FrontendRb))
	            {
					RedrawObjectInfoMenu(_selectedProp);
				}

				if (Game.IsControlJustReleased(0, GTA.Control.LookBehind))
				{
					Entity mainProp = new Prop(0);
					if (_selectedProp is Prop)
						mainProp = PropStreamer.CreateProp(_selectedProp.Model, _selectedProp.Position, _selectedProp.Rotation, !PropStreamer.StaticProps.Contains(_selectedProp.Handle));
					//mainProp = World.CreateProp(_selectedProp.Model, _selectedProp.Position, _selectedProp.Rotation, false, false);
					else if (_selectedProp is Vehicle)
						mainProp = World.CreateVehicle(_selectedProp.Model, _selectedProp.Position);
					else if (_selectedProp is Ped)
						mainProp = Function.Call<Ped>(Hash.CLONE_PED, ((Ped)_selectedProp).Handle, _selectedProp.Rotation.Z, 1, 1);
					_currentProps.Add(mainProp.Handle);
					if (PropStreamer.StaticProps.Contains(_selectedProp.Handle))
					{
						PropStreamer.StaticProps.Add(mainProp.Handle);
						mainProp.FreezePosition = true;
					}
					_selectedProp = mainProp;
					RedrawObjectInfoMenu(_selectedProp);
				}

				if (Game.IsControlJustPressed(0, GTA.Control.CreatorDelete))
				{
					_currentProps.Remove(_selectedProp.Model.Hash);
					//if (Function.Call<bool>(Hash.IS_ENTITY_AN_OBJECT, _selectedProp))
						//PropStreamer.RemoveProp((Prop)_selectedProp);
					if (PropStreamer.StaticProps.Contains(_selectedProp.Handle)) PropStreamer.StaticProps.Remove(_selectedProp.Handle);
					_selectedProp.Delete();
					_selectedProp = null;
					_objectInfoMenu.Visible = false;
				}

				if (Game.IsControlJustPressed(0, GTA.Control.PhoneCancel) || Game.IsControlJustPressed(0, GTA.Control.Attack))
                {
                    _selectedProp = null;
					_objectInfoMenu.Visible = false;
				}
				InstructionalButtonsStart();
				InstructionalButtonsSelected();
				InstructionalButtonsEnd();
			}

        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F7 && !_menuPool.IsAnyMenuOpen())
            {
                _mainMenu.Visible = !_mainMenu.Visible;
            }
        }

        public void OnIndexChange(UIMenu sender, int index)
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

        public void OnObjectSelect(UIMenu sender, UIMenuItem item, int index)
        {
	        int objectHash;
	        switch (_currentObjectType)
	        {
			    case ObjectTypes.Prop:
					objectHash = ObjectDatabase.MainDb[_objectsMenu.MenuItems[_objectsMenu.CurrentSelection].Text];
					//_snappedProp = World.CreateProp(ObjectPreview.LoadObject(objectHash),VectorExtensions.RaycastEverything(new Vector2(0f, 0f)), false, false);
					_snappedProp = PropStreamer.CreateProp(ObjectPreview.LoadObject(objectHash), VectorExtensions.RaycastEverything(new Vector2(0f, 0f)), new Vector3(0, 0, 0), true);
					break;
				case ObjectTypes.Vehicle:
			        objectHash = ObjectDatabase.VehicleDb[_objectsMenu.MenuItems[_objectsMenu.CurrentSelection].Text];
			        _snappedProp = World.CreateVehicle(ObjectPreview.LoadObject(objectHash), VectorExtensions.RaycastEverything(new Vector2(0f, 0f)));
					break;
				case ObjectTypes.Ped:
					objectHash = ObjectDatabase.PedDb[_objectsMenu.MenuItems[_objectsMenu.CurrentSelection].Text];
			        _snappedProp = World.CreatePed(ObjectPreview.LoadObject(objectHash), VectorExtensions.RaycastEverything(new Vector2(0f, 0f)));
					break;
	        }
			_currentProps.Add(_snappedProp.Handle);
            _isChoosingObject = false;
            _objectsMenu.Visible = false;
			_previewProp?.Delete();
        }

        public void RedrawObjectsMenu(string searchQuery = null, ObjectTypes type = ObjectTypes.Prop)
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

	    public void RedrawFormatMenu()
	    {
			_formatMenu.Clear();
			_formatMenu.AddItem(new UIMenuItem("XML", "Default format for Map Editor. Choose this one if you have no idea. This saves props, vehicles and peds."));
			_formatMenu.AddItem(new UIMenuItem("Simple Trainer", "Format used in Simple Trainer mod (objects.ini). Only saves props."));
		    if (_savingMap)
		    {
			    _formatMenu.AddItem(new UIMenuItem("C# Code",
				    "Directly outputs to C# code to spawn your entities. Saves props, vehicles and peds."));
			    _formatMenu.AddItem(new UIMenuItem("Raw",
				    "Writes the entity and their position and rotation. Useful for taking coordinates."));
		    }
		    _formatMenu.RefreshIndex();
		}

	    private Scaleform _scaleform;
	    private void InstructionalButtonsStart()
	    {
		    if(!_drawInstructionalbuttons) return;
			_scaleform.CallFunction("CLEAR_ALL");
			_scaleform.CallFunction("TOGGLE_MOUSE_BUTTONS", 0);
			_scaleform.CallFunction("CREATE_CONTAINER");
		}

	    private void InstructionalButtonsFreelook()
	    {
			if (!_drawInstructionalbuttons) return;
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
			if (!_drawInstructionalbuttons) return;
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
			if (!_drawInstructionalbuttons) return;
			_scaleform.CallFunction("SET_DATA_SLOT", 0, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.FrontendRb, 0), "");
			_scaleform.CallFunction("SET_DATA_SLOT", 1, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.FrontendLb, 0), "Rotate Entity");
			_scaleform.CallFunction("SET_DATA_SLOT", 2, Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int)Control.Attack, 0), "Accept");
		}

		private void InstructionalButtonsEnd()
		{
			if (!_drawInstructionalbuttons) return;
			_scaleform.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", -1);
		}

	    private void RedrawObjectInfoMenu(Entity ent)
	    {
			if(ent == null) return;
		    string name = "";
		    if (Function.Call<bool>(Hash.IS_ENTITY_AN_OBJECT, ent.Handle))
			    name = ObjectDatabase.MainDb.First(x => x.Value == ent.Model.Hash).Key.ToUpper();
			if (Function.Call<bool>(Hash.IS_ENTITY_A_VEHICLE, ent.Handle))
				name = ObjectDatabase.VehicleDb.First(x => x.Value == ent.Model.Hash).Key.ToUpper();
			if (Function.Call<bool>(Hash.IS_ENTITY_A_PED, ent.Handle))
				name = ObjectDatabase.PedDb.First(x => x.Value == ent.Model.Hash).Key.ToUpper();

		    _objectInfoMenu.Subtitle.Caption = "~b~" + name + " PROPERTIES";
			_objectInfoMenu.Clear();
			List<dynamic> possiblePositions = new List<dynamic>();
		    for (int i = -50000; i <= 50000; i++)
		    {
			    possiblePositions.Add(i * 0.1);
		    }
		    var posXitem = new UIMenuListItem("Position X", possiblePositions, Convert.ToInt32(Math.Round((ent.Position.X * 10) + 50000)));
			var posYitem = new UIMenuListItem("Position Y", possiblePositions, Convert.ToInt32(Math.Round((ent.Position.Y * 10) + 50000)));
			var posZitem = new UIMenuListItem("Position Z", possiblePositions, Convert.ToInt32(Math.Round((ent.Position.Z * 10) + 50000)));

			var rotXitem = new UIMenuListItem("Rotation X", possiblePositions, Convert.ToInt32(Math.Round((ent.Rotation.X * 10) + 50000)));
			var rotYitem = new UIMenuListItem("Rotation Y", possiblePositions, Convert.ToInt32(Math.Round((ent.Rotation.Y * 10) + 50000)));
			var rotZitem = new UIMenuListItem("Rotation Z", possiblePositions, Convert.ToInt32(Math.Round((ent.Rotation.Z * 10) + 50000)));

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
	}
}
