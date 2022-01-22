using Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCMPG
{
    public class SCMPGGameScreen : Game.GameScreen
    {
        public override void Leave() {
            Log.Information("Del World");
        base.Leave();
        }
    }
}
