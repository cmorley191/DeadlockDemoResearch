﻿using DeadlockDemo = DemoFile.Game.Deadlock;

namespace DeadlockDemoResearch.DataModels
{
  public enum EEntityPvsState
  {
    Active = 1,
    InactiveButPresent = 2,
    Deleted = 3,
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

  public enum NpcStateMasks : int
  {
    Init = 1 << DeadlockDemo.NPC_STATE.NPC_STATE_INIT,  // 1
    Idle = 1 << DeadlockDemo.NPC_STATE.NPC_STATE_IDLE,  // 2
    Alert = 1 << DeadlockDemo.NPC_STATE.NPC_STATE_ALERT,  // 4
    Combat = 1 << DeadlockDemo.NPC_STATE.NPC_STATE_COMBAT,  // 8
    Script = 1 << DeadlockDemo.NPC_STATE.NPC_STATE_SCRIPT, // 16
    Dead = 1 << DeadlockDemo.NPC_STATE.NPC_STATE_DEAD,  // 32
    Inert = 1 << DeadlockDemo.NPC_STATE.NPC_STATE_INERT, // 64
    SynchronizedSecondary = 1 << DeadlockDemo.NPC_STATE.NPC_STATE_SYNCHRONIZED_SECONDARY, // 128

    MIN = Init,
    MAX = Init | Idle | Alert | Combat | Script | Dead | Inert | SynchronizedSecondary,
  }

  public enum ELane
  {
    Yellow = 1,
    Blue = 4,
    Green = 6,
  }

  public enum ESubclassId : uint
  {
    TrooperSupport = 747565566,
    TrooperZiplinePackage = 3237674373,
    TrooperMelee = 3831103061,
    TrooperRanged = 4164228731,

    TowerTier1Amber = 2977181093,
    TowerTier1Sapphire = 2713571573,
    TowerTier3 = 2458524739,

    WalkerInsideLanesAmber = 3493103073,
    WalkerInsideLanesSapphire = 736669903,
    WalkerOutsideLanesAmber = 4174712489,
    WalkerOutsideLanesSapphire = 3601160001,

    SoulOrbFountain = 7345281,
    SoulOrbVendingMachine = 828604450,
    SoulOrbUrn = 3283937835,
    SoulOrbPlayer = 3027388212,
    SoulOrbTowersWalkers = 2172658167,
    SoulOrbTrooper = 494398941,
    SoulOrbMcGinnisTurret = 2222749624,
  }

  public enum ETrooperSubclassId : uint
  {
    Support = ESubclassId.TrooperSupport,
    ZiplinePackage = ESubclassId.TrooperZiplinePackage, 
    Melee = ESubclassId.TrooperMelee, 
    Ranged = ESubclassId.TrooperRanged, 
  }

  public enum ETrooperActiveSubclassId : uint
  {
    Support = ETrooperSubclassId.Support,
    // Zipline isn't Active
    Melee = ETrooperSubclassId.Melee,
    Ranged = ETrooperSubclassId.Ranged,
  }

  public enum ESoulOrbSubclassId : uint
  {
    SoulOrbFountain = ESubclassId.SoulOrbFountain,
    SoulOrbVendingMachine = ESubclassId.SoulOrbVendingMachine,
    SoulOrbUrn = ESubclassId.SoulOrbUrn,
    SoulOrbPlayer = ESubclassId.SoulOrbPlayer,
    SoulOrbTowersWalkers = ESubclassId.SoulOrbTowersWalkers,
    SoulOrbTrooper = ESubclassId.SoulOrbTrooper,
    SoulOrbMcGinnisTurret = ESubclassId.SoulOrbMcGinnisTurret,
  }

