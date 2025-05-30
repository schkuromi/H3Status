const globalConfig = {
  webSocketPort: 9504,
  overlayScale: 1,

  showScorePanel: true,

  showEventLog: true,
  showScoreCounter: true,
  showHealth: true,
  showLevelBar: true,
  showPhaseBar: true,

  eventLogLength: 6, // How many items can appear in the log
  eventLogLifeTime: 4, // How long items will stay in the log (seconds)
  eventLogDelay: 0.1, // Pause between adding items to the log (seconds)
  pauseEventLog: false, // Pause log instead of overflowing
  scoreCounterDigits: 7,

  showAmmoPanel: true,

  showWeaponName: true,
  showAmmoCounter: true,
};
