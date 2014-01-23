using System;
using System.Collections.Generic;

namespace Ants
{

  public class Location : IEquatable<Location>
  {

    /// <summary>
    /// Gets the row of this location.
    /// </summary>
    public int Row
    {
      get;
      private set;
    }

    /// <summary>
    /// Gets the column of this location.
    /// </summary>
    public int Col
    {
      get;
      private set;
    }

    public Location (string input)
    {
      string[] parts = input.Split(',');
      this.Row = int.Parse(parts[0]);
      this.Col = int.Parse(parts[1]);
    }

    public Location (int row, int col)
    {
      this.Row = row;
      this.Col = col;
    }

    public override bool Equals (object obj)
    {
      if (ReferenceEquals (null, obj))
        return false;
      if (ReferenceEquals (this, obj))
        return true;
      if (obj.GetType() != typeof (Location))
        return false;

      return Equals ((Location) obj);
    }

    public bool Equals (Location other)
    {
      if (ReferenceEquals (null, other))
        return false;
      if (ReferenceEquals (this, other))
        return true;

      return other.Row == this.Row && other.Col == this.Col;
    }

    public static bool operator == (Location a, Location b)
    {
      return a.Equals(b);
    }

    public static bool operator != (Location a, Location b)
    {
      return !(a == b);
    }

    public override int GetHashCode()
    {
      unchecked
      {
        return (this.Row * 397) ^ this.Col;
      }
    }

    public override string ToString()
    {
      return this.Row + "," + this.Col;
    }
  }

  public class TeamLocation : Location, IEquatable<TeamLocation>
  {
    /// <summary>
    /// Gets the team of this ant.
    /// </summary>
    public int Team
    {
      get;
      private set;
    }

    public TeamLocation (int row, int col, int team) : base (row, col)
    {
      this.Team = team;
    }

    public bool Equals (TeamLocation other)
    {
      return base.Equals (other);
    }

    public override bool Equals(object other)
    {
      return base.Equals(other);
    }

    public override int GetHashCode()
    {
      return base.GetHashCode();
    }

    public static bool operator == (TeamLocation a, TeamLocation b)
    {
      return a.Equals(b);
    }

    public static bool operator != (TeamLocation a, TeamLocation b)
    {
      return !(a == b);
    }

    /*public bool Equals(TeamLocation other) {
        return base.Equals (other) && other.Team == Team;
    }

    public override int GetHashCode()
    {
        unchecked {
            int result = this.Col;
            result = (result * 397) ^ this.Row;
            result = (result * 397) ^ this.Team;
            return result;
        }
    }*/
  }

  public class Ant : TeamLocation, IEquatable<Ant>
  {
    public Ant (int row, int col, int team) : base (row, col, team)
    {
    }

    public bool Equals (Ant other)
    {
      return base.Equals (other);
    }

    public override bool Equals(object other)
    {
      return base.Equals(other);
    }

    public override int GetHashCode()
    {
      return base.GetHashCode();
    }

    public static bool operator == (Ant a, Ant b)
    {
      return a.Equals(b);
    }

    public static bool operator != (Ant a, Ant b)
    {
      return !(a == b);
    }
  }

  public class AntHill : TeamLocation, IEquatable<AntHill>
  {
    public AntHill (int row, int col, int team) : base (row, col, team)
    {
    }

    public bool Equals (AntHill other)
    {
      return base.Equals (other);
    }

    public override bool Equals(object other)
    {
      return base.Equals(other);
    }

    public override int GetHashCode()
    {
      return base.GetHashCode();
    }

    public static bool operator == (AntHill a, AntHill b)
    {
      return a.Equals(b);
    }

    public static bool operator != (AntHill a, AntHill b)
    {
      return !(a == b);
    }
  }
}

