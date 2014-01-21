using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Ants {

	class MyBot : Bot {
		private Dictionary<State, Dictionary<Action, double>> Q;
		private const double Alpha = 0.1; 
		private const int Beta = 96; 
		private const double Gamma = 0.5; 
		private int maxDistance;
		private List<Action> actions = new List<Action>();
		private Dictionary<State, Action> performedActions = new Dictionary<State, Action>();
		private int previousMyAntsCount;
		private string logFileName;
		private bool useLearned;

		// DoTurn is run once per turn
		public override void DoTurn (IGameState gameState) {
			//Init Q, with data from previous runs.
			initQ(gameState);

			//Learn new shit this turn?
			useLearned = new Random().Next(0,100) < Beta;

			// Q's updaten
			updateQ(gameState);

			// loop through all my ants and try to give them orders
			orderAnts(gameState);

			FileStream fs1 = new FileStream(logFileName, FileMode.Create, FileAccess.Write);
			StreamWriter sw = new StreamWriter(fs1);
			foreach(KeyValuePair<State, Dictionary<Action, double>> pair in Q)
			{
				State state = pair.Key;
				foreach(KeyValuePair<Action, double>pair2 in pair.Value)
				{
					Action action = pair2.Key;
					double q = pair2.Value;

					sw.WriteLine(state.GetHashCode() + "," + action.GetHashCode() + "," + q);
				}
			}
			sw.Close();
			fs1.Close();

		}

		public void initQ(IGameState gameState)
		{
			if (Q == null) {
			 	logFileName = "q" + gameState.Width + "," + gameState.Height + ".log";

				Dictionary<int, Dictionary<int, double>> knownQ = new Dictionary<int, Dictionary<int, double>>();

				if(!File.Exists(logFileName))
				{
					File.Create(logFileName);
				}

				FileStream fs = new FileStream(logFileName, FileMode.Open, FileAccess.Read);
				StreamReader sr = new StreamReader(fs);
				string line;
				while ((line = sr.ReadLine()) != null)
				{
					string[] parts = line.Split(',');
					int stateCode = int.Parse(parts[0]);
					int actionCode = int.Parse(parts[1]);
					double q = double.Parse(parts[2]);
					if (!knownQ.ContainsKey(stateCode))
					{
						knownQ[stateCode] = new Dictionary<int, double>();
					}
					knownQ[stateCode][actionCode] = q;
				}
				sr.Close();
				fs.Close();

				maxDistance = gameState.Width / 2 + gameState.Height / 2;
				Q = new Dictionary<State, Dictionary<Action, double>>();

				foreach (StateParameter parameter in (StateParameter[]) Enum.GetValues(typeof(StateParameter)))
				{
					Action a = new Action(parameter, ActionDirection.Towards);
					actions.Add(a);
					a = new Action(parameter, ActionDirection.AwayFrom);
					actions.Add(a);
				}

				//This is ugly
				Dictionary<StateParameter, int> distances = new Dictionary<StateParameter, int>();
				for (int i = 0; i <= maxDistance + 1; i++)
				{
					distances[StateParameter.Food] = i;

					//for (int j = 0; j <= maxDistance + 1; j++) {						
					//	distances[StateParameter.OwnAnt] = j;

					//	for (int k = 0; k <= 1; k++) {						
					//		distances[StateParameter.OwnHill] = k;

							State s = new State(new Dictionary<StateParameter, int>(distances));
							Q[s] = new Dictionary<Action, double>();

							foreach (Action a in actions) {
								int stateCode = s.GetHashCode();
								int actionCode = a.GetHashCode();
								double q = 0.0;
								if (knownQ.ContainsKey(stateCode) && knownQ[stateCode].ContainsKey(actionCode)) {
									q = knownQ[stateCode][actionCode];
								}			
								Q[s][a] = q;
							}
					//	}
					//}
				}
			}
		}
		
		public void updateQ(IGameState gameState)
		{
			foreach(KeyValuePair<State, Action>pair in performedActions)
			{
				State fromState = pair.Key;
				Action action = pair.Value;

				State nextState = fromState.ApplyAction(action);

				// Loss of ant: -100, gain of ant: 100
				int r = (gameState.MyAnts.Count - previousMyAntsCount) * 100;

				double q = Q[fromState][action];
				double maxQ = MaxQ(nextState);
				Q[fromState][action] = (1.0 - Alpha) * q + Alpha * (r + Gamma * maxQ);
			}
			performedActions.Clear();
			previousMyAntsCount = gameState.MyAnts.Count;
		}

		public void orderAnts(IGameState gameState)
		{
			foreach (Ant ant in gameState.MyAnts) {
				//Build the state per ant.
				Location target;
				Action nextAction = null;

				State state = buildState(gameState, ant);

				if (useLearned)
				{
					// Use learned Q values
					double maxQ = double.MinValue;
					foreach(KeyValuePair<Action, double>pair in Q[state])
					{
						Action action = pair.Key;
						double q = pair.Value;
						if (q > maxQ && state.Targets[action.Parameter] != null)
						{
							maxQ = q;
							nextAction = action;
						}
					}
				}
				else {
					// Random
					int distance;
					do {
						nextAction = actions[new Random().Next(actions.Count)];
						distance = state.Distances[nextAction.Parameter];
					}
					while(distance == maxDistance + 1 || state.Targets[nextAction.Parameter] == null);
				}
				
				target = state.Targets[nextAction.Parameter];

				if (target == null)
					continue;

				performedActions[state] = nextAction;

				List<Direction> directions = (List<Direction>)(gameState.GetDirections(ant, target));
				if (directions.Count == 0)
					continue;

				Direction direction = directions[new Random().Next(directions.Count)];
				if (nextAction.Direction == ActionDirection.AwayFrom)
					direction = direction.Opposite();

				IssueOrder(ant, direction);
			}
		}
		//Is ugly
		public State buildState(IGameState gameState, Location ant)
		{
			Dictionary<StateParameter, int> distances = new Dictionary<StateParameter, int>();
			Dictionary<StateParameter, Location> targets = new Dictionary<StateParameter, Location>();
			
			int foodDistance = maxDistance + 1;
			List<Location> closestFoods = new List<Location>();
			foreach(Location food in gameState.FoodTiles) {
				int delta = gameState.GetDistance(ant, food);
				if (delta < foodDistance)
				{ 
					closestFoods.Clear();
					foodDistance = delta;
				}
				if (delta <= foodDistance) {
					closestFoods.Add(food);
				}
			}
			Location closestFood = closestFoods.Count == 0 ? null : closestFoods[new Random().Next(closestFoods.Count)];

			distances[StateParameter.Food] = foodDistance;
			targets[StateParameter.Food] = closestFood;

			/*
			int friendDistance = maxDistance + 1;
			List<Location> closestFriends = new List<Location>();
			foreach(Location friend in gameState.MyAnts) {
				if (friend == ant) continue;
				int delta = gameState.GetDistance(ant, friend);
				if (delta < friendDistance) 
				{
					closestFriends.Clear();
					friendDistance = delta;
				}
				if (delta <= friendDistance) {
					closestFriends.Add(friend);
				}
			}
			Location closestFriend = closestFriends.Count == 0 ? null : closestFriends[new Random().Next(closestFriends.Count)];

			distances[StateParameter.OwnAnt] = friendDistance;
			targets[StateParameter.OwnAnt] = closestFriend;

			bool onHill = false;
			Location closestHill = null;
			foreach(Location hill in gameState.MyHills) {
				if (ant == hill)
				{
					onHill = true;
					closestHill = hill;
					break;
				}
			}
			distances[StateParameter.OwnHill] = onHill ? 0 : 1;
			targets[StateParameter.OwnHill] = closestHill;
			*/

			return new State(distances, targets);
		}

		public double MaxQ(State state)
		{
			double maxQ = double.MinValue;
			foreach(Action action in actions)
			{
				double q = Q[state][action];
				if (q > maxQ)
					maxQ = q;
			}
			return maxQ;
		}

		public static void Main (string[] args) {
			new Ants().PlayGame(new MyBot());
		}
	}
}