using System;
using System.Collections.Generic;

namespace Ants {

	public enum ActionDirection { Towards, AwayFrom }

	class Action
	{
		public StateParameter Parameter;
		public ActionDirection Direction; 

		public Action(StateParameter parameter, ActionDirection direction)
		{
			Parameter = parameter;
			Direction = direction;
		}

		public override int GetHashCode()
		{
			return (int)Parameter * 100 + (int)Direction;
		}
	}
}