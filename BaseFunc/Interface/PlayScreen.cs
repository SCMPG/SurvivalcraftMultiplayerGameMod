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
            RemoveGameIL();
            Log.Information("Loading PlayScreen");

            WorldInfo WorldInfo = new WorldInfo {
                DirectoryName = "SSSL",
                LastSaveTime = DateTime.Now,
                Size = 1,
                SerializationVersion = GameType.Local.ToString(),
                PlayerInfos = new List<PlayerInfo>(),
                WorldSettings = new WorldSettings()
            };
            List<WorldInfo> worldInfos = new List<WorldInfo>(WorldsManager.WorldInfos);
            WorldsManager.m_worldInfos.Add(WorldInfo);
            




            var a = new GameListMessage {
            ServerPriority=110,
                ServerName="CN.01"
            };
           Log.Information((Message.Read( Message.Write(a)) as GameListMessage).ServerName);
        }
        public override void Update()
        {
            if (base.Children.Find<ButtonWidget>("Play").IsClicked && base.m_worldsListWidget.SelectedItem != null)
            {
                Log.Information("UpLoad World");
                //(WorldInfo)m_worldsListWidget.SelectedItem;
            }
            base.Update();
        }
        private void RemoveGameIL() {
            List<WorldInfo> worldInfos = new List<WorldInfo>(WorldsManager.WorldInfos);
            foreach (var item in worldInfos)
            {
                if (item.SerializationVersion == GameType.Internet.ToString() || item.SerializationVersion == GameType.Local.ToString())
                {
                    Log.Error(item.SerializationVersion+" World that is not Clear");
                }

            }
        }
    }
}
