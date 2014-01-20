using System;
using System.Collections.Generic;

namespace Ants {

	public enum StateParameter { Food, EnemyAnt }

	class State
	{
		public Dictionary<StateParameter, int> Distances = new Dictionary<StateParameter, int>();

		public State(Dictionary<StateParameter, int> distances)
		{
			Distances = distances;
		}

		public override int GetHashCode()
		{
			int code = 0;
			foreach (KeyValuePair<StateParameter, int> pair in Distances)
			{
				code += pair.Value * (int)pair.Key * 100;
			}
			return code;
		}
	}
}