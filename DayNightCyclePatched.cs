using System;
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
        //private static T Lerp<T>(T a, T b, T t) => ((T)1.0 - t)*a + t*b;
        private static float Lerp(float a, float b, float t) => (1.0f - t)*a + t*b;

        private static float GetTimeSpeed(DayNightCycle __instance)
        {
            float easeInOut = HarmonyPatchUnityPlugin.ModConfig.EaseInOutLerp;
            float daySpeed = HarmonyPatchUnityPlugin.ModConfig.DayTimeSpeed;
            float nightSpeed = HarmonyPatchUnityPlugin.ModConfig.NightTimeSpeed;

            if (easeInOut <= 0.0f)
                return (__instance.IsDay())? daySpeed : nightSpeed;

            float timeOfDay = __instance.GetDayScalar();

            float midDay = (__instance.sunRiseTime + __instance.sunSetTime) / 2.0f;
            float t = Math.Abs(midDay - timeOfDay) - __instance.sunRiseTime;
            t = (t < 0.0f)? (t / __instance.sunRiseTime) : (t / (1.0f - __instance.sunRiseTime));
            t = Math.Sign(t) * (float)Math.Pow((double)Math.Abs(t), (double)easeInOut);
            t = 0.5f * t + t;

            return Lerp(nightSpeed, daySpeed, t);
        }

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

            float timeSpeedMod = DayNightCyclePatched.GetTimeSpeed(__instance);
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
    }
}
