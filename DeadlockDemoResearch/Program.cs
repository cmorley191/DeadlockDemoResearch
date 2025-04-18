using DeadlockDemoResearch.DataModels;
using GraphAlgorithms;
using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime;
using System.Text.Json;
using static CMsgServerNetworkStats.Types;
using DeadlockDemo = DemoFile.Game.Deadlock;

namespace DeadlockDemoResearch
{
  public class Program
  {
    static void Main(string[] args)
    {
      MainAsync(args).GetAwaiter().GetResult();
    }

    private static readonly JsonSerializerOptions serializerOptions = new()
    {
      WriteIndented = true,
      Converters = { new CustomJsonSerializerVector3Converter() },
    };
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
        Console.WriteLine($"Enter the directories to search for demo files, one directory on each line. Enter an empty line at the end of the list:");
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

      var towers = new Dictionary<DeadlockDemo.CNPC_TrooperBoss, TowerHistory>();
      var walkers = new Dictionary<DeadlockDemo.CNPC_Boss_Tier2, WalkerHistory>();

      var urnPickups = new Dictionary<DeadlockDemo.CCitadelItemPickupIdol, UrnHistory>();
      var urnDropoffSpots = new Dictionary<DeadlockDemo.CCitadelIdolReturnTrigger, UrnDropoffSpotHistory>();
      var inactiveUrns = new List<UnifiedUrnHistory>();
      UnifiedUrnHistory? activeUrn = null;

      var soulOrbs = new Dictionary<DeadlockDemo.CItemXP, SoulOrbHistory>();

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

          if (previousFrame != null)
          {
            var gameTickDiff = demo.CurrentGameTick.Value - previousFrame.GameTick;
            if (demo.CurrentDemoTick.Value - previousFrame.DemoTick != gameTickDiff) throw new Exception();
            if (gameTickDiff == 0) Console.WriteLine($"Note: duplicate game tick at frame {iFrame}");
            else if (gameTickDiff < 3) Console.WriteLine($"Warning: short duration of frame {iFrame}");
            else if (gameTickDiff > 4) Console.WriteLine($"Warning: long duration of frame {iFrame}");
          }


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
              ? (demo.CurrentGameTick.Value - lastMatchClockVariables.Value.gameTick) / Frame.TICKS_PER_SECOND
              : 0.0f
            ),
          GameClockSection =
            (uint?)rules?.VariableHistory.Count(v => v.newMatchClock.HasValue) ?? 0,
          PostGame = rules != null && rules.VariableHistory[^1].variables.GameState == ERulesPermittedGameState.PostGame,

