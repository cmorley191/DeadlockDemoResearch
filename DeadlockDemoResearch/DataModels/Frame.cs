using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadlockDemoResearch.DataModels
{

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

}
