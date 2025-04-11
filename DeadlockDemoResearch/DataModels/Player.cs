using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

using DeadlockDemo = DemoFile.Game.Deadlock;

namespace DeadlockDemoResearch.DataModels
{
  public interface IPlayerConstants
  {
    [JsonIgnore]
    public DeadlockDemo.CCitadelPlayerPawn HeroPawn { get; }
    public DeadlockDemo.TeamNumber Team { get; }
    public string Name { get; }
    public ulong SteamId { get; }
    public byte LobbyPlayerSlot { get; }
    public EHero HeroId { get; }
  }

  public record PlayerConstants : IPlayerConstants
  {
    [JsonIgnore]
    public /*required*/ DeadlockDemo.CCitadelPlayerPawn HeroPawn { get; init; }
    public required DeadlockDemo.TeamNumber Team { get; init; }
    public required string Name { get; init; }
    public required ulong SteamId { get; init; }
    public required byte LobbyPlayerSlot { get; init; }
    public required EHero HeroId { get; init; }

    public static PlayerConstants CopyFrom(IPlayerConstants other) => new PlayerConstants
    {
      HeroPawn = other.HeroPawn,
      Team = other.Team,
      Name = other.Name,
      SteamId = other.SteamId,
      LobbyPlayerSlot = other.LobbyPlayerSlot,
      HeroId = other.HeroId,
    };
  }

  public interface IPlayerVariables
  {
    public PlayerConnectedStateMasks Connected { get; }
    public StateMask DisabledState { get; }
    public StateMask EnabledState { get; }
    public StateMask EnabledPredictedState { get; }
    public EPlayerUrnState UrnState { get; }
    public Vector3 BodyPosition { get; }
    public float CamYaw { get; }
    public float CamPitch { get; }
    public byte Level { get; }
    public int Health { get; }
    public int MaxHealth { get; }
    public bool IsAlive { get; }
  }

  public record PlayerVariables : IPlayerVariables
  {
    public required PlayerConnectedStateMasks Connected { get; init; }
    public required StateMask DisabledState { get; init; }
    public required StateMask EnabledState { get; init; }
    public required StateMask EnabledPredictedState { get; init; }
    public required EPlayerUrnState UrnState { get; init; }
    public required Vector3 BodyPosition { get; init; }
    public required float CamYaw { get; init; }
    public required float CamPitch { get; init; }
    public required byte Level { get; init; }
    public required int Health { get; init; }
    public required int MaxHealth { get; init; }
    public required bool IsAlive { get; init; }

    public static PlayerVariables CopyFrom(IPlayerVariables other) => new()
    {
      Connected = other.Connected,
      DisabledState = other.DisabledState,
      EnabledState = other.EnabledState,
      EnabledPredictedState = other.EnabledPredictedState,
      UrnState = other.UrnState,
      BodyPosition = other.BodyPosition,
      CamYaw = other.CamYaw,
      CamPitch = other.CamPitch,
      Level = other.Level,
      Health = other.Health,
      MaxHealth = other.MaxHealth,
      IsAlive = other.IsAlive,
    };
  }

  public class PlayerView : IPlayerConstants, IPlayerVariables
  {
    public required DeadlockDemo.CCitadelPlayerController Controller { get; init; }

    public DeadlockDemo.CCitadelPlayerPawn HeroPawn => Controller.HeroPawn ?? throw new NullReferenceException(nameof(HeroPawn));
    private bool heroPawnAccessible() => Controller.HeroPawn != null;
    public DeadlockDemo.TeamNumber Team => Controller.CitadelTeamNum;
    private bool teamValid() => Enum.IsDefined(Controller.CitadelTeamNum) && Controller.CitadelTeamNum != DeadlockDemo.TeamNumber.Unassigned;
    public bool IsOnPlayingTeam => Team == DeadlockDemo.TeamNumber.Amber || Team == DeadlockDemo.TeamNumber.Sapphire;
    public string Name => Controller.PlayerName;
    private bool nameValid() => !string.IsNullOrEmpty(Controller.PlayerName);
    public ulong SteamId => Controller.SteamID;
    public byte LobbyPlayerSlot => (byte)Controller.LobbyPlayerSlot.Value;
    private bool lobbyPlayerSlotAccessible() => Controller.LobbyPlayerSlot.Value >= byte.MinValue && Controller.LobbyPlayerSlot.Value <= byte.MaxValue;
    public EHero HeroId =>
      HeroPawn.CCitadelHeroComponent.HeroLoading.Value == 0
      ? (EHero)HeroPawn.CCitadelHeroComponent.HeroID.Value
      : (EHero)HeroPawn.CCitadelHeroComponent.HeroLoading.Value;
    private bool heroIdValid() =>
      (HeroPawn.CCitadelHeroComponent.HeroLoading.Value == 0 && Enum.IsDefined((EHero)HeroPawn.CCitadelHeroComponent.HeroID.Value))
      || (HeroPawn.CCitadelHeroComponent.HeroID.Value == 0 && Enum.IsDefined((EHero)HeroPawn.CCitadelHeroComponent.HeroLoading.Value));

