using System;
using System.Collections.Generic;

namespace Ants {

	class MyBot : Bot {
		int[,] R;
		double [,] Q;
		const double Alpha = 0.6; //
		const double Gamma = 0.6; //

		const int WaterReward 	= -10;
		const int FoodReward 	= 50;

		// DoTurn is run once per turn
		public override void DoTurn (IGameState state) {
			if(R == null) {
				R = new int[state.Width, state.Height];
				Q = new double[state.Width, state.Height];
			}

			for (int x = 0; x < state.Width; x++) {
				for (int y = 0; y < state.Height; y++) {
					if (!state.IsPassable(x, y))
						R[x,y] = WaterReward;
					else if (state[x,y] == Tile.Food)
						R[x,y] = FoodReward; 
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

			double q = Q[state, action];
			int r = R[state, action];
			double maxQ = maxQ(action);
			Q[state, action] = (1.0 - Alpha) * q + Alpha * (r + Gamma * maxQ);
		}
		
		
		public static void Main (string[] args) {
			new Ants().PlayGame(new MyBot());
		}
	}
}