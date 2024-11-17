using System.Text.Json;
using System.Text.Json.Serialization;
using DeadlockDemo = DemoFile.Game.Deadlock;

namespace DeadlockDemoResearch
{
  public class Program
  {
    static void Main(string[] args)
    {
      MainAsync(args).GetAwaiter().GetResult();
    }

    private static readonly JsonSerializerOptions serializerOptions = new() { WriteIndented = true };
    private static string SerializeJsonObject<T>(T value)
    {
      return JsonSerializer.Serialize<T>(value, serializerOptions).TrimStart('{', '\n').TrimEnd('}', '\n');
    }

    private enum DemoFileType
    {
      Dem = 1,
      DemBz2 = 2,
    }

    public interface IRulesConstants
    {
      public int ExperimentalGameplayState { get; }
      public DeadlockDemo.ECitadelMatchMode MatchMode { get; }
    }
    public record RulesConstants : IRulesConstants
    {
      public required int ExperimentalGameplayState { get; init; }
      public required DeadlockDemo.ECitadelMatchMode MatchMode { get; init; }

      public static RulesConstants FromView(RulesView view) => new RulesConstants
      {
        ExperimentalGameplayState = view.ExperimentalGameplayState,
        MatchMode = view.MatchMode,
      };
    }

    public interface IRulesPreGameConstants
    {
      public float GameStartTime { get; }
    }
    public record RulesPreGameConstants : IRulesPreGameConstants
    {
      public required float GameStartTime { get; init; }

      public static RulesPreGameConstants FromView(RulesView view) => new RulesPreGameConstants
      {
        GameStartTime = view.GameStartTime,
      };
    }

    public interface IRulesVariables
    {
      public ERulesPermittedGameState GameState { get; }
      public EPauseState? PauseState { get; }
    }
    public record RulesVariables : IRulesVariables
    {
      public required ERulesPermittedGameState GameState { get; init; }
      public required EPauseState? PauseState { get; init; }

      public static RulesVariables FromView(RulesView view) => new RulesVariables
      {
        GameState = view.GameState,
        PauseState = view.PauseState,
      };
    }

    public class RulesView : IRulesConstants, IRulesPreGameConstants, IRulesVariables
    {
      public required DeadlockDemo.CCitadelGameRules Rules { get; init; }

      public int ExperimentalGameplayState => Rules.ExperimentalGameplayState;
      public DeadlockDemo.ECitadelMatchMode MatchMode => Rules.MatchMode;
      private bool matchModeValid() =>
        Rules.MatchMode == DeadlockDemo.ECitadelMatchMode.k_ECitadelMatchMode_Unranked
        || Rules.MatchMode == DeadlockDemo.ECitadelMatchMode.k_ECitadelMatchMode_Ranked;

      public float GameStartTime => Rules.GameStartTime.Value;
      private bool gameStartTimeValid() => Rules.GameStartTime.Value >= 0;

      public ERulesPermittedGameState GameState => (ERulesPermittedGameState)Rules.GameState;
      private bool gameStateValid() => Enum.IsDefined((ERulesPermittedGameState)Rules.GameState);

      public EPauseState? PauseState =>
        Rules.GamePaused
        ? (
          Rules.PauseTeam == -1
          ? EPauseState.UnpausingCountdown
          : (EPauseState)Rules.PauseTeam
        )
        : EPauseState.Unpaused;
      private bool pausedByTeamValid() =>
        Rules.GamePaused
        ? (Rules.PauseTeam == -1 || Enum.IsDefined((EPlayingTeam)Rules.PauseTeam))
        : Rules.PauseTeam == -1;

      public bool AllValid() => matchModeValid() && gameStartTimeValid() && gameStateValid() && pausedByTeamValid();
    }

    public class RulesHistory
    {
      public RulesHistory(RulesView view)
      {
        View = view;
        Constants = RulesConstants.FromView(View);
      }

      public RulesView View { get; private init; }
      public RulesConstants Constants { get; private init; }
      public RulesPreGameConstants? PreGameConstants { get; private set; }
      private static readonly RulesPreGameConstants prePreGame_PreGameConstants = new RulesPreGameConstants { GameStartTime = 0 };
      public List<(uint gameTick, float? newMatchClock, RulesVariables variables)> VariableHistory { get; } = [];

