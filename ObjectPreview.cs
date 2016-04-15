using System;
using System.Collections.Generic;
using System.Drawing;
using GTA;
using GTA.Native;
using NativeUI;

namespace MapEditor
{
    public static class ObjectPreview
    {
        private static int globalCounter = 0;

        public static Model LoadObject(int hash)
        {
            int counter = 0;
            var res = UIMenu.GetScreenResolutionMantainRatio();
            var wid = Convert.ToInt32(res.Width)/2;
            var hei = Convert.ToInt32(res.Height)/2;
            var m = new Model(hash);
	        if (!m.IsValid || !m.IsInCdImage)
	        {
		        if (!ObjectDatabase.InvalidHashes.Contains(hash))
		        {
			        ObjectDatabase.InvalidHashes.Add(hash);
					ObjectDatabase.SaveInvalidHashes();
		        }
		        return null;
	        }
            globalCounter++;
            var sc = new Scaleform(0);
            sc.Load("instructional_buttons");
            sc.CallFunction("CLEAR_ALL");
            sc.CallFunction("TOGGLE_MOUSE_BUTTONS", 0);
            sc.CallFunction("CREATE_CONTAINER");
            sc.CallFunction("SET_DATA_SLOT", 0, "b_50", Translation.Translate("Loading Model"));
            sc.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", -1);
            while (!m.IsLoaded && counter < 200)
			{
                m.Request();
                Script.Yield();
                counter++;
                //new UIResText("LOADING . . . " + globalCounter, new Point(wid, hei), 2f, Color.White, GTA.Font.Pricedown, UIResText.Alignment.Centered).Draw();
                sc.Render2D();
            }
            return m;
        }
    }
}