
using System.Numerics;
using static CMsgServerNetworkStats.Types;
using DeadlockDemo = DemoFile.Game.Deadlock;

namespace DeadlockDemoResearch.DataModels
{
  public interface IUrnDropoffSpotConstants
  {
    public EUrnDropoffSpot SpotId { get; }
  }

  public record UrnDropoffSpotConstants : IUrnDropoffSpotConstants
  {
    public record UrnDropoffSpotBounds
    {
      public required Vector3 Origin { get; init; }
      public required Vector3 Mins { get; init; }
      public required Vector3 Maxs { get; init; }
    }

    public static readonly Dictionary<EUrnDropoffSpot, UrnDropoffSpotBounds> ExpectedBounds = new() {
      {
        EUrnDropoffSpot.PurpleAmber,
        new() {
          Origin = new(4544f, -3392f, 496f),
          Mins = new(-64f, -112f, -80f),
          Maxs = new(64f, 112f, 176f),
        }
      },
      {
        EUrnDropoffSpot.GreenAmber,
        new() {
          Origin = new(-3680f, -3072f, 496f),
          Mins = new(-63.999756f, -111.998535f, -80f),
          Maxs = new(64.000244f, 112.001465f, 176f),
        }
      },
      {
        EUrnDropoffSpot.YellowSapphire,
        new() {
          Origin = new(-4544f, 3392f, 496f),
          Mins = new(-64f, -112f, -80f),
          Maxs = new(64f, 112f, 176f),
        }
      },
      {
        EUrnDropoffSpot.BlueSapphire,
        new() {
          Origin = new(3680f, 3072f, 496f),
          Mins = new(-64.001465f, -112.000244f, -80f),
          Maxs = new(63.998535f, 111.999756f, 176f),
        }
      },
      {
        EUrnDropoffSpot.YellowMiddle,
        new() {
          Origin = new(-6579f, 0f, 208f),
          Mins = new(-72.00001f, -104.00001f, -80f),
          Maxs = new(72.00001f, 104.00001f, 176f),
        }
      },
      {
        EUrnDropoffSpot.PurpleMiddle,
        new() {
          Origin = new(6579f, 0f, 208f),
          Mins = new(-72.00001f, -112f, -80f),
          Maxs = new(72.00001f, 112f, 176f),
        }
      },
    };


    public required EUrnDropoffSpot SpotId { get; init; }

    public static UrnDropoffSpotConstants CopyFrom(IUrnDropoffSpotConstants other) => new()
    {
      SpotId = other.SpotId,
    };
  }

  public interface IUrnDropoffSpotVariables
  {
    public EUrnDropoffSpotState State { get; }
  }

  public record UrnDropoffSpotVariables : IUrnDropoffSpotVariables
  {
    public required EUrnDropoffSpotState State { get; init; }

    public static UrnDropoffSpotVariables CopyFrom(IUrnDropoffSpotVariables other) => new()
    {
      State = other.State,
    };
  }

  public class UrnDropoffSpotView : IUrnDropoffSpotConstants, IUrnDropoffSpotVariables
  {
    public required DeadlockDemo.CCitadelIdolReturnTrigger Entity { get; init; }
    public required EUrnDropoffSpot? SpotIdHint { get; init; }

    public EUrnDropoffSpot SpotId {
      get {
        var bounds = new UrnDropoffSpotConstants.UrnDropoffSpotBounds()
        {
          Origin = MiscFunctions.ConvertVector(Entity.Origin),
          Mins = MiscFunctions.ConvertVector(Entity.Collision.Mins),
          Maxs = MiscFunctions.ConvertVector(Entity.Collision.Maxs),
        };
        if (SpotIdHint != null && bounds == UrnDropoffSpotConstants.ExpectedBounds[SpotIdHint.Value])
        {
          return SpotIdHint.Value;
        }
        return UrnDropoffSpotConstants.ExpectedBounds.First(p => bounds == p.Value).Key;
      }
    }
    private bool spotIdAccessible()
    {
      var bounds = new UrnDropoffSpotConstants.UrnDropoffSpotBounds()
      {
        Origin = MiscFunctions.ConvertVector(Entity.Origin),
        Mins = MiscFunctions.ConvertVector(Entity.Collision.Mins),
        Maxs = MiscFunctions.ConvertVector(Entity.Collision.Maxs),
      };
      if (SpotIdHint != null && bounds == UrnDropoffSpotConstants.ExpectedBounds[SpotIdHint.Value])
      {
        return true;
      }
      return UrnDropoffSpotConstants.ExpectedBounds.Any(p => bounds == p.Value);
    }