          ReplayClockTime =
            lastMatchClockEvent == null
            ? (
              frames.Count == 0
              ? 0.0f
              : (demo.CurrentGameTick.Value - frames.First().GameTick) / Frame.TICKS_PER_SECOND
            )
            : lastMatchClockEvent.Value.ev.MatchTime + (
              lastMatchClockEvent.Value.ev.Paused || lastMatchClockEvent.Value.iFrame == iFrame
              ? 0.0f
              : (demo.CurrentGameTick.Value - frames.First(f => f.iFrame == lastMatchClockEvent.Value.iFrame).GameTick) / Frame.TICKS_PER_SECOND // could do .Single but first is faster
            ),
          ReplayClockSection = (uint)matchClockEvents.Count,
          ReplayClockPaused = matchClockEvents.Select(e => e.ev.Paused).LastOrDefault(defaultValue: false),
          ReplayClockTopOfScreenIsZero = rules?.PreGameConstants == null,
        };
        frames.Add(frame);
        //Console.WriteLine(frame);

        if (
          previousFrame != null
          && previousFrame.GameTick != frame.GameTick
          && previousFrame.GameClockPaused == frame.GameClockPaused
          && (
            previousFrame.GameClockPaused
            != (previousFrame.GameClockTime == frame.GameClockTime)
          )
        ) throw new Exception();

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
                !existingHistory.VariableHistory[^1].variables.IsAlive
                && trooperEntity.IsAlive
              )
              {
                // existingHistory is losing ownership of this trooperEntity
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
                && !trooper.VariableHistory[^1].variables.IsAlive
                && trooper.VariableHistory[^2].variables.IsAlive
              )
              {
                thisFrameDiedZiplineTroopers.Add(trooper);
              }
            }
            else if (newTrooper && trooper.VariableHistory[0].variables.IsAlive)
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

          var thisFrameDiedZiplineTroopersGrouped =
            thisFrameDiedZiplineTroopers
            .Select(zt => new { zt, ut = allUnifiedTroopers.Single(ut => ut.ZiplineTrooper == zt) })
            .GroupBy(zt => zt.zt.Constants.Team == DeadlockDemo.TeamNumber.Amber ? (int)zt.zt.Constants.Lane : -(int)zt.zt.Constants.Lane)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());
          var thisFrameBornActiveTroopersGrouped =
            thisFrameBornActiveTroopers
            .GroupBy(at => at.Constants.Team == DeadlockDemo.TeamNumber.Amber ? (int)at.Constants.Lane : -(int)at.Constants.Lane)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());
          if (thisFrameBornActiveTroopersGrouped.Keys.Any(k => !thisFrameDiedZiplineTroopersGrouped.ContainsKey(k))) throw new Exception(nameof(thisFrameBornActiveTroopersGrouped));

          foreach (var k in thisFrameDiedZiplineTroopersGrouped.Keys)
          {
            var diedZiplineTroopers = thisFrameDiedZiplineTroopersGrouped[k].ToList();
            var bornActiveTroopers = thisFrameBornActiveTroopersGrouped.GetValueOrDefault(k, defaultValue: []).ToList();

            if (!(
              diedZiplineTroopers.Count == bornActiveTroopers.Count
              || (
                diedZiplineTroopers.Count > bornActiveTroopers.Count
                && diedZiplineTroopers.Count(zt => zt.zt.IsBuggedSpawnZipline) >= (diedZiplineTroopers.Count - bornActiveTroopers.Count)
              )
            )) throw new Exception(nameof(bornActiveTroopers));

            if (diedZiplineTroopers.Count == 1)
            {
              diedZiplineTroopers[0].ut.SetActiveTrooper(bornActiveTroopers[0]);
              continue;
            }

            // Hungarian algorithm: minimize distances from activeTrooper spawn point to the ziplineTrooper death point (lastLifeFrame->firstDeathFrame) line segment.
            // Bugged spawn zipline troopers might fall out of the world, resulting in no born active trooper.
            //   (i.e., more zipline troopers died than active troopers were spawned)
            //   In this case add dummy active troopers that zipline troopers can be assigned to as a last resort (high distance).
            float[,] distanceMatrix = new float[diedZiplineTroopers.Count, diedZiplineTroopers.Count];
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
              // dummy active troopers
              foreach (var i in bornActiveTroopers.Count.Through(diedZiplineTroopers.Count - 1))
              {
                // The map size is ~30_000 by ~30_000, so a distance of 200_000 should be higher than any distance we'll see.
                distanceMatrix[zt.index, i] = 200_000f * 200_000f; // DistanceSquared
              }
            }

            var matchings = new HungarianAlgorithm(distanceMatrix).Run();
            if (matchings == null) throw new NullReferenceException(nameof(matchings));

            var matchedActiveTrooper = new bool[diedZiplineTroopers.Count]; // includes dummies
            foreach (var (zt, iAT) in diedZiplineTroopers.Zip(matchings))
            {
              if (!(iAT >= 0 && iAT < diedZiplineTroopers.Count)) throw new Exception(nameof(iAT));
              if (matchedActiveTrooper[iAT]) throw new Exception(nameof(matchedActiveTrooper));
              matchedActiveTrooper[iAT] = true;
              if (bornActiveTroopers.TryGet(iAT, out var at))
              {
                if (zt.ut.ActiveTrooper != null) throw new Exception(nameof(zt.ut.ActiveTrooper));
                zt.ut.SetActiveTrooper(at);
              }
              else
              {
                // leave unmatched - this is probably a bugged spawn zipline that fell out of the world
                if (zt.ut.VariableHistory.Last().state != EUnifiedTrooperState.Packed_DeadBuggedFellOutOfWorld) throw new Exception(nameof(iAT));
              }
            }
          }
        }

        {
          var seenTowers = new HashSet<TowerHistory>();
          foreach (var towerEntity in demo.Entities.OfType<DeadlockDemo.CNPC_TrooperBoss>()) // no, compiler, this won't result in an empty sequence you dummy
          {
            TowerHistory tower;
            if (towers.TryGetValue(towerEntity, out var existingHistory))
            {
              tower = existingHistory;
            }
            else
            {
              var view = new TowerView { Entity = towerEntity };
              tower = new TowerHistory(view);
              if (towers.Any(t => t.Value.Constants.Position == tower.Constants.Position)) throw new Exception(nameof(tower.Constants.Position));
              towers.Add(towerEntity, tower);
            }

            if (!towerEntity.IsActive) throw new Exception(nameof(towerEntity.IsActive));

            if (seenTowers.Contains(tower)) throw new Exception(nameof(seenTowers));
            seenTowers.Add(tower);
            tower.AfterFrame(frame, deleted: false);
          }

          foreach (var tower in towers)
          {
            if (!seenTowers.Contains(tower.Value))
            {
              tower.Value.AfterFrame(frame, deleted: true);
            }
          }
        }

        {
          var seenWalkers = new HashSet<WalkerHistory>();
          foreach (var walkerEntity in demo.Entities.OfType<DeadlockDemo.CNPC_Boss_Tier2>()) // no, compiler, this won't result in an empty sequence you dummy
          {
            WalkerHistory walker;
            if (walkers.TryGetValue(walkerEntity, out var existingHistory))
            {
              walker = existingHistory;
            }
            else
            {
              var view = new WalkerView { Entity = walkerEntity };
              walker = new WalkerHistory(view);
              if (walkers.Any(t => t.Value.VariableHistory.Count > 1 || t.Value.VariableHistory[0].variables.Position == walker.View.Position)) throw new Exception(nameof(walker.View.Position));
              walkers.Add(walkerEntity, walker);
            }

            if (!walkerEntity.IsActive) throw new Exception(nameof(walkerEntity.IsActive));

            if (seenWalkers.Contains(walker)) throw new Exception(nameof(seenWalkers));
            seenWalkers.Add(walker);
            walker.AfterFrame(frame, deleted: false);
          }

          foreach (var walker in walkers)
          {
            if (!seenWalkers.Contains(walker.Value))
            {
              walker.Value.AfterFrame(frame, deleted: true);
            }
          }
        }

        {
          foreach (var shrine in demo.Entities.OfType<DeadlockDemo.CCitadel_Destroyable_Building>()) // no, compiler, this won't result in an empty sequence you dummy
          {
            // note: check cellX to determine the side?
            // 28 is near yellow, 34 near purple
          }
        }

        {
          foreach (var patron in demo.Entities.OfType<DeadlockDemo.CNPC_Boss_Tier3>()) // no, compiler, this won't result in an empty sequence you dummy
          {

          }
        }

        {
          foreach (var neutral in demo.Entities.OfType<DeadlockDemo.CNPC_TrooperNeutral>()) // no, compiler, this won't result in an empty sequence you dummy
          {

          }
        }

        {
          foreach (var midboss in demo.Entities.OfType<DeadlockDemo.CNPC_MidBoss>()) // no, compiler, this won't result in an empty sequence you dummy
          {
            // TODO: test there's only 0 or 1?
          }
        }

        {


          (UrnHistory value, bool justDeleted)? seenUrnPickup = null;
          {
            foreach (var urnPickupEntity in demo.Entities.OfType<DeadlockDemo.CCitadelItemPickupIdol>()) // no, compiler, this won't result in an empty sequence you dummy
            {
              if (seenUrnPickup != null) throw new Exception(nameof(seenUrnPickup));

              if (urnPickups.TryGetValue(urnPickupEntity, out var existingHistory))
              {
                seenUrnPickup = (existingHistory, justDeleted: false);
              }
              else
              {
                var view = new UrnView { Entity = urnPickupEntity };
                seenUrnPickup = (new UrnHistory(view), justDeleted: false);
                urnPickups.Add(urnPickupEntity, seenUrnPickup.Value.value);
              }

              if (!urnPickupEntity.IsActive) throw new Exception(nameof(urnPickupEntity.IsActive));

              seenUrnPickup.Value.value.AfterFrame(frame, deleted: false);
            }

            foreach (var urnPickup in urnPickups.Values)
            {
              if (seenUrnPickup == null || seenUrnPickup.Value.justDeleted || urnPickup != seenUrnPickup.Value.value)
              {
                if (!urnPickup.VariableHistory[^1].deleted)
                {
                  if (seenUrnPickup == null)
                  {
                    seenUrnPickup = (urnPickup, justDeleted: true);
                  }
                  else if (
                    !seenUrnPickup.Value.justDeleted
                    && activeUrn != null
                    && (
                      activeUrn.VariableHistory[^1].state == EUnifiedUrnState.WaitingForFirstPickup
                      || activeUrn.VariableHistory[^1].state == EUnifiedUrnState.DroppedSpawned
                    )
                    && urnPickup == activeUrn.VariableHistory[^1].unheldUrn
                  )
                  {
                    inactiveUrns.Add(activeUrn);
                    activeUrn = null;
                  }
                  else throw new Exception(nameof(urnPickups));
                }
                urnPickup.AfterFrame(frame, deleted: true);
              }
            }
          }

          UrnDropoffSpotHistory? enabledUrnDropoffSpot = null;
          {
            if (urnDropoffSpots.Count == 0)
            {
              foreach (var urnDropoffSpotEntity in demo.Entities.OfType<DeadlockDemo.CCitadelIdolReturnTrigger>()) // no, compiler, this won't result in an empty sequence you dummy
              {
                if (urnDropoffSpots.ContainsKey(urnDropoffSpotEntity)) throw new Exception(nameof(urnDropoffSpots));

                UrnDropoffSpotHistory urnDropoffSpot;
                {
                  var tempView = new UrnDropoffSpotView { Entity = urnDropoffSpotEntity, SpotIdHint = null };
                  if (!tempView.AllAccessible() || !tempView.ConstantsValid()) throw new Exception(nameof(tempView));
                  urnDropoffSpot = new(new UrnDropoffSpotView { Entity = urnDropoffSpotEntity, SpotIdHint = tempView.SpotId });
                }
                if (urnDropoffSpots.Values.Any(s => s.Constants.SpotId == urnDropoffSpot.Constants.SpotId)) throw new Exception(nameof(urnDropoffSpots));
                urnDropoffSpots.Add(urnDropoffSpotEntity, urnDropoffSpot);

                if (!urnDropoffSpotEntity.IsActive) throw new Exception(nameof(urnDropoffSpotEntity.IsActive));

                urnDropoffSpot.AfterFrame(frame);

                if (urnDropoffSpot.VariableHistory[0].variables.State != EUrnDropoffSpotState.Inactive) throw new Exception(nameof(IUrnDropoffSpotVariables.State));
              }

              if (urnDropoffSpots.Count != 6 && urnDropoffSpots.Count != 0) throw new Exception(nameof(urnDropoffSpots.Count));
            }
            else
            {
              var seenSpots = new HashSet<DeadlockDemo.CCitadelIdolReturnTrigger>();
              foreach (var urnDropoffSpotEntity in demo.Entities.OfType<DeadlockDemo.CCitadelIdolReturnTrigger>()) // no, compiler, this won't result in an empty sequence you dummy
              {
                if (!urnDropoffSpots.TryGetValue(urnDropoffSpotEntity, out var urnDropoffSpot)) throw new Exception(nameof(urnDropoffSpots));
                if (seenSpots.Contains(urnDropoffSpotEntity)) throw new Exception(nameof(seenSpots));
                seenSpots.Add(urnDropoffSpotEntity);

                if (!urnDropoffSpotEntity.IsActive) throw new Exception(nameof(urnDropoffSpotEntity.IsActive));

                urnDropoffSpot.AfterFrame(frame);
              }

              if (seenSpots.Count != urnDropoffSpots.Count) throw new Exception(nameof(seenSpots));
            }

            foreach (var spot in urnDropoffSpots.Values)
            {
              if (spot.VariableHistory[^1].variables.State != EUrnDropoffSpotState.Inactive)
              {
                if (enabledUrnDropoffSpot != null) throw new Exception(nameof(spot));
                enabledUrnDropoffSpot = spot;
              }
            }
          }

          DeadlockDemo.CCitadel_Ability_GoldenIdol? seenUrnAbility = null;
          {
            var seenUrnAbilities = demo.Entities.OfType<DeadlockDemo.CCitadel_Ability_GoldenIdol>().ToList(); // no, compiler, this won't result in an empty sequence you dummy
            if (seenUrnAbilities.Count > 1) throw new Exception(nameof(seenUrnAbilities));
            else if (seenUrnAbilities.TryGet(0, out var seenUrnAbility_))
            {
              seenUrnAbility = seenUrnAbility_;
              if (seenUrnAbility.OwnerEntity == null) throw new Exception(nameof(seenUrnAbility.OwnerEntity));
            }
          }

          {
            // activeUrn also has a special case in the urnPickup deletion loop above -- see that code too

            if (activeUrn == null)
            {
              if (players.Any(
                p => p.VariableHistory[^1].variables.UrnState == EPlayerUrnState.Holding
                || p.VariableHistory[^1].variables.UrnState == EPlayerUrnState.HoldingAndReturning
              )) throw new Exception(nameof(players));
              if (enabledUrnDropoffSpot != null) throw new Exception(nameof(enabledUrnDropoffSpot));

              if (seenUrnPickup != null)
              {
                if (
                  seenUrnPickup.Value.justDeleted
                  || seenUrnPickup.Value.value.Constants.Type != EDroppedUrnType.NeverPickedUp
                  || seenUrnPickup.Value.value.VariableHistory[^1].variables.Position.Z <= UrnVariables.ExpectedSpawnLandingZ
                ) throw new Exception(nameof(seenUrnPickup));

                activeUrn = new(seenUrnPickup.Value.value);
                activeUrn.VariableHistory.Add((
                  iFrame: iFrame,
                  state: EUnifiedUrnState.Descending,
                  holdingPlayer: null,
                  unheldUrn: activeUrn.SpawnUrn
                ));
              }
            }
            else
            {
              var lastState = activeUrn.VariableHistory[^1].state;
              switch (lastState)
              {
                case EUnifiedUrnState.Descending:
                  {
                    if (players.Any(
                      p => p.VariableHistory[^1].variables.UrnState == EPlayerUrnState.Holding
                      || p.VariableHistory[^1].variables.UrnState == EPlayerUrnState.HoldingAndReturning
                    )) throw new Exception(nameof(players));
                    if (enabledUrnDropoffSpot != null || activeUrn.DropoffSpot != null) throw new Exception(nameof(enabledUrnDropoffSpot));

                    if (seenUrnPickup == null || seenUrnPickup.Value.justDeleted || seenUrnPickup.Value.value != activeUrn.SpawnUrn) throw new Exception(nameof(seenUrnPickup));
                    if (activeUrn.SpawnUrn.VariableHistory[^1].variables.Position.Z == UrnVariables.ExpectedSpawnLandingZ)
                    {
                      activeUrn.VariableHistory.Add((
                        iFrame: iFrame,
                        state: EUnifiedUrnState.WaitingForFirstPickup,
                        holdingPlayer: null,
                        unheldUrn: activeUrn.SpawnUrn
                      ));
                    }
                  }
                  break;

                case EUnifiedUrnState.WaitingForFirstPickup:
                case EUnifiedUrnState.DroppedSpawned:
                  {
                    if (
                      seenUrnPickup == null
                      || seenUrnPickup.Value.value != (lastState == EUnifiedUrnState.WaitingForFirstPickup ? activeUrn.SpawnUrn : activeUrn.VariableHistory[^1].unheldUrn)
                    ) throw new Exception(nameof(seenUrnPickup));

                    if (seenUrnPickup.Value.justDeleted)
                    {
                      PlayerHistory? pickupPlayer = null;
                      foreach (var player in players)
                      {
                        if (player.VariableHistory[^1].iFrame != iFrame) continue;

                        if (
                          lastState == EUnifiedUrnState.WaitingForFirstPickup
                          && player.VariableHistory[^1].variables.UrnState == EPlayerUrnState.HoldingAndReturning
                        ) throw new Exception(nameof(player));
                        
                        if (
                          player.VariableHistory[^1].variables.UrnState == EPlayerUrnState.Holding
                          || player.VariableHistory[^1].variables.UrnState == EPlayerUrnState.HoldingAndReturning
                        )
                        {
                          if (pickupPlayer != null) throw new Exception(nameof(player));
                          pickupPlayer = player;

                          if (player.VariableHistory[^2].variables.UrnState != EPlayerUrnState.NotHoldingButPickingUp) throw new Exception(nameof(player));
                        }
                      }

                      if (enabledUrnDropoffSpot == null) throw new Exception(nameof(enabledUrnDropoffSpot));
                      if (lastState == EUnifiedUrnState.WaitingForFirstPickup)
                      {
                        if (activeUrn.DropoffSpot != null) throw new Exception(nameof(activeUrn.DropoffSpot));
                        activeUrn.DropoffSpot = enabledUrnDropoffSpot;
                      }
                      else
                      {
                        if (activeUrn.DropoffSpot == null) throw new Exception(nameof(activeUrn.DropoffSpot));
                      }

                      if (pickupPlayer == null)
                      {
                        // Suspect this is a very special case when a player uses a movement ability (or does something else illegal) right at the moment of pickup,
                        // causing it to *immediately* drop in the same frame.
                        // A good example of this is 20:00 in 34748372 where I think the player tried to do a movement ability the moment they picked up the urn.
                        // We'll fall back to the urn ability, which links to the picked-up player but it's a little annoying since the behavior of this ability
                        //   is overall quite weird (sometimes it takes a frame or to to appear on normal pickups).
                        if (seenUrnAbility == null) throw new Exception(nameof(pickupPlayer));
                        if (enabledUrnDropoffSpot.VariableHistory[^1].variables.State != EUrnDropoffSpotState.ActiveForDroppedUrn) throw new Exception(nameof(enabledUrnDropoffSpot));
                        pickupPlayer = players.Single(p => p.View.HeroPawn == seenUrnAbility.OwnerEntity);
                        if (pickupPlayer.VariableHistory[^1].variables.UrnState != EPlayerUrnState.NotHoldingButPickingUp) throw new Exception(nameof(seenUrnAbility));
                        Console.WriteLine($"Warning: obscure urn pickup edge case occured at {frame}");
                        activeUrn.VariableHistory.Add((
                          iFrame: iFrame,
                          state: EUnifiedUrnState.PickedUp,
                          holdingPlayer: players.Single(p => p.View.HeroPawn == seenUrnAbility.OwnerEntity),
                          unheldUrn: null
                        ));
                        activeUrn.VariableHistory.Add((
                          iFrame: iFrame,
                          state: EUnifiedUrnState.DroppedSpawning,
                          holdingPlayer: null,
                          unheldUrn: null
                        ));
                      }
                      else
                      {
                        if (
                          enabledUrnDropoffSpot.VariableHistory[^1].variables.State
                          != (pickupPlayer.Constants.Team == DeadlockDemo.TeamNumber.Amber ? EUrnDropoffSpotState.ActiveForAmber : EUrnDropoffSpotState.ActiveForSapphire)
                        ) throw new Exception(nameof(enabledUrnDropoffSpot));
                        activeUrn.VariableHistory.Add((
                          iFrame: iFrame,
                          state:
                            (pickupPlayer.VariableHistory[^1].variables.UrnState == EPlayerUrnState.Holding)
                            ? EUnifiedUrnState.PickedUp
                            : EUnifiedUrnState.Returning,
                          holdingPlayer: pickupPlayer,
                          unheldUrn: null
                        ));
                      }
                    }
                    else
                    {
                      if (players.Any(
                        p => p.VariableHistory[^1].variables.UrnState == EPlayerUrnState.Holding
                        || p.VariableHistory[^1].variables.UrnState == EPlayerUrnState.HoldingAndReturning
                      )) throw new Exception(nameof(players));
                      if (
                        lastState == EUnifiedUrnState.WaitingForFirstPickup
                        ? enabledUrnDropoffSpot != null
                        : (
                          enabledUrnDropoffSpot == null
                          || enabledUrnDropoffSpot != activeUrn.DropoffSpot
                          || enabledUrnDropoffSpot.VariableHistory[^1].variables.State != EUrnDropoffSpotState.ActiveForDroppedUrn
                        )
                      ) throw new Exception(nameof(enabledUrnDropoffSpot));
                    }
                  }
                  break;

                case EUnifiedUrnState.PickedUp:
                case EUnifiedUrnState.Returning:
                  {
                    foreach (var player in players)
                    {
                      if (player.VariableHistory[^1].iFrame != iFrame) continue;
                      if (activeUrn != null && player == activeUrn.VariableHistory[^1].holdingPlayer)
                      {
                        if (
                          player.VariableHistory[^1].variables.UrnState == EPlayerUrnState.NotHolding 
                          || player.VariableHistory[^1].variables.UrnState == EPlayerUrnState.NotHoldingButPickingUp // you probably can't pick up right away but just in case
                        )
                        {
                          if (enabledUrnDropoffSpot == null)
                          {
                            if (seenUrnPickup != null) throw new Exception(nameof(seenUrnPickup));
                            if (player.VariableHistory[^1].variables.UrnState != EPlayerUrnState.NotHolding) throw new Exception(nameof(player));

                            activeUrn.VariableHistory.Add((
                              iFrame: iFrame,
                              state: EUnifiedUrnState.Returned,
                              holdingPlayer: null,
                              unheldUrn: null
                            ));

                            inactiveUrns.Add(activeUrn);
                            activeUrn = null;
                          }
                          else
                          {
                            if (
                              enabledUrnDropoffSpot != activeUrn.DropoffSpot
                              || enabledUrnDropoffSpot.VariableHistory[^1].variables.State != EUrnDropoffSpotState.ActiveForDroppedUrn
                            ) throw new Exception(nameof(enabledUrnDropoffSpot));

                            if (seenUrnPickup.HasValue)
                            {
                              if (
                                seenUrnPickup.Value.justDeleted
                                || (
                                  seenUrnPickup.Value.value.Constants.Type
                                  != (player.Constants.Team == DeadlockDemo.TeamNumber.Amber ? EDroppedUrnType.DroppedByAmber : EDroppedUrnType.DroppedBySapphire)
                                )
                              ) throw new Exception(nameof(seenUrnPickup));

                              activeUrn.VariableHistory.Add((
                                iFrame: iFrame,
                                state: EUnifiedUrnState.DroppedSpawned,
                                holdingPlayer: null,
                                unheldUrn: seenUrnPickup.Value.value
                              ));
                            }
                            else
                            {
                              if (player.VariableHistory[^1].variables.UrnState != EPlayerUrnState.NotHolding) throw new Exception(nameof(player));
                              activeUrn.VariableHistory.Add((
                                iFrame: iFrame,
                                state: EUnifiedUrnState.DroppedSpawning,
                                holdingPlayer: null,
                                unheldUrn: null
                              ));
                            }
                          }
                        }
                        else
                        {
                          if (seenUrnPickup != null) throw new Exception(nameof(seenUrnPickup));

                          if (
                            enabledUrnDropoffSpot == null
                            || enabledUrnDropoffSpot != activeUrn.DropoffSpot
                            || (
                              enabledUrnDropoffSpot.VariableHistory[^1].variables.State
                              != (player.Constants.Team == DeadlockDemo.TeamNumber.Amber ? EUrnDropoffSpotState.ActiveForAmber : EUrnDropoffSpotState.ActiveForSapphire)
                            )
                          ) throw new Exception(nameof(enabledUrnDropoffSpot));

                          var newState =
                            player.VariableHistory[^1].variables.UrnState == EPlayerUrnState.HoldingAndReturning
                            ? EUnifiedUrnState.Returning
                            : EUnifiedUrnState.PickedUp;
                          if (newState != lastState)
                          {
                            activeUrn.VariableHistory.Add((
                              iFrame: iFrame,
                              state: newState,
                              holdingPlayer: player,
                              unheldUrn: null
                            ));
                          }
                        }
                      }
                    }

                    foreach (var player in players)
                    {
                      if (player.VariableHistory[^1].iFrame != iFrame) continue;
                      if (activeUrn != null && activeUrn.VariableHistory[^1].state == EUnifiedUrnState.DroppedSpawned)
                      {
                        if (
                          player.VariableHistory[^1].variables.UrnState != EPlayerUrnState.NotHolding
                          && player.VariableHistory[^1].variables.UrnState != EPlayerUrnState.NotHoldingButPickingUp
                        ) throw new Exception(nameof(player));
                      }
                      else if (!(
                        activeUrn != null
                        && (
                          activeUrn.VariableHistory[^1].state == EUnifiedUrnState.PickedUp
                          || activeUrn.VariableHistory[^1].state == EUnifiedUrnState.Returning
                        )
                        && player == activeUrn.VariableHistory[^1].holdingPlayer
                      ))
                      {
                        if (player.VariableHistory[^1].variables.UrnState != EPlayerUrnState.NotHolding) throw new Exception(nameof(player));
                      }
                    }
                  }
                  break;

                case EUnifiedUrnState.DroppedSpawning:
                  {
                    if (
                      enabledUrnDropoffSpot == null
                      || enabledUrnDropoffSpot != activeUrn.DropoffSpot
                      || enabledUrnDropoffSpot.VariableHistory[^1].variables.State != EUrnDropoffSpotState.ActiveForDroppedUrn
                    ) throw new Exception(nameof(enabledUrnDropoffSpot));

                    if (seenUrnPickup.HasValue)
                    {
                      if (
                        seenUrnPickup.Value.justDeleted
                        || (
                          seenUrnPickup.Value.value.Constants.Type
                          // guaranteed there will have been two variable histories already: DroppedSpawning only ever comes after PickedUp/Returning (sort of)
                          != (activeUrn.VariableHistory[^2].holdingPlayer.Constants.Team == DeadlockDemo.TeamNumber.Amber ? EDroppedUrnType.DroppedByAmber : EDroppedUrnType.DroppedBySapphire)
                        )
                      ) throw new Exception(nameof(seenUrnPickup));

                      if (players.Any(p =>
                        p.VariableHistory[^1].variables.UrnState == EPlayerUrnState.Holding
                        || p.VariableHistory[^1].variables.UrnState == EPlayerUrnState.HoldingAndReturning
                      )) throw new Exception(nameof(players));

                      activeUrn.VariableHistory.Add((
                        iFrame: iFrame,
                        state: EUnifiedUrnState.DroppedSpawned,
                        holdingPlayer: null,
                        unheldUrn: seenUrnPickup.Value.value
                      ));
                    }
                    else
                    {
                      if (players.Any(p => 
                        p.VariableHistory[^1].variables.UrnState != EPlayerUrnState.NotHolding
                        && !(
                          // see the very special case in the WaitingForPickup->DroppedSpawning transition logic (this is checking for / allowing it)
                          activeUrn.VariableHistory[^1].iFrame == activeUrn.VariableHistory[^2].iFrame
                          && activeUrn.VariableHistory[^2].holdingPlayer == p
                          && p.VariableHistory[^1].variables.UrnState == EPlayerUrnState.NotHoldingButPickingUp
                        )
                      )) throw new Exception(nameof(players));
                    }
                  }
                  break;

                case EUnifiedUrnState.Returned:
                default:
                  throw new Exception(nameof(activeUrn));
              }
            }
          }
        }

        {
          if (previousFrame == null || rules?.PreGameConstants == null)
          {
            if (demo.Entities.OfType<DeadlockDemo.CItemXP>().Any()) throw new Exception();
          } 
          else
          {
            var seenSoulOrbs = new HashSet<SoulOrbHistory>();
            foreach (var soulOrbEntity in demo.Entities.OfType<DeadlockDemo.CItemXP>()) // no, compiler, this won't result in an empty sequence you dummy
            {
              SoulOrbHistory soulOrb;
              if (soulOrbs.TryGetValue(soulOrbEntity, out var existingHistory))
              {
                soulOrb = existingHistory;
              }
              else
              {
                var view = new SoulOrbView { Entity = soulOrbEntity };
                soulOrb = new SoulOrbHistory(view);
                soulOrbs.Add(soulOrbEntity, soulOrb);
              }

              var pvsState = soulOrbEntity.IsActive ? EEntityPvsState.Active : EEntityPvsState.InactiveButPresent;

              if (seenSoulOrbs.Contains(soulOrb)) throw new Exception(nameof(seenSoulOrbs));
              seenSoulOrbs.Add(soulOrb);
              soulOrb.AfterFrame(previousFrame, frame, rules.PreGameConstants.GameStartTime, pvsState);
            }

            foreach (var soulOrb in soulOrbs)
            {
              if (!seenSoulOrbs.Contains(soulOrb.Value))
              {
                soulOrb.Value.AfterFrame(previousFrame, frame, rules.PreGameConstants.GameStartTime, pvsState: EEntityPvsState.Deleted);
              }
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
        serverInfoReceived = true;
        if (!info.HasTickInterval) throw new Exception(nameof(info.HasTickInterval));
        const float expectedTickInterval = 1f / Frame.TICKS_PER_SECOND;
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
        var buggedSpawnFloatingZiplineTroopers = allTroopers.Count(t => t.IsBuggedSpawnZipline && t.VariableHistory.Count > 1);
        if (buggedSpawnFloatingZiplineTroopers != 0)
        {
          var buggedSpawnFloatingZiplineFellOutOfWorldTroopers = allUnifiedTroopers.Count(ut => 
            ut.VariableHistory.Last().state is (
              EUnifiedTrooperState.Packed_DeadBuggedFellOutOfWorld 
              or EUnifiedTrooperState.Unpacked_InactiveBuggedFellOutOfWorld
            )
          );
          Console.WriteLine($"Warning: {buggedSpawnFloatingZiplineTroopers} troopers with wrong zipline spawn floating bug. {buggedSpawnFloatingZiplineFellOutOfWorldTroopers} of these fell out of the world.");
        }
        var unifiedSubtroopers = new HashSet<TrooperHistory>();
        foreach (var ut in allUnifiedTroopers)
        {
          if (!unifiedSubtroopers.Add(ut.ZiplineTrooper)) throw new Exception(nameof(ut.ZiplineTrooper));
          if (ut.ActiveTrooper != null && !unifiedSubtroopers.Add(ut.ActiveTrooper.Value.trooper)) throw new Exception(nameof(ut.ActiveTrooper));
        }
      }

      if (towers.Count != 2 /*teams*/ * (3 /*tier 1's*/ + 6 /*tier 3's*/)) throw new Exception(nameof(towers));
      if (walkers.Count != 2 /*teams*/ * 3 /*lanes*/) throw new Exception(nameof(towers));
      // urn dropoff spot count checked in AfterFrame

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
        Console.WriteLine($">>>> Enter a command: quit, meta, break, go_frame, go_demo, go_game, go_game_time(s,t), go_replay_time(s,t), player(i), "
                          + $"dump_players, dump_troopers, dump_towers, dump_walkers, dump_urns, dump_orbs");
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
          Console.WriteLine($"Experimental gameplay state: {rules.Constants.GameplayExperiment}");
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
            Console.WriteLine($"{player.index}: {player.value.Constants.Name} (slot{player.value.Constants.LobbyPlayerSlot} {player.value.Constants.Team} {player.value.Constants.HeroId} {player.value.Constants.SteamId})");
          }
          continue;
        }

        if (command == "break")
        {
          Console.WriteLine($"Set a breakpoint on this Console.WriteLine line.");
          continue;
        }

        if (command == "dump_players")
        {
          var filename = $"{matchId}_players.json";
          await File.WriteAllTextAsync(
            filename,
            SerializeJsonObject(
              players
              .Select(t => new
              {
                Constants = t.Constants,
                Variables = t.VariableHistory.Select(v => new { v.iFrame, v.variables }),
              })
              .ToList()
            )
          );
          Console.WriteLine($"Wrote to {filename}");
          ReleaseUnusedMemoryNow();
          continue;
        }

        if (command == "dump_troopers")
        {
          var filename = $"{matchId}_troopers.json";
          await File.WriteAllTextAsync(
            filename,
            SerializeJsonObject(
              allUnifiedTroopers
              .Select(t => new
              {
                ZiplineConstants = t.ZiplineConstants,
                IsBuggedSpawnZipline = t.ZiplineTrooper.IsBuggedSpawnZipline,
                ActiveConstants = t.ActiveTrooper?.constants,
                Variables = t.VariableHistory.Select(v => new { v.iFrame, v.pvsState, v.state, v.variables }),
              })
              .ToList()
            )
          );
          Console.WriteLine($"Wrote to {filename}");
          ReleaseUnusedMemoryNow();
          continue;
        }
        if (command == "dump_partial_troopers")
        {
          var filename = $"{matchId}_partial_troopers.json";
          await File.WriteAllTextAsync(
            filename,
            SerializeJsonObject(
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
          ReleaseUnusedMemoryNow();
          continue;
        }

        if (command == "dump_towers")
        {
          var filename = $"{matchId}_towers.json";
          await File.WriteAllTextAsync(
            filename,
            SerializeJsonObject(
              towers.Values
              .Select(t => new
              {
                Constants = t.Constants,
                Variables = t.VariableHistory.Select(v => new { v.iFrame, v.deleted, v.variables }),
              })
              .ToList()
            )
          );
          Console.WriteLine($"Wrote to {filename}");
          ReleaseUnusedMemoryNow();
          continue;
        }

        if (command == "dump_walkers")
        {
          var filename = $"{matchId}_walkers.json";
          await File.WriteAllTextAsync(
            filename,
            SerializeJsonObject(
              walkers.Values
              .Select(t => new
              {
                Constants = t.Constants,
                Variables = t.VariableHistory.Select(v => new { v.iFrame, v.deleted, v.variables }),
              })
              .ToList()
            )
          );
          Console.WriteLine($"Wrote to {filename}");
          ReleaseUnusedMemoryNow();
          continue;
        }

        if (command == "dump_urns")
        {
          var filename = $"{matchId}_urns.json";
          await File.WriteAllTextAsync(
            filename,
            SerializeJsonObject(
                  new
                  {
                    /*
                    Players =
                      players
                      .Select(t => new
                      {
                        Constants = t.Constants,
                        Variables = t.VariableHistory.Select(v => new { v.iFrame, v.variables }),
                      })
                      .ToList(),
                    */
                    UrnPickups =
                      urnPickups
                      .Select(p => new
                      {
                        Constants = p.Value.Constants,
                        Variables = p.Value.VariableHistory.Select(v => new { v.iFrame, v.deleted, v.variables }),
                      })
                      .ToList(),
                    UrnDropoffSpots =
                      urnDropoffSpots
                      .Select(t => new
                      {
                        Constants = t.Value.Constants,
                        Variables = t.Value.VariableHistory.Select(v => new { v.iFrame, v.variables }),
                      })
                      .ToList(),
                    UnifiedUrns =
                      inactiveUrns.Concat(activeUrn == null ? Enumerable.Empty<UnifiedUrnHistory>() : activeUrn.Yield())
                      .Select(u => new
                      {
                        SpawnConstants = u.SpawnConstants,
                        DropoffSpotConstants = u.DropoffSpot?.Constants,
                        Variables = u.VariableHistory.Select(v => new
                        {
                          v.iFrame,
                          v.state,
                          Player = v.holdingPlayer?.Constants.Name,
                          Urn = v.unheldUrn?.View.Entity.EntityIndex.Value,
                        }),
                      })
                      .ToList(),
              }
            )
          );
          Console.WriteLine($"Wrote to {filename}");
          ReleaseUnusedMemoryNow();
          continue;
        }

        if (command == "dump_orbs")
        {
          var filename = $"{matchId}_orbs.json";
          await File.WriteAllTextAsync(
            filename,
            SerializeJsonObject(
              soulOrbs.Values
              .Select(t => new
              {
                Constants = t.Constants,
                FirstFrameOneChangeConstants = t.FirstFrameOneChangeConstants,
                SubsequentOneChangeConstants = t.SubsequentOneChangeConstants?.oneChangeConstants,
                SubsequentOneChangeConstantsIFrame = t.SubsequentOneChangeConstants?.iFrame ?? 0,
                Variables = t.VariableHistory.Select(v => new { v.iFrame, v.pvsState, v.variables }),
              })
              .ToList()
            )
          );
          Console.WriteLine($"Wrote to {filename}");
          ReleaseUnusedMemoryNow();
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

    private static void ReleaseUnusedMemoryNow()
    {
      var originalLOHCompactionMode = GCSettings.LargeObjectHeapCompactionMode;
      GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
      GC.Collect();
      GC.WaitForPendingFinalizers();
      GC.Collect();
      GCSettings.LargeObjectHeapCompactionMode = originalLOHCompactionMode;
      Process currentProcess = Process.GetCurrentProcess();
      currentProcess.MinWorkingSet = currentProcess.MinWorkingSet;
    }
  }
}
