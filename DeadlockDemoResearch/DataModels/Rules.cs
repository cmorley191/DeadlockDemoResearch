using DeadlockDemo = DemoFile.Game.Deadlock;

namespace DeadlockDemoResearch.DataModels
{
  public interface IRulesConstants
  {
    public string GameplayExperiment { get; }
    public DeadlockDemo.ECitadelMatchMode MatchMode { get; }
  }
  public record RulesConstants : IRulesConstants
  {
    public required string GameplayExperiment { get; init; }
    public required DeadlockDemo.ECitadelMatchMode MatchMode { get; init; }

    public static RulesConstants CopyFrom(IRulesConstants other) => new()
    {
      GameplayExperiment = other.GameplayExperiment,
      MatchMode = other.MatchMode,
    };
  }

  public interface IRulesPreGameConstants
  {
    public float GameStartTime { get; }
  }
  public record RulesPreGameConstants : IRulesPreGameConstants
  {
    public required float GameStartTime { get; init; }

    public static RulesPreGameConstants CopyFrom(IRulesPreGameConstants other) => new()
    {
      GameStartTime = other.GameStartTime,
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

    public static RulesVariables CopyFrom(IRulesVariables other) => new RulesVariables
    {
      GameState = other.GameState,
      PauseState = other.PauseState,
    };
  }

  public class RulesView : IRulesConstants, IRulesPreGameConstants, IRulesVariables
  {
    public required DeadlockDemo.CCitadelGameRules Rules { get; init; }

    public string GameplayExperiment => Rules.GameplayExperiment.Value;
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
      Constants = RulesConstants.CopyFrom(View);
    }

    public RulesView View { get; private init; }
    public RulesConstants Constants { get; private init; }
    public RulesPreGameConstants? PreGameConstants { get; private set; }
    private static readonly RulesPreGameConstants prePreGame_PreGameConstants = new RulesPreGameConstants { GameStartTime = 0 };
    public List<(uint gameTick, float? newMatchClock, RulesVariables variables)> VariableHistory { get; } = [];

    public void AfterFrame(Frame? previousFrame, uint frameGameTick, int matchId)
    {
      if (!View.AllValid()) throw new Exception($"{frameGameTick}: invalid elements on rules");
      if (RulesConstants.CopyFrom(View) != Constants) throw new Exception($"{frameGameTick}: constants changed on rules");

      if (View.GameState == ERulesPermittedGameState.WaitingForPlayersToJoin)
      {
        if (RulesPreGameConstants.CopyFrom(View) != prePreGame_PreGameConstants) throw new Exception($"{frameGameTick}: invalid pre-pre-game constants on rules");
      }
      else
      {
        var thisFrameConstants = RulesPreGameConstants.CopyFrom(View);
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

      var frameVariables = RulesVariables.CopyFrom(View);
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
}

