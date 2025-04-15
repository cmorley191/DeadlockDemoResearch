
using System.Numerics;
using System.Runtime.CompilerServices;
using DeadlockDemo = DemoFile.Game.Deadlock;

namespace DeadlockDemoResearch.DataModels
{
  public interface IWalkerConstants
  {
    public uint EntityIndex { get; }
    public DeadlockDemo.TeamNumber Team { get; }
    public ELane Lane { get; }
    public ESubclassId Subclass { get; }
    public int MaxHealth { get; }
  }

  public record WalkerConstants : IWalkerConstants
  {
    public required uint EntityIndex { get; init; }
    public required DeadlockDemo.TeamNumber Team { get; init; }
    public required ELane Lane { get; init; }
    public required ESubclassId Subclass { get; init; }
    public required int MaxHealth { get; init; }

    public static WalkerConstants CopyFrom(IWalkerConstants other) => new()
    {
      EntityIndex = other.EntityIndex,
      Team = other.Team,
      Lane = other.Lane,
      Subclass = other.Subclass,
      MaxHealth = other.MaxHealth,
    };
  }

  public interface IWalkerVariables
  {
    //public NpcStateMasks NpcState { get; }
    //public byte LifeState { get; }
    //public StateMask DisabledState { get; }
    //public StateMask EnabledState { get; }
    //public StateMask EnabledPredictedState { get; }
    public bool IsVulnerable { get; }
    public bool IsFrozenByKelvinUlt { get; }
    public bool HasBackdoorProtection { get; }
    public Vector3 Position { get; }
    public float Yaw { get; }
    public float Pitch { get; }
    public int Health { get; }
  }

  public record WalkerVariables : IWalkerVariables
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
    public required bool IsVulnerable { get; init; }
    public required bool IsFrozenByKelvinUlt { get; init; }
    public required bool HasBackdoorProtection { get; init; }
    public required Vector3 Position { get; init; }
    public required float Yaw { get; init; }
    public required float Pitch { get; init; }
    public required int Health { get; init; }

