using DeadlockDemoResearch.DataModels;
using GraphAlgorithms;
using System.Linq.Expressions;
using System.Numerics;
using System.Text.Json;
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

      RulesHistory? rules = null;

      uint iCmd = 0;
      uint iFrame = 0;
      Frame? frame = null;
      var thisFrameNetTicks = new List<CNETMsg_Tick>();
      DeadlockDemo.Source1MatchClockEvent? thisFrameMatchClockEvent = null;
      var matchClockEvents = new List<(uint iFrame, DeadlockDemo.Source1MatchClockEvent ev)>();

      var frames = new List<Frame>();

      var players = new List<PlayerHistory>();

      var owningTroopers = new Dictionary<DeadlockDemo.CNPC_Trooper, TrooperHistory>();
      var allTroopers = new List<TrooperHistory>();
      var allUnifiedTroopers = new List<UnifiedTrooperHistory>();

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

        {
          var seenPlayers = new HashSet<PlayerHistory>();
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
            if (seenPlayers.Contains(player)) throw new Exception(nameof(seenPlayers));
            seenPlayers.Add(player);
            // players should never leave the pvs
            if (!playerController.IsActive) throw new Exception(nameof(playerController));

            if (player.View.IsOnPlayingTeam)
            {
              player.AfterFrame(frame);
            }
          }
          // players should never be deleted
          if (players.Any(p => !seenPlayers.Contains(p))) throw new Exception(nameof(seenPlayers));
        }

        {
          var seenTroopers = new HashSet<TrooperHistory>();
          var thisFrameDiedZiplineTroopers = new List<TrooperHistory>();
          var thisFrameBornActiveTroopers = new List<TrooperHistory>();
          foreach (var trooperEntity in
            demo.Entities.OfType<DeadlockDemo.CNPC_Trooper>() // no, compiler, this won't result in an empty sequence you dummy
            .Where(t => t is not DeadlockDemo.CNPC_TrooperBoss)
          )
          {
            // DemoFile.Sdk.CEntityInstance<DemoFile.DeadlockDemoParser> e = trooper; // seeing as we can do that!

            TrooperHistory trooper;
            TrooperHistory? trooperLosingOwnership;
            bool newTrooper;
            if (owningTroopers.TryGetValue(trooperEntity, out var existingHistory))
            {
              if (
                existingHistory.VariableHistory.Last().variables.Health == 0
                && trooperEntity.Health > 0
              )
              {
                trooperLosingOwnership = existingHistory;
                trooper = new TrooperHistory(existingHistory.View, iEntityOwnership: existingHistory.IEntityOwnership + 1);
                newTrooper = true;
                owningTroopers[trooperEntity] = trooper;
                allTroopers.Add(trooper);
              }
              else
              {
                trooper = existingHistory;
                newTrooper = false;
                trooperLosingOwnership = null;
              }
            }
            else
            {
              var view = new TrooperView { Entity = trooperEntity };
              trooper = new TrooperHistory(view, iEntityOwnership: 0);
              newTrooper = true;
              trooperLosingOwnership = null;
              owningTroopers.Add(trooperEntity, trooper);
              allTroopers.Add(trooper);
            }

            var pvsState = trooperEntity.IsActive ? EEntityPvsState.Active : EEntityPvsState.InactiveButPresent;

            if (trooperLosingOwnership != null)
            {
              if (seenTroopers.Contains(trooperLosingOwnership)) throw new Exception(nameof(seenTroopers));
              seenTroopers.Add(trooperLosingOwnership);
              trooperLosingOwnership.AfterFrame(frame, pvsState, ownsEntity: false);
            }

            if (seenTroopers.Contains(trooper)) throw new Exception(nameof(seenTroopers));
            seenTroopers.Add(trooper);
            trooper.AfterFrame(frame, pvsState, ownsEntity: true);

            if (trooper.Constants.Subclass == ETrooperSubclassId.ZiplinePackage)
            {
              if (newTrooper)
              {
                allUnifiedTroopers.Add(new UnifiedTrooperHistory(trooper));
              }
              else if (
                trooper.VariableHistory.Count > 1
                && trooper.VariableHistory[^1].iFrame == iFrame
                && trooper.VariableHistory[^1].variables.Health == 0
                && trooper.VariableHistory[^2].variables.Health > 0
              )
              {
                thisFrameDiedZiplineTroopers.Add(trooper);
              }
            }
            else if (newTrooper && trooper.VariableHistory[0].variables.Health > 0)
            {
              thisFrameBornActiveTroopers.Add(trooper);
            }
          }

          foreach (var trooper in owningTroopers)
          {
            if (!seenTroopers.Contains(trooper.Value))
            {
              trooper.Value.AfterFrame(frame, EEntityPvsState.Deleted, ownsEntity: true);
            }
          }

          if (thisFrameDiedZiplineTroopers.Count != thisFrameBornActiveTroopers.Count) throw new Exception(nameof(thisFrameDiedZiplineTroopers));
          foreach (var (diedZiplineTroopersGroup, bornActiveTroopersGroup) in 
            thisFrameDiedZiplineTroopers
            .Select(zt => new { zt, ut = allUnifiedTroopers.Single(ut => ut.ZiplineTrooper == zt) })
            .GroupBy(zt => zt.zt.Constants.Team == DeadlockDemo.TeamNumber.Amber ? (int)zt.zt.Constants.Lane : -(int)zt.zt.Constants.Lane)
            .OrderBy(g => g.Key)
            .Zip(
              thisFrameBornActiveTroopers
              .GroupBy(at => at.Constants.Team == DeadlockDemo.TeamNumber.Amber ? (int)at.Constants.Lane : -(int)at.Constants.Lane)
              .OrderBy(g => g.Key)
            )
          )
          {
            if (diedZiplineTroopersGroup.Key != bornActiveTroopersGroup.Key) throw new Exception(nameof(diedZiplineTroopersGroup));

            var diedZiplineTroopers = diedZiplineTroopersGroup.ToList();
            var bornActiveTroopers = bornActiveTroopersGroup.ToList();

            if (diedZiplineTroopers.Count != bornActiveTroopers.Count) throw new Exception(nameof(diedZiplineTroopers));

            if (diedZiplineTroopers.Count == 1)
            {
              diedZiplineTroopers[0].ut.SetActiveTrooper(bornActiveTroopers[0]);
              continue;
            }

            // minimize distances from activeTrooper spawn point to the ziplineTrooper (lastLifeFrame->firstDeathFrame) line segment
            float[,] distanceMatrix = new float[diedZiplineTroopers.Count, bornActiveTroopers.Count];
            foreach (var zt in diedZiplineTroopers.Indexed())
            {
              var ztLastAlivePosition = zt.value.zt.VariableHistory[^2].variables.Position;
              var ztFirstDeadPosition = zt.value.zt.VariableHistory[^1].variables.Position;
              var ztMotionVector = ztFirstDeadPosition - ztLastAlivePosition;
              var ztMotionSquared = ztMotionVector.LengthSquared();
              if (ztMotionSquared == 0)
              {
                foreach (var at in bornActiveTroopers.Indexed())
                {
                  distanceMatrix[zt.index, at.index] = Vector3.DistanceSquared(at.value.VariableHistory[0].variables.Position, ztLastAlivePosition);
                }
              }
              else
              {
                foreach (var at in bornActiveTroopers.Indexed())
                {
                  var atPosition = at.value.VariableHistory[0].variables.Position;
                  var nearestParametricAlongZTMotion = MathF.Max(0, MathF.Min(1, Vector3.Dot(atPosition - ztLastAlivePosition, ztMotionVector) / ztMotionSquared));
                  distanceMatrix[zt.index, at.index] = Vector3.DistanceSquared(atPosition, ztLastAlivePosition + (nearestParametricAlongZTMotion * ztMotionVector));
                }
              }
            }

            var matchings = new HungarianAlgorithm(distanceMatrix).Run();
            if (matchings == null) throw new NullReferenceException(nameof(matchings));

            var matchedActiveTrooper = new bool[bornActiveTroopers.Count];
            foreach (var (zt, iAT) in diedZiplineTroopers.Zip(matchings))
            {
              if (!bornActiveTroopers.TryGet(iAT, out var at)) throw new Exception(nameof(iAT));
              if (matchedActiveTrooper[iAT]) throw new Exception(nameof(matchedActiveTrooper));
              if (zt.ut.ActiveTrooper != null) throw new Exception(nameof(zt.ut.ActiveTrooper));
              zt.ut.SetActiveTrooper(at);
              matchedActiveTrooper[iAT] = true;
            }
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

      {
        Console.WriteLine($"Note: {allUnifiedTroopers.Count(t => t.ZiplineConstants.IsNonStarter)} non-starter troopers ({allUnifiedTroopers.Count} total)");
        var unifiedSubtroopers = new HashSet<TrooperHistory>();
        foreach (var ut in allUnifiedTroopers)
        {
          if (!unifiedSubtroopers.Add(ut.ZiplineTrooper)) throw new Exception(nameof(ut.ZiplineTrooper));
          if (ut.ActiveTrooper != null && !unifiedSubtroopers.Add(ut.ActiveTrooper.Value.trooper)) throw new Exception(nameof(ut.ActiveTrooper));
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
        Console.WriteLine($">>>> Enter a command: quit, meta, break, go_frame, go_demo, go_game, go_game_time(s,t), go_replay_time(s,t), player(i), dump_troopers");
        // not shown: dump_partial_troopers

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

        if (command == "dump_troopers")
        {
          var filename = $"{matchId}_troopers.json";
          await File.WriteAllTextAsync(
            filename,
            JsonSerializer.Serialize(
              allUnifiedTroopers
              .Select(t => new
              {
                ZiplineConstants = t.ZiplineConstants,
                ActiveConstants = t.ActiveTrooper?.constants,
                Variables = t.VariableHistory.Select(v => new { v.iFrame, v.pvsState, v.state, v.variables }),
              })
              .ToList()
            )
          );
          Console.WriteLine($"Wrote to {filename}");
          continue;
        }
        if (command == "dump_partial_troopers")
        {
          var filename = $"{matchId}_partial_troopers.json";
          await File.WriteAllTextAsync(
            filename,
            JsonSerializer.Serialize(
              allTroopers
              .Select(t => new
              {
                Constants = t.Constants,
                Variables = t.VariableHistory.Select(v => new { v.iFrame, v.pvsState, v.variables }),
              })
              .ToList()
            )
          );
          Console.WriteLine($"Wrote to {filename}");
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
  }
}
