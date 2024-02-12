﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace ValdyrSubnauticaMods
{
    [HarmonyPatch(typeof(DayNightCycle))]
    internal class DayNightCyclePatched
    {
        [HarmonyPatch(nameof(DayNightCycle.Update))]
        [HarmonyPrefix]
        public static void Update_Prefix(DayNightCycle __instance)
        {
            isInSkipTimeMode = __instance.IsInSkipTimeMode();
        }

        [HarmonyPatch(nameof(DayNightCycle.Update))]
        [HarmonyPostfix]
        public static void Update_Postfix(DayNightCycle __instance)
        {
            if (__instance.debugFreeze || __instance.dayNightSpeed == 0.0f)
            {
                return;
            }

            if (isInSkipTimeMode != __instance.IsInSkipTimeMode())
            {
                isInSkipTimeMode = __instance.IsInSkipTimeMode();
                DayNightCyclePatched.timePassedMod = __instance.timePassedAsDouble;
                return;
            }

            float timeSpeedMod = (__instance.IsDay())? DayNightCyclePatched.daySpeedMod : DayNightCyclePatched.nightSpeedMod;
            DayNightCyclePatched.timePassedMod += (double)Time.deltaTime * timeSpeedMod;
        }

        [HarmonyPatch(nameof(DayNightCycle.GetDay))]
        [HarmonyPrefix]
        public static bool GetDay_Prefix(ref double __result)
        {
            __result = DayNightCyclePatched.timePassedMod / 1200.0;
            return false;
        }

        [HarmonyPatch(nameof(DayNightCycle.GetDayScalar))]
        [HarmonyPrefix]
        public static bool GetDayScalar_Prefix(ref float __result)
        {
            __result = Mathf.Repeat((float)(UWE.Utils.Repeat(DayNightCyclePatched.timePassedMod, 1200.0) / 1200.0), 1f);
            return false;
        }

        [HarmonyPatch(nameof(DayNightCycle.SetDayNightTime))]
        [HarmonyPostfix]
        public static void SetDayNightTime_Postfix(DayNightCycle __instance, float scalar)
        {
            DayNightCyclePatched.timePassedMod = __instance.timePassedAsDouble;
        }

        [HarmonyPatch(nameof(DayNightCycle.GetTimeOfYear))]
        [HarmonyPrefix]
        public static bool GetTimeOfYear_Prefix(ref float __result)
        {
            double num = 10.0 * 1200.0;
            __result = Mathf.Repeat((float)(UWE.Utils.Repeat(DayNightCyclePatched.timePassedMod, num) / num), 1f);
            return false;
        }

        [HarmonyPatch(nameof(DayNightCycle.OnProtoSerialize))]
        [HarmonyPrefix]
        public static void OnProtoSerialize_Prefix(DayNightCycle __instance)
        {
            __instance.timePassedAsDouble = DayNightCyclePatched.timePassedMod;
        }

        [HarmonyPatch(nameof(DayNightCycle.OnProtoDeserialize))]
        [HarmonyPostfix]
        public static void OnProtoDeserialize_Postfix(DayNightCycle __instance)
        {
            DayNightCyclePatched.timePassedMod = __instance.timePassedAsDouble;
        }

        private static bool isInSkipTimeMode = false;
        private static double timePassedMod = 0.0f;
        public static float daySpeedMod = 0.1f;
        public static float nightSpeedMod = 1.0f;
    }
}
