using System.Windows.Forms;

namespace MapEditor
{
	public class Settings
	{
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
	}
}