      public void AfterFrame(Frame? previousFrame, uint frameGameTick, int matchId)
      {
        if (!View.AllValid()) throw new Exception($"{frameGameTick}: invalid elements on rules");
        if (RulesConstants.FromView(View) != Constants) throw new Exception($"{frameGameTick}: constants changed on rules");

        if (View.GameState == ERulesPermittedGameState.WaitingForPlayersToJoin)
        {
          if (RulesPreGameConstants.FromView(View) != prePreGame_PreGameConstants) throw new Exception($"{frameGameTick}: invalid pre-pre-game constants on rules");
        }
        else
        {
          var thisFrameConstants = RulesPreGameConstants.FromView(View);
          if (PreGameConstants == null) PreGameConstants = thisFrameConstants;
          else if (thisFrameConstants != PreGameConstants) throw new Exception($"{frameGameTick}: pre-game constants changed on rules");
        }

        // rules constants across *all* games
        {
          // idk what this is
          if (View.Rules.FreezePeriod) throw new Exception(nameof(View.Rules.FreezePeriod));

          // appear to be unused
          if (View.Rules.LevelStartTime.Value != 0) throw new Exception(nameof(View.Rules.LevelStartTime));
          if (View.Rules.RoundStartTime.Value != 0) throw new Exception(nameof(View.Rules.RoundStartTime));

          if (View.Rules.MinimapMins != new DemoFile.Vector(-10752, -10752, 0)) throw new Exception(nameof(View.Rules.MinimapMins));
          if (View.Rules.MinimapMaxs != new DemoFile.Vector(10752, 10752, 0)) throw new Exception(nameof(View.Rules.MinimapMaxs));

          if (View.Rules.NoDeathEnabled) throw new Exception(nameof(View.Rules.NoDeathEnabled));
          if (View.Rules.FastCooldownsEnabled) throw new Exception(nameof(View.Rules.FastCooldownsEnabled));
          if (View.Rules.StaminaCooldownsEnabled) throw new Exception(nameof(View.Rules.StaminaCooldownsEnabled));
          if (View.Rules.UnlimitedAmmoEnabled) throw new Exception(nameof(View.Rules.UnlimitedAmmoEnabled));
          if (View.Rules.InfiniteResourcesEnabled) throw new Exception(nameof(View.Rules.InfiniteResourcesEnabled));
          if (View.Rules.FlexSlotsForcedUnlocked) throw new Exception(nameof(View.Rules.FlexSlotsForcedUnlocked));

          if (View.Rules.GameMode != DeadlockDemo.ECitadelGameMode.k_ECitadelGameMode_Normal) throw new Exception(nameof(View.Rules.GameMode));

          // idk what this is
          if (View.Rules.ServerPaused) throw new Exception(nameof(View.Rules.ServerPaused));

          // idk what this is
          if (View.Rules.RequiresReportCardDismissal) throw new Exception(nameof(View.Rules.RequiresReportCardDismissal));

          // guessing this is forfeit, which should be unused
          if (View.Rules.GGTeam != 16 || View.Rules.GGEndsAtTime.Value != 0) throw new Exception(nameof(View.Rules.GGTeam));

          if (View.Rules.MatchID.Value != matchId) throw new Exception(nameof(View.Rules.MatchID));

          // appears to be unused
          if (View.Rules.HeroDiedTime.Value != 0) throw new Exception(nameof(View.Rules.HeroDiedTime));
        }

        var frameVariables = RulesVariables.FromView(View);
        if (VariableHistory.Count == 0)
        {
          if (frameVariables != new RulesVariables
          {
            GameState = ERulesPermittedGameState.WaitingForPlayersToJoin,
            PauseState = EPauseState.Unpaused,
          }) throw new Exception(nameof(frameVariables));
          if (View.Rules.TotalPausedTicks != 0) throw new Exception(nameof(View.Rules.TotalPausedTicks));
          if (View.Rules.PauseStartTick != 0) throw new Exception(nameof(View.Rules.PauseStartTick));
          if (View.Rules.MatchClockUpdateTick != 0) throw new Exception(nameof(View.Rules.MatchClockUpdateTick));
          if (View.Rules.MatchClockAtLastUpdate != 0) throw new Exception(nameof(View.Rules.MatchClockAtLastUpdate));

          VariableHistory.Add((gameTick: frameGameTick, newMatchClock: null, frameVariables));
        }
        else
        {
          if (previousFrame == null) throw new Exception(nameof(previousFrame));

          var previousVariables = VariableHistory[VariableHistory.Count - 1];

          var lastClockUpdate = VariableHistory
            .Where(v => v.newMatchClock != null)
            .Select(v => new { v.gameTick, newMatchClock = v.newMatchClock ?? throw new NullReferenceException("should never happen due to Where clause in above line") })
            .LastOrDefault(defaultValue: new { gameTick = 0u, newMatchClock = 0f });

          if (frameVariables == previousVariables.variables)
          {
            if (View.Rules.MatchClockUpdateTick != lastClockUpdate.gameTick) throw new Exception(nameof(View.Rules.MatchClockUpdateTick));
            if (View.Rules.MatchClockAtLastUpdate != lastClockUpdate.newMatchClock) throw new Exception(nameof(View.Rules.MatchClockAtLastUpdate));
          }
          else
          {
            if (
              frameVariables.GameState != previousVariables.variables.GameState
              && previousVariables.variables.GameState switch
              {
                ERulesPermittedGameState.WaitingForPlayersToJoin => (frameVariables.GameState != ERulesPermittedGameState.PreGameWait),
                ERulesPermittedGameState.PreGameWait => (frameVariables.GameState != ERulesPermittedGameState.GameInProgress),
                ERulesPermittedGameState.GameInProgress => (frameVariables.GameState != ERulesPermittedGameState.PostGame),
                _ => true,
              }
            ) throw new Exception(nameof(frameVariables.GameState));

            if (
              frameVariables.PauseState != previousVariables.variables.PauseState
              && previousVariables.variables.PauseState switch
              {
                EPauseState.Unpaused => !(frameVariables.PauseState == EPauseState.PausedByAmber || frameVariables.PauseState == EPauseState.PausedBySapphire),
                EPauseState.PausedByAmber
                or EPauseState.PausedBySapphire => frameVariables.PauseState != EPauseState.UnpausingCountdown,
                EPauseState.UnpausingCountdown => frameVariables.PauseState != EPauseState.Unpaused,
                _ => true,
              }
            ) throw new Exception(nameof(frameVariables.PauseState));

            uint gameTick = frameGameTick;
            float? newMatchClock = null;
            if (
              (
                frameVariables.PauseState != previousVariables.variables.PauseState
                && (frameVariables.PauseState == EPauseState.Unpaused || previousVariables.variables.PauseState == EPauseState.Unpaused)
              )
              || (
                frameVariables.GameState != previousVariables.variables.GameState
                && (frameVariables.GameState == ERulesPermittedGameState.PreGameWait || frameVariables.GameState == ERulesPermittedGameState.PostGame)
              )
            )
            {
              if (View.Rules.MatchClockUpdateTick < 0) throw new Exception(nameof(View.Rules.MatchClockUpdateTick));
              gameTick = (uint)View.Rules.MatchClockUpdateTick;
              if (!(
                gameTick > lastClockUpdate.gameTick
                || gameTick > previousFrame.GameTick
                || gameTick <= frameGameTick
              )) throw new Exception(nameof(gameTick));

              newMatchClock = View.Rules.MatchClockAtLastUpdate;
              if (frameVariables.GameState != previousVariables.variables.GameState && frameVariables.GameState == ERulesPermittedGameState.PreGameWait)
              {
                if (newMatchClock != 0) throw new Exception(nameof(newMatchClock));
              }
              else if (frameVariables.PauseState != previousVariables.variables.PauseState && frameVariables.PauseState == EPauseState.Unpaused)
              {
                // a little arbitrary...
                // this is from witnessing:
                //  - game ran for ~45s
                //  - pause for ~30s
                //  - unpause for 0.1s
                //  - pause for ~30s
                //  - unpause for 0.1s (clock set back 0.000007s at start of unpause)
                //  - pause for ~30s
                //  - unpause forever (clock set back 0.000006s at start of unpause)
                // which seems to imply small corrections can happen when the previous unpause was super short
                if (lastClockUpdate.newMatchClock - newMatchClock > 0.0001) throw new Exception(nameof(newMatchClock));
              }
              else if (newMatchClock <= lastClockUpdate.newMatchClock) throw new Exception(nameof(newMatchClock));

              if (frameVariables.PauseState != previousVariables.variables.PauseState)
              {
                if ((frameVariables.PauseState == EPauseState.Unpaused) != (View.Rules.PauseStartTick == 0)) throw new Exception(nameof(View.Rules.PauseStartTick));

                var previouslyPausedTicks =
                  0.Through(VariableHistory.Count - 3)
                  .Sum(i =>
                    (
                      (VariableHistory[i].variables.PauseState == EPauseState.PausedByAmber || VariableHistory[i].variables.PauseState == EPauseState.PausedBySapphire)
                      && VariableHistory[i + 2].variables.PauseState == EPauseState.Unpaused
                    )
                    ? VariableHistory[i + 2].gameTick - VariableHistory[i].gameTick
                    : 0
                  );
                var newCountedPausedTicks = View.Rules.TotalPausedTicks - previouslyPausedTicks;

                if (View.Rules.PauseStartTick != 0)
                {
                  if (View.Rules.PauseStartTick != gameTick) throw new Exception(nameof(gameTick));
                  if (newCountedPausedTicks != 0) throw new Exception(nameof(newCountedPausedTicks));
                }
                else
                {
                  var startOfPauseVariables = VariableHistory.Last(v => v.variables.PauseState == EPauseState.PausedByAmber || v.variables.PauseState == EPauseState.PausedBySapphire);
                  if (newCountedPausedTicks != gameTick - startOfPauseVariables.gameTick) throw new Exception(nameof(newCountedPausedTicks));
                }
              }

              if (frameVariables.GameState != previousVariables.variables.GameState)
              {
                if (frameVariables.PauseState != EPauseState.Unpaused
                  && previousVariables.variables.PauseState != EPauseState.Unpaused) throw new Exception($"{frameGameTick}: unexpected game state change during pause in rules");
              }
            }

            VariableHistory.Add((gameTick, newMatchClock, frameVariables));
          }
        }
      }
    }

