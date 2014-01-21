using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Ants {

	class MyBot : Bot {
		private Dictionary<State, Dictionary<Action, double>> Q;
		private const double Alpha = 0.1; 
		private const double Gamma = 0.5; 
		private int maxDistance;
		private List<Action> actions = new List<Action>();
		private Dictionary<State, Action> performedActions = new Dictionary<State, Action>();
		private int previousMyAntsCount;

		// DoTurn is run once per turn
		public override void DoTurn (IGameState gameState) {
			if (Q == null) {
				Dictionary<int, Dictionary<int, double>> knownQ = new Dictionary<int, Dictionary<int, double>>();

				FileStream fs = new FileStream("q.log", FileMode.Open, FileAccess.Read);
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
				for (int i = -1; i <= maxDistance; i++)
				{
					Dictionary<StateParameter, int> distances = new Dictionary<StateParameter, int>();
					distances[StateParameter.Food] = i == -1 ? int.MaxValue : i;

					for (int j = -1; j <= maxDistance; j++) {						
						distances[StateParameter.EnemyAnt] = j == -1 ? int.MaxValue : j;

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
					}
				}
			}

			// Q's updaten
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


			// loop through all my ants and try to give them orders
			foreach (Ant ant in gameState.MyAnts) {
				//Build the state per ant.
				Location target;
				Action nextAction = null;

				State state = buildState(gameState, ant);

				// Use learned Q values
				/*double maxQ = double.MinValue;
				foreach(KeyValuePair<Action, double>pair in Q[state])
				{
					if (pair.Value > maxQ)
					{
						maxQ = pair.Value;
						nextAction = pair.Key;
					}
				}
				target = state.Targets[nextAction.Parameter];*/

				// Random
				int distance;
				do {
					nextAction = actions[new Random().Next(actions.Count)];
					distance = state.Distances[nextAction.Parameter];
					target = state.Targets[nextAction.Parameter];
				}
				while(distance == int.MaxValue);

				performedActions[state] = nextAction;

				Direction direction = gameState.GetDirections(ant, target).First();
				if (nextAction.Direction == ActionDirection.AwayFrom)
					direction = direction.Opposite();
				IssueOrder(ant, direction);
			}

			FileStream fs1 = new FileStream("q.log", FileMode.Create, FileAccess.Write);
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
		
		//Is ugly
		public State buildState(IGameState gameState, Location loc)
		{
			Dictionary<StateParameter, int> distances = new Dictionary<StateParameter, int>();
			Dictionary<StateParameter, Location> targets = new Dictionary<StateParameter, Location>();
			
			int foodDistance = int.MaxValue;
			Location closestFood = null;
			foreach(Location food in gameState.FoodTiles) {
				int delta = gameState.GetDistance(loc, food);
				if (delta < foodDistance)
				{ 
					foodDistance = delta;
					closestFood = food;
				}
			}

			distances[StateParameter.Food] = foodDistance;
			targets[StateParameter.Food] = closestFood;

			int enemyDistance = int.MaxValue;
			Location closestEnemy = null;
			foreach(Location enemy in gameState.EnemyAnts) {
				int delta = gameState.GetDistance(loc, enemy);
				if (delta < enemyDistance) 
				{
					enemyDistance = delta;
					closestEnemy = enemy;
				}
			}
			distances[StateParameter.EnemyAnt] = enemyDistance;
			targets[StateParameter.EnemyAnt] = closestEnemy;

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