  // re-generate these from !GlobalTypes.json with generate_modifier_states.py
  public enum ModifierStateShift
  {
    MaterialOverride = 0,
    EnteringVehicle = 1,
    ExitingVehicle = 2,
    UnrestrictedMovement = 3,
    ForceAlertState = 4,
    DisableSquad = 5,
    Immobilized = 6,
    Disarmed = 7,
    Muted = 8,
    Silenced = 9,
    SilenceMovementAbilites = 10,
    SilencedHidden = 11,
    Stunned = 12,
    Invulnerable = 13,
    TechInvulnerable = 14,
    TechDamageInvulnerable = 15,
    TechUntargetableByEnemies = 16,
    StatusImmune = 17,
    Unstoppable = 18,
    OutOfGame = 19,
    CommandRestricted = 20,
    Charging = 21,
    Obscured = 22,
    InvisibleToEnemy = 23,
    InvisibleToEnemyCast = 24,
    IgnoredByNpcTargeting = 25,
    NpcTargetableWhileInvulnerable = 26,
    Sprinting = 27,
    Unkillable = 28,
    GroundDashing = 29,
    AdditionalAirMoves = 30,
    UnlimitedAirDashes = 31,
    AirDashing = 32,
    MovementAbilityRestricted = 33,
    SprintNoInterrupt = 34,
    SprintDisabled = 35,
    InShop = 36,
    InFountain = 37,
    InEnemyBase = 38,
    InFriendlyBase = 39,
    InMidBossTemple = 40,
    InShoptunnelSapphire = 41,
    InShoptunnelAmber = 42,
    DashDisabled = 43,
    DashDisabledDebuff = 44,
    Burning = 45,
    HealthRegenDisabled = 46,
    HealthExternalRegenDisabled = 47,
    HealingDisabled = 48,
    DamageMovementPenaltyImmune = 49,
    BusyWithAction = 50,
    Slowed = 51,
    ShootingDisabled = 52,
    ShootingForcedOn = 53,
    MeleeAttacksOnly = 54,
    Sliding = 55,
    VisibleToEnemy = 56,
    InfiniteClip = 57,
    KnockdownImmune = 58,
    JumpDisabled = 59,
    DuckingDisabled = 60,
    DuckingForced = 61,
    AirDuckingForced = 62,
    IsAsleep = 63,
    GlowThroughWallsToEnemy = 64,
    GlowThroughWallsToProvider = 65,
    GlowThroughWallsToProviderTeammatesWithinRange = 66,
    GlowToProvider = 67,
    GlowInLosToEnemy = 68,
    DisableAirSpreadPenalty = 69,
    UsingZipline = 70,
    InAlternateDimension = 71,
    AnimgraphControlledMovement = 72,
    CombatAbilitiesDisabled = 73,
    Chained = 74,
    AllArmorDisabled = 75,
    BulletInvulnerable = 76,
    BreakIcePath = 77,
    ReflectAttacks = 78,
    MantleDisabled = 79,
    AiForceAggro = 80,
    AiForceCalm = 81,
    DroneAttached = 82,
    AimForward = 83,
    AimForwardWithPitch = 84,
    ZiplineDisabled = 85,
    ZiplineIntro = 86,
    RespawnCredit = 87,
    RespawnCreditPersonal = 88,
    DisplayLifetime = 89,
    MeleeDisabled = 90,
    MeleeDisabledDebuff = 91,
    Glitched = 92,
    SlidingDisabled = 93,
    SlidingForcedTry = 94,
    ReloadDisabled = 95,
    ReloadMeleeFullSpeed = 96,
    ManualReloadDisabled = 97,
    UnitStatusHealthHidden = 98,
    UnitStatusHidden = 99,
    FadeAlphaToZero = 100,
    FriendlyFireEnabled = 101,
    Flying = 102,
    Scoped = 103,
    RollingBall = 104,
    ViscousCubed = 105,
    SlowImmune = 106,
    RootImmune = 107,
    IsMeleeTarget = 108,
    GlowToAlliesAsEnemy = 109,
    LadderDisable = 110,
    IgnoreBullets = 111,
    IgnoreMelee = 112,
    HideCrosshair = 113,
    HideStamina = 114,
    HideAmmo = 115,
    ApplyVerticalDrag = 116,
    NoWindup = 117,
    NoShootlockoutOnJump = 118,
    TitanLaserValidTarget = 119,
    HasHollowPointBullets = 120,
    SuppressAirDrag = 121,
    Icebeaming = 122,
    PowerSlashing = 123,
    InAllySmoke = 124,
    InEnemySmoke = 125,
    NoOutgoingDamage = 126,
    NoIncomingDamage = 127,
    ChronoSwapping = 128,
    BouncePadStomping = 129,
    FlyingBeetleTarget = 130,
    DreamweaverSleep = 131,
    PickingUpIdol = 132,
    HoldingIdol = 133,
    ReturnIdolArea = 134,
    ReturningIdol = 135,
    DropIdol = 136,
    GalvanicStormBuff = 137,
    InAbilityAllowZoom = 138,
    InAbilityDisableZoom = 139,
    StaminaRegenPaused = 140,
    Prematch = 141,
    SelfDestruct = 142,
    CoopTetherActive = 143,
    CoopTetherLockedTarget = 144,
    LockedAimAngles = 145,
    Icepathing = 146,
    BackdoorProtected = 147,
    InCombat = 148,
    DashjumpStaminaFree = 149,
    YamatoShadowForm = 150,
    InMidBossPit = 151,
    CastsIgnoreChanneling = 152,
    AllowMeleeWhenChanneling = 153,
    AllowDashWhenChanneling = 154,
    CanActiveReload = 155,
    DiggerBurrowChannel = 156,
    DiggerSpin = 157,
    NearRejuvinator = 158,
    NearPunchableItem = 159,
    InTier3Phase2BossPit = 160,
    TakesFulldamageNoFalloff = 161,
    PredatoryStatueTarget = 162,
    FootstepSoundsDisable = 163,
    MovementFoleySoundsDisable = 164,
    DoNotDrawModel = 165,
    ShivFrenzied = 166,
    ForceAnimDuck = 167,
    ForceAnimKeepStill = 168,
    SiphonBulletLoss = 169,
    ApplyFovMouseSensitivityScale = 170,
    NearClimbableRope = 171,
    IsClimbingRope = 172,
    ForceCanParry = 173,
    IsMagicShockImmune = 174,
    InSlideChargedMeleeAttack = 175,
    ForceGroundJump = 176,
    Frozen = 177,
    AssassinateLowhealthTarget = 178,
    TeleporterDisabled = 179,
    InSmallInteriorSpace = 180,
    InMediumInteriorSpace = 181,
    EnableCloakDesat = 182,
    NanoRecentDamage = 183,
    DisableInattack2Deselect = 184,
    FathomDoNotRequireStandStillForInvis = 185,
    PulldownToGround = 186,
    InSelfBubble = 187,
    GunslingerMark = 188,
    GhostLifedrained = 189,
    InvisibleOnMinimap = 190,
    Ulting = 191,
    ViperVenom = 192,
    MagicianUlt = 193,
    AnimOnGround = 194,
    AbilityMovement = 195,
    AbilityMovementDebuff = 196,
    Count = 197,
    Invalid = 198,
  }

