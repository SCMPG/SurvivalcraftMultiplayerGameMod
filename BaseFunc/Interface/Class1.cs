
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using Engine.Input;
using Game;
using GameEntitySystem;
using TemplatesDatabase;

namespace SCMPG
{
	public class SCMPGPlayerScreen : PlayerScreen
	{
		public override void Leave()
		{
			base.Leave();
			Log.Information("Cool");
		}

		public override void Update()
		{
			base.Update();
			if (base.Input.Back || base.Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
			{
				DialogsManager.ShowDialog(null, new MessageDialog("S", "s", "OK", "Cancel", null));

			}
		}
	}
}