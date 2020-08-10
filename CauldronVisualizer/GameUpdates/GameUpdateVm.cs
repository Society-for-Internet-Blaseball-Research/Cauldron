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

		public string InningDescription
		{
			get
			{
				if (Update.topOfInning)
					return $"Top of {Update.inning+1}, {Update.halfInningOuts} outs";
				else
					return $"Bottom of {Update.inning+1}, {Update.halfInningOuts} outs";
			}
		}
		public GameUpdateVm(Game update)
		{
			m_update = update;
		}
	}
}