  public enum ModifierStateIndex
  {
    MaterialOverride = ModifierStateShift.MaterialOverride / 32,
    EnteringVehicle = ModifierStateShift.EnteringVehicle / 32,
    ExitingVehicle = ModifierStateShift.ExitingVehicle / 32,
    UnrestrictedMovement = ModifierStateShift.UnrestrictedMovement / 32,
    ForceAlertState = ModifierStateShift.ForceAlertState / 32,
    DisableSquad = ModifierStateShift.DisableSquad / 32,
    Immobilized = ModifierStateShift.Immobilized / 32,
    Disarmed = ModifierStateShift.Disarmed / 32,
    Muted = ModifierStateShift.Muted / 32,
    Silenced = ModifierStateShift.Silenced / 32,
    SilenceMovementAbilites = ModifierStateShift.SilenceMovementAbilites / 32,
    SilencedHidden = ModifierStateShift.SilencedHidden / 32,
    Stunned = ModifierStateShift.Stunned / 32,
    Invulnerable = ModifierStateShift.Invulnerable / 32,
    TechInvulnerable = ModifierStateShift.TechInvulnerable / 32,
    TechDamageInvulnerable = ModifierStateShift.TechDamageInvulnerable / 32,
    TechUntargetableByEnemies = ModifierStateShift.TechUntargetableByEnemies / 32,
    StatusImmune = ModifierStateShift.StatusImmune / 32,
    Unstoppable = ModifierStateShift.Unstoppable / 32,
    OutOfGame = ModifierStateShift.OutOfGame / 32,
    CommandRestricted = ModifierStateShift.CommandRestricted / 32,
    Charging = ModifierStateShift.Charging / 32,
    Obscured = ModifierStateShift.Obscured / 32,
    InvisibleToEnemy = ModifierStateShift.InvisibleToEnemy / 32,
    InvisibleToEnemyCast = ModifierStateShift.InvisibleToEnemyCast / 32,
    IgnoredByNpcTargeting = ModifierStateShift.IgnoredByNpcTargeting / 32,
    NpcTargetableWhileInvulnerable = ModifierStateShift.NpcTargetableWhileInvulnerable / 32,
    Sprinting = ModifierStateShift.Sprinting / 32,
    Unkillable = ModifierStateShift.Unkillable / 32,
    GroundDashing = ModifierStateShift.GroundDashing / 32,
    AdditionalAirMoves = ModifierStateShift.AdditionalAirMoves / 32,
    UnlimitedAirDashes = ModifierStateShift.UnlimitedAirDashes / 32,
    AirDashing = ModifierStateShift.AirDashing / 32,
    MovementAbilityRestricted = ModifierStateShift.MovementAbilityRestricted / 32,
    SprintNoInterrupt = ModifierStateShift.SprintNoInterrupt / 32,
    SprintDisabled = ModifierStateShift.SprintDisabled / 32,
    InShop = ModifierStateShift.InShop / 32,
    InFountain = ModifierStateShift.InFountain / 32,
    InEnemyBase = ModifierStateShift.InEnemyBase / 32,
    InFriendlyBase = ModifierStateShift.InFriendlyBase / 32,
    InMidBossTemple = ModifierStateShift.InMidBossTemple / 32,
    InShoptunnelSapphire = ModifierStateShift.InShoptunnelSapphire / 32,
    InShoptunnelAmber = ModifierStateShift.InShoptunnelAmber / 32,
    DashDisabled = ModifierStateShift.DashDisabled / 32,
    DashDisabledDebuff = ModifierStateShift.DashDisabledDebuff / 32,
    Burning = ModifierStateShift.Burning / 32,
    HealthRegenDisabled = ModifierStateShift.HealthRegenDisabled / 32,
    HealthExternalRegenDisabled = ModifierStateShift.HealthExternalRegenDisabled / 32,
    HealingDisabled = ModifierStateShift.HealingDisabled / 32,
    DamageMovementPenaltyImmune = ModifierStateShift.DamageMovementPenaltyImmune / 32,
    BusyWithAction = ModifierStateShift.BusyWithAction / 32,
    Slowed = ModifierStateShift.Slowed / 32,
    ShootingDisabled = ModifierStateShift.ShootingDisabled / 32,
    ShootingForcedOn = ModifierStateShift.ShootingForcedOn / 32,
    MeleeAttacksOnly = ModifierStateShift.MeleeAttacksOnly / 32,
    Sliding = ModifierStateShift.Sliding / 32,
    VisibleToEnemy = ModifierStateShift.VisibleToEnemy / 32,
    InfiniteClip = ModifierStateShift.InfiniteClip / 32,
    KnockdownImmune = ModifierStateShift.KnockdownImmune / 32,
    JumpDisabled = ModifierStateShift.JumpDisabled / 32,
    DuckingDisabled = ModifierStateShift.DuckingDisabled / 32,
    DuckingForced = ModifierStateShift.DuckingForced / 32,
    AirDuckingForced = ModifierStateShift.AirDuckingForced / 32,
    IsAsleep = ModifierStateShift.IsAsleep / 32,
    GlowThroughWallsToEnemy = ModifierStateShift.GlowThroughWallsToEnemy / 32,
    GlowThroughWallsToProvider = ModifierStateShift.GlowThroughWallsToProvider / 32,
    GlowThroughWallsToProviderTeammatesWithinRange = ModifierStateShift.GlowThroughWallsToProviderTeammatesWithinRange / 32,
    GlowToProvider = ModifierStateShift.GlowToProvider / 32,
    GlowInLosToEnemy = ModifierStateShift.GlowInLosToEnemy / 32,
    DisableAirSpreadPenalty = ModifierStateShift.DisableAirSpreadPenalty / 32,
    UsingZipline = ModifierStateShift.UsingZipline / 32,
    InAlternateDimension = ModifierStateShift.InAlternateDimension / 32,
    AnimgraphControlledMovement = ModifierStateShift.AnimgraphControlledMovement / 32,
    CombatAbilitiesDisabled = ModifierStateShift.CombatAbilitiesDisabled / 32,
    Chained = ModifierStateShift.Chained / 32,
    AllArmorDisabled = ModifierStateShift.AllArmorDisabled / 32,
    BulletInvulnerable = ModifierStateShift.BulletInvulnerable / 32,
    BreakIcePath = ModifierStateShift.BreakIcePath / 32,
    ReflectAttacks = ModifierStateShift.ReflectAttacks / 32,
    MantleDisabled = ModifierStateShift.MantleDisabled / 32,
    AiForceAggro = ModifierStateShift.AiForceAggro / 32,
    AiForceCalm = ModifierStateShift.AiForceCalm / 32,
    DroneAttached = ModifierStateShift.DroneAttached / 32,
    AimForward = ModifierStateShift.AimForward / 32,
    AimForwardWithPitch = ModifierStateShift.AimForwardWithPitch / 32,
    ZiplineDisabled = ModifierStateShift.ZiplineDisabled / 32,
    ZiplineIntro = ModifierStateShift.ZiplineIntro / 32,
    RespawnCredit = ModifierStateShift.RespawnCredit / 32,
    RespawnCreditPersonal = ModifierStateShift.RespawnCreditPersonal / 32,
    DisplayLifetime = ModifierStateShift.DisplayLifetime / 32,
    MeleeDisabled = ModifierStateShift.MeleeDisabled / 32,
    MeleeDisabledDebuff = ModifierStateShift.MeleeDisabledDebuff / 32,
    Glitched = ModifierStateShift.Glitched / 32,
    SlidingDisabled = ModifierStateShift.SlidingDisabled / 32,
    SlidingForcedTry = ModifierStateShift.SlidingForcedTry / 32,
    ReloadDisabled = ModifierStateShift.ReloadDisabled / 32,
    ReloadMeleeFullSpeed = ModifierStateShift.ReloadMeleeFullSpeed / 32,
    ManualReloadDisabled = ModifierStateShift.ManualReloadDisabled / 32,
    UnitStatusHealthHidden = ModifierStateShift.UnitStatusHealthHidden / 32,
    UnitStatusHidden = ModifierStateShift.UnitStatusHidden / 32,
    FadeAlphaToZero = ModifierStateShift.FadeAlphaToZero / 32,
    FriendlyFireEnabled = ModifierStateShift.FriendlyFireEnabled / 32,
    Flying = ModifierStateShift.Flying / 32,
    Scoped = ModifierStateShift.Scoped / 32,
    RollingBall = ModifierStateShift.RollingBall / 32,
    ViscousCubed = ModifierStateShift.ViscousCubed / 32,
    SlowImmune = ModifierStateShift.SlowImmune / 32,
    RootImmune = ModifierStateShift.RootImmune / 32,
    IsMeleeTarget = ModifierStateShift.IsMeleeTarget / 32,
    GlowToAlliesAsEnemy = ModifierStateShift.GlowToAlliesAsEnemy / 32,
    LadderDisable = ModifierStateShift.LadderDisable / 32,
    IgnoreBullets = ModifierStateShift.IgnoreBullets / 32,
    IgnoreMelee = ModifierStateShift.IgnoreMelee / 32,
    HideCrosshair = ModifierStateShift.HideCrosshair / 32,
    HideStamina = ModifierStateShift.HideStamina / 32,
    HideAmmo = ModifierStateShift.HideAmmo / 32,
    ApplyVerticalDrag = ModifierStateShift.ApplyVerticalDrag / 32,
    NoWindup = ModifierStateShift.NoWindup / 32,
    NoShootlockoutOnJump = ModifierStateShift.NoShootlockoutOnJump / 32,
    TitanLaserValidTarget = ModifierStateShift.TitanLaserValidTarget / 32,
    HasHollowPointBullets = ModifierStateShift.HasHollowPointBullets / 32,
    SuppressAirDrag = ModifierStateShift.SuppressAirDrag / 32,
    Icebeaming = ModifierStateShift.Icebeaming / 32,
    PowerSlashing = ModifierStateShift.PowerSlashing / 32,
    InAllySmoke = ModifierStateShift.InAllySmoke / 32,
    InEnemySmoke = ModifierStateShift.InEnemySmoke / 32,
    NoOutgoingDamage = ModifierStateShift.NoOutgoingDamage / 32,
    NoIncomingDamage = ModifierStateShift.NoIncomingDamage / 32,
    ChronoSwapping = ModifierStateShift.ChronoSwapping / 32,
    BouncePadStomping = ModifierStateShift.BouncePadStomping / 32,
    FlyingBeetleTarget = ModifierStateShift.FlyingBeetleTarget / 32,
    DreamweaverSleep = ModifierStateShift.DreamweaverSleep / 32,
    PickingUpIdol = ModifierStateShift.PickingUpIdol / 32,
    HoldingIdol = ModifierStateShift.HoldingIdol / 32,
    ReturnIdolArea = ModifierStateShift.ReturnIdolArea / 32,
    ReturningIdol = ModifierStateShift.ReturningIdol / 32,
    DropIdol = ModifierStateShift.DropIdol / 32,
    GalvanicStormBuff = ModifierStateShift.GalvanicStormBuff / 32,
    InAbilityAllowZoom = ModifierStateShift.InAbilityAllowZoom / 32,
    InAbilityDisableZoom = ModifierStateShift.InAbilityDisableZoom / 32,
    StaminaRegenPaused = ModifierStateShift.StaminaRegenPaused / 32,
    Prematch = ModifierStateShift.Prematch / 32,
    SelfDestruct = ModifierStateShift.SelfDestruct / 32,
    CoopTetherActive = ModifierStateShift.CoopTetherActive / 32,
    CoopTetherLockedTarget = ModifierStateShift.CoopTetherLockedTarget / 32,
    LockedAimAngles = ModifierStateShift.LockedAimAngles / 32,
    Icepathing = ModifierStateShift.Icepathing / 32,
    BackdoorProtected = ModifierStateShift.BackdoorProtected / 32,
    InCombat = ModifierStateShift.InCombat / 32,
    DashjumpStaminaFree = ModifierStateShift.DashjumpStaminaFree / 32,
    YamatoShadowForm = ModifierStateShift.YamatoShadowForm / 32,
    InMidBossPit = ModifierStateShift.InMidBossPit / 32,
    CastsIgnoreChanneling = ModifierStateShift.CastsIgnoreChanneling / 32,
    AllowMeleeWhenChanneling = ModifierStateShift.AllowMeleeWhenChanneling / 32,
    AllowDashWhenChanneling = ModifierStateShift.AllowDashWhenChanneling / 32,
    CanActiveReload = ModifierStateShift.CanActiveReload / 32,
    DiggerBurrowChannel = ModifierStateShift.DiggerBurrowChannel / 32,
    DiggerSpin = ModifierStateShift.DiggerSpin / 32,
    NearRejuvinator = ModifierStateShift.NearRejuvinator / 32,
    NearPunchableItem = ModifierStateShift.NearPunchableItem / 32,
    InTier3Phase2BossPit = ModifierStateShift.InTier3Phase2BossPit / 32,
    TakesFulldamageNoFalloff = ModifierStateShift.TakesFulldamageNoFalloff / 32,
    PredatoryStatueTarget = ModifierStateShift.PredatoryStatueTarget / 32,
    FootstepSoundsDisable = ModifierStateShift.FootstepSoundsDisable / 32,
    MovementFoleySoundsDisable = ModifierStateShift.MovementFoleySoundsDisable / 32,
    DoNotDrawModel = ModifierStateShift.DoNotDrawModel / 32,
    ShivFrenzied = ModifierStateShift.ShivFrenzied / 32,
    ForceAnimDuck = ModifierStateShift.ForceAnimDuck / 32,
    ForceAnimKeepStill = ModifierStateShift.ForceAnimKeepStill / 32,
    SiphonBulletLoss = ModifierStateShift.SiphonBulletLoss / 32,
    ApplyFovMouseSensitivityScale = ModifierStateShift.ApplyFovMouseSensitivityScale / 32,
    NearClimbableRope = ModifierStateShift.NearClimbableRope / 32,
    IsClimbingRope = ModifierStateShift.IsClimbingRope / 32,
    ForceCanParry = ModifierStateShift.ForceCanParry / 32,
    IsMagicShockImmune = ModifierStateShift.IsMagicShockImmune / 32,
    InSlideChargedMeleeAttack = ModifierStateShift.InSlideChargedMeleeAttack / 32,
    ForceGroundJump = ModifierStateShift.ForceGroundJump / 32,
    Frozen = ModifierStateShift.Frozen / 32,
    AssassinateLowhealthTarget = ModifierStateShift.AssassinateLowhealthTarget / 32,
    TeleporterDisabled = ModifierStateShift.TeleporterDisabled / 32,
    InSmallInteriorSpace = ModifierStateShift.InSmallInteriorSpace / 32,
    InMediumInteriorSpace = ModifierStateShift.InMediumInteriorSpace / 32,
    EnableCloakDesat = ModifierStateShift.EnableCloakDesat / 32,
    NanoRecentDamage = ModifierStateShift.NanoRecentDamage / 32,
    DisableInattack2Deselect = ModifierStateShift.DisableInattack2Deselect / 32,
    FathomDoNotRequireStandStillForInvis = ModifierStateShift.FathomDoNotRequireStandStillForInvis / 32,
    PulldownToGround = ModifierStateShift.PulldownToGround / 32,
    InSelfBubble = ModifierStateShift.InSelfBubble / 32,
    GunslingerMark = ModifierStateShift.GunslingerMark / 32,
    GhostLifedrained = ModifierStateShift.GhostLifedrained / 32,
    InvisibleOnMinimap = ModifierStateShift.InvisibleOnMinimap / 32,
    Ulting = ModifierStateShift.Ulting / 32,
    ViperVenom = ModifierStateShift.ViperVenom / 32,
    MagicianUlt = ModifierStateShift.MagicianUlt / 32,
    AnimOnGround = ModifierStateShift.AnimOnGround / 32,
    AbilityMovement = ModifierStateShift.AbilityMovement / 32,
    AbilityMovementDebuff = ModifierStateShift.AbilityMovementDebuff / 32,
    Count = ModifierStateShift.Count / 32,
    Invalid = ModifierStateShift.Invalid / 32,
  }