    public record Frame
    {
      public required uint iFrame { get; init; }
      public required uint iCmd { get; init; }

      /*
       * DISCLAIMER:
       * 
       * Basically all of this stuff is speculation. 
       * Valve's timing design is a mystery; this attempts to solve it.
       */

      /// <summary>
      /// The less-useful, "file" time counter. The haste-inspector tool indexes by this tick.
      /// </summary>
      /// 
      /// Speculation:
      /// * Starts -1 in a short "PreRecord" section of metadata demo commands/events,
      ///   then counts up from 0 (3, sometimes 4, for each packet).
      public required int DemoTick { get; init; }

      /// <summary>
      /// The more-useful, match time counter. 
      /// Other game variables reference this tick, like Rules.MatchClockUpdateTick.
      /// Starts typically higher than 0, usually something around 125.
      /// </summary>
      /// 
      /// Speculation:
      /// * Stays constant in the DemoTick's "PreRecord" section, then
      ///   starts counting up (3, sometimes 4, for each packet).
      /// * Clearly follows the tick rate, which is usually 60 ticks per second.
      /// * The Replay Viewer's clock zeroes at the higher-than-0 starting value. 
      ///   In other words, when the viewer's clock reads 0, this tick is at the starting
      ///   tick, not at tick 0.
      ///    * Whereas, variables like the PreGame Rules constant, "GameStartTime", count from tick 0.
      ///    * This has the annoying result that events (like GameStartTime, or pauses) tend to show
      ///      up on the Replay Viewer's clock ~2.1s earlier than they actually happen in the viewing window.
      ///      (2.1 = 125 / 60)
      public required uint GameTick { get; init; }

      /// <summary>
      /// A "real time clock" since the match server activated (a non-stopping clock from GameTick 0). 
      /// This clock seems to be the foundational reference for what other game variables go by, like Rules.GameStartTime.
      /// </summary>
      public float GameTickTime => GameTick / 60.0f;
      
      /// <summary>
      /// The time shown on the top of the screen when the game was actually being played.
      /// </summary>
      /// 
      /// The following rules govern the clock (that this variable is tracking):
      /// - At the start of the lobby, this clock is frozen at 00:00.000
      /// - When the PreGameWait "Game Starting..." countdown starts, this clock starts.
      /// - This clock pauses and resumes whenever players pause or resume the game.
      /// 
      /// That's pretty much it! As you can see it's a pretty useful clock.
      /// 
      /// Minor caveat: the game sometimes makes very small adjustments to the clock 
      ///   when several pauses happen in a row.
      public required float GameClockTime { get; init; }

      /// <summary>
      /// How many times the GameClockTime clock has been frozen or resumed.
      /// </summary>
      public required uint GameClockSection { get; init; }
      /// <summary>
      /// Whether or not the clock is stopped in this section (including the very first section before PreGame).
      /// </summary>
      public bool GameClockPaused => GameClockSection % 2 == 0;

      /// <summary>
      /// The clock shown when the game is viewed in the Replay Viewer.
      /// This clock almost NEVER matches the clock that was shown when the game was actually being played.
      /// 
      /// This variable, ReplayClockTime, is the clock shown next to the timeline slider at the bottom of the Replay Viewer.
      /// See also ReplayClockTopOfScreenTime, which is the clock shown at the top of the screen in the Replay Viewer.
      /// ReplayClockTopOfScreenTime *almost* always matches this variable, ReplayClockTime.
      /// </summary>
      /// 
      /// The Replay Viewer's clock is a weird amalgamation of
      ///  - GameTickTime,
      ///  - GameClockTime,
      ///  - and a couple other totally baffling timing adjustments.
      /// 
      /// - At the start of Replay Viewer playback, this clock is 00:00 and is running.
      ///   (unlike GameClockTime which is frozen)
      ///    - the clock zeroes out on the first frame's game tick (usually around 125),
      ///      unlike PreGameWait's GameStartTime which zeroes at game tick 0
      /// - While players are connecting (before the PreGameWait "Game Starting..." countdown), 
      ///   this clock may just randomly decide to reset back to 00:00 (via a "Source1 Legacy MatchClock" event).
      /// - After all these resets, the clock sort of follows the pattern that GameClockTime would follow (pauses and unpauses),
      ///   but of course now it's been zeroed out at the wrong time / started this pattern at the wrong time
      ///    - if the clock didn't decide to randomly reset itself before PreGameWait, it might actually
      ///      reach the first pause earlier than expected (e.g., jump from 5:28 to the first pause at 5:40),
      ///      as if it was aligned to a zero *earlier* than game tick 0
      /// 
      /// Beware that, though the clock by the timeline is right, the timeline slider itself is unrelatedly, 
      /// annoyingly offset by a few pixels each time you click/drag it to seek (as seen by the resulting seeked time being different).
      public required float ReplayClockTime { get; init; }

