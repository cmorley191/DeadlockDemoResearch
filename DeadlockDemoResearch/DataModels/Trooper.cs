
using System.Numerics;
using System.Runtime.CompilerServices;
using DeadlockDemo = DemoFile.Game.Deadlock;

namespace DeadlockDemoResearch.DataModels
{
  public interface ITrooperConstants
  {
    public uint EntityIndex { get; }
    public ETrooperSubclassId Subclass { get; }
    public DeadlockDemo.TeamNumber Team { get; }
    public ELane Lane { get; }
  }

  public record TrooperConstants : ITrooperConstants
  {
    public required uint EntityIndex { get; init; }
    public required ETrooperSubclassId Subclass { get; init; }
    public required DeadlockDemo.TeamNumber Team { get; init; }
    public required ELane Lane { get; init; }

    public static TrooperConstants CopyFrom(ITrooperConstants other) => new()
    {
      EntityIndex = other.EntityIndex,
      Subclass = other.Subclass,
      Team = other.Team,
      Lane = other.Lane,
    };
  }

  public class StateMask
  {
    public required uint[] Mask { get; init; }

    public static StateMask From(uint[] mask)
    {
      var x = new StateMask { Mask = new uint[7] };
      Array.Copy(mask, x.Mask, 7);
      return x;
    }

    public override bool Equals(object? obj) =>
      obj is StateMask other && Mask.SequenceEqual(other.Mask);
    public override int GetHashCode() =>
      HashCode.Combine(Mask.Aggregate(0, HashCode.Combine));
  }

  public interface ITrooperVariables
  {
    //public NpcStateMasks NpcState { get; }
    //public byte LifeState { get; }
    public ETrooperRelevantState TrooperState { get; }
    public Vector3 Position { get; }
    public float Yaw { get; }
    public float Pitch { get; }
    public int Health { get; }
    public int MaxHealth { get; }
    public bool IsAlive { get; }
  }

  public record TrooperVariables : ITrooperVariables
  {
    //public required NpcStateMasks NpcState { get; init; }
    //public required byte LifeState { get; init; }
    public required ETrooperRelevantState TrooperState { get; init; }
    public required Vector3 Position { get; init; }
    public required float Yaw { get; init; }
    public required float Pitch { get; init; }
    public required int Health { get; init; }
    public required int MaxHealth { get; init; }
    public required bool IsAlive { get; init; }

    public static TrooperVariables CopyFrom(ITrooperVariables other) => new()
    {
      //NpcState = other.NpcState,
      //LifeState = other.LifeState,
      TrooperState = other.TrooperState,
      Position = other.Position,
      Yaw = other.Yaw,
      Pitch = other.Pitch,
      Health = other.Health,
      MaxHealth = other.MaxHealth,
      IsAlive = other.IsAlive,
    };
  }

  public class TrooperView : ITrooperConstants, ITrooperVariables
  {
    public required DeadlockDemo.CNPC_Trooper Entity { get; init; }

    public uint EntityIndex => Entity.EntityIndex.Value;
    public ETrooperSubclassId Subclass => (ETrooperSubclassId)Entity.SubclassID.Value;
    private bool subclassValid() => Enum.IsDefined((ETrooperSubclassId)Entity.SubclassID.Value);
    public DeadlockDemo.TeamNumber Team => Entity.CitadelTeamNum;
    private bool teamValid() =>
      Enum.IsDefined(Entity.CitadelTeamNum)
      && Entity.CitadelTeamNum != DeadlockDemo.TeamNumber.Unassigned
      && Entity.CitadelTeamNum != DeadlockDemo.TeamNumber.Spectator;
    public ELane Lane => (ELane)Entity.Lane;
    private bool laneValid() => Enum.IsDefined((ELane)Entity.Lane);


