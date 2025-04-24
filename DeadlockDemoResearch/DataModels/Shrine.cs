
using System.Numerics;
using System.Runtime.CompilerServices;
using DeadlockDemo = DemoFile.Game.Deadlock;

namespace DeadlockDemoResearch.DataModels
{
  public interface IShrineConstants
  {
    public uint EntityIndex { get; }
    public DeadlockDemo.TeamNumber Team { get; }
    /// true=Yellow ; false=Green
    public bool IsYellowLane { get; }
  }

  public record ShrineConstants : IShrineConstants
  {
    public required uint EntityIndex { get; init; }
    public required DeadlockDemo.TeamNumber Team { get; init; }
    public required bool IsYellowLane { get; init; }

    public static ShrineConstants CopyFrom(IShrineConstants other) => new()
    {
      EntityIndex = other.EntityIndex,
      Team = other.Team,
      IsYellowLane = other.IsYellowLane,
    };
  }

  public interface IShrineVariables
  {
    //public NpcStateMasks NpcState { get; }
    //public byte LifeState { get; }
    //public StateMask DisabledState { get; }
    //public StateMask EnabledState { get; }
    //public StateMask EnabledPredictedState { get; }
    //public bool IsVulnerable { get; }
    //public bool IsFrozenByKelvinUlt { get; }
    //public bool HasBackdoorProtection { get; }
    
    public int Health { get; }
  }

  public record ShrineVariables : IShrineVariables
  {
    public static readonly List<(DeadlockDemo.TeamNumber team, ELane lane, Vector3 position)> ExpectedStartingPositions = [
      (DeadlockDemo.TeamNumber.Amber, ELane.Yellow, new(-6144f, -4864f, 376f)),
      (DeadlockDemo.TeamNumber.Amber, ELane.Blue,   new(-1664f, -3200f, 376f)),
      (DeadlockDemo.TeamNumber.Amber, ELane.Green, new(5504f, -5120f, 376f)),

      (DeadlockDemo.TeamNumber.Sapphire, ELane.Yellow, new(-5504f, 5120f, 376f)),
      (DeadlockDemo.TeamNumber.Sapphire, ELane.Blue,   new(1672f, 3200f, 376f)),
      (DeadlockDemo.TeamNumber.Sapphire, ELane.Green, new(6144f, 4864f, 376f)),
    ];


    //public required NpcStateMasks NpcState { get; init; }
    //public required byte LifeState { get; init; }
    //public required StateMask DisabledState { get; init; }
    //public required StateMask EnabledState { get; init; }
    //public required StateMask EnabledPredictedState { get; init; }
    //public required bool IsVulnerable { get; init; }
    //public required bool IsFrozenByKelvinUlt { get; init; }
    //public required bool HasBackdoorProtection { get; init; }
    public required int Health { get; init; }

    public static ShrineVariables CopyFrom(IShrineVariables other) => new()
    {
      //NpcState = other.NpcState,
      //LifeState = other.LifeState,
      //DisabledState = other.DisabledState,
      //EnabledState = other.EnabledState,
      //EnabledPredictedState = other.EnabledPredictedState,
      //IsVulnerable = other.IsVulnerable,
      //IsFrozenByKelvinUlt = other.IsFrozenByKelvinUlt,
      //HasBackdoorProtection = other.HasBackdoorProtection,
      Health = other.Health,
    };
  }

  public class ShrineView : IShrineConstants, IShrineVariables
  {
    public required DeadlockDemo.CCitadel_Destroyable_Building Entity { get; init; }

    public uint EntityIndex => Entity.EntityIndex.Value;
    public DeadlockDemo.TeamNumber Team => Entity.CitadelTeamNum;
    private bool teamValid() =>
      Enum.IsDefined(Entity.CitadelTeamNum)
      && Entity.CitadelTeamNum != DeadlockDemo.TeamNumber.Unassigned
      && Entity.CitadelTeamNum != DeadlockDemo.TeamNumber.Spectator;
    public bool IsYellowLane => Entity.Origin.X < 0;
    private bool positionValid() =>
      Entity.Origin.Z == 512
      && Entity.Origin.Y == (Team == DeadlockDemo.TeamNumber.Amber ? -7296 : 7296)
      && MathF.Abs(Entity.Origin.X) == 1152;
    private bool subclassValid() => Entity.SubclassID.Value == 746131114;
    private bool maxHealthValid() => Entity.MaxHealth == 4000;

    private DeadlockDemo.CModifierProperty modifierProp => Entity.ModifierProp ?? throw new NullReferenceException(nameof(Entity.ModifierProp));
    private bool modifierPropAccessible() => Entity.ModifierProp != null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool stateMaskZero(uint[] mask) =>
      mask[0] == 0 && mask[1] == 0 && mask[2] == 0 && mask[3] == 0 && mask[4] == 0 && mask[5] == 0 && mask[6] == 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool allSame(bool x, bool y, bool z) => (x == y) && (x == z);
    //public StateMask DisabledState => StateMask.From(modifierProp.DisabledStateMask);
    //public StateMask EnabledState => StateMask.From(modifierProp.EnabledStateMask);
    //public StateMask EnabledPredictedState => StateMask.From(modifierProp.EnabledPredictedStateMask);
    private bool statesAccessible() =>
      modifierProp.DisabledStateMask.Length == 7
      && modifierProp.EnabledStateMask.Length == 7
      && modifierProp.EnabledPredictedStateMask.Length == 7;
    private bool statesValid() =>
      stateMaskZero(modifierProp.DisabledStateMask)
      && stateMaskZero(modifierProp.EnabledPredictedStateMask);
    public int Health => Entity.Health;
    private bool healthValid() => 
      Entity.IsAlive
      && Entity.Health <= Entity.MaxHealth
      && Entity.Destroyed ? Entity.Health == 0 : Entity.Health > 0;

    public bool AllAccessible() => modifierPropAccessible() && statesAccessible();
    public bool ConstantsValid() => teamValid() && positionValid() && subclassValid() && maxHealthValid();
    public bool VariablesValid() => statesValid() && healthValid();
  }

  public class ShrineHistory
  {
    public ShrineHistory(ShrineView view)
    {
      View = view;
      if (!View.AllAccessible() || !View.ConstantsValid()) throw new Exception(nameof(View));
      Constants = ShrineConstants.CopyFrom(view);
    }

    public ShrineView View { get; private init; }
    public ShrineConstants Constants { get; private init; }
    public List<(uint iFrame, ShrineVariables variables)> VariableHistory { get; } = [];

    public void AfterFrame(Frame frame)
    {
      if (!View.AllAccessible() || !View.VariablesValid()) throw new Exception($"{frame}: invalid elements on shrine {View.Entity.EntityIndex.Value}");
      if (ShrineConstants.CopyFrom(View) != Constants) throw new Exception($"{frame}: constants changed on shrine {View.Entity.EntityIndex.Value}");

      var frameVariables = ShrineVariables.CopyFrom(View);

      if (
        VariableHistory.Count > 0
        && VariableHistory[^1].variables.Health == 0
        && frameVariables != VariableHistory[^1].variables
      ) throw new Exception($"{frame}: variables changed on destroyed shrine {View.Entity.EntityIndex.Value}");

      if (
        VariableHistory.Count == 0
        || frameVariables != VariableHistory[^1].variables
      )
      {
        VariableHistory.Add((frame.iFrame, frameVariables));
      }
    }
  }
}
