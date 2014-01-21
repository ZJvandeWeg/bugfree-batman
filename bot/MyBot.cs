using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Ants {

	class MyBot : Bot {
		private Dictionary<Location, Dictionary<Direction, double>> Q;
		private const double Alpha = 1.0; 
		private const int Beta = 100; 
		private const double Gamma = 0;
		private Dictionary<Location, Direction> performedMoves = new Dictionary<Location, Direction>();
		private string logFileName;
		private bool useLearned;

		// DoTurn is run once per turn
		public override void DoTurn (IGameState gameState) {
			//Init Q, with data from previous runs.
			initQ(gameState);

			//Learn new shit this turn?
			useLearned = new Random().Next(0,100) < Beta;

			updateQ(gameState);

			// loop through all my ants and try to give them orders
			orderAnts(gameState);

			FileStream fs1 = new FileStream(logFileName, FileMode.Create, FileAccess.Write);
			StreamWriter sw = new StreamWriter(fs1);
			foreach(KeyValuePair<Location, Dictionary<Direction, double>> pair in Q)
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
			fs1.Close();

		}

		public void initQ(IGameState gameState)
		{
			if (Q != null) return;

		 	logFileName = "q" + gameState.Width + "," + gameState.Height + ".log";

			Dictionary<Location, Dictionary<Direction, double>> knownQ = new Dictionary<Location, Dictionary<Direction, double>>();

			if(!File.Exists(logFileName))
			{
				File.Create(logFileName);
			}

			FileStream fs = new FileStream(logFileName, FileMode.Open, FileAccess.Read);
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

			Q = new Dictionary<Location, Dictionary<Direction, double>>();

			//This is ugly
			for (int x = 0; x < gameState.Width; x++)
			{
				for (int y = 0; y < gameState.Height; y++)
				{	
					Location loc = new Location(x, y);
					Q[loc] = new Dictionary<Direction, double>();

					foreach (Direction dir in (Direction[]) Enum.GetValues(typeof(Direction)))
					{
						double q = 0.0;
						if (knownQ.ContainsKey(loc) && knownQ[loc].ContainsKey(dir)) {
							q = knownQ[loc][dir];
						}			
						Q[loc][dir] = q;
					}
				}
			}
		}
		
		public void updateQ(IGameState gameState)
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

				double q = Q[loc][dir];
				double maxQ = MaxQ(nextLocation);
				Q[loc][dir] = (1.0 - Alpha) * q + Alpha * (r + Gamma * maxQ);
			}
			performedMoves.Clear();
		}

		public void orderAnts(IGameState gameState)
		{
			foreach (Ant ant in gameState.MyAnts) {
				Location loc = (Location)ant;
				//Build the state per ant.

				Direction nextDirection = Direction.North;

				if (useLearned)
				{
					// Use learned Q values
					double maxQ = double.MinValue;
					List<Direction> directions = new List<Direction>();
					foreach(KeyValuePair<Direction, double>pair in Q[loc])
					{
						Direction dir = pair.Key;
						double q = pair.Value;
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
				}

				performedMoves[loc] = nextDirection;

				IssueOrder(loc, nextDirection);
			}
		}

		public double MaxQ(Location loc)
		{
			double maxQ = double.MinValue;
			foreach (Direction dir in (Direction[]) Enum.GetValues(typeof(Direction)))
			{
				double q = Q[loc][dir];
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