    //public NpcStateMasks NpcState => (NpcStateMasks)Entity.NPCState;
    private bool npcStateValid() => (int)Entity.NPCState >= (int)NpcStateMasks.MIN && (int)Entity.NPCState <= (int)NpcStateMasks.MAX;
    //public byte LifeState => Entity.LifeState;
    private bool lifeStateValid() => Entity.LifeState >= 0 && Entity.LifeState <= 2 && ((Entity.LifeState == 0) == Entity.IsAlive);
    private DeadlockDemo.CModifierProperty modifierProp => Entity.ModifierProp ?? throw new NullReferenceException(nameof(Entity.ModifierProp));
    private bool modifierPropAccessible() => Entity.ModifierProp != null;
    public ETrooperRelevantState TrooperState =>
      Subclass switch
      {
        ETrooperSubclassId.ZiplinePackage =>
          (modifierProp.EnabledPredictedStateMask[(int)ModifierStateIndex.UsingZipline] & (uint)ModifierStateMask.UsingZipline) != 0
          ? ETrooperRelevantState.UsingZipline
          : ETrooperRelevantState.None,
        _ =>
          (modifierProp.EnabledStateMask[(int)ModifierStateIndex.SelfDestruct] & (uint)ModifierStateMask.SelfDestruct) != 0
          ? ETrooperRelevantState.SelfDestruct
          : ETrooperRelevantState.None,
      };
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool stateMaskZero(uint[] mask) =>
      mask[0] == 0 && mask[1] == 0 && mask[2] == 0 && mask[3] == 0 && mask[4] == 0 && mask[5] == 0 && mask[6] == 0;
    private bool trooperStateAccessible() =>
      modifierProp.DisabledStateMask.Length == 7
      && modifierProp.EnabledStateMask.Length == 7
      && modifierProp.EnabledPredictedStateMask.Length == 7;
    private bool trooperStateValid() =>
      stateMaskZero(modifierProp.DisabledStateMask)
      && (Subclass == ETrooperSubclassId.ZiplinePackage || stateMaskZero(modifierProp.EnabledPredictedStateMask));
    public Vector3 Position => MiscFunctions.ConvertVector(Entity.Origin);
    public float Yaw => Entity.Rotation.Yaw;
    public float Pitch => Entity.Rotation.Pitch;
    public int Health => Entity.Health;
    private bool healthValid() => Entity.IsAlive ? Entity.Health > 0 : (Entity.Health >= 0 && Entity.Health <= 1);
    public int MaxHealth => Entity.MaxHealth;
    private bool maxHealthValid() => Entity.MaxHealth >= 0;
    public bool IsAlive => Entity.IsAlive;


    public bool AllAccessible() => modifierPropAccessible() && trooperStateAccessible();
    public bool ConstantsValid() => subclassValid() && teamValid() && laneValid();
    public bool VariablesValid() => npcStateValid() && lifeStateValid() && trooperStateValid() && healthValid() && maxHealthValid();
  }

  public class TrooperHistory
  {
    public TrooperHistory(TrooperView view, int iEntityOwnership)
    {
      View = view;
      if (!View.AllAccessible() || !View.ConstantsValid()) throw new Exception(nameof(View));
      IEntityOwnership = iEntityOwnership;
      Constants = TrooperConstants.CopyFrom(view);
    }

    public TrooperView View { get; private init; }
    public int IEntityOwnership { get; private init; }
    public TrooperConstants Constants { get; private init; }
    public List<(uint iFrame, EEntityPvsState pvsState, TrooperVariables variables)> VariableHistory { get; } = [];

    public bool OwnsEntity { get; private set; } = true;
    public void AfterFrame(Frame frame, EEntityPvsState pvsState, bool ownsEntity)
    {
      if (!OwnsEntity) throw new Exception($"{frame}: AfterFrame on trooper after put out of use {View.Entity.EntityIndex.Value}");
      OwnsEntity = ownsEntity;

      if (Constants.Subclass == ETrooperSubclassId.ZiplinePackage && pvsState != EEntityPvsState.Active) throw new Exception(nameof(pvsState));

      if (!View.AllAccessible() || !View.VariablesValid()) throw new Exception($"{frame}: invalid elements on trooper {View.Entity.EntityIndex.Value}");
      if (TrooperConstants.CopyFrom(View) != Constants) throw new Exception($"{frame}: constants changed on trooper {View.Entity.EntityIndex.Value}");

      var frameVariables = TrooperVariables.CopyFrom(View);

      if (
        VariableHistory.Count > 0
        && pvsState == VariableHistory[VariableHistory.Count - 1].pvsState
        && pvsState != EEntityPvsState.Active
        && frameVariables != VariableHistory[VariableHistory.Count - 1].variables
      ) throw new Exception($"{frame}: variables changed on inactive trooper {View.Entity.EntityIndex.Value}");

      if (ownsEntity && (
        VariableHistory.Count == 0
        || pvsState != VariableHistory[VariableHistory.Count - 1].pvsState
        || frameVariables != VariableHistory[VariableHistory.Count - 1].variables
      ))
      {
        VariableHistory.Add((frame.iFrame, pvsState, frameVariables));
      }
    }
  }

  public record UnifiedTrooperZiplineConstants
  {
    public required uint ZiplineTrooperEntityIndex { get; init; }
    public required bool IsNonStarter { get; init; }
    public required DeadlockDemo.TeamNumber Team { get; init; }
    public required ELane Lane { get; init; }
  }

  public record UnifiedTrooperActiveConstants
  {
    public required uint ActiveTrooperEntityIndex { get; init; }
    public required ETrooperActiveSubclassId Subclass { get; init; }
  }

  public class UnifiedTrooperHistory
  {
    public UnifiedTrooperHistory(TrooperHistory ziplineTrooper)
    {
      ZiplineTrooper = ziplineTrooper;
      if (ZiplineTrooper.Constants.Subclass != ETrooperSubclassId.ZiplinePackage) throw new Exception(nameof(ZiplineTrooper.Constants.Subclass));
      ZiplineConstants = new()
      {
        ZiplineTrooperEntityIndex = ZiplineTrooper.Constants.EntityIndex,
        IsNonStarter = ZiplineTrooper.VariableHistory[0].variables.Health == 0,
        Team = ZiplineTrooper.Constants.Team,
        Lane = ZiplineTrooper.Constants.Lane,
      };
    }

