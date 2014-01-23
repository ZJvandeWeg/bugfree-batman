namespace Ants
{
  class PerformedAction
  {
    public State fromState;
    public Action action;
    public Ant ownAnt;
    public Location nextLocation;
    public Ant friend;

    public PerformedAction(State fromState, Action action, Ant ownAnt, Location nextLocation,
                           Ant friend)
    {
      this.fromState = fromState;
      this.action = action;
      this.ownAnt = ownAnt;
      this.friend = friend;
      this.nextLocation = nextLocation;
    }
  }
}