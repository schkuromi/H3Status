using HarmonyLib;
using SimpleJSON;
// using System.Text;
using FistVR;

namespace H3Status.Patches
{

    internal static class VersionHandler
    {
        public static JSONObject GetVersionInfo()
        {
            var versionJSON = new JSONObject();

            versionJSON["version"] = Plugin.Version;

            return versionJSON;
        }
    }

    [HarmonyPatch]
    internal static class SceneHandler
    {
        public static string activeScene = string.Empty;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SteamVR_LoadLevel), nameof(SteamVR_LoadLevel.Begin))]
        private static void LoadLevel(string levelName)
        {
            activeScene = levelName;

            var sceneEventJSON = new JSONObject();

            sceneEventJSON["type"] = "sceneEvent";
            var sceneJSON = sceneEventJSON["status"].AsObject;
            sceneJSON["name"] = levelName;

            Server.ServerBehavior.SendMessage(sceneEventJSON);
        }
    }

    [HarmonyPatch]
    internal static class TNHPhaseHandler
    {
        private static string[] holdNamesInstitution = {"HUB", "LIBRARY", "GARDEN", "ATRIUM", "LOBBY", "HEDRONS", "TURBINE", "HYDRO", "SPILLWAY", "RODS", "STORAGE", "APRROACH", "CROSSOVER", "PIPEWORKS", "VOID", "CONCOURSE", "BUNKER", "INCLINATOR", "ABYSS", "SUBSTATION"};
        private static string[] supplyNamesInstitution = { "ARRAY", "STUDIO", "SUITE", "LOFT", "PENTHOUSE", "GREENWALL", "FENESTRA", "JUDGEMENT", "PRESIDIO", "DISSONANCE", "CLERESTORY", "HELIX", "FACILITY", "STACKS", "ALTAR", "PUMP" };

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TNH_Manager), nameof(TNH_Manager.SetPhase))]
        private static void TakeAndHoldPhase(TNH_Phase p, TNH_Manager __instance)
        {
            var levelEventJSON = new JSONObject();

            levelEventJSON["type"] = "TNHPhaseEvent";
            var levelJSON = levelEventJSON["status"].AsObject;
            levelJSON["phase"] = p.ToString();
            levelJSON["level"] = __instance.m_level;
            levelJSON["count"] = __instance.m_maxLevels;
            levelJSON["seed"] = __instance.m_seed;
            levelJSON["hold"] = __instance.m_curHoldIndex;
            var supplyPointsJSON = levelJSON["supply"].AsArray;
            foreach(int i in __instance.m_activeSupplyPointIndicies)
            {
                supplyPointsJSON.Add(i);
            }

            if (SceneHandler.activeScene == "Institution")
            {
                levelJSON["holdName"] = holdNamesInstitution[__instance.m_curHoldIndex];
                var supplyPointNamesJSON = levelJSON["supplyNames"].AsArray;
                foreach(int i in __instance.m_activeSupplyPointIndicies)
                {
                    supplyPointNamesJSON.Add(supplyNamesInstitution[i]);
                }
            }

            Server.ServerBehavior.SendMessage(levelEventJSON);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TNH_HoldPoint), nameof(TNH_HoldPoint.BeginAnalyzing))]
        [HarmonyPatch(typeof(TNH_HoldPoint), nameof(TNH_HoldPoint.IdentifyEncryption))]
        [HarmonyPatch(typeof(TNH_HoldPoint), nameof(TNH_HoldPoint.CompletePhase))]
        private static void HoldPhase(TNH_HoldPoint __instance)
        {
            var phaseEventJSON = new JSONObject();

            phaseEventJSON["type"] = "TNHHoldPhaseEvent";
            var phaseJSON = phaseEventJSON["status"].AsObject;
            phaseJSON["phase"] = __instance.m_state.ToString();
            phaseJSON["level"] = __instance.m_phaseIndex;
            phaseJSON["count"] = __instance.H.Phases.Count;
            phaseJSON["encryption"] = __instance.m_curPhase.Encryption.ToString();

            Server.ServerBehavior.SendMessage(phaseEventJSON);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TNH_Manager), nameof(TNH_Manager.OnSosigAlert))]
        private static void OnSosigAlert(TNH_Manager __instance)
        {
            if (__instance.Phase != TNH_Phase.Take) return;
            if (__instance.m_alertedThisPhaseFlag) return;

            var bonusEventJSON = new JSONObject();
            bonusEventJSON["type"] = "TNHLostStealthBonus";
            Server.ServerBehavior.SendMessage(bonusEventJSON);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TNH_Manager), nameof(TNH_Manager.OnSosigKill))]
        private static void OnSosigKill(Sosig s, TNH_Manager __instance)
        {
            if (__instance.m_hasGuardBeenKilledThatWasAltered) return;

            if (s.GetDiedFromIFF() == GM.CurrentPlayerBody.GetPlayerIFF() &&
                __instance.Phase == TNH_Phase.Take &&
                s.HasEverBeenAlerted())
            {
                var bonusEventJSON = new JSONObject();
                bonusEventJSON["type"] = "TNHLostStealthBonus";
                Server.ServerBehavior.SendMessage(bonusEventJSON);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TNH_Manager), nameof(TNH_Manager.PlayerTookDamage))]
        private static void TakePlayerTookDamageTake(TNH_Manager __instance)
        {
            if (__instance.m_tookDamageThisPhaseFlag) return;

            var bonusEventJSON = new JSONObject();
            bonusEventJSON["type"] = "TNHLostNoHitBonus";
            Server.ServerBehavior.SendMessage(bonusEventJSON);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TNH_HoldPoint), nameof(TNH_HoldPoint.PlayerTookDamage))]
        private static void PlayerTookDamageHold(TNH_HoldPoint __instance)
        {
            if (__instance.m_hasBeenDamagedThisPhase && __instance.m_hasBeenDamagedThisHold) return;

            var bonusEventJSON = new JSONObject();
            bonusEventJSON["type"] = "TNHLostNoHitBonus";
            Server.ServerBehavior.SendMessage(bonusEventJSON);
        }
    }

    [HarmonyPatch]
    internal static class TNHScoreHandler
    {
        private static int[] eventMultiplier = { 5000, 5, 1000, 1000, 50, 100, 50, 50, 50, 50, 25, 500, 1000, 50, 500 };

        private static int GetEventScore(TNH_Manager.ScoringEvent ev, int num)
        {
            return num * eventMultiplier[(int)ev];
        }

        private static int GetTotalScore()
        {
            int score = 0;
            int multiplier = GetMultiplier();

            for (int i = 0; i <= 14; i++)
            {
                score += GetEventScore((TNH_Manager.ScoringEvent)i, GM.TNH_Manager.Nums[i]);
            }

            return score * multiplier;
        }

        private static int GetMultiplier()
        {
            int multiplier = 1;

            if (GM.TNHOptions.TargetModeSetting == TNHSetting_TargetMode.AllTypes)
            {
                multiplier += 3;
            }
            else if (GM.TNHOptions.TargetModeSetting == TNHSetting_TargetMode.Simple)
            {
                multiplier += 2;
            }
            if (GM.TNHOptions.AIDifficultyModifier == TNHModifier_AIDifficulty.Standard)
            {
                multiplier += 3;
            }
            if (GM.TNHOptions.RadarModeModifier == TNHModifier_RadarMode.Standard)
            {
                multiplier += 2;
            }
            else if (GM.TNHOptions.RadarModeModifier != TNHModifier_RadarMode.Omnipresent)
            {
                multiplier += 3;
            }

            return multiplier;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TNH_Manager), nameof(TNH_Manager.IncrementScoringStat))]
        private static void IncrementScoringStat(TNH_Manager.ScoringEvent ev, int num, TNH_Manager __instance)
        {
            int baseScore = __instance.ReturnSummedBaseScore();

            Plugin.Logger.LogInfo($"{ev}: {GetEventScore(ev, num) * GetMultiplier()} ({GetEventScore(ev, num)}x{GetMultiplier()})");
            // Plugin.Logger.LogInfo(GetTotalScore());

            var scoreEventJSON = new JSONObject();

            scoreEventJSON["type"] = "TNHScoreEvent";
            var scoreJSON = scoreEventJSON["status"].AsObject;
            scoreJSON["type"] = ev.ToString();
            scoreJSON["value"] = GetEventScore(ev, num);
            scoreJSON["mult"] = GetMultiplier();
            scoreJSON["score"] = GetTotalScore();

            Server.ServerBehavior.SendMessage(scoreEventJSON);
        }
    }

    [HarmonyPatch]
    internal static class PlayerHealthHandler
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FVRPlayerBody), nameof(FVRPlayerBody.RegisterPlayerHit))]
        private static void RegisterPlayerHit(float DamagePoints, bool FromSelf, int iff, FVRPlayerBody __instance)
        {
            var healthEventJSON = new JSONObject();

            healthEventJSON["type"] = "playerDamage";
            var healthJSON = healthEventJSON["status"].AsObject;
            healthJSON["amount"] = DamagePoints;
            healthJSON["health"] = __instance.Health;
            healthJSON["maxHealth"] = __instance.m_startingHealth;

            Server.ServerBehavior.SendMessage(healthEventJSON);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FVRPlayerBody), nameof(FVRPlayerBody.HarmPercent))]
        private static void HarmPercent(float f, FVRPlayerBody __instance)
        {
            var healthEventJSON = new JSONObject();

            healthEventJSON["type"] = "playerDamage";
            var healthJSON = healthEventJSON["status"].AsObject;
            healthJSON["amount"] = -(__instance.m_startingHealth * f);
            healthJSON["health"] = __instance.Health;
            healthJSON["maxHealth"] = __instance.m_startingHealth;

            Server.ServerBehavior.SendMessage(healthEventJSON);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FVRPlayerBody), nameof(FVRPlayerBody.HealPercent))]
        private static void HealPercent(float f, FVRPlayerBody __instance)
        {
            var healthEventJSON = new JSONObject();

            healthEventJSON["type"] = "playerHeal";
            var healthJSON = healthEventJSON["status"].AsObject;
            healthJSON["amount"] = __instance.m_startingHealth * f;
            healthJSON["health"] = __instance.Health;
            healthJSON["maxHealth"] = __instance.m_startingHealth;

            Server.ServerBehavior.SendMessage(healthEventJSON);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GM), nameof(GM.KillPlayer), new[] {typeof(bool), typeof(int)})]
        private static void KillPlayer(bool KilledSelf, int iff)
        {
            var healthEventJSON = new JSONObject();

            healthEventJSON["type"] = "playerKill";

            Server.ServerBehavior.SendMessage(healthEventJSON);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FVRPlayerBody), nameof(FVRPlayerBody.ActivatePower))]
        private static void ActivatePower(PowerupType type, PowerUpIntensity intensity, PowerUpDuration d, bool isPuke, bool isInverted, float DurationOverride = -1f)
        {
            float duration = 1f;

            switch (d)
           	{
               	case PowerUpDuration.Full:
              		duration = 30f;
              		break;
               	case PowerUpDuration.Short:
              		duration = 20f;
              		break;
               	case PowerUpDuration.VeryShort:
              		duration = 10f;
              		break;
               	case PowerUpDuration.Blip:
              		duration = 2f;
              		break;
               	case PowerUpDuration.SuperLong:
              		duration = 40f;
              		break;
           	}

            if (DurationOverride > 0f)
           	{
          		duration = DurationOverride;
           	}

            var powerEventJSON = new JSONObject();

            powerEventJSON["type"] = "playerBuff";
            var powerJSON = powerEventJSON["status"].AsObject;
            powerJSON["type"] = type.ToString();
            powerJSON["duration"] = duration;
            powerJSON["inverted"] = isInverted;

            Server.ServerBehavior.SendMessage(powerEventJSON);
        }
    }

    [HarmonyPatch]
    internal static class WeaponAmmoHandler
    {
        private static bool isUpdatePending = false;
        private static JSONObject ammoEventJSON = new JSONObject();

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FVRFireArm), nameof(FVRFireArm.FVRFixedUpdate))]
        private static void HandlePendingEvent()
        {
            if (isUpdatePending)
            {
                isUpdatePending = false;
                Server.ServerBehavior.SendMessage(ammoEventJSON);
            }
        }

        private static void UpdateAmmoCount(FVRFireArm fireArm)
        {
            if (fireArm == null || fireArm.m_hand == null) return;
            string weaponName = string.Empty;
            int? weaponHand = fireArm.m_hand.IsThisTheRightHand ? 1 : 0;
            string roundType = fireArm.RoundType.ToString();
            string roundClass = roundType;
            int currentAmmo = 0;
            int spentAmmo = 0;
            int maxCapacity = 0;

            try { roundClass = AM.GetDefaultRoundClass(fireArm.RoundType).ToString(); }
            catch { }

            if (fireArm.ObjectWrapper != null)
			{
				if (IM.HasSpawnedID(fireArm.ObjectWrapper.ItemID))
				{
					ItemSpawnerID spawnerID = IM.GetSpawnerID(fireArm.ObjectWrapper.ItemID);
					weaponName = spawnerID.DisplayName;
				}
				else
				{
					weaponName = fireArm.ObjectWrapper.DisplayName;
				}
			}

			if (fireArm.Magazine != null)
			{
                maxCapacity += fireArm.Magazine.m_capacity;
                currentAmmo += fireArm.Magazine.m_numRounds;

                if (fireArm.Magazine.LoadedRounds != null)
                {
                    for (int i = 0; i < fireArm.Magazine.LoadedRounds.Length; i++)
                    {
                        if (fireArm.Magazine.LoadedRounds[i] != null)
                        {
                            roundClass = fireArm.Magazine.LoadedRounds[i].LR_Class.ToString();
                        }
                    }
                }
            }

            if (fireArm.BeltDD != null)
            {
                currentAmmo += fireArm.BeltDD.m_roundsOnBelt;
            }

            if (fireArm.FChambers != null)
            {
                maxCapacity += fireArm.FChambers.Count;

                foreach (var chamber in fireArm.FChambers)
                {
                    if (chamber.m_round == null) continue;

                    if (chamber.IsSpent)
                    {
                        spentAmmo += 1;
                    }
                    else
                    {
                        roundClass = chamber.m_round.RoundClass.ToString();
                        currentAmmo += 1;
                    }
                }
            }

            ammoEventJSON["type"] = "ammoEvent";
            var ammoJSON = ammoEventJSON["status"].AsObject;
            ammoJSON["weapon"] = weaponName;
            ammoJSON["hand"] = weaponHand;
            ammoJSON["roundType"] = roundType;
            ammoJSON["roundClass"] = roundClass;
            ammoJSON["current"] = currentAmmo;
            ammoJSON["spent"] = spentAmmo;
            ammoJSON["capacity"] = maxCapacity;

            isUpdatePending = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.AddRound), new[] {typeof(FireArmRoundClass), typeof(bool), typeof(bool)})]
        [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.AddRound), new[] {typeof(FVRFireArmRound), typeof(bool), typeof(bool), typeof(bool)})]
        // [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.UpdateBulletDisplay))]
        private static void UpdateAmmoMagazine(FVRFireArmMagazine __instance)
        {
            UpdateAmmoCount(__instance.FireArm);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FVRFireArm), nameof(FVRFireArm.LoadMag))]
        [HarmonyPatch(typeof(FVRFireArm), nameof(FVRFireArm.EjectMag))]
        // [HarmonyPatch(typeof(FVRFireArm), nameof(FVRFireArm.LoadClip))]
        // [HarmonyPatch(typeof(FVRFireArm), nameof(FVRFireArm.EjectClip))]
        private static void UpdateAmmoFireArm(FVRFireArm __instance)
        {
            UpdateAmmoCount(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FVRFireArmChamber), nameof(FVRFireArmChamber.UpdateProxyDisplay))]
        private static void UpdateAmmoChamber(FVRFireArmChamber __instance)
        {
            if (__instance.Firearm != null)
            {
                UpdateAmmoCount(__instance.Firearm);
            }
            else
            {
                FVRFireArm fireArm = __instance.transform.parent?.gameObject.GetComponent<FVRFireArm>();
                if (fireArm != null)
                {
                    UpdateAmmoCount(fireArm);
                }
                else
                {
                    Plugin.Logger.LogError("Can't get chamber fireArm");
                }
            }
        }
    }

}