    public TrooperHistory ZiplineTrooper { get; private init; }
    public UnifiedTrooperZiplineConstants ZiplineConstants { get; private init; }

    public (TrooperHistory trooper, UnifiedTrooperActiveConstants constants)? ActiveTrooper { get; private set; }
    public void SetActiveTrooper(TrooperHistory trooper)
    {
      if (ActiveTrooper != null) throw new Exception(nameof(ActiveTrooper));

      if (ZiplineConstants.IsNonStarter) throw new Exception(nameof(ZiplineConstants.IsNonStarter));

      if (!Enum.IsDefined((ETrooperActiveSubclassId)trooper.Constants.Subclass)) throw new Exception(nameof(trooper.Constants.Subclass));
      if (trooper.Constants.Team != ZiplineConstants.Team) throw new Exception(nameof(trooper.Constants.Team));
      if (trooper.Constants.Lane != ZiplineConstants.Lane) throw new Exception(nameof(trooper.Constants.Lane));

      ActiveTrooper = (
        trooper: trooper,
        constants: new()
        {
          ActiveTrooperEntityIndex = trooper.Constants.EntityIndex,
          Subclass = (ETrooperActiveSubclassId)trooper.Constants.Subclass,
        }
      );
    }

    public IEnumerable<(uint iFrame, EEntityPvsState pvsState, EUnifiedTrooperState state, TrooperVariables variables)> VariableHistory {
      get {
        int i;
        EUnifiedTrooperState state = 0;
        for (i = 0; true; i++)
        {
          if (i >= ZiplineTrooper.VariableHistory.Count) yield break;

          if (i == 0)
          {
            if (ZiplineTrooper.VariableHistory[i].variables.Health == 0) state = EUnifiedTrooperState.NonStarter;
            else state = EUnifiedTrooperState.Packed_WaitingToZipline;
          }

          if ((state == EUnifiedTrooperState.NonStarter) != (ZiplineConstants.IsNonStarter)) throw new Exception(nameof(ZiplineConstants.IsNonStarter));

          var ziplineState = ZiplineTrooper.VariableHistory[i].variables.TrooperState;
          if (
            state == EUnifiedTrooperState.Packed_WaitingToZipline
            && ziplineState == ETrooperRelevantState.UsingZipline
          ) state = EUnifiedTrooperState.Packed_Ziplining;

          if (
            state == EUnifiedTrooperState.Packed_Ziplining
            && ziplineState == ETrooperRelevantState.None
          ) state = EUnifiedTrooperState.Packed_DroppingOffZipline;

          if (
            (state == EUnifiedTrooperState.Packed_Ziplining || state == EUnifiedTrooperState.Packed_DroppingOffZipline)
            && ZiplineTrooper.VariableHistory[i].variables.Health == 0
          ) state = EUnifiedTrooperState.Unpacked_Active;

          if (
            state switch
            {
              EUnifiedTrooperState.NonStarter
              or EUnifiedTrooperState.Packed_WaitingToZipline
              or EUnifiedTrooperState.Packed_Ziplining
              or EUnifiedTrooperState.Packed_DroppingOffZipline => true,
              _ => false
            }
          )
          {
            yield return (
              iFrame: ZiplineTrooper.VariableHistory[i].iFrame, 
              pvsState: ZiplineTrooper.VariableHistory[i].pvsState, 
              state, 
              variables: ZiplineTrooper.VariableHistory[i].variables
            );
          }
          else
          {
            break;
          }
        }

        switch (state)
        {
          case EUnifiedTrooperState.Unpacked_Active:
          case EUnifiedTrooperState.Unpacked_SelfDestructing:
          case EUnifiedTrooperState.Unpacked_Dead:
            {
              if (ActiveTrooper == null) throw new NullReferenceException(nameof(ActiveTrooper));
              for (i = 0; true; i++)
              {
                if (i >= ActiveTrooper.Value.trooper.VariableHistory.Count) yield break;

                var activeState = ActiveTrooper.Value.trooper.VariableHistory[i].variables.TrooperState;
                if (
                  state == EUnifiedTrooperState.Unpacked_Active
                  && activeState == ETrooperRelevantState.SelfDestruct
                ) state = EUnifiedTrooperState.Unpacked_SelfDestructing;

                if (
                  (state == EUnifiedTrooperState.Unpacked_Active || state == EUnifiedTrooperState.Unpacked_SelfDestructing)
                  && ActiveTrooper.Value.trooper.VariableHistory[i].variables.Health == 0
                ) state = EUnifiedTrooperState.Unpacked_Dead;

                yield return (
                  iFrame: ActiveTrooper.Value.trooper.VariableHistory[i].iFrame, 
                  pvsState: ActiveTrooper.Value.trooper.VariableHistory[i].pvsState, 
                  state, 
                  variables: ActiveTrooper.Value.trooper.VariableHistory[i].variables
                );
              }
            }

          default:
            if (ActiveTrooper != null) throw new Exception(nameof(ActiveTrooper));
            yield break;
        }
      }
    }
  }
}
