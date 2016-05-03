using System.Windows.Forms;

namespace MapEditor
{
	public class Settings
	{
	    public Settings()
	    {
	        CameraSensivity = 30;
	        GamepadCameraSensitivity = 5;
	        KeyboardMovementSensitivity = 30;
	        GamepadMovementSensitivity = 15;
	        Gamepad = true;
	        InstructionalButtons = true;
	        CrosshairType = MapEditor.CrosshairType.Crosshair;
	        PropCounterDisplay = true;
	        SnapCameraToSelectedObject = true;
	        ActivationKey = Keys.F7;
	        AutosaveInterval = 5;
	        DrawDistance = -1;
	        LoadScripts = true;
	        Translation = "Auto";
	        OmitInvalidObjects = true;
	    }

	    public string Translation;
		public bool Gamepad;
		public MapEditor.CrosshairType CrosshairType;
		public int CameraSensivity;
	    public int GamepadCameraSensitivity;
	    public int KeyboardMovementSensitivity;
	    public int GamepadMovementSensitivity;
		public bool InstructionalButtons;
		public bool PropCounterDisplay;
		public bool SnapCameraToSelectedObject;
		public Keys ActivationKey;
	    public int AutosaveInterval;
	    public int DrawDistance;
	    public bool LoadScripts;
	    public bool? BoundingBox;
	    public bool OmitInvalidObjects;
	}
}