using System;
using System.Collections.Generic;

namespace Ants
{

public enum StateParameter { OwnAnt }

class State
{
    public Dictionary<StateParameter, int> Distances;
    public Dictionary<StateParameter, List<Location>> Targets;

    public State(Dictionary<StateParameter, int> distances, Dictionary<StateParameter, List<Location>> targets) : this(distances)
    {
        Targets = targets;
    }

    public State(Dictionary<StateParameter, int> distances)
    {
        Distances = distances;
    }

    public override int GetHashCode()
    {
        // HashCode is based on all state parameters and their respective distances
        int code = 0;
        foreach (KeyValuePair<StateParameter, int> pair in Distances)
        {
            code += pair.Value * (int)Math.Pow(100, (int)pair.Key);
        }
        return code;
    }

    public override bool Equals(Object obj)
    {
        if (obj == null || !(obj is State))
            return false;

        State other = (State)obj;
        foreach (KeyValuePair<StateParameter, int> pair in this.Distances)
        {
            int distance = pair.Value;
            int otherDistance = other.Distances[pair.Key];
            if (distance != otherDistance)
                return false;
        }
        return true;
    }

    public State ApplyAction(Action action)
    {
        Dictionary<StateParameter, int> distances = new Dictionary<StateParameter, int>(this.Distances);
        if (action.Direction == ActionDirection.Towards)
            distances[action.Parameter]--;
        else
            distances[action.Parameter]++;

        return new State(distances);
    }
}
}