    public PlayerConnectedStateMasks Connected => (PlayerConnectedStateMasks)Controller.Connected;
    private bool connectedValid() => (int)Controller.Connected >= (int)PlayerConnectedStateMasks.MIN && (int)Controller.Connected <= (int)PlayerConnectedStateMasks.MAX;
    private bool controllerModifierPropInaccessible() => Controller.ModifierProp == null;
    private DeadlockDemo.CModifierProperty pawnModifierProp => HeroPawn.ModifierProp ?? throw new NullReferenceException(nameof(HeroPawn.ModifierProp));
    private bool pawnModifierPropAccessible() => HeroPawn.ModifierProp != null;
    public StateMask DisabledState => StateMask.From(pawnModifierProp.DisabledStateMask);
    public StateMask EnabledState => StateMask.From(pawnModifierProp.EnabledStateMask);
    public StateMask EnabledPredictedState => StateMask.From(pawnModifierProp.EnabledPredictedStateMask);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool stateMaskZero(uint[] mask) =>
      mask[0] == 0 && mask[1] == 0 && mask[2] == 0 && mask[3] == 0 && mask[4] == 0 && mask[5] == 0;
    private bool statesAccessible() =>
      pawnModifierProp.DisabledStateMask.Length == 6
      && pawnModifierProp.EnabledStateMask.Length == 6
      && pawnModifierProp.EnabledPredictedStateMask.Length == 6;
    public EPlayerUrnState UrnState =>
      (pawnModifierProp.EnabledStateMask[(int)ModifierStateIndex.ReturningIdol] & (uint)ModifierStateMask.ReturningIdol) != 0
      ? EPlayerUrnState.HoldingAndReturning
      : (pawnModifierProp.EnabledStateMask[(int)ModifierStateIndex.HoldingIdol] & (uint)ModifierStateMask.HoldingIdol) != 0
      ? EPlayerUrnState.Holding
      : EPlayerUrnState.NotHolding;
    private bool urnStateValid()
    {
      var holding = (pawnModifierProp.EnabledStateMask[(int)ModifierStateIndex.HoldingIdol] & (uint)ModifierStateMask.HoldingIdol) != 0;
      if (holding != ((pawnModifierProp.EnabledPredictedStateMask[(int)ModifierStateIndex.HoldingIdol] & (uint)ModifierStateMask.HoldingIdol) != 0)) return false;
      var returning = (pawnModifierProp.EnabledStateMask[(int)ModifierStateIndex.ReturningIdol] & (uint)ModifierStateMask.ReturningIdol) != 0;
      if (returning && !holding) return false;
      if (returning != ((pawnModifierProp.EnabledPredictedStateMask[(int)ModifierStateIndex.ReturningIdol] & (uint)ModifierStateMask.ReturningIdol) != 0)) return false;
      return true;
    }
    public Vector3 BodyPosition => MiscFunctions.ConvertVector(HeroPawn.Origin);
    public float CamYaw => HeroPawn.ClientCamera.Yaw;
    public float CamPitch => HeroPawn.ClientCamera.Pitch;
    public byte Level => (byte)HeroPawn.Level;
    private bool levelAccessible() => HeroPawn.Level >= byte.MinValue && HeroPawn.Level <= byte.MaxValue;
    public int Health => HeroPawn.Health;
    private bool healthValid() => HeroPawn.Health >= 0 && Controller.Health == 0;
    public int MaxHealth => HeroPawn.MaxHealth;
    private bool maxHealthValid() => HeroPawn.MaxHealth >= 0 && Controller.MaxHealth == 0;
    public bool IsAlive => HeroPawn.IsAlive;
    private bool isAliveValid() => Controller.IsAlive;


    public bool AllAccessible() => 
      heroPawnAccessible() && lobbyPlayerSlotAccessible() 
      && controllerModifierPropInaccessible() // inaccessible is intentional -- notify if this ever becomes accessible
      && pawnModifierPropAccessible() && statesAccessible() 
      && levelAccessible();
    public bool ConstantsValid() => teamValid() && nameValid() && heroIdValid();
    public bool VariablesValid() => connectedValid() && urnStateValid() && healthValid() && maxHealthValid() && isAliveValid();
  }

  public class PlayerHistory
  {
    public PlayerHistory(PlayerView view)
    {
      View = view;
      if (!View.AllAccessible() || !View.ConstantsValid()) throw new Exception(nameof(View));
      Constants = PlayerConstants.CopyFrom(view);
    }

    public PlayerView View { get; private init; }
    public PlayerConstants Constants { get; private init; }
    public List<(uint iFrame, PlayerVariables variables)> VariableHistory { get; } = [];

    public void AfterFrame(Frame frame)
    {
      if (!View.AllAccessible() || !View.VariablesValid()) throw new Exception($"{frame}: invalid elements on player \"{Constants.Name}\"");
      if (PlayerConstants.CopyFrom(View) != Constants) throw new Exception($"{frame}: constants changed on player \"{Constants.Name}\"");

      var frameVariables = PlayerVariables.CopyFrom(View);
      if (VariableHistory.Count == 0 || frameVariables != VariableHistory[VariableHistory.Count - 1].variables)
      {
        VariableHistory.Add((frame.iFrame, frameVariables));
      }
    }
  }

}
