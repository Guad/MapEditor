using System;
using System.Collections.Generic;
using System.Drawing;
using GTA;
using NativeUI;

namespace MapEditor
{
    public static class ObjectPreview
    {
        public static List<int> ActiveObjects;

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

			while (!m.IsLoaded && counter < 200)
			{
                m.Request();
                Script.Yield();
                counter++;
                new UIResText("LOADING . . .", new Point(wid, hei), 2f, Color.White, GTA.Font.Pricedown, UIResText.Alignment.Centered).Draw();
            }
            return m;
        }
    }
}