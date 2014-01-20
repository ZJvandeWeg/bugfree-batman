using System;
using System.Collections.Generic;

namespace Ants {

	class MyBot : Bot {
		Dictionary<State, Dictionary<Action, double>> Q;
		const double Alpha = 0.6; 
		const double Gamma = 0.6; 
		int maxDistance;
		List<Action> actions = new List<Action>();

		// DoTurn is run once per turn
		public override void DoTurn (IGameState state) {
			if (Q == null) {

				maxDistance = state.Width / 2 + state.Height / 2;


				Q = new Dictionary<State, Dictionary<Action, double>>();

				foreach (StateParameter parameter in (StateParameter[]) Enum.GetValues(typeof(StateParameter)))
				{
					Action a = new Action(parameter, ActionDirection.Towards);
					actions.Add(a);
					a = new Action(parameter, ActionDirection.AwayFrom);
					actions.Add(a);
				}


				foreach (StateParameter parameter in (StateParameter[]) Enum.GetValues(typeof(StateParameter)))
				{
					for (int i = 0; i <= maxDistance; i++)
					{
						Dictionary<StateParameter, int> distances = new Dictionary<StateParameter, int>();
						distances[StateParameter.Food] = i;

						for (int j = 0; j <= maxDistance; j++)
						{
							distances[StateParameter.EnemyAnt] = j;
						}

						State s = new State(distances);
						Q[s] = new Dictionary<Action, double>();

						foreach (Action a in actions)
						{
							Q[s][a] = 0.0;
						}
					}
				}
			}

			// loop through all my ants and try to give them orders
			foreach (Ant ant in state.MyAnts) {
				
				// try all the directions
				foreach (Direction direction in Ants.Aim.Keys) {

					// GetDestination will wrap around the map properly
					// and give us a new location
					Location newLoc = state.GetDestination(ant, direction);

					// GetIsPassable returns true if the location is land
					if (state.GetIsPassable(newLoc)) {
						IssueOrder(ant, direction);
						// stop now, don't give 1 and multiple orders
						break;
					}
				}
				// check if we have time left to calculate more orders
				if (state.TimeRemaining < 10) break;
			}

		//	double q = Q[state, action];
		//	int r = R[state, action];
		//`	double maxQ = maxQ(action);
		//	Q[state, action] = (1.0 - Alpha) * q + Alpha * (r + Gamma * maxQ);
		}
		
		
		public static void Main (string[] args) {
			new Ants().PlayGame(new MyBot());
		}
	}
}