    public static WalkerVariables CopyFrom(IWalkerVariables other) => new()
    {
      //NpcState = other.NpcState,
      //LifeState = other.LifeState,
      //DisabledState = other.DisabledState,
      //EnabledState = other.EnabledState,
      //EnabledPredictedState = other.EnabledPredictedState,
      IsVulnerable = other.IsVulnerable,
      IsFrozenByKelvinUlt = other.IsFrozenByKelvinUlt,
      HasBackdoorProtection = other.HasBackdoorProtection,
      Position = other.Position,
      Yaw = other.Yaw,
      Pitch = other.Pitch,
      Health = other.Health,
    };
  }

  public class WalkerView : IWalkerConstants, IWalkerVariables
  {
    public required DeadlockDemo.CNPC_Boss_Tier2 Entity { get; init; }

    public uint EntityIndex => Entity.EntityIndex.Value;
    public DeadlockDemo.TeamNumber Team => Entity.CitadelTeamNum;
    private bool teamValid() =>
      Enum.IsDefined(Entity.CitadelTeamNum)
      && Entity.CitadelTeamNum != DeadlockDemo.TeamNumber.Unassigned
      && Entity.CitadelTeamNum != DeadlockDemo.TeamNumber.Spectator;
    public ELane Lane => (ELane)Entity.Lane;
    private bool laneValid() => Enum.IsDefined((ELane)Entity.Lane);
    public ESubclassId Subclass => (ESubclassId)Entity.SubclassID.Value;
    private bool subclassValid() =>
      Entity.SubclassID.Value == (uint)(
        (Lane == ELane.Yellow || Lane == ELane.Green)
        ? (Team == DeadlockDemo.TeamNumber.Amber ? ESubclassId.WalkerOutsideLanesAmber : ESubclassId.WalkerOutsideLanesSapphire)
        : (Team == DeadlockDemo.TeamNumber.Amber ? ESubclassId.WalkerInsideLanesAmber : ESubclassId.WalkerInsideLanesSapphire)
      );
    public int MaxHealth => Entity.MaxHealth;
    private bool maxHealthValid() => Entity.MaxHealth == ((Lane == ELane.Yellow || Lane == ELane.Green) ? 7000 : 9000);

    //public NpcStateMasks NpcState => (NpcStateMasks)Entity.NPCState;
    private bool npcStateValid() => (int)Entity.NPCState >= (int)NpcStateMasks.MIN && (int)Entity.NPCState <= (int)NpcStateMasks.MAX;
    //public byte LifeState => Entity.LifeState;
    private bool lifeStateValid() => Entity.LifeState >= 0 && Entity.LifeState <= 2 && ((Entity.LifeState == 0) == Entity.IsAlive);
    private DeadlockDemo.CModifierProperty modifierProp => Entity.ModifierProp ?? throw new NullReferenceException(nameof(Entity.ModifierProp));
    private bool modifierPropAccessible() => Entity.ModifierProp != null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool stateMaskZero(uint[] mask) =>
      mask[0] == 0 && mask[1] == 0 && mask[2] == 0 && mask[3] == 0 && mask[4] == 0 && mask[5] == 0 && mask[6] == 0;
    //public StateMask EnabledState => StateMask.From(modifierProp.EnabledStateMask);
    public bool IsVulnerable => (modifierProp.EnabledStateMask[(int)ModifierStateIndex.Invulnerable] & (uint)ModifierStateMask.Invulnerable) == 0;
    public bool IsFrozenByKelvinUlt => (modifierProp.EnabledStateMask[(int)ModifierStateIndex.NoIncomingDamage] & (uint)ModifierStateMask.NoIncomingDamage) != 0;
    public bool HasBackdoorProtection => (modifierProp.EnabledStateMask[(int)ModifierStateIndex.BackdoorProtected] & (uint)ModifierStateMask.BackdoorProtected) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool allSame(bool x, bool y, bool z) => (x == y) && (x == z);
    private bool statesAccessible() =>
      modifierProp.DisabledStateMask.Length == 7
      && modifierProp.EnabledStateMask.Length == 7
      && modifierProp.EnabledPredictedStateMask.Length == 7;
    private bool statesValid() => 
      stateMaskZero(modifierProp.DisabledStateMask)
      && stateMaskZero(modifierProp.EnabledPredictedStateMask)
      && (
        Entity.IsAlive
        ? (
          (modifierProp.EnabledStateMask[(int)ModifierStateIndex.HealthRegenDisabled] & (uint)ModifierStateMask.HealthRegenDisabled) != 0
          && (modifierProp.EnabledStateMask[(int)ModifierStateIndex.HealingDisabled] & (uint)ModifierStateMask.HealingDisabled) != 0
          && allSame( // IsVulnerable is defined by all three of these
            (modifierProp.EnabledStateMask[(int)ModifierStateIndex.Invulnerable] & (uint)ModifierStateMask.Invulnerable) != 0,
            (modifierProp.EnabledStateMask[(int)ModifierStateIndex.TechUntargetableByEnemies] & (uint)ModifierStateMask.TechUntargetableByEnemies) != 0,
            (modifierProp.EnabledStateMask[(int)ModifierStateIndex.NpcTargetableWhileInvulnerable] & (uint)ModifierStateMask.NpcTargetableWhileInvulnerable) != 0
          )
          && allSame( // IsFrozenByKelvinUlt is defined by all three of these
            (modifierProp.EnabledStateMask[(int)ModifierStateIndex.TechDamageInvulnerable] & (uint)ModifierStateMask.TechDamageInvulnerable) != 0,
            (modifierProp.EnabledStateMask[(int)ModifierStateIndex.BulletInvulnerable] & (uint)ModifierStateMask.BulletInvulnerable) != 0,
            (modifierProp.EnabledStateMask[(int)ModifierStateIndex.NoIncomingDamage] & (uint)ModifierStateMask.NoIncomingDamage) != 0
          )
        )
        : stateMaskZero(modifierProp.EnabledStateMask)
      );
    public Vector3 Position => MiscFunctions.ConvertVector(Entity.Origin);
    public float Yaw => Entity.Rotation.Yaw;
    public float Pitch => Entity.Rotation.Pitch;
    public int Health => Entity.Health;
    // Would like to check:
    // && (Entity.MaxHealth == 0 || Entity.Health <= Entity.MaxHealth)
    // but when max health increases in game, Health can actually increase *first* for a few frames before MaxHealth is updated
    private bool healthValid() => Entity.IsAlive ? Entity.Health > 0 : Entity.Health == 0;


    public bool AllAccessible() => modifierPropAccessible() && statesAccessible();
    public bool ConstantsValid() => teamValid() && laneValid() && subclassValid() && maxHealthValid();
    public bool VariablesValid() => npcStateValid() && lifeStateValid() && statesValid() && healthValid();
  }

  public class WalkerHistory
  {
    public WalkerHistory(WalkerView view)
    {
      View = view;
      if (!View.AllAccessible() || !View.ConstantsValid()) throw new Exception(nameof(View));
      if (
        !WalkerVariables.ExpectedStartingPositions
        .Any(p => p.team == View.Team && p.lane == View.Lane && p.position == View.Position)
      ) throw new Exception(nameof(WalkerVariables.ExpectedStartingPositions));
      Constants = WalkerConstants.CopyFrom(view);
    }

    public WalkerView View { get; private init; }
    public WalkerConstants Constants { get; private init; }
    public List<(uint iFrame, bool deleted, WalkerVariables variables)> VariableHistory { get; } = [];

    public void AfterFrame(Frame frame, bool deleted)
    {
      if (!View.AllAccessible() || !View.VariablesValid()) throw new Exception($"{frame}: invalid elements on walker {View.Entity.EntityIndex.Value}");
      if (WalkerConstants.CopyFrom(View) != Constants) throw new Exception($"{frame}: constants changed on walker {View.Entity.EntityIndex.Value}");

      var frameVariables = WalkerVariables.CopyFrom(View);

      if (
        VariableHistory.Count > 0
        && VariableHistory[^1].deleted
        && (!deleted || frameVariables != VariableHistory[^1].variables)
      ) throw new Exception($"{frame}: variables changed on deleted walker {View.Entity.EntityIndex.Value}");

      if (deleted && frameVariables.Health > 0) throw new Exception(nameof(deleted));

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
}