    public EUrnDropoffSpotState State =>
      Entity.Disabled
      ? EUrnDropoffSpotState.Inactive
      : Entity.CitadelTeamNum == DeadlockDemo.TeamNumber.Unassigned
      ? EUrnDropoffSpotState.ActiveForDroppedUrn_OrInitializing
      : Entity.CitadelTeamNum == DeadlockDemo.TeamNumber.Amber
      ? EUrnDropoffSpotState.ActiveForAmber
      : EUrnDropoffSpotState.ActiveForSapphire;
    private bool stateValid() =>
      Entity.Disabled
      ? Entity.CitadelTeamNum == DeadlockDemo.TeamNumber.Unassigned
      : (
        Entity.CitadelTeamNum == DeadlockDemo.TeamNumber.Unassigned
        || Entity.CitadelTeamNum == DeadlockDemo.TeamNumber.Amber
        || Entity.CitadelTeamNum == DeadlockDemo.TeamNumber.Sapphire
      );


    public bool AllAccessible() => spotIdAccessible();
    public bool ConstantsValid() => true;
    public bool VariablesValid() => stateValid();
  }

  public class UrnDropoffSpotHistory
  {
    public UrnDropoffSpotHistory(UrnDropoffSpotView view)
    {
      View = view;
      if (!View.AllAccessible() || !View.ConstantsValid()) throw new Exception(nameof(View));
      Constants = UrnDropoffSpotConstants.CopyFrom(view);
    }

    public UrnDropoffSpotView View { get; private init; }
    public UrnDropoffSpotConstants Constants { get; private init; }
    public List<(uint iFrame, UrnDropoffSpotVariables variables)> VariableHistory { get; } = [];

    public void AfterFrame(Frame frame)
    {
      if (!View.AllAccessible() || !View.VariablesValid()) throw new Exception(nameof(View));
      if (UrnDropoffSpotConstants.CopyFrom(View) != Constants) throw new Exception(nameof(UrnDropoffSpotConstants));

      var frameVariables = UrnDropoffSpotVariables.CopyFrom(View);

      if (
        VariableHistory.Count == 0
        || frameVariables != VariableHistory[^1].variables
      )
      {
        VariableHistory.Add((frame.iFrame, frameVariables));
      }
    }
  }

  public interface IUrnConstants
  {
    public EDroppedUrnType Type { get; }
  }

  public record UrnConstants : IUrnConstants
  {
    public required EDroppedUrnType Type { get; init; }

    public static UrnConstants CopyFrom(IUrnConstants other) => new()
    {
      Type = other.Type,
    };
  }

  public interface IUrnVariables
  {
    public Vector3 Position { get; }
  }

  public record UrnVariables : IUrnVariables
  {
    public const float ExpectedPlusMinusSpawnX = 6584f;
    public const float ExpectedSpawnY = 0f;
    public const float ExpectedSpawnZ = 1939.1875f;
    public const float ExpectedSpawnZTolerance = 10f;
    public const float ExpectedSpawnLandingZ = 132f;

    public required Vector3 Position { get; init; }

    public static UrnVariables CopyFrom(IUrnVariables other) => new()
    {
      Position = other.Position,
    };
  }

  public class UrnView : IUrnConstants, IUrnVariables
  {
    public required DeadlockDemo.CCitadelItemPickupIdol Entity { get; init; }

    public EDroppedUrnType Type =>
      (int)Entity.CitadelTeamNum == 4
      ? EDroppedUrnType.NeverPickedUp
      : Entity.CitadelTeamNum == DeadlockDemo.TeamNumber.Amber
      ? EDroppedUrnType.DroppedByAmber
      : EDroppedUrnType.DroppedBySapphire;
    private bool typeValid() =>
      (int)Entity.CitadelTeamNum == 4
      ? (
        Entity.FallRate == 144.0f
        && Entity.ModelName.Value == ""
      )
      : (
        Entity.FallRate == 0f
        && Entity.ModelName.Value == "models/null.vmdl"
        && (Entity.CitadelTeamNum == DeadlockDemo.TeamNumber.Amber || Entity.CitadelTeamNum == DeadlockDemo.TeamNumber.Sapphire)
      );

