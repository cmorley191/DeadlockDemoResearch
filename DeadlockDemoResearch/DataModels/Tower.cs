
using System.Numerics;
using System.Runtime.CompilerServices;
using DeadlockDemo = DemoFile.Game.Deadlock;

namespace DeadlockDemoResearch.DataModels
{
  public interface ITowerConstants
  {
    public uint EntityIndex { get; }
    /// <summary>
    /// True=T3, False=T1
    /// </summary>
    public bool IsTierThree { get; }
    public DeadlockDemo.TeamNumber Team { get; }
    public ELane Lane { get; }
    public Vector3 Position { get; }
    public ESubclassId Subclass { get; }
    public int MaxHealth { get; }
  }

  public record TowerConstants : ITowerConstants
  {
    public static readonly List<(DeadlockDemo.TeamNumber team, ELane lane, Vector3 position)> ExpectedTier1Positions = [
      (DeadlockDemo.TeamNumber.Amber, ELane.Yellow, new(-8128f, -1856f, 248.03125f)),
      (DeadlockDemo.TeamNumber.Amber, ELane.Blue,   new(256f, -1792f, 376.03125f)),
      (DeadlockDemo.TeamNumber.Amber, ELane.Green, new(7040f, -1984f, 248.03125f)),

      (DeadlockDemo.TeamNumber.Sapphire, ELane.Yellow, new(-7040f, 1984f, 248.03125f)),
      (DeadlockDemo.TeamNumber.Sapphire, ELane.Blue,   new(-256f, 1792f, 376.03125f)),
      (DeadlockDemo.TeamNumber.Sapphire, ELane.Green, new(8128f, 1856f, 248.03125f)),
    ];
    public static readonly List<(DeadlockDemo.TeamNumber team, ELane lane, Vector3 position)> ExpectedTier3Positions = [
      (DeadlockDemo.TeamNumber.Amber, ELane.Yellow, new(-1920f, -6592f, 512.03125f)),
      (DeadlockDemo.TeamNumber.Amber, ELane.Yellow, new(-1664f, -6336f, 512.03125f)),
      (DeadlockDemo.TeamNumber.Amber, ELane.Blue,   new(-128f, -5696f, 512.03125f)),
      (DeadlockDemo.TeamNumber.Amber, ELane.Blue,   new(128f, -5696f, 512.0625f)), // yep this one's got a weird Z
      (DeadlockDemo.TeamNumber.Amber, ELane.Green, new(1664f, -6336f, 512.03125f)),
      (DeadlockDemo.TeamNumber.Amber, ELane.Green, new(1920f, -6592f, 512.03125f)),

      (DeadlockDemo.TeamNumber.Sapphire, ELane.Yellow, new(-1920f, 6592f, 512.03125f)),
      (DeadlockDemo.TeamNumber.Sapphire, ELane.Yellow, new(-1664f, 6336f, 512.03125f)),
      (DeadlockDemo.TeamNumber.Sapphire, ELane.Blue,   new(-128f, 5696f, 512.03125f)),
      (DeadlockDemo.TeamNumber.Sapphire, ELane.Blue,   new(128f, 5696f, 512.03125f)),
      (DeadlockDemo.TeamNumber.Sapphire, ELane.Green, new(1664f, 6336f, 512.03125f)),
      (DeadlockDemo.TeamNumber.Sapphire, ELane.Green, new(1920f, 6592f, 512.03125f)),
    ];

    public required uint EntityIndex { get; init; }
    public required bool IsTierThree { get; init; }
    public required DeadlockDemo.TeamNumber Team { get; init; }
    public required ELane Lane { get; init; }
    public required Vector3 Position { get; init; }
    public required ESubclassId Subclass { get; init; }
    public required int MaxHealth { get; init; }

    public static TowerConstants CopyFrom(ITowerConstants other) => new()
    {
      EntityIndex = other.EntityIndex,
      IsTierThree = other.IsTierThree,
      Team = other.Team,
      Lane = other.Lane,
      Position = other.Position,
      Subclass = other.Subclass,
      MaxHealth = other.MaxHealth,
    };
  }

  public interface ITowerVariables
  {
    //public NpcStateMasks NpcState { get; }
    //public byte LifeState { get; }
    //public StateMask EnabledState { get; }
    public bool IsTargeting { get; }
    public bool IsFrozenByKelvinUlt { get; }
    public bool HasBackdoorProtection { get; }
    public int Health { get; }
  }

