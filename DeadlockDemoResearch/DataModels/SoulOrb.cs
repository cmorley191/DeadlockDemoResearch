using System.Numerics;
using DeadlockDemo = DemoFile.Game.Deadlock;

namespace DeadlockDemoResearch.DataModels
{
  public interface ISoulOrbConstants
  {
    public uint EntityIndex { get; }
    public ESoulOrbSubclassId Subclass { get; }
    public byte InterpolationFrame { get; }
    public float CreateTime { get; }
    public DeadlockDemo.TeamNumber Team { get; }
    public float TimeLaunch { get; }
    public float AttackableTime { get; }
    public int LaunchNum { get; }

    public ulong InteractsAs { get; }
    public ulong InteractsWith { get; }
    public ulong InteractsExclude { get; }
    public byte CollisionFunctionMask { get; }
  }

  public record SoulOrbConstants : ISoulOrbConstants
  {
    public required uint EntityIndex { get; init; }
    public required ESoulOrbSubclassId Subclass { get; init; }
    public required byte InterpolationFrame { get; init; }
    public required float CreateTime { get; init; }
    public required DeadlockDemo.TeamNumber Team { get; init; }
    public required float TimeLaunch { get; init; }
    public required float AttackableTime { get; init; }
    public required int LaunchNum { get; init; }

    public required ulong InteractsAs { get; init; }
    public static readonly List<ulong> PlaceholderInteractsAsValues = [131072, 4297195520, 2149711872];
    public static readonly List<ulong> PermittedInteractsAsValues = [8800390217728, 8798242734080];
    public required ulong InteractsWith { get; init; }
    public const ulong PlaceholderInteractsWith = 0;
    public const ulong PermittedInteractsWith = 34360537089;
    public required ulong InteractsExclude { get; init; }
    public const ulong PlaceholderInteractsExclude = 0;
    public const ulong PermittedInteractsExclude = 786436;
    public required byte CollisionFunctionMask { get; init; }
    public const byte PlaceholderCollisionFunctionMask = 0;
    public const byte PermittedCollisionFunctionMask = 7;

    public static SoulOrbConstants CopyFrom(ISoulOrbConstants other) => new()
    {
      EntityIndex = other.EntityIndex,
      Subclass = other.Subclass,
      InterpolationFrame = other.InterpolationFrame,
      CreateTime = other.CreateTime,
      Team = other.Team,
      TimeLaunch = other.TimeLaunch,
      AttackableTime = other.AttackableTime,
      LaunchNum = other.LaunchNum,

      InteractsAs = other.InteractsAs,
      InteractsWith = other.InteractsWith,
      InteractsExclude = other.InteractsExclude,
      CollisionFunctionMask = other.CollisionFunctionMask,
    };
  }

  public interface ISoulOrbVariables
  {
    public float SimulationTime { get; }
    public Vector3 Position { get; }
  }

  public record SoulOrbVariables : ISoulOrbVariables
  {
    public required float SimulationTime { get; init; }
    public required Vector3 Position { get; init; }

    public static SoulOrbVariables CopyFrom(ISoulOrbVariables other) => new()
    {
      SimulationTime = other.SimulationTime,
      Position = other.Position,
    };
  }

  public class SoulOrbView : ISoulOrbConstants, ISoulOrbVariables
  {
    public required DeadlockDemo.CItemXP Entity { get; init; }

    public uint EntityIndex => Entity.EntityIndex.Value;
    public ESoulOrbSubclassId Subclass => (ESoulOrbSubclassId)Entity.SubclassID.Value;
    private bool subclassValid() => Enum.IsDefined(Subclass);
    public byte InterpolationFrame => Entity.InterpolationFrame;
    private bool interpolationFrameValid() => Entity.InterpolationFrame <= 3;
    public float CreateTime => Entity.CreateTime.Value;
    public DeadlockDemo.TeamNumber Team => Entity.CitadelTeamNum;
    private bool teamValid() => 
      Entity.CitadelTeamNum == DeadlockDemo.TeamNumber.Amber 
      || Entity.CitadelTeamNum == DeadlockDemo.TeamNumber.Sapphire;
    public float TimeLaunch => Entity.TimeLaunch.Value;

    public ulong InteractsAs => Entity.Collision.CollisionAttribute.InteractsAs;
    public ulong InteractsWith => Entity.Collision.CollisionAttribute.InteractsWith;
    public ulong InteractsExclude => Entity.Collision.CollisionAttribute.InteractsExclude;
    public byte CollisionFunctionMask => Entity.Collision.CollisionAttribute.CollisionFunctionMask;

    public float SimulationTime => Entity.SimulationTime;
    public Vector3 Position => MiscFunctions.ConvertVector(Entity.Origin);
    public float AttackableTime => Entity.AttackableTime.Value;
    public int LaunchNum => Entity.LaunchNum;
    private bool modifierPropInaccessible() => Entity.ModifierProp == null;


    public bool AllAccessible() => modifierPropInaccessible();
    public bool ConstantsValid() => subclassValid() && interpolationFrameValid() && teamValid();
    public bool VariablesValid() => true;
  }

  public class SoulOrbHistory
  {
    public SoulOrbHistory(SoulOrbView view)
    {
      View = view;
      if (!View.AllAccessible() || !View.ConstantsValid()) throw new Exception(nameof(View));
      FirstFrameConstants = SoulOrbConstants.CopyFrom(view);
    }