      /// <summary>
      /// Whether players are connecting, and thus causing the top screen clock in the Replay Viewer to show 00:00.
      /// 
      /// The clock shown at the top of the screen when the game is viewed in the Replay Viewer
      /// is *almost* always the same as the clock shown by the timeline slider at the bottom (see ReplayClockTime).
      /// The exception is that this clock is frozen at 00:00 when players are connecting 
      /// (before the PreGameWait "Game Starting..." countdown).
      /// </summary>
      public required bool ReplayClockTopOfScreenIsZero { get; init; }

      /// <summary>
      /// How many times the GameClockTime clock has been frozen, resumed, or manually reset to 00:00.
      /// </summary>
      public required uint ReplayClockSection { get; init; }
      /// <summary>
      /// Whether or not the clock is stopped in this section (this clock only pauses due to player pauses).
      /// </summary>
      public required bool ReplayClockPaused { get; init; }

      public override string ToString() => $"Frame {iFrame} ({DemoTick} / {GameTick} / {GameClockSection}:{GameClockTime} / {ReplayClockSection}:{ReplayClockTime})";
    }

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
      public required DeadlockDemo.CCitadelPlayerPawn HeroPawn { get; init; }
      public required DeadlockDemo.TeamNumber Team { get; init; }
      public required string Name { get; init; }
      public required ulong SteamId { get; init; }
      public required byte LobbyPlayerSlot { get; init; }
      public required EHero HeroId { get; init; }

      public static PlayerConstants FromView(PlayerView view) => new PlayerConstants
      {
        HeroPawn = view.HeroPawn,
        Team = view.Team,
        Name = view.Name,
        SteamId = view.SteamId,
        LobbyPlayerSlot = view.LobbyPlayerSlot,
        HeroId = view.HeroId,
      };
    }

    public interface IPlayerVariables
    {
      public PlayerConnectedStateMasks Connected { get; }
      public float BodyX { get; }
      public float BodyY { get; }
      public float BodyZ { get; }
      public float CamYaw { get; }
      public float CamPitch { get; }
      public byte Level { get; }
      public int Health { get; }
      public int? DifferingHeroPawnHealth { get; }
      public int MaxHealth { get; }
      public int? DifferingHeroPawnMaxHealth { get; }
      public bool IsAlive { get; }
      public bool MatchingHeroPawnIsAlive { get; }
    }

    public record PlayerVariables : IPlayerVariables
    {
      public required PlayerConnectedStateMasks Connected { get; init; }
      public required float BodyX { get; init; }
      public required float BodyY { get; init; }
      public required float BodyZ { get; init; }
      public required float CamYaw { get; init; }
      public required float CamPitch { get; init; }
      public required byte Level { get; init; }
      public required int Health { get; init; }
      public required int? DifferingHeroPawnHealth { get; init; }
      public required int MaxHealth { get; init; }
      public required int? DifferingHeroPawnMaxHealth { get; init; }
      public required bool IsAlive { get; init; }
      public required bool MatchingHeroPawnIsAlive { get; init; }

      public static PlayerVariables FromView(PlayerView view) => new PlayerVariables
      {
        Connected = view.Connected,
        BodyX = view.BodyX,
        BodyY = view.BodyY,
        BodyZ = view.BodyZ,
        CamYaw = view.CamYaw,
        CamPitch = view.CamPitch,
        Level = view.Level,
        Health = view.Health,
        DifferingHeroPawnHealth = view.DifferingHeroPawnHealth,
        MaxHealth = view.MaxHealth,
        DifferingHeroPawnMaxHealth = view.DifferingHeroPawnMaxHealth,
        IsAlive = view.IsAlive,
        MatchingHeroPawnIsAlive = view.MatchingHeroPawnIsAlive,
      };
    }

    public class PlayerView : IPlayerConstants, IPlayerVariables
    {
      public required DeadlockDemo.CCitadelPlayerController Controller { get; init; }

      public DeadlockDemo.CCitadelPlayerPawn HeroPawn => Controller.HeroPawn ?? throw new NullReferenceException(nameof(HeroPawn));
      private bool heroPawnValid() => Controller.HeroPawn != null;
      public DeadlockDemo.TeamNumber Team => Controller.CitadelTeamNum;
      private bool teamValid() => Enum.IsDefined(Controller.CitadelTeamNum) && Controller.CitadelTeamNum != DeadlockDemo.TeamNumber.Unassigned;
      public bool IsOnPlayingTeam => Team == DeadlockDemo.TeamNumber.Amber || Team == DeadlockDemo.TeamNumber.Sapphire;
      public string Name => Controller.PlayerName;
      private bool nameValid() => !string.IsNullOrEmpty(Controller.PlayerName);
      public ulong SteamId => Controller.SteamID;
      public byte LobbyPlayerSlot => (byte)Controller.LobbyPlayerSlot.Value;
      private bool lobbyPlayerSlotValid() => Controller.LobbyPlayerSlot.Value >= byte.MinValue && Controller.LobbyPlayerSlot.Value <= byte.MaxValue;
      public EHero HeroId => (EHero)HeroPawn.CCitadelHeroComponent.HeroID.Value;
      private bool heroIdValid() => heroPawnValid() && HeroPawn.CCitadelHeroComponent.HeroID.Value >= byte.MinValue && HeroPawn.CCitadelHeroComponent.HeroID.Value <= byte.MaxValue;


