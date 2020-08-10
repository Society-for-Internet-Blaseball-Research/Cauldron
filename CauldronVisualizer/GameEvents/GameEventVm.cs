using Cauldron;
using System;
using System.Collections.Generic;
using System.Text;

namespace CauldronVisualizer
{
	public class GameEventVm
	{
		public GameEvent Event => m_event;
		GameEvent m_event;

		public string InningDescription
		{
			get
			{
				if (m_event.topOfInning)
					return $"Top of {m_event.inning + 1}, {m_event.outsBeforePlay} outs";
				else
					return $"Bottom of {m_event.inning + 1}, {m_event.outsBeforePlay} outs";
			}
		}
		public GameEventVm(GameEvent e)
		{
			m_event = e;
		}
	}
}