  public enum ModifierStateMask : uint
  {
    MaterialOverride = 1u << (ModifierStateShift.MaterialOverride % 32),
    EnteringVehicle = 1u << (ModifierStateShift.EnteringVehicle % 32),
    ExitingVehicle = 1u << (ModifierStateShift.ExitingVehicle % 32),
    UnrestrictedMovement = 1u << (ModifierStateShift.UnrestrictedMovement % 32),
    ForceAlertState = 1u << (ModifierStateShift.ForceAlertState % 32),
    DisableSquad = 1u << (ModifierStateShift.DisableSquad % 32),
    Immobilized = 1u << (ModifierStateShift.Immobilized % 32),
    Disarmed = 1u << (ModifierStateShift.Disarmed % 32),
    Muted = 1u << (ModifierStateShift.Muted % 32),
    Silenced = 1u << (ModifierStateShift.Silenced % 32),
    SilenceMovementAbilites = 1u << (ModifierStateShift.SilenceMovementAbilites % 32),
    SilencedHidden = 1u << (ModifierStateShift.SilencedHidden % 32),
    Stunned = 1u << (ModifierStateShift.Stunned % 32),
    Invulnerable = 1u << (ModifierStateShift.Invulnerable % 32),
    TechInvulnerable = 1u << (ModifierStateShift.TechInvulnerable % 32),
    TechDamageInvulnerable = 1u << (ModifierStateShift.TechDamageInvulnerable % 32),
    TechUntargetableByEnemies = 1u << (ModifierStateShift.TechUntargetableByEnemies % 32),
    StatusImmune = 1u << (ModifierStateShift.StatusImmune % 32),
    Unstoppable = 1u << (ModifierStateShift.Unstoppable % 32),
    OutOfGame = 1u << (ModifierStateShift.OutOfGame % 32),
    CommandRestricted = 1u << (ModifierStateShift.CommandRestricted % 32),
    Charging = 1u << (ModifierStateShift.Charging % 32),
    Obscured = 1u << (ModifierStateShift.Obscured % 32),
    InvisibleToEnemy = 1u << (ModifierStateShift.InvisibleToEnemy % 32),
    InvisibleToEnemyCast = 1u << (ModifierStateShift.InvisibleToEnemyCast % 32),
    IgnoredByNpcTargeting = 1u << (ModifierStateShift.IgnoredByNpcTargeting % 32),
    NpcTargetableWhileInvulnerable = 1u << (ModifierStateShift.NpcTargetableWhileInvulnerable % 32),
    Sprinting = 1u << (ModifierStateShift.Sprinting % 32),
    Unkillable = 1u << (ModifierStateShift.Unkillable % 32),
    GroundDashing = 1u << (ModifierStateShift.GroundDashing % 32),
    AdditionalAirMoves = 1u << (ModifierStateShift.AdditionalAirMoves % 32),
    UnlimitedAirDashes = 1u << (ModifierStateShift.UnlimitedAirDashes % 32),
    AirDashing = 1u << (ModifierStateShift.AirDashing % 32),
    MovementAbilityRestricted = 1u << (ModifierStateShift.MovementAbilityRestricted % 32),
    SprintNoInterrupt = 1u << (ModifierStateShift.SprintNoInterrupt % 32),
    SprintDisabled = 1u << (ModifierStateShift.SprintDisabled % 32),
    InShop = 1u << (ModifierStateShift.InShop % 32),
    InFountain = 1u << (ModifierStateShift.InFountain % 32),
    InEnemyBase = 1u << (ModifierStateShift.InEnemyBase % 32),
    InFriendlyBase = 1u << (ModifierStateShift.InFriendlyBase % 32),
    InMidBossTemple = 1u << (ModifierStateShift.InMidBossTemple % 32),
    InShoptunnelSapphire = 1u << (ModifierStateShift.InShoptunnelSapphire % 32),
    InShoptunnelAmber = 1u << (ModifierStateShift.InShoptunnelAmber % 32),
    DashDisabled = 1u << (ModifierStateShift.DashDisabled % 32),
    DashDisabledDebuff = 1u << (ModifierStateShift.DashDisabledDebuff % 32),
    Burning = 1u << (ModifierStateShift.Burning % 32),
    HealthRegenDisabled = 1u << (ModifierStateShift.HealthRegenDisabled % 32),
    HealthExternalRegenDisabled = 1u << (ModifierStateShift.HealthExternalRegenDisabled % 32),
    HealingDisabled = 1u << (ModifierStateShift.HealingDisabled % 32),
    DamageMovementPenaltyImmune = 1u << (ModifierStateShift.DamageMovementPenaltyImmune % 32),
    BusyWithAction = 1u << (ModifierStateShift.BusyWithAction % 32),
    Slowed = 1u << (ModifierStateShift.Slowed % 32),
    ShootingDisabled = 1u << (ModifierStateShift.ShootingDisabled % 32),
    ShootingForcedOn = 1u << (ModifierStateShift.ShootingForcedOn % 32),
    MeleeAttacksOnly = 1u << (ModifierStateShift.MeleeAttacksOnly % 32),
    Sliding = 1u << (ModifierStateShift.Sliding % 32),
    VisibleToEnemy = 1u << (ModifierStateShift.VisibleToEnemy % 32),
    InfiniteClip = 1u << (ModifierStateShift.InfiniteClip % 32),
    KnockdownImmune = 1u << (ModifierStateShift.KnockdownImmune % 32),
    JumpDisabled = 1u << (ModifierStateShift.JumpDisabled % 32),
    DuckingDisabled = 1u << (ModifierStateShift.DuckingDisabled % 32),
    DuckingForced = 1u << (ModifierStateShift.DuckingForced % 32),
    AirDuckingForced = 1u << (ModifierStateShift.AirDuckingForced % 32),
    IsAsleep = 1u << (ModifierStateShift.IsAsleep % 32),
    GlowThroughWallsToEnemy = 1u << (ModifierStateShift.GlowThroughWallsToEnemy % 32),
    GlowThroughWallsToProvider = 1u << (ModifierStateShift.GlowThroughWallsToProvider % 32),
    GlowThroughWallsToProviderTeammatesWithinRange = 1u << (ModifierStateShift.GlowThroughWallsToProviderTeammatesWithinRange % 32),
    GlowToProvider = 1u << (ModifierStateShift.GlowToProvider % 32),
    GlowInLosToEnemy = 1u << (ModifierStateShift.GlowInLosToEnemy % 32),
    DisableAirSpreadPenalty = 1u << (ModifierStateShift.DisableAirSpreadPenalty % 32),
    UsingZipline = 1u << (ModifierStateShift.UsingZipline % 32),
    InAlternateDimension = 1u << (ModifierStateShift.InAlternateDimension % 32),
    AnimgraphControlledMovement = 1u << (ModifierStateShift.AnimgraphControlledMovement % 32),
    CombatAbilitiesDisabled = 1u << (ModifierStateShift.CombatAbilitiesDisabled % 32),
    Chained = 1u << (ModifierStateShift.Chained % 32),
    AllArmorDisabled = 1u << (ModifierStateShift.AllArmorDisabled % 32),
    BulletInvulnerable = 1u << (ModifierStateShift.BulletInvulnerable % 32),
    BreakIcePath = 1u << (ModifierStateShift.BreakIcePath % 32),
    ReflectAttacks = 1u << (ModifierStateShift.ReflectAttacks % 32),
    MantleDisabled = 1u << (ModifierStateShift.MantleDisabled % 32),
    AiForceAggro = 1u << (ModifierStateShift.AiForceAggro % 32),
    AiForceCalm = 1u << (ModifierStateShift.AiForceCalm % 32),
    DroneAttached = 1u << (ModifierStateShift.DroneAttached % 32),
    AimForward = 1u << (ModifierStateShift.AimForward % 32),
    AimForwardWithPitch = 1u << (ModifierStateShift.AimForwardWithPitch % 32),
    ZiplineDisabled = 1u << (ModifierStateShift.ZiplineDisabled % 32),
    ZiplineIntro = 1u << (ModifierStateShift.ZiplineIntro % 32),
    RespawnCredit = 1u << (ModifierStateShift.RespawnCredit % 32),
    RespawnCreditPersonal = 1u << (ModifierStateShift.RespawnCreditPersonal % 32),
    DisplayLifetime = 1u << (ModifierStateShift.DisplayLifetime % 32),
    MeleeDisabled = 1u << (ModifierStateShift.MeleeDisabled % 32),
    MeleeDisabledDebuff = 1u << (ModifierStateShift.MeleeDisabledDebuff % 32),
    Glitched = 1u << (ModifierStateShift.Glitched % 32),
    SlidingDisabled = 1u << (ModifierStateShift.SlidingDisabled % 32),
    SlidingForcedTry = 1u << (ModifierStateShift.SlidingForcedTry % 32),
    ReloadDisabled = 1u << (ModifierStateShift.ReloadDisabled % 32),
    ReloadMeleeFullSpeed = 1u << (ModifierStateShift.ReloadMeleeFullSpeed % 32),
    ManualReloadDisabled = 1u << (ModifierStateShift.ManualReloadDisabled % 32),
    UnitStatusHealthHidden = 1u << (ModifierStateShift.UnitStatusHealthHidden % 32),
    UnitStatusHidden = 1u << (ModifierStateShift.UnitStatusHidden % 32),
    FadeAlphaToZero = 1u << (ModifierStateShift.FadeAlphaToZero % 32),
    FriendlyFireEnabled = 1u << (ModifierStateShift.FriendlyFireEnabled % 32),
    Flying = 1u << (ModifierStateShift.Flying % 32),
    Scoped = 1u << (ModifierStateShift.Scoped % 32),
    RollingBall = 1u << (ModifierStateShift.RollingBall % 32),
    ViscousCubed = 1u << (ModifierStateShift.ViscousCubed % 32),
    SlowImmune = 1u << (ModifierStateShift.SlowImmune % 32),
    RootImmune = 1u << (ModifierStateShift.RootImmune % 32),
    IsMeleeTarget = 1u << (ModifierStateShift.IsMeleeTarget % 32),
    GlowToAlliesAsEnemy = 1u << (ModifierStateShift.GlowToAlliesAsEnemy % 32),
    LadderDisable = 1u << (ModifierStateShift.LadderDisable % 32),
    IgnoreBullets = 1u << (ModifierStateShift.IgnoreBullets % 32),
    IgnoreMelee = 1u << (ModifierStateShift.IgnoreMelee % 32),
    HideCrosshair = 1u << (ModifierStateShift.HideCrosshair % 32),
    HideStamina = 1u << (ModifierStateShift.HideStamina % 32),
    HideAmmo = 1u << (ModifierStateShift.HideAmmo % 32),
    ApplyVerticalDrag = 1u << (ModifierStateShift.ApplyVerticalDrag % 32),
    NoWindup = 1u << (ModifierStateShift.NoWindup % 32),
    NoShootlockoutOnJump = 1u << (ModifierStateShift.NoShootlockoutOnJump % 32),
    TitanLaserValidTarget = 1u << (ModifierStateShift.TitanLaserValidTarget % 32),
    HasHollowPointBullets = 1u << (ModifierStateShift.HasHollowPointBullets % 32),
    SuppressAirDrag = 1u << (ModifierStateShift.SuppressAirDrag % 32),
    Icebeaming = 1u << (ModifierStateShift.Icebeaming % 32),
    PowerSlashing = 1u << (ModifierStateShift.PowerSlashing % 32),
    InAllySmoke = 1u << (ModifierStateShift.InAllySmoke % 32),
    InEnemySmoke = 1u << (ModifierStateShift.InEnemySmoke % 32),
    NoOutgoingDamage = 1u << (ModifierStateShift.NoOutgoingDamage % 32),
    NoIncomingDamage = 1u << (ModifierStateShift.NoIncomingDamage % 32),
    ChronoSwapping = 1u << (ModifierStateShift.ChronoSwapping % 32),
    BouncePadStomping = 1u << (ModifierStateShift.BouncePadStomping % 32),
    FlyingBeetleTarget = 1u << (ModifierStateShift.FlyingBeetleTarget % 32),
    DreamweaverSleep = 1u << (ModifierStateShift.DreamweaverSleep % 32),
    PickingUpIdol = 1u << (ModifierStateShift.PickingUpIdol % 32),
    HoldingIdol = 1u << (ModifierStateShift.HoldingIdol % 32),
    ReturnIdolArea = 1u << (ModifierStateShift.ReturnIdolArea % 32),
    ReturningIdol = 1u << (ModifierStateShift.ReturningIdol % 32),
    DropIdol = 1u << (ModifierStateShift.DropIdol % 32),
    GalvanicStormBuff = 1u << (ModifierStateShift.GalvanicStormBuff % 32),
    InAbilityAllowZoom = 1u << (ModifierStateShift.InAbilityAllowZoom % 32),
    InAbilityDisableZoom = 1u << (ModifierStateShift.InAbilityDisableZoom % 32),
    StaminaRegenPaused = 1u << (ModifierStateShift.StaminaRegenPaused % 32),
    Prematch = 1u << (ModifierStateShift.Prematch % 32),
    SelfDestruct = 1u << (ModifierStateShift.SelfDestruct % 32),
    CoopTetherActive = 1u << (ModifierStateShift.CoopTetherActive % 32),
    CoopTetherLockedTarget = 1u << (ModifierStateShift.CoopTetherLockedTarget % 32),
    LockedAimAngles = 1u << (ModifierStateShift.LockedAimAngles % 32),
    Icepathing = 1u << (ModifierStateShift.Icepathing % 32),
    BackdoorProtected = 1u << (ModifierStateShift.BackdoorProtected % 32),
    InCombat = 1u << (ModifierStateShift.InCombat % 32),
    DashjumpStaminaFree = 1u << (ModifierStateShift.DashjumpStaminaFree % 32),
    YamatoShadowForm = 1u << (ModifierStateShift.YamatoShadowForm % 32),
    InMidBossPit = 1u << (ModifierStateShift.InMidBossPit % 32),
    CastsIgnoreChanneling = 1u << (ModifierStateShift.CastsIgnoreChanneling % 32),
    AllowMeleeWhenChanneling = 1u << (ModifierStateShift.AllowMeleeWhenChanneling % 32),
    AllowDashWhenChanneling = 1u << (ModifierStateShift.AllowDashWhenChanneling % 32),
    CanActiveReload = 1u << (ModifierStateShift.CanActiveReload % 32),
    DiggerBurrowChannel = 1u << (ModifierStateShift.DiggerBurrowChannel % 32),
    DiggerSpin = 1u << (ModifierStateShift.DiggerSpin % 32),
    NearRejuvinator = 1u << (ModifierStateShift.NearRejuvinator % 32),
    NearPunchableItem = 1u << (ModifierStateShift.NearPunchableItem % 32),
    InTier3Phase2BossPit = 1u << (ModifierStateShift.InTier3Phase2BossPit % 32),
    TakesFulldamageNoFalloff = 1u << (ModifierStateShift.TakesFulldamageNoFalloff % 32),
    PredatoryStatueTarget = 1u << (ModifierStateShift.PredatoryStatueTarget % 32),
    FootstepSoundsDisable = 1u << (ModifierStateShift.FootstepSoundsDisable % 32),
    MovementFoleySoundsDisable = 1u << (ModifierStateShift.MovementFoleySoundsDisable % 32),
    DoNotDrawModel = 1u << (ModifierStateShift.DoNotDrawModel % 32),
    ShivFrenzied = 1u << (ModifierStateShift.ShivFrenzied % 32),
    ForceAnimDuck = 1u << (ModifierStateShift.ForceAnimDuck % 32),
    ForceAnimKeepStill = 1u << (ModifierStateShift.ForceAnimKeepStill % 32),
    SiphonBulletLoss = 1u << (ModifierStateShift.SiphonBulletLoss % 32),
    ApplyFovMouseSensitivityScale = 1u << (ModifierStateShift.ApplyFovMouseSensitivityScale % 32),
    NearClimbableRope = 1u << (ModifierStateShift.NearClimbableRope % 32),
    IsClimbingRope = 1u << (ModifierStateShift.IsClimbingRope % 32),
    ForceCanParry = 1u << (ModifierStateShift.ForceCanParry % 32),
    IsMagicShockImmune = 1u << (ModifierStateShift.IsMagicShockImmune % 32),
    InSlideChargedMeleeAttack = 1u << (ModifierStateShift.InSlideChargedMeleeAttack % 32),
    ForceGroundJump = 1u << (ModifierStateShift.ForceGroundJump % 32),
    Frozen = 1u << (ModifierStateShift.Frozen % 32),
    AssassinateLowhealthTarget = 1u << (ModifierStateShift.AssassinateLowhealthTarget % 32),
    TeleporterDisabled = 1u << (ModifierStateShift.TeleporterDisabled % 32),
    InSmallInteriorSpace = 1u << (ModifierStateShift.InSmallInteriorSpace % 32),
    InMediumInteriorSpace = 1u << (ModifierStateShift.InMediumInteriorSpace % 32),
    EnableCloakDesat = 1u << (ModifierStateShift.EnableCloakDesat % 32),
    NanoRecentDamage = 1u << (ModifierStateShift.NanoRecentDamage % 32),
    DisableInattack2Deselect = 1u << (ModifierStateShift.DisableInattack2Deselect % 32),
    FathomDoNotRequireStandStillForInvis = 1u << (ModifierStateShift.FathomDoNotRequireStandStillForInvis % 32),
    PulldownToGround = 1u << (ModifierStateShift.PulldownToGround % 32),
    InSelfBubble = 1u << (ModifierStateShift.InSelfBubble % 32),
    GunslingerMark = 1u << (ModifierStateShift.GunslingerMark % 32),
    GhostLifedrained = 1u << (ModifierStateShift.GhostLifedrained % 32),
    InvisibleOnMinimap = 1u << (ModifierStateShift.InvisibleOnMinimap % 32),
    Ulting = 1u << (ModifierStateShift.Ulting % 32),
    ViperVenom = 1u << (ModifierStateShift.ViperVenom % 32),
    MagicianUlt = 1u << (ModifierStateShift.MagicianUlt % 32),
    AnimOnGround = 1u << (ModifierStateShift.AnimOnGround % 32),
    AbilityMovement = 1u << (ModifierStateShift.AbilityMovement % 32),
    AbilityMovementDebuff = 1u << (ModifierStateShift.AbilityMovementDebuff % 32),
    Count = 1u << (ModifierStateShift.Count % 32),
    Invalid = 1u << (ModifierStateShift.Invalid % 32),
  }