      public PlayerConnectedStateMasks Connected => (PlayerConnectedStateMasks)Controller.Connected;
      private bool connectedValid() => (int)Controller.Connected >= (int)PlayerConnectedStateMasks.MIN && (int)Controller.Connected <= (int)PlayerConnectedStateMasks.MAX;
      public float BodyX => HeroPawn.Origin.X;
      public float BodyY => HeroPawn.Origin.Y;
      public float BodyZ => HeroPawn.Origin.Z;
      public float CamYaw => HeroPawn.ClientCamera.Yaw;
      public float CamPitch => HeroPawn.ClientCamera.Pitch;
      public byte Level => (byte)HeroPawn.Level;
      private bool levelValid() => heroPawnValid() && HeroPawn.Level >= byte.MinValue && HeroPawn.Level <= byte.MaxValue;
      public int Health => Controller.Health;
      public int? DifferingHeroPawnHealth => HeroPawn.Health == Controller.Health ? null : HeroPawn.Health;
      public int MaxHealth => Controller.MaxHealth;
      public int? DifferingHeroPawnMaxHealth => HeroPawn.MaxHealth == Controller.MaxHealth ? null : HeroPawn.MaxHealth;
      public bool IsAlive => Controller.IsAlive;
      public bool MatchingHeroPawnIsAlive => HeroPawn.IsAlive == Controller.IsAlive;


      public bool AllValid() =>
        heroPawnValid() && teamValid() && nameValid() && lobbyPlayerSlotValid() && heroIdValid()
        && connectedValid() && levelValid();
    }

    public class PlayerHistory
    {
      public PlayerHistory(PlayerView view)
      {
        View = view;
        Constants = PlayerConstants.FromView(view);
      }

      public PlayerView View { get; private init; }
      public PlayerConstants Constants { get; private init; }
      public List<(uint iFrame, PlayerVariables variables)> VariableHistory { get; } = [];

      public void AfterFrame(Frame frame)
      {
        if (!View.AllValid()) throw new Exception($"{frame}: invalid elements on player \"{Constants.Name}\"");
        if (PlayerConstants.FromView(View) != Constants) throw new Exception($"{frame}: constants changed on player \"{Constants.Name}\"");

        var frameVariables = PlayerVariables.FromView(View);
        if (VariableHistory.Count == 0 || frameVariables != VariableHistory[VariableHistory.Count - 1].variables)
        {
          VariableHistory.Add((frame.iFrame, frameVariables));
        }
      }
    }


