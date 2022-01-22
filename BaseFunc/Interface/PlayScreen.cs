using Engine;
using Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCMPG
{
    public class SCMPGPlayScreen : Game.PlayScreen
    {
        /*public SCMPGPlayScreen() {
            base.m_worldsListWidget.ItemClicked += delegate (object item)
            {
                if (item != null && base.m_worldsListWidget.SelectedItem == item)
                {
                    Log.Information("UpLoad World");
                }
            };
        }*/
        public override void Enter(object[] parameters) { 
        base.Enter(parameters);
            Log.Information("Loading PlayScreen");
        }
        public override void Update()
        {
            if (base.Children.Find<ButtonWidget>("Play").IsClicked && base.m_worldsListWidget.SelectedItem != null)
            {
                Log.Information("UpLoad World");
            }
            base.Update();
        }
    }
}
