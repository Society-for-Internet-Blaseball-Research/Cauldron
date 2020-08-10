using Cauldron;
using System;
using System.Collections.Generic;
using System.Text;

namespace CauldronVisualizer
{
	public class GameUpdateVm
	{
		public Game Update => m_update;
		Game m_update;
		public GameUpdateVm(Game update)
		{
			m_update = update;
		}
	}
}