    static async Task MainAsync(string[] args)
    {
      List<string> demoDirectories;
      if (args.Length == 0)
      {
        Console.WriteLine($"(note: you can provide directories as the command line arguments to this program to save time)");
        Console.WriteLine($"Enter the directories to search for demo files, one on each line. Enter an empty line at the end of the list:");
        Console.WriteLine($@"  (e.g. C:\path\to\Steam\steamapps\common\Deadlock\game\citadel\replays )");
        demoDirectories = [];
        while (true)
        {
          var line = Console.ReadLine();
          if (string.IsNullOrEmpty(line?.Trim()))
          {
            break;
          }
          demoDirectories.Add(line);
        }
        if (demoDirectories.Count == 0)
        {
          Console.WriteLine($"Need to enter at least one directory to search for demo files.");
          return;
        }
      } 
      else
      {
        demoDirectories = args.ToList();
      }
      Console.WriteLine($"Searching the following directories for demos:");
      foreach (var item in demoDirectories)
      {
        Console.WriteLine($" - {item}");
      }

      var maybeDemoSelection = await PromptForDemoFile(demoDirectories);
      if (maybeDemoSelection == null) return;
      var matchId = maybeDemoSelection.Value.matchId;
      Console.WriteLine($"Loading {matchId} from \"{maybeDemoSelection.Value.path}\"...");

      var readStartTime = MiscFunctions.GetUnixNowMillis();

      using var fileStream = File.OpenRead(maybeDemoSelection.Value.path);
      using var demoStream = StreamDemoData(fileStream, maybeDemoSelection.Value.type);

      var demo = new DemoFile.DeadlockDemoParser();

      var players = new List<PlayerHistory>();

      RulesHistory? rules = null;

      uint iCmd = 0;
      uint iFrame = 0;
      Frame? frame = null;
      var thisFrameNetTicks = new List<CNETMsg_Tick>();
      DeadlockDemo.Source1MatchClockEvent? thisFrameMatchClockEvent = null;
      var matchClockEvents = new List<(uint iFrame, DeadlockDemo.Source1MatchClockEvent ev)>();

      var frames = new List<Frame>();

      void AfterFrame()
      {
        var previousFrame = frame;
        if (previousFrame == null && iFrame > 0) throw new Exception(nameof(previousFrame)); // sanity check

        if (demo.CurrentDemoTick != DemoFile.DemoTick.PreRecord)
        {
          if (rules == null) rules = new RulesHistory(new RulesView { Rules = demo.GameRules });

          rules.AfterFrame(
            previousFrame,
            demo.CurrentGameTick.Value,
            matchId
          );
        }

        if (thisFrameMatchClockEvent != null)
        {
          // TODO matchClockEvents checks
          matchClockEvents.Add((iFrame, thisFrameMatchClockEvent));
        }

        var lastMatchClockVariables = rules?.VariableHistory.Cast<(uint gameTick, float? newMatchClock, RulesVariables variables)?>().LastOrDefault(v => v.Value.newMatchClock.HasValue);
        var lastMatchClockEvent = matchClockEvents.Cast<(uint iFrame, DeadlockDemo.Source1MatchClockEvent ev)?>().LastOrDefault();
        frame = new Frame
        {
          iFrame = iFrame,
          iCmd = iCmd,
          DemoTick = demo.CurrentDemoTick.Value,
          GameTick = demo.CurrentGameTick.Value,

          GameClockTime =
            lastMatchClockVariables == null
            ? 0.0f // 00:00 until PreGameWait
            : lastMatchClockVariables.Value.newMatchClock.Value + ( // HasValue checked a few lines up
              lastMatchClockVariables.Value.variables.PauseState == EPauseState.Unpaused
              ? (demo.CurrentGameTick.Value - lastMatchClockVariables.Value.gameTick) / 60.0f
              : 0.0f
            ),
          GameClockSection =
            (uint?)rules?.VariableHistory.Count(v => v.newMatchClock.HasValue) ?? 0,

          ReplayClockTime =
            lastMatchClockEvent == null
            ? (
              frames.Count == 0
              ? 0.0f
              : (demo.CurrentGameTick.Value - frames.First().GameTick) / 60.0f
            )
            : lastMatchClockEvent.Value.ev.MatchTime + (
              lastMatchClockEvent.Value.ev.Paused || lastMatchClockEvent.Value.iFrame == iFrame
              ? 0.0f
              : (demo.CurrentGameTick.Value - frames.First(f => f.iFrame == lastMatchClockEvent.Value.iFrame).GameTick) / 60.0f // could do .Single but first is faster
            ),
          ReplayClockSection = (uint)matchClockEvents.Count,
          ReplayClockPaused = matchClockEvents.Select(e => e.ev.Paused).LastOrDefault(defaultValue: false),
          ReplayClockTopOfScreenIsZero = rules?.PreGameConstants == null,
        };
        frames.Add(frame);
        //Console.WriteLine(frame);

        thisFrameNetTicks.Clear();
        thisFrameMatchClockEvent = null;

        foreach (var playerController in demo.PlayersIncludingDisconnected)
        {
          PlayerHistory player;
          if (players.TryFirst(p => p.View.Controller == playerController, out var existingPlayerHistory))
          {
            player = existingPlayerHistory;
          }
          else
          {
            if (
              playerController.HeroPawn == null
              || !Enum.IsDefined((EHero)playerController.HeroPawn.CCitadelHeroComponent.HeroID.Value)
            ) continue;
            var playerView = new PlayerView { Controller = playerController };
            if (!playerView.IsOnPlayingTeam) continue;
            player = new PlayerHistory(playerView);
            players.Add(player);
          }

          if (player.View.IsOnPlayingTeam)
          {
            player.AfterFrame(frame);
          }
        }

        iFrame++;
      }

      demo.DemoEvents.DemoPacket += p => AfterFrame();
      demo.DemoEvents.DemoFullPacket += p => AfterFrame();

      bool serverInfoReceived = false;
      demo.PacketEvents.SvcServerInfo += (CSVCMsg_ServerInfo info) =>
      {
        if (serverInfoReceived) throw new Exception(nameof(serverInfoReceived));
        if (!info.HasTickInterval) throw new Exception(nameof(info.HasTickInterval));
        const float expectedTickInterval = 1f / 60f;
        // A 4 hour game would only be off by 0.01s with this error level.
        // In tests, the actual error appears to be 1/10th of this level.
        if (MathF.Abs(info.TickInterval - expectedTickInterval) > 0.00000001f) throw new Exception($"{nameof(info.TickInterval)} {info.TickInterval}");
      };
      demo.PacketEvents.NetTick += t =>
      {
        if (thisFrameMatchClockEvent != null) throw new Exception($"NetTick between match clock event and frame");
        thisFrameNetTicks.Add(t);
      };
      demo.Source1GameEvents.MatchClock += e =>
      {
        if (thisFrameNetTicks.Count == 0) throw new Exception($"Match clock event before net tick");
        if (thisFrameMatchClockEvent != null) throw new Exception($"Two match clock events before a frame");
        thisFrameMatchClockEvent = e;
      };


      var reader = DemoFile.DemoFileReader.Create(demo, demoStream);
      await reader.StartReadingAsync(CancellationToken.None);
      while (true)
      {
        try
        {
          var isStop = !(await reader.MoveNextAsync(CancellationToken.None));
          //Console.WriteLine($"cmd {iCmd}");
          if (isStop) break;
        }
        catch
        {
          Console.WriteLine($"Exception on cmd {iCmd} ({frame})");
          throw;
        }
        iCmd++;
      }

      Console.WriteLine($"Done reading. ({MiscFunctions.GetUnixNowMillis() - readStartTime}ms)");

      if (rules == null || rules.PreGameConstants == null) throw new Exception(nameof(rules));
      if (!serverInfoReceived) Console.WriteLine($"Warning: no server info?");

      {
        if (matchClockEvents.Any(e => e.ev.GameEventName != "match_clock")) throw new Exception(nameof(matchClockEvents));

        var numZeroMatchClocks = matchClockEvents.TakeWhile(e => e.ev.MatchTime == 0).Count();
        if (matchClockEvents.Take(numZeroMatchClocks).Any(e => e.ev.Paused)) throw new Exception(nameof(numZeroMatchClocks));

        var replayZeroGameTick =
          numZeroMatchClocks == 0
          ? frames.First().GameTick
          : frames.Single(f => f.iFrame == matchClockEvents[numZeroMatchClocks - 1].iFrame).GameTick;
        var gameZeroGameTick = rules.VariableHistory.First(f => f.variables.GameState == ERulesPermittedGameState.PreGameWait).gameTick;
        var switchToInProgressGameTick = rules.VariableHistory.First(f => f.variables.GameState == ERulesPermittedGameState.GameInProgress).gameTick;

        foreach ((var matchClockEvent, var rulesVariables) in 
          matchClockEvents.Skip(numZeroMatchClocks)
          .Zip(
            rules.VariableHistory
            .SkipWhile(f => f.variables.GameState == ERulesPermittedGameState.WaitingForPlayersToJoin)
            .Skip(1)
            .Where(f =>
              f.gameTick != switchToInProgressGameTick
              && f.variables.PauseState != EPauseState.UnpausingCountdown
            )
          )
        )
        {
          if (
            (!matchClockEvent.ev.Paused) != (rulesVariables.variables.PauseState == EPauseState.Unpaused)
            || !rulesVariables.newMatchClock.HasValue
            || matchClockEvent.ev.MatchTime != rulesVariables.newMatchClock.Value
            || (
              numZeroMatchClocks > 0
              && (
                Math.Abs(
                  (int)(frames.Single(f => f.iFrame == matchClockEvent.iFrame).GameTick - replayZeroGameTick)
                  - (int)(rulesVariables.gameTick - gameZeroGameTick)
                )
                > 4
              )
            )
          ) throw new Exception(nameof(matchClockEvent));
        }
      }

      const int replayViewerTimelinePixels = 960;
      const int replayViewerTimelineSliderWidthPixels = 6;
      const int replayViewerTimelineSeekPixelOffset = -(replayViewerTimelineSliderWidthPixels / 2);
      var replayViewerTimelineFrameAtEachPixel =
        0.Through(replayViewerTimelinePixels - 2)
        .Select(iPixel =>
        {
          var gameTick = frames.First().GameTick + ((frames.Last().GameTick - frames.First().GameTick - 1) * (uint)iPixel / (replayViewerTimelinePixels - 1));
          return new { iPixel = (uint)iPixel, frame = frames.Last(f => f.GameTick <= gameTick) };
        })
        .Concat(new { iPixel = (uint)(replayViewerTimelinePixels - 1), frame = frames.Last() }.Yield())
        .ToList();
      Frame frameToReplayViewerTimelineSeekToForFrame(Frame frame)
      {
        var targetPixel = replayViewerTimelineFrameAtEachPixel.LastOrDefault(f => f.frame.iFrame <= frame.iFrame, defaultValue: replayViewerTimelineFrameAtEachPixel.First());
        return
          replayViewerTimelineFrameAtEachPixel
          [Math.Max(0, Math.Min(replayViewerTimelinePixels - 1, (int)targetPixel.iPixel - replayViewerTimelineSeekPixelOffset))]
          .frame;
      }

      Console.WriteLine();
      iFrame = 0;
      while (true)
      {
        Console.WriteLine();

        var currentFrame = frames.LastOrDefault(i => i.iFrame <= iFrame, frames.First());
        Console.WriteLine($"{currentFrame} (through {frames.FirstOrDefault(i => i.iFrame > iFrame, frames.Last())}) (seek to {frameToReplayViewerTimelineSeekToForFrame(currentFrame).ReplayClockSection}:{frameToReplayViewerTimelineSeekToForFrame(currentFrame).ReplayClockTime})");

        Console.WriteLine();
        Console.WriteLine($">>>> Enter a command: quit, meta, break, go_frame, go_demo, go_game, go_game_time(s,t), go_replay_time(s,t), player(i)");
        var command = Console.ReadLine();
        if (command == null) throw new NullReferenceException(nameof(command));
        command = command.Trim();

        if (command == "quit" || command == "exit" || command == "stop") break;

        if (command == "meta")
        {
          Console.WriteLine();
          Console.WriteLine($"MatchID:                     {matchId}");
          Console.WriteLine($"Match Mode:                  {rules.Constants.MatchMode}");
          Console.WriteLine($"Experimental gameplay state: {rules.Constants.ExperimentalGameplayState}");
          Console.WriteLine($"Pre-Game \"start time\":     {rules.PreGameConstants.GameStartTime}");
          Console.WriteLine($"State changes:");
          Console.WriteLine(
            string.Join(
              '\n',
              rules.VariableHistory
              .SelectMany(v => new[]
                {
                  $" - {v.variables.GameState}{(v.variables.PauseState == EPauseState.Unpaused ? "" : $" ({v.variables.PauseState})")}",
                  $"   - GameTick {v.gameTick}",
                  $"   - {frames.FirstOrDefault(f => f.GameTick >= v.gameTick, defaultValue: frames.Last())}",
                }
                .Concat(v.newMatchClock == null ? Enumerable.Empty<string>() : $"   - New Clock: {v.newMatchClock}".Yield())
              )
            )
          );
          Console.WriteLine($"Last frame: {frames.Last()}");
          Console.WriteLine($"Match Clocks:");
          Console.WriteLine(
            string.Join(
              '\n',
              matchClockEvents
              .SelectMany(e => new[]
              {
                $" - {e.ev.MatchTime} {(e.ev.Paused ? "Paused" : "Unpaused")}",
                $"   - {frames.Single(f => f.iFrame == e.iFrame)}",
              })
            )
          );
          Console.WriteLine();
          /*
          Console.WriteLine($"Game time sections:");
          foreach (var section in frames.GroupBy(f => f.GameTimeSection))
          {
            Console.WriteLine($"{section.Key}: {section.First()} to {section.Last()}");
          }
          */

          Console.WriteLine();
          Console.WriteLine($"Players:");
          foreach (var player in players.Indexed())
          {
            Console.WriteLine($"{player.index}: {player.value.Constants.Name}");
          }
          continue;
        }

        if (command == "break")
        {
          Console.WriteLine($"Set a breakpoint on this Console.WriteLine line.");
          continue;
        }

        var goes = new[] { "go_frame", "go_demo", "go_game", "go_game_time", "go_replay_time" };
        if (goes.Any(c => command.StartsWith(c)))
        {
          var commandText = goes.OrderByDescending(c => c.Length).First(c => command.StartsWith(c));
          var commandArgs = command.Substring(commandText.Length).Replace(",", " ").Replace("(", " ").Replace(")", " ").Split(" ").Where(part => part != "").ToList();
          if (
            commandArgs.Count != (commandText == "go_game_time" || commandText == "go_replay_time" ? 2 : 1)
            || !uint.TryParse(commandArgs[0], out var arg1)
          )
          {
            Console.WriteLine($"Bad usage");
            continue;
          }

          if (commandText == "go_game_time" || commandText == "go_replay_time")
          {
            var go_game_time = commandText == "go_game_time";
            if (arg1 > (go_game_time ? frames.Last().GameClockSection : frames.Last().ReplayClockSection) || !float.TryParse(commandArgs[1], out var arg2))
            {
              Console.WriteLine($"Bad usage");
              continue;
            }
            iFrame = Math.Max(
                1, 
                frames.FirstOrDefault(
                  f => (go_game_time ? f.GameClockSection : f.ReplayClockSection) == arg1 && (go_game_time ? f.GameClockTime : f.ReplayClockTime) > arg2, 
                  frames.Last(f => (go_game_time ? f.GameClockSection : f.ReplayClockSection) == arg1)
                ).iFrame
              ) - 1;
          }
          else if (commandText == "go_frame")
          {
            iFrame = arg1;
          }
          else
          {
            var go_demo = commandText == "go_demo";
            iFrame = frames.LastOrDefault(
              f => (go_demo ? f.DemoTick : f.GameTick) <= arg1,
              frames.First()
            ).iFrame;
          }
          continue;
        }

        if (command.StartsWith("player"))
        {
          if (
            !int.TryParse(command.Substring("player".Length).Trim(), out var arg1)
            || !players.TryGet(arg1, out var player)
          )
          {
            Console.WriteLine($"Bad usage");
            continue;
          }

          Console.WriteLine(SerializeJsonObject(player.Constants));
          var variable = player.VariableHistory.LastOrDefault(v => v.iFrame <= iFrame, player.VariableHistory.First());
          Console.WriteLine();
          Console.WriteLine($"Variables at {currentFrame}:");
          Console.WriteLine($"\ttechnically: {//
            frames.Single(f => f.iFrame == variable.iFrame)} through {frames.SingleOrDefault(f => f.iFrame == variable.iFrame + 1, frames.Last())}");
          Console.WriteLine(SerializeJsonObject(variable.variables));
          Console.WriteLine();

          continue;
        }

        Console.WriteLine($"Bad usage");
      }
    }