    public Vector3 Position => MiscFunctions.ConvertVector(Entity.Origin);


    public bool AllAccessible() => true;
    public bool ConstantsValid() => typeValid();
    public bool VariablesValid() => true;
  }

  public class UrnHistory
  {
    public UrnHistory(UrnView view)
    {
      View = view;
      if (!View.AllAccessible() || !View.ConstantsValid()) throw new Exception(nameof(View));
      Constants = UrnConstants.CopyFrom(view);
    }

    public UrnView View { get; private init; }
    public UrnConstants Constants { get; private init; }
    public List<(uint iFrame, bool deleted, UrnVariables variables)> VariableHistory { get; } = [];

    public void AfterFrame(Frame frame, bool deleted)
    {
      if (!View.AllAccessible() || !View.VariablesValid()) throw new Exception(nameof(View));
      if (UrnConstants.CopyFrom(View) != Constants) throw new Exception(nameof(UrnConstants));

      var frameVariables = UrnVariables.CopyFrom(View);

      if (Constants.Type == EDroppedUrnType.NeverPickedUp)
      {
        // pretty roughly, all this basically checks: X and Y stay at expected, Z is always descending to the landing point
        if (frameVariables.Position.Y != UrnVariables.ExpectedSpawnY) throw new Exception(nameof(frameVariables.Position));
        else if (VariableHistory.Count == 0)
        {
          if (
            MathF.Abs(frameVariables.Position.X) != UrnVariables.ExpectedPlusMinusSpawnX
            || frameVariables.Position.Y != UrnVariables.ExpectedSpawnY
            || MathF.Abs(frameVariables.Position.Z - UrnVariables.ExpectedSpawnZ) > UrnVariables.ExpectedSpawnZTolerance
          ) throw new Exception(nameof(frameVariables.Position));
        }
        else if (frameVariables.Position.X != VariableHistory[^1].variables.Position.X) throw new Exception(nameof(frameVariables.Position));
        else if (VariableHistory.Count == 1)
        {
          if (frameVariables.Position.Z > VariableHistory[^1].variables.Position.Z) throw new Exception(nameof(frameVariables.Position));
        }
        else
        {
          if (
            VariableHistory[^1].variables.Position.Z == UrnVariables.ExpectedSpawnLandingZ
            ? frameVariables.Position.Z != UrnVariables.ExpectedSpawnLandingZ
            : frameVariables.Position.Z >= VariableHistory[^2].variables.Position.Z
          ) throw new Exception(nameof(frameVariables.Position));
        }
      }

      if (
        VariableHistory.Count > 0
        && VariableHistory[^1].deleted
        && (!deleted || frameVariables != VariableHistory[^1].variables)
      ) throw new Exception(nameof(deleted));

      if (
        VariableHistory.Count == 0
        || deleted != VariableHistory[^1].deleted
        || frameVariables != VariableHistory[^1].variables
      )
      {
        VariableHistory.Add((frame.iFrame, deleted, frameVariables));
      }
    }
  }

  public record class UnifiedUrnSpawnConstants
  {
    public required EUrnPickupSpot InitialPickupSpot { get; init; }
  }

  public record class UnifiedUrnFirstPickupConstants
  {
    public required EUrnDropoffSpot DropoffSpot { get; init; }
  }

  public class UnifiedUrnHistory
  {
    public UnifiedUrnHistory(UrnHistory spawnUrn)
    {
      SpawnUrn = spawnUrn;
      SpawnConstants = new()
      {
        InitialPickupSpot = spawnUrn.VariableHistory[0].variables.Position.X < 0 ? EUrnPickupSpot.YellowMiddle : EUrnPickupSpot.PurpleMiddle,
      };
    }

    public UrnHistory SpawnUrn { get; private init; }
    public UnifiedUrnSpawnConstants SpawnConstants { get; private init; }

    public List<(uint iFrame, EUnifiedUrnState state, PlayerHistory? holdingPlayer, UrnHistory? unheldUrn)> VariableHistory { get; } = [];
    private UrnDropoffSpotHistory? _dropoffSpot = null;
    public UrnDropoffSpotHistory? DropoffSpot { get => _dropoffSpot; set { if (_dropoffSpot != null) throw new Exception(); _dropoffSpot = value; } }
  }
}
