using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Ants
{

  class MyBot : Bot
  {
    private Dictionary<Location, Dictionary<Direction, double>> Qabsolute;
    private Dictionary<State, Dictionary<Action, double>> Qrelative;
    private const double Alpha = 0.001;
    private const int Beta = 10; //How much we learn each turn
    private const double Gamma = 0.0;
    private Dictionary<Location, Location> performedMoves = new Dictionary<Location, Location>();
    private List<PerformedAction> performedActions = new List<PerformedAction>();
    private string logAbsolute;
    private const string logRelative = "Relative.Q";
    private bool useLearned;
    private const int maxDistance = 99;
    private IGameState gameState;
    private List<Action> actions = new List<Action>();
    private Random random;

    // DoTurn is run once per turn
    public override void DoTurn (IGameState gameState)
    {
      this.random = new Random();
      this.gameState = gameState;
      this.useLearned = random.Next(0, 100) < Beta;

      initAbsoluteQ();
      initRelativeQ();

      updateRelativeQ();
      updateAbsoluteQ();

      orderAnts();

      writeAbsoluteLog();
      writeRelativeLog();
    }

    public void writeAbsoluteLog()
    {
      // Write the absolute Q-table down to a file.

      FileStream fs = new FileStream(logAbsolute, FileMode.Create, FileAccess.Write);
      StreamWriter sw = new StreamWriter(fs);

      foreach (KeyValuePair<Location, Dictionary<Direction, double>> pair in Qabsolute)
      {
        Location loc = pair.Key;
        foreach (KeyValuePair<Direction, double>pair2 in pair.Value)
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
      // Write the relative Q-table down to a file.

      FileStream fs = new FileStream(logRelative, FileMode.Create, FileAccess.Write);
      StreamWriter sw = new StreamWriter(fs);

      foreach (KeyValuePair<State, Dictionary<Action, double>> pair in Qrelative)
      {
        State state = pair.Key;
        foreach (KeyValuePair<Action, double>pair2 in pair.Value)
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

      Dictionary<Location, Dictionary<Direction, double>> knownQ = new
        Dictionary<Location, Dictionary<Direction, double>>();

      if (File.Exists(logAbsolute))
      {
        // Read the known absolute Q-values from the log.

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
      }

      // Initialize the absolute Q-table, taking known Q-values into account.

      Qabsolute = new Dictionary<Location, Dictionary<Direction, double>>();

      for (int x = 0; x < gameState.Width; x++)
      {
        for (int y = 0; y < gameState.Height; y++)
        {
          Location loc = new Location(x, y);
          Qabsolute[loc] = new Dictionary<Direction, double>();

          foreach (Direction dir in (Direction[]) Enum.GetValues(typeof(Direction)))
          {
            double q = 0.0;

            // Take known Q-value into account
            if (knownQ.ContainsKey(loc) && knownQ[loc].ContainsKey(dir))
            {
              q = knownQ[loc][dir];
            }
            Qabsolute[loc][dir] = q;
          }
        }
      }
    }

    public void initRelativeQ()
    {
      if (Qrelative != null) return;

      Dictionary<int, Dictionary<int, double>> knownQ = new Dictionary<int, Dictionary<int, double>>();

      if (File.Exists(logRelative))
      {
        // Read the known absolute Q-values from the log.

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
      }

      // Initialize the absolute Q-table, taking known Q-values into account.

      Qrelative = new Dictionary<State, Dictionary<Action, double>>();

      // Determine all possible actions, that is [towards] and [away from] for every state param.
      foreach (StateParameter parameter in (StateParameter[]) Enum.GetValues(typeof(StateParameter)))
      {
        Action a = new Action(parameter, ActionDirection.Towards);
        actions.Add(a);
        a = new Action(parameter, ActionDirection.AwayFrom);
        actions.Add(a);
      }

      // This code is not actually prepared to handle multiple StateParameters,
      // but this is not an issue because right now we only have one anyway: OwnAnt.
      Dictionary<StateParameter, int> distances = new Dictionary<StateParameter, int>();
      for (int i = 0; i <= maxDistance; i++)
      {
        distances[StateParameter.OwnAnt] = i;

        State s = new State(new Dictionary<StateParameter, int>(distances));
        Qrelative[s] = new Dictionary<Action, double>();

        foreach (Action a in actions)
        {
          int stateCode = s.GetHashCode();
          int actionCode = a.GetHashCode();
          double q = 0.0;

          // Take known Q-value into account
          if (knownQ.ContainsKey(stateCode) && knownQ[stateCode].ContainsKey(actionCode))
          {
            q = knownQ[stateCode][actionCode];
          }
          Qrelative[s][a] = q;
        }
      }
    }

    public void updateAbsoluteQ()
    {
      // For every move performed in the last turn, see if we walked into water.
      foreach (KeyValuePair<Location, Location>pair in performedMoves)
      {
        Location loc = pair.Key;
        Location nextLocation = pair.Value;

        Direction dir = gameState.GetDirections(loc, nextLocation).First();

        int r = 0;
        // If our move wasn't successful, i.e. we didn't end up where we wanted to go but stayed in place,
        // this indicates a blocked move: WATER.
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
      // For every action performed in the previous turn, see if it resulted in DEATH.
      foreach (PerformedAction performedAction in performedActions)
      {
        State fromState = performedAction.fromState;
        Action action = performedAction.action;
        Ant ownAnt = performedAction.ownAnt;
        Location nextLocation = performedAction.nextLocation;
        Ant friend = performedAction.friend;

        Location friendNextLocation = performedMoves[friend];

        // If our move wasn't successful, i.e. if we walked into water and stayed in place,
        // whether we died can't actually be ascribed to our last move.
        if (!gameState.DeadTiles.Contains(nextLocation) &&
            !gameState.MyAnts.Contains(nextLocation))
        {
          // Move niet gelukt
          continue;
        }

        int r = 0;
        if (gameState.DeadTiles.Contains(friendNextLocation) ||
            gameState.MyAnts.Contains(friendNextLocation))
        {
          // If our closest friend's move was successful, and we both moved to the same place: DEATH.
          if (nextLocation == friendNextLocation)
          {
            r = -1000;
          }
        }
        else
        {
          // If our closest friend's move was not successful, but we moved to their place: DEATH.
          if (nextLocation == friend)
          {
            r = -1000;
          }
        }

        State nextState = fromState.ApplyAction(action);

        double q = Qrelative[fromState][action];
        double minQ = MinRelativeQ(nextState);
        Qrelative[fromState][action] = (1.0 - Alpha) * q + Alpha * (r + Gamma * minQ);
      }
      performedActions.Clear();
    }

    public void orderAnts()
    {
      // Make a move for every ant
      foreach (Ant ant in gameState.MyAnts)
      {
        Location loc = (Location)ant;

        State state = buildState(ant);
        Direction nextDirection = Direction.North;

        if (useLearned)
        {
          // Use learned Q values
          double maxQ = double.MinValue;
          List<Direction> directions = new List<Direction>();

          // For every possible irection to move in, calculate the total Q-value.
          foreach (KeyValuePair<Direction, double>pair in Qabsolute[loc])
          {
            Direction dir = pair.Key;
            double absoluteQ = pair.Value;

            // Calculate the relative Q-value for moving in this direction.
            double relativeQ = 0.0;
            foreach (Location friend in state.Targets[StateParameter.OwnAnt])
            {
              Action action;

              List<Direction> directionsToward = (List<Direction>)gameState.GetDirections(loc, friend);

              // If the direction in question is one of the directions we can use to get to the friend,
              // it's a "towards" action, otherwise "away from"
              if (directionsToward.Contains(dir))
              {
                action = new Action(StateParameter.OwnAnt, ActionDirection.Towards);
              }
              else
              {
                action = new Action(StateParameter.OwnAnt, ActionDirection.AwayFrom);
              }

              // Read and add the Q-value for this state/action.
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
          nextDirection = directions[random.Next(directions.Count)];
        }
        else
        {
          // Pick a random direction
          Direction[] directions = (Direction[]) Enum.GetValues(typeof(Direction));
          nextDirection = directions[random.Next(directions.Length)];
        }

        Location nextLocation = gameState.GetDestination(loc, nextDirection);

        // Make not of all performed relative actions
        foreach (Ant friend in state.Targets[StateParameter.OwnAnt])
        {
          Action action;

          List<Direction> directionsToward = (List<Direction>)gameState.GetDirections(loc, friend);

          // If the direction in question is one of the directions we can use to get to the friend,
          // it's a "towards" action, otherwise "away from"
          if (directionsToward.Contains(nextDirection))
          {
            action = new Action(StateParameter.OwnAnt, ActionDirection.Towards);
          }
          else
          {
            action = new Action(StateParameter.OwnAnt, ActionDirection.AwayFrom);
          }

          performedActions.Add(new PerformedAction(state, action, ant, nextLocation, friend));
        }

        // Make not of the performed move.
        performedMoves[loc] = nextLocation;

        IssueOrder(loc, nextDirection);
      }
    }


    public State buildState(Location ant)
    {
      Dictionary<StateParameter, int> distances = new Dictionary<StateParameter, int>();
      Dictionary<StateParameter, List<Location>> targets = new
        Dictionary<StateParameter, List<Location>>();

      // Find the closest friends
      int friendDistance = maxDistance;
      List<Location> closestFriends = new List<Location>();
      foreach (Location friend in gameState.MyAnts)
      {
        if (friend == ant) continue;
        int delta = gameState.GetDistance(ant, friend);
        if (delta < friendDistance)
        {
          closestFriends.Clear();
          friendDistance = delta;
        }
        if (delta <= friendDistance)
        {
          closestFriends.Add(friend);
        }
      }

      distances[StateParameter.OwnAnt] = friendDistance;
      targets[StateParameter.OwnAnt] = closestFriends;

      return new State(distances, targets);
    }

    public double MinRelativeQ(State state)
    {
      double minQ = double.MaxValue;
      foreach (Action action in actions)
      {
        double q = Qrelative[state][action];
        if (q < minQ)
          minQ = q;
      }
      return minQ;
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

    public static void Main (string[] args)
    {
      new Ants().PlayGame(new MyBot());
    }
  }
}