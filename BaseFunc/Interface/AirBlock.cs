using Engine;
using Engine.Graphics;
using Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SCMPG
{
    
    public static class SCMPGManager{
        public static event Action Initialize;
        public static void Update() {
            Initialize += SCMPGC.Func1;
            Initialize += SCMPGC.Func2;
            Initialize += SCMPGC.Func3;
            SCMPGManager.Initialize?.Invoke();
        }
    }
    public class SCMPGC {
        public static void Func1()
        {
            Log.Information("111");
            Game.ScreensManager.FindScreen<LoadingScreen>("Loading").AddLoadAction(delegate {
                Game.ScreensManager.m_screens["Player"] = new SCMPGPlayerScreen();
                Game.ScreensManager.m_screens["Play"] = new SCMPGPlayScreen();
                Game.ScreensManager.m_screens["Game"] = new SCMPGGameScreen();
            });
        }
        public static void Func2()
        {
            Log.Information("222");
        }
        public static void Func3()
        {
            Log.Information("333");
        }

    }
    
    public class AirBlock: Game.AirBlock
    {
        public new const int Index = 0;
        private bool IsAdded=false;

        public override void Initialize()
        {
            if (!IsAdded)
            {
                IsAdded=true;
                SCMPGManager.Update();
            }
           
            base.Initialize();
        }
    }
}

// var fieldInfo = typeof(ScreensManager).GetField("m_screens", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
//((Dictionary<string, Screen>)fieldInfo.GetValue(null))["Player"] = new SCMPGPlayerScreen(); ;
//var c = (Dictionary<string, Screen>)fieldInfo.GetValue(null);
//var c = Game.ScreensManager.m_screens;

//  Log.Information();
//  c["Play"] = new NewWorldScreen();