    public SoulOrbView View { get; private init; }
    public SoulOrbConstants FirstFrameConstants { get; private init; }
    public (uint iFrame, SoulOrbConstants constants)? SubsequentConstants { get; private set; } = null;
    public List<(uint iFrame, EEntityPvsState pvsState, SoulOrbVariables variables)> VariableHistory { get; } = [];

    public void AfterFrame(Frame previousFrame, Frame currentFrame, float gameStartTime, EEntityPvsState pvsState)
    {
      if (!View.AllAccessible() || !View.VariablesValid()) throw new Exception();

      var frameConstants = SoulOrbConstants.CopyFrom(View);
      if (SubsequentConstants == null)
      {
        if (frameConstants != FirstFrameConstants)
        {
          SubsequentConstants = (currentFrame.iFrame, frameConstants);
        }
      }
      else
      {
        if (frameConstants != SubsequentConstants.Value.constants) throw new Exception();
      }

      var frameVariables = SoulOrbVariables.CopyFrom(View);

      if (
        VariableHistory.Count > 0
        && VariableHistory[^1].pvsState == EEntityPvsState.Deleted
        && (pvsState != EEntityPvsState.Deleted || frameVariables != VariableHistory[^1].variables)
      ) throw new Exception();

      if (
        VariableHistory.Count == 0
        || frameVariables.SimulationTime != VariableHistory[^1].variables.SimulationTime
      )
      {
        // if a simulation time update is occurring, it should be the correct time
        if (!(
          frameVariables.SimulationTime > previousFrame.GameTickTime
          && frameVariables.SimulationTime <= currentFrame.GameTickTime
        )) throw new Exception();
      }

      var simulationGameClockTime = currentFrame.GameClockTime - (currentFrame.GameTickTime - frameVariables.SimulationTime);
      var launchSimulationGameClockTime = FirstFrameConstants.TimeLaunch - gameStartTime;

      if (VariableHistory.Count == 0)
      {
        var launchDelay = launchSimulationGameClockTime - simulationGameClockTime;
        var attackableDelay = FirstFrameConstants.AttackableTime - FirstFrameConstants.TimeLaunch;
        if (FirstFrameConstants.Subclass is (ESoulOrbSubclassId.SoulOrbFountain or ESoulOrbSubclassId.SoulOrbTrooper))
        {
          if (!(launchDelay >= 0.4f && launchDelay <= 0.7f)) throw new Exception();
          if (!(MathF.Abs(attackableDelay - 0.2f) < 0.001f)) throw new Exception();
        } 
        else
        {
          if (!(MathF.Abs(launchDelay - 0.3f) < 0.001f)) throw new Exception();
          if (!(MathF.Abs(attackableDelay - 0.12f) < 0.001f)) throw new Exception();
        }
      }
      else if (previousFrame.GameTick == currentFrame.GameTick)
      {
        if (frameVariables.SimulationTime != VariableHistory[^1].variables.SimulationTime) throw new Exception();
      }
      else
      {
        if (
          previousFrame.GameClockPaused && currentFrame.GameClockPaused
          || pvsState != EEntityPvsState.Active
        )
        {
          // if we're paused or inactive, simulation time shouldn't be updating
          if (frameVariables.SimulationTime != VariableHistory[^1].variables.SimulationTime) throw new Exception();
        }
        else
        {
          // if we're unpaused and active, and...
          if (simulationGameClockTime >= launchSimulationGameClockTime)
          {
            // if we've definitely launched, simulation time should be updating
            if (frameVariables.SimulationTime == VariableHistory[^1].variables.SimulationTime) throw new Exception();
          }
          else if (VariableHistory.Count >= 2 && VariableHistory[^1].iFrame == previousFrame.iFrame)
          {
            // we definitely didn't launch in the *last* frame transition, so simulation time shouldn't have been updating
            if (VariableHistory[^1].variables.SimulationTime != VariableHistory[^2].variables.SimulationTime) throw new Exception();
          }
          // the above logic leaves open the case "we are about to launch between this frame and the next", which is optional - sometimes sim time updates, sometimes it doesn't
        }
      }

      if (
        VariableHistory.Count == 0
        || pvsState != VariableHistory[^1].pvsState
        || frameVariables != VariableHistory[^1].variables
      )
      {
        VariableHistory.Add((currentFrame.iFrame, pvsState, frameVariables));
      }
      

      /*
      if (!(
        View.SimulationTime > previousFrame.GameTickTime
        && View.SimulationTime <= currentFrame.GameTickTime
      )) throw new Exception();

      if (VariableHistory.Count == 1)
      {
        var launchGameClockTime = FirstFrameConstants.TimeLaunch - gameStartTime;
        var launchDelay = launchGameClockTime - currentFrame.GameClockTime;
        if (!(launchDelay >= 0.4 && launchDelay <= 0.7)) throw new Exception();
        var attackableDelay = FirstFrameConstants.AttackableTime - FirstFrameConstants.TimeLaunch;
        if (!(
          Math.Abs(attackableDelay - 0.2) < 0.001f
          || Math.Abs(attackableDelay - 0.12) < 0.001f
        )) throw new Exception();
      }
      */
    }
  }
}