    private static async Task<(string path, DemoFileType type, int matchId)?> PromptForDemoFile(List<string> demoDirectories)
    {
      var demoPaths = new Dictionary<int, (string path, DemoFileType type)>();
      foreach (var demoPath in demoDirectories.SelectMany(Directory.GetFiles))
      {
        var filename = Path.GetFileName(demoPath);
        string noExtensionFilename;
        DemoFileType type;
        if (filename.EndsWith(".dem.bz2"))
        {
          noExtensionFilename = filename.Substring(0, filename.Length - ".dem.bz2".Length);
          type = DemoFileType.DemBz2;
        }
        else if (filename.EndsWith(".dem"))
        {
          noExtensionFilename = filename.Substring(0, filename.Length - ".dem".Length);
          type = DemoFileType.Dem;
        }
        else continue;

        if (noExtensionFilename.Contains(".dem") || noExtensionFilename.Contains(".bz2")) continue;

        if (!int.TryParse(noExtensionFilename, out var matchId)) continue;
        if (noExtensionFilename != matchId.ToString()) continue;
        if (matchId < 1_000_000 || matchId > 50_000_000) continue;

        if (demoPaths.ContainsKey(matchId)) continue;
        demoPaths.Add(matchId, (path: demoPath, type));
      }

      const string recentDemoFilename = "recent_demo.txt";
      int? recentDemoMatchId = null;
      if (File.Exists(recentDemoFilename))
      {
        var recentDemoText = await File.ReadAllTextAsync(recentDemoFilename);
        recentDemoText = recentDemoText.Trim();
        if (
          !int.TryParse(recentDemoText, out var recentDemoMatchIdFromFile)
          || recentDemoText != recentDemoMatchIdFromFile.ToString()
        ) throw new Exception(nameof(recentDemoText));
        recentDemoMatchId = recentDemoMatchIdFromFile;
      }

      if (recentDemoMatchId != null && !demoPaths.ContainsKey(recentDemoMatchId.Value)) recentDemoMatchId = null;

      Console.WriteLine($"Available demos:");
      foreach (var matchId in demoPaths.Keys.OrderBy(i => i))
      {
        Console.WriteLine($" - {matchId}{(recentDemoMatchId == matchId ? " (recently opened)" : "")}{(demoPaths[matchId].type == DemoFileType.DemBz2 ? " (bz2)" : "")}");
      }
      Console.WriteLine();
      Console.Write($"Enter match id:{(recentDemoMatchId == null ? "" : $" [{recentDemoMatchId.Value}]")}    ");

      var enteredMatchIdText = Console.ReadLine();
      if (enteredMatchIdText == null) throw new NullReferenceException(nameof(enteredMatchIdText));

      int? maybeEnteredMatchId = null;
      if (enteredMatchIdText.Trim() == "")
      {
        maybeEnteredMatchId = recentDemoMatchId;
      }
      else if (
        int.TryParse(enteredMatchIdText.Trim(), out var enteredMatchId)
        && enteredMatchIdText.Trim() == enteredMatchId.ToString()
      )
      {
        maybeEnteredMatchId = enteredMatchId;
      }

      if (
        maybeEnteredMatchId == null
        || !demoPaths.TryGetValue(maybeEnteredMatchId.Value, out var selectedPath)
      )
      {
        Console.WriteLine("Invalid input.");
        return null;
      }

      await File.WriteAllTextAsync(recentDemoFilename, maybeEnteredMatchId.Value.ToString());

      return (path: selectedPath.path, type: selectedPath.type, matchId: maybeEnteredMatchId.Value);
    }