  public record TowerVariables : ITowerVariables
  {
    //public required NpcStateMasks NpcState { get; init; }
    //public required byte LifeState { get; init; }
    //public required StateMask EnabledState { get; init; }
    public required bool IsTargeting { get; init; }
    public required bool IsFrozenByKelvinUlt { get; init; }
    public required bool HasBackdoorProtection { get; init; }
    public required int Health { get; init; }

    public static TowerVariables CopyFrom(ITowerVariables other) => new()
    {
      //NpcState = other.NpcState,
      //LifeState = other.LifeState,
      //EnabledState = other.EnabledState,
      IsTargeting = other.IsTargeting,
      IsFrozenByKelvinUlt = other.IsFrozenByKelvinUlt,
      HasBackdoorProtection = other.HasBackdoorProtection,
      Health = other.Health,
    };
  }

  public class TowerView : ITowerConstants, ITowerVariables
  {
    public required DeadlockDemo.CNPC_TrooperBoss Entity { get; init; }

    public uint EntityIndex => Entity.EntityIndex.Value;
    public bool IsTierThree => Entity is DeadlockDemo.CNPC_TrooperBarrackBoss;
    public DeadlockDemo.TeamNumber Team => Entity.CitadelTeamNum;
    private bool teamValid() =>
      Enum.IsDefined(Entity.CitadelTeamNum)
      && Entity.CitadelTeamNum != DeadlockDemo.TeamNumber.Unassigned
      && Entity.CitadelTeamNum != DeadlockDemo.TeamNumber.Spectator;
    public ELane Lane => (ELane)Entity.Lane;
    private bool laneValid() => Enum.IsDefined((ELane)Entity.Lane);
    public Vector3 Position => MiscFunctions.ConvertVector(Entity.Origin);
    private bool positionValid() =>
      (IsTierThree ? TowerConstants.ExpectedTier3Positions : TowerConstants.ExpectedTier1Positions)
        .Any(p => p.team == Team && p.lane == Lane && p.position == Position);
    public ESubclassId Subclass => (ESubclassId)Entity.SubclassID.Value;
    private bool subclassValid() =>
      Entity.SubclassID.Value == (uint)(
        IsTierThree
        ? ESubclassId.TowerTier3
        : Team == DeadlockDemo.TeamNumber.Amber
        ? ESubclassId.TowerTier1Amber
        : ESubclassId.TowerTier1Sapphire
      );
    public int MaxHealth => Entity.MaxHealth;
    private bool maxHealthValid() => Entity.MaxHealth == (IsTierThree ? 4300 : 5500);

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
    public bool IsTargeting => (modifierProp.EnabledStateMask[(int)ModifierStateIndex.Invulnerable] & (uint)ModifierStateMask.Invulnerable) == 0;
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
          && allSame( // IsTargeting is defined by all three of these
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
    public int Health => Entity.Health;
    private bool healthValid() => Entity.IsAlive ? Entity.Health > 0 : Entity.Health == 0;


    public bool AllAccessible() => modifierPropAccessible() && statesAccessible();
    public bool ConstantsValid() => teamValid() && laneValid() && positionValid() && subclassValid() && maxHealthValid();
    public bool VariablesValid() => npcStateValid() && lifeStateValid() && statesValid() && healthValid();
  }

  public class TowerHistory
  {
    public TowerHistory(TowerView view)
    {
      View = view;
      if (!View.AllAccessible() || !View.ConstantsValid()) throw new Exception(nameof(View));
      Constants = TowerConstants.CopyFrom(view);
    }

    public TowerView View { get; private init; }
    public TowerConstants Constants { get; private init; }
    public List<(uint iFrame, bool deleted, TowerVariables variables)> VariableHistory { get; } = [];

    public void AfterFrame(Frame frame, bool deleted)
    {
      if (!View.AllAccessible() || !View.VariablesValid()) throw new Exception($"{frame}: invalid elements on tower {View.Entity.EntityIndex.Value}");
      if (TowerConstants.CopyFrom(View) != Constants) throw new Exception($"{frame}: constants changed on tower {View.Entity.EntityIndex.Value}");

      var frameVariables = TowerVariables.CopyFrom(View);

      if (
        VariableHistory.Count > 0
        && VariableHistory[^1].deleted
        && (!deleted || frameVariables != VariableHistory[^1].variables)
      ) throw new Exception($"{frame}: variables changed on deleted tower {View.Entity.EntityIndex.Value}");

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
