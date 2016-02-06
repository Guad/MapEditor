using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using Microsoft.ClearScript;
using Microsoft.ClearScript.Windows;

namespace MapEditor
{
    public class JavascriptHook : Script
    {
        public JavascriptHook()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
        }

        public static List<JScriptEngine> ScriptEngines = new List<JScriptEngine>();

        public void OnTick(object sender, EventArgs e)
        {
            lock (ScriptEngines)
            foreach (var engine in ScriptEngines)
            {
                try
                {
                    engine.Script.script.InvokeTick();
                }
                catch (ScriptEngineException ex)
                {
                    LogException(ex);
                }
            }
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            lock (ScriptEngines)
            foreach (var engine in ScriptEngines)
            {
                try
                {
                    engine.Script.script.InvokeKeyDown(e);
                }
                catch (ScriptEngineException ex)
                {
                    LogException(ex);
                }
            }
        }

        public void OnKeyUp(object sender, KeyEventArgs e)
        {
            lock (ScriptEngines)
            foreach (var engine in ScriptEngines)
            {
                try
                {
                    engine.Script.script.InvokeKeyUp(e);
                }
                catch (ScriptEngineException ex)
                {
                    LogException(ex);
                }
            }
        }

        public static void StartScript(string script, List<int> identEnts)
        {
            var scriptEngine = new JScriptEngine();
            var collection = new HostTypeCollection(Assembly.LoadFrom("scripthookvdotnet.dll"),
                Assembly.LoadFrom("scripts\\NativeUI.dll"));
            scriptEngine.AddHostObject("API", collection);
            scriptEngine.AddHostObject("host", new HostFunctions());
            scriptEngine.AddHostObject("script", new ScriptContext());
            scriptEngine.AddHostType("Enumerable", typeof(Enumerable));
            scriptEngine.AddHostType("List", typeof(IList));
            scriptEngine.AddHostType("KeyEventArgs", typeof(KeyEventArgs));
            scriptEngine.AddHostType("Keys", typeof(Keys));
            
            foreach (var obj in identEnts)
            {
                var name = PropStreamer.Identifications[obj];
                if (MapEditor.IsPed(new Prop(obj)))
                    scriptEngine.AddHostObject(name, new Ped(obj));
                else if (MapEditor.IsVehicle(new Prop(obj)))
                    scriptEngine.AddHostObject(name, new Vehicle(obj));
                else if (MapEditor.IsProp(new Prop(obj)))
                    scriptEngine.AddHostObject(name, new Prop(obj));
            }

            try
            {
                scriptEngine.Execute(script);
            }
            catch (ScriptEngineException ex)
            {
                LogException(ex);
            }
            finally
            {
                lock (ScriptEngines)
                    ScriptEngines.Add(scriptEngine);
            }
        }

        public static void StopAllScripts()
        {
            lock (ScriptEngines)
            {
                foreach (var engine in ScriptEngines)
                {
                    engine.Script.script.InvokeDispose();
                    engine.Dispose();
                }

                ScriptEngines.Clear();
            }
        }

        private static void LogException(Exception ex)
        {
            Func<string, int, string[]> splitter = (string input, int everyN) =>
            {
                var list = new List<string>();
                for (int i = 0; i < input.Length; i += everyN)
                {
                    list.Add(input.Substring(i, Math.Min(everyN, input.Length - i)));
                }
                return list.ToArray();
            };

            UI.Notify("~r~~h~Map Javascript Error~h~~w~");

            foreach (var s in splitter(ex.Message, 99))
            {
                UI.Notify(s);
            }

            if (ex.InnerException != null)
                foreach (var s in splitter(ex.InnerException.Message, 99))
                {
                    UI.Notify(s);
                }
        }
    }

    public class ScriptContext
    {
        public enum ReturnType
        {
            Int = 0,
            UInt = 1,
            Long = 2,
            ULong = 3,
            String = 4,
            Vector3 = 5,
            Vector2 = 6,
            Float = 7
        }

        public void CallNative(string hash, params object[] args)
        {
            Hash ourHash;
            if (!Hash.TryParse(hash, out ourHash))
                return;
            Function.Call(ourHash, args.Select(o => new InputArgument(o)).ToArray());
        }

        public object ReturnNative(string hash, int returnType, params object[] args)
        {
            Hash ourHash;
            if (!Hash.TryParse(hash, out ourHash))
                return null;
            var fArgs = args.Select(o => new InputArgument(o)).ToArray();
            switch ((ReturnType)returnType)
            {
                case ReturnType.Int:
                    return Function.Call<int>(ourHash, fArgs);
                case ReturnType.UInt:
                    return Function.Call<uint>(ourHash, fArgs);
                case ReturnType.Long:
                    return Function.Call<long>(ourHash, fArgs);
                case ReturnType.ULong:
                    return Function.Call<ulong>(ourHash, fArgs);
                case ReturnType.String:
                    return Function.Call<string>(ourHash, fArgs);
                case ReturnType.Vector3:
                    return Function.Call<Vector3>(ourHash, fArgs);
                case ReturnType.Vector2:
                    return Function.Call<Vector2>(ourHash, fArgs);
                case ReturnType.Float:
                    return Function.Call<float>(ourHash, fArgs);
                default:
                    return null;
            }
        }

        public void MoveProp(Prop prop, Vector3 newPos, Vector3 newRot, int duration)
        {
            var start = Game.GameTime;
            while (Game.GameTime - start <= duration)
            {
                prop.Position = NativeUI.MiscExtensions.LinearVectorLerp(prop.Position, newPos, Game.GameTime - start, duration);
                prop.Rotation = NativeUI.MiscExtensions.LinearVectorLerp(prop.Rotation, newRot, Game.GameTime - start, duration);
                Script.Yield();
            }
        }

        public bool IsPed(int ent)
        {
            return Function.Call<bool>(Hash.IS_ENTITY_A_PED, ent);
        }

        public bool IsVehicle(int ent)
        {
            return Function.Call<bool>(Hash.IS_ENTITY_A_VEHICLE, ent);
        }

        public bool IsProp(int ent)
        {
            return Function.Call<bool>(Hash.IS_ENTITY_AN_OBJECT, ent);
        }

        public int[] GetCurrentMap()
        {
            return PropStreamer.GetAllHandles();
        }

        public event EventHandler OnDispose;
        public event EventHandler OnTick;
        public event KeyEventHandler OnKeyDown;
        public event KeyEventHandler OnKeyUp;

        public void InvokeTick()
        {
            OnTick?.Invoke(this, EventArgs.Empty);
        }

        public void InvokeKeyDown(KeyEventArgs e)
        {
            OnKeyDown?.Invoke(this, e);
        }

        public void InvokeKeyUp(KeyEventArgs e)
        {
            OnKeyUp?.Invoke(this, e);
        }

        public void InvokeDispose()
        {
            OnDispose?.Invoke(this, EventArgs.Empty);
        }
    }

}