    private static Stream StreamDemoData(FileStream fileStream, DemoFileType type)
    {
      switch (type)
      {
        case DemoFileType.DemBz2:
          {
            // for some reason, I can't just return the BZip2Stream; EOF gets reached too soon
            var loadedStream = new MemoryStream();
            Console.WriteLine($"\t(loading full bz2 contents into memory, decompressed)");
            using (var decompressionStream = new SharpCompress.Compressors.BZip2.BZip2Stream(
              fileStream,
              SharpCompress.Compressors.CompressionMode.Decompress,
              decompressConcatenated: true
            ))
            {
              decompressionStream.CopyTo(loadedStream);
            }
            loadedStream.Seek(0, SeekOrigin.Begin);
            Console.WriteLine($"\t(loaded into memory)");
            return loadedStream;
          }

        case DemoFileType.Dem:
          return fileStream;

        default: throw new ArgumentException(nameof(type));
      }
    }

    public enum ERulesPermittedGameState
    {
      WaitingForPlayersToJoin = DeadlockDemo.EGameState.EGameState_WaitingForPlayersToJoin,
      PreGameWait = DeadlockDemo.EGameState.EGameState_PreGameWait,
      GameInProgress = DeadlockDemo.EGameState.EGameState_GameInProgress,
      PostGame = DeadlockDemo.EGameState.EGameState_PostGame,
    }

    public enum EPlayingTeam
    {
      Amber = DeadlockDemo.TeamNumber.Amber,
      Sapphire = DeadlockDemo.TeamNumber.Sapphire,
    }

    public enum EPauseState
    {
      Unpaused = 1,
      PausedByAmber = EPlayingTeam.Amber,
      PausedBySapphire = EPlayingTeam.Sapphire,
      UnpausingCountdown = 4,
    }

    public enum PlayerConnectedStateMasks
    {
      NeverConnected = 0,
      Connected = 1 << DeadlockDemo.PlayerConnectedState.PlayerConnected,
      Connecting = 1 << DeadlockDemo.PlayerConnectedState.PlayerConnecting,
      Reconnecting = 1 << DeadlockDemo.PlayerConnectedState.PlayerReconnecting,
      Disconnecting = 1 << DeadlockDemo.PlayerConnectedState.PlayerDisconnecting,
      Disconnected = 1 << DeadlockDemo.PlayerConnectedState.PlayerDisconnected,
      Reserved = 1 << DeadlockDemo.PlayerConnectedState.PlayerReserved,

      MIN = NeverConnected,
      MAX = Connected | Connecting | Reconnecting | Disconnecting | Disconnected | Reserved,
    }
  }
}