  public enum ETrooperRelevantState
  {
    None = 1,
    UsingZipline = 2,
    SelfDestruct = 3,
  }

  public enum EUnifiedTrooperState
  {
    NonStarter = -1,

    Packed_WaitingToZipline = 1,
    Packed_Ziplining = 2,
    Packed_DroppingOffZipline = 3,
    Packed_DeadBuggedFellOutOfWorld = 4,
    Unpacked_InactiveBuggedFellOutOfWorld = 5,
    Unpacked_Active = 6,
    Unpacked_SelfDestructing = 7,
    Unpacked_Dead = 8,
  }

  public enum EUrnDropoffSpot
  {
    YellowAmber = 1,
    GreenAmber = 2,
    YellowMiddle = 3,
    GreenMiddle = 4,
    YellowSapphire = 5,
    GreenSapphire = 6,
  }

  public enum EUrnPickupSpot
  {
    YellowMiddle = EUrnDropoffSpot.YellowMiddle,
    GreenMiddle = EUrnDropoffSpot.GreenMiddle,
  }

  public enum EUrnDropoffSpotState
  {
    Inactive = 1,
    ActiveForDroppedUrn = 2,
    ActiveForAmber = 3,
    ActiveForSapphire = 4,
  }

  public enum EDroppedUrnType
  {
    NeverPickedUp = 1,
    DroppedByAmber = 2,
    DroppedBySapphire = 3,
  }

  public enum EPlayerUrnState
  {
    NotHolding = 1,
    NotHoldingButPickingUp = 2,
    Holding = 3,
    HoldingAndReturning = 4,
  }

  public enum EUnifiedUrnState
  {
    Descending = 1,
    WaitingForFirstPickup = 2,
    PickedUp = 3,
    DroppedSpawning = 4,
    DroppedSpawned = 5,
    Returning = 6,
    Returned = 7,
  }
}
