using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Ants {

	class MyBot : Bot {
		private Dictionary<Location, Dictionary<Direction, double>> Qabsolute;
		private Dictionary<State, Dictionary<Action, double>> Qrelative;
		private const double Alpha = 1.0; 
		private const int Beta = 100; 
		private const double Gamma = 0.1;
		private Dictionary<Location, Direction> performedMoves = new Dictionary<Location, Direction>();
		private Dictionary<State, List<OurTuple<Action, Location>>> performedActions = new Dictionary<State, List<OurTuple<Action, Location>>>();
		private string logAbsolute;
		private string logRelative;
		private bool useLearned;
		private int maxDistance;
		private IGameState gameState;
		private List<Action> actions = new List<Action>();

		// DoTurn is run once per turn
		public override void DoTurn (IGameState gameState) {
			this.gameState = gameState;
			initAbsoluteQ();
			initRelativeQ();

			//Will we do random moves this turn, learning goes faster.
			useLearned = new Random().Next(0,100) < Beta;

			updateAbsoluteQ();
			updateRelativeQ();

			// loop through all my ants and try to give them orders
			orderAnts();

			writeAbsoluteLog();
			writeRelativeLog();			
		}

		public void writeAbsoluteLog()
		{
			FileStream fs = new FileStream(logAbsolute, FileMode.Create, FileAccess.Write);
			StreamWriter sw = new StreamWriter(fs);
			foreach(KeyValuePair<Location, Dictionary<Direction, double>> pair in Qabsolute)
			{
				Location loc = pair.Key;
				foreach(KeyValuePair<Direction, double>pair2 in pair.Value)
				{
					Direction direction = pair2.Key;
					double q = pair2.Value;

					sw.WriteLine(loc.ToString() + " " + direction.ToChar() + " " + q);
				}
			}
			sw.Close();
			fs.Close();
		}

		public void writeRelativeLog()
		{
			FileStream fs = new FileStream(logRelative, FileMode.Create, FileAccess.Write);
			StreamWriter sw = new StreamWriter(fs);
			foreach(KeyValuePair<State, Dictionary<Action, double>> pair in Qrelative)
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
			fs.Close();
		}

		public void initAbsoluteQ()
		{
			if (Qabsolute != null) return;

		 	logAbsolute = "Absolute" + gameState.Width + "," + gameState.Height + ".Q";

			Dictionary<Location, Dictionary<Direction, double>> knownQ = new Dictionary<Location, Dictionary<Direction, double>>();

			if(!File.Exists(logAbsolute)) {
				File.Create(logAbsolute);
			}

			FileStream fs = new FileStream(logAbsolute, FileMode.Open, FileAccess.Read);
			StreamReader sr = new StreamReader(fs);
			string line;
			while ((line = sr.ReadLine()) != null)
			{
				string[] parts = line.Split();
				Location loc = new Location(parts[0]);

				Direction dir;
				switch (parts[1][0])
				{
					case 'e':
						dir = Direction.East;
						break;

					case 'n':
						dir = Direction.North;
						break;

					case 's':
						dir = Direction.South;
						break;

					case 'w':
					default:
						dir = Direction.West;
						break;
				}

				double q = double.Parse(parts[2]);
				if (!knownQ.ContainsKey(loc))
				{
					knownQ[loc] = new Dictionary<Direction, double>();
				}
				knownQ[loc][dir] = q;
			}
			sr.Close();
			fs.Close();

			Qabsolute = new Dictionary<Location, Dictionary<Direction, double>>();

			//This is ugly
			for (int x = 0; x < gameState.Width; x++)
			{
				for (int y = 0; y < gameState.Height; y++)
				{	
					Location loc = new Location(x, y);
					Qabsolute[loc] = new Dictionary<Direction, double>();

					foreach (Direction dir in (Direction[]) Enum.GetValues(typeof(Direction)))
					{
						double q = 0.0;
						if (knownQ.ContainsKey(loc) && knownQ[loc].ContainsKey(dir)) {
							q = knownQ[loc][dir];
						}			
						Qabsolute[loc][dir] = q;
					}
				}
			}
		}
		
		public void initRelativeQ()
		{
			if (Qrelative == null) return;

		 	logRelative = "Relative" + gameState.Width + "," + gameState.Height + ".Q";

			Dictionary<int, Dictionary<int, double>> knownQ = new Dictionary<int, Dictionary<int, double>>();

			if(!File.Exists(logRelative))
			{
				File.Create(logRelative);
			}

			FileStream fs = new FileStream(logRelative, FileMode.Open, FileAccess.Read);
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
			Qrelative = new Dictionary<State, Dictionary<Action, double>>();

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
				distances[StateParameter.OwnAnt] = i;

				State s = new State(new Dictionary<StateParameter, int>(distances));
				Qrelative[s] = new Dictionary<Action, double>();

				foreach (Action a in actions) {
					int stateCode = s.GetHashCode();
					int actionCode = a.GetHashCode();
					double q = 0.0;
					if (knownQ.ContainsKey(stateCode) && knownQ[stateCode].ContainsKey(actionCode)) {
						q = knownQ[stateCode][actionCode];
					}			
					Qrelative[s][a] = q;
				}
			}
		}

		public void updateAbsoluteQ()
		{
			foreach(KeyValuePair<Location, Direction>pair in performedMoves)
			{
				Location loc = pair.Key;
				Direction dir = pair.Value;

				Location nextLocation = gameState.GetDestination(loc, dir);

				int r = 0;
				if (!gameState.MyAnts.Contains(nextLocation) && 
					!gameState.DeadTiles.Contains(nextLocation) &&
					!gameState.DeadTiles.Contains(loc))
					r = -100;

				// We don't need Gamma here, because surrounding locations aren't affected by one location's
				// negative reward due to it not being passable.
				// We don't need Alpha here, either, because a location either *is* or *is not* passable, there's
				// no weight to the information we just acquired.
				Qabsolute[loc][dir] = r;
			}

			performedMoves.Clear();
		}

		public void updateRelativeQ()
		{
			foreach(KeyValuePair<State, List<OurTuple<Action, Location>>>pair in performedActions)
			{
				State fromState = pair.Key;

				foreach(OurTuple<Action, Location>tuple in pair.Value)
				{
					Action action = tuple.Item1;
					Location nextLocation = tuple.Item2;

					State nextState = fromState.ApplyAction(action);

					int r = 0;
					if (gameState.DeadTiles.Contains(nextLocation))
						r = -200;

					double q = Qrelative[fromState][action];
					double maxQ = MaxRelativeQ(nextState);
					Qrelative[fromState][action] = (1.0 - Alpha) * q + Alpha * (r + Gamma * maxQ);
				}
			}
			performedActions.Clear();
		}

		public void orderAnts()
		{
			foreach (Ant ant in gameState.MyAnts) {
				Location loc = (Location)ant;
				//Build the state per ant.

				State state = buildState(ant);

				Direction nextDirection = Direction.North;
				
				List<OurTuple<Action, Location>> actions = new List<OurTuple<Action, Location>>();
					
				if (useLearned)
				{
					// Use learned Q values
					double maxQ = double.MinValue;
					List<Direction> directions = new List<Direction>();
					foreach(KeyValuePair<Direction, double>pair in Qabsolute[loc])
					{
						Direction dir = pair.Key;
						double absoluteQ = pair.Value;

						double relativeQ = 0.0;
						foreach(Location friend in state.Targets[StateParameter.OwnAnt])
						{
							Action action;

							List<Direction> directionsToward = (List<Direction>)gameState.GetDirections(ant, friend);
							if (directionsToward.Contains(dir))
							{
								action = new Action(StateParameter.OwnAnt, ActionDirection.Towards);
							}
							else {
								action = new Action(StateParameter.OwnAnt, ActionDirection.AwayFrom);
							}

							relativeQ += Qrelative[state][action];
						}
						double q = absoluteQ + relativeQ;

						if (q > maxQ)
						{
							directions.Clear();
							maxQ = q;
						}
						if (q >= maxQ)
						{
							directions.Add(dir);
						}
					}
					nextDirection = directions[new Random().Next(directions.Count)];
				}
				else {
					// Random
					Direction[] directions = (Direction[]) Enum.GetValues(typeof(Direction));
					nextDirection = directions[new Random().Next(directions.Length)];
					
					foreach(Location friend in state.Targets[StateParameter.OwnAnt])
					{
						Action action;

						List<Direction> directionsToward = (List<Direction>)gameState.GetDirections(ant, friend);
						if (directionsToward.Contains(nextDirection))
						{
							action = new Action(StateParameter.OwnAnt, ActionDirection.Towards);
						}
						else {
							action = new Action(StateParameter.OwnAnt, ActionDirection.AwayFrom);
						}

						actions.Add(new OurTuple<Action, Location>(action, friend));
					}
				}

				performedMoves[loc] = nextDirection;
				performedActions[state] = actions;

				IssueOrder(loc, nextDirection);
			}
		}

		/*
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
		*/
		//Is ugly
		public State buildState(Location ant)
		{
			Dictionary<StateParameter, int> distances = new Dictionary<StateParameter, int>();
			Dictionary<StateParameter, List<Location>> targets = new Dictionary<StateParameter, List<Location>>();
	
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

			distances[StateParameter.OwnAnt] = friendDistance;
			targets[StateParameter.OwnAnt] = closestFriends;

			return new State(distances, targets);
		}

		public double MaxRelativeQ(State state)
		{
			double maxQ = double.MinValue;
			foreach (Action action in actions)
			{
				double q = Qrelative[state][action];
				if (q > maxQ)
					maxQ = q;
			}
			return maxQ;
		}

		public double MaxAbsoluteQ(Location loc)
		{
			double maxQ = double.MinValue;
			foreach (Direction dir in (Direction[]) Enum.GetValues(typeof(Direction)))
			{
				double q = Qabsolute[loc][dir];
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