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
        private static float Clamp01(float a) => Math.Min(Math.Max(a, 0.0f), 1.0f);

        private static float GetTimeSpeed(DayNightCycle __instance)
        {
            float easeInOut = HarmonyPatchUnityPlugin.ModConfig.EaseInOutLerp;
            float daySpeed = HarmonyPatchUnityPlugin.ModConfig.DayTimeSpeed;
            float nightSpeed = HarmonyPatchUnityPlugin.ModConfig.NightTimeSpeed;

            if (easeInOut <= 0.0f)
                return (__instance.IsDay())? daySpeed : nightSpeed;

            float timeOfDay = __instance.GetDayScalar();

            //https://www.desmos.com/calculator/lubwgmbtax
            float midDay = (__instance.sunRiseTime + __instance.sunSetTime) / 2.0f;
            float t = (1.0f - Math.Abs(midDay - timeOfDay) / midDay) - __instance.sunRiseTime;
            t = (t < 0.0f)? (t / __instance.sunRiseTime) : (t / (__instance.sunSetTime));
            t = Math.Sign(t) * (float)Math.Pow((double)Math.Abs(t), (double)easeInOut);
            t = 0.5f*t + 0.5f;

            return Lerp(nightSpeed, daySpeed, Clamp01(t));
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
            //__instance.timePassedAsDouble = DayNightCyclePatched.timePassedMod;
            UInt32 timePassedGame = Convert.ToUInt32(__instance.timePassedAsDouble);
            UInt32 timePassedMod  = Convert.ToUInt32(DayNightCyclePatched.timePassedMod);

            const UInt64 packedTimesFlag = 0x8000000000000000;
            UInt64 packedU64 = (((UInt64)timePassedGame) << 32) | ((UInt64)timePassedMod);
            packedU64 = packedTimesFlag | packedU64; //hopefully 2147483647 vanilla game seconds haven't passed (68 years in game)

            Int64 packedI64 = BitConverter.ToInt64(BitConverter.GetBytes(packedU64), 0);
            __instance.timePassedAsDouble = BitConverter.Int64BitsToDouble(packedI64);

            HarmonyPatchUnityPlugin.Log.LogInfo(": Saving, time game: " + timePassedGame);
            HarmonyPatchUnityPlugin.Log.LogInfo(":         time mod: " + timePassedMod);
            HarmonyPatchUnityPlugin.Log.LogInfo(":         packed:   0x" + packedI64.ToString("X"));
            HarmonyPatchUnityPlugin.Log.LogInfo(":         packedUI: 0x" + packedU64.ToString("X"));
        }

        [HarmonyPatch(nameof(DayNightCycle.OnProtoDeserialize))]
        [HarmonyPrefix]
        public static void OnProtoDeserialize_Prefix(DayNightCycle __instance)
        {
            const UInt64 packedTimesFlag = 0x8000000000000000;

            Int64 packedI64 = BitConverter.DoubleToInt64Bits(__instance.timePassedAsDouble);
            UInt64 packedU64 = BitConverter.ToUInt64(BitConverter.GetBytes(packedI64), 0);

            if ((packedU64 & packedTimesFlag) != 0)
            {
                UInt32 timePassedGame = (UInt32)((packedU64 & 0x7fffffff00000000) >> 32);
                UInt32 timePassedMod =  (UInt32)((packedU64 & 0x00000000ffffffff));

                __instance.timePassedAsDouble = (double)timePassedGame;
                DayNightCyclePatched.timePassedMod = (double)timePassedMod;

                HarmonyPatchUnityPlugin.Log.LogInfo(":Load new save, packed:   0x" + packedI64.ToString("X"));
                HarmonyPatchUnityPlugin.Log.LogInfo(":               packedUI: 0x" + packedU64.ToString("X"));
                HarmonyPatchUnityPlugin.Log.LogInfo(":               time game: " + __instance.timePassedAsDouble);
                HarmonyPatchUnityPlugin.Log.LogInfo(":               time mod: " + DayNightCyclePatched.timePassedMod);
            }
            else //old save, with direct overwrite
            {
                DayNightCyclePatched.timePassedMod = __instance.timePassedAsDouble;

                HarmonyPatchUnityPlugin.Log.LogInfo(": Load old save, time: " + __instance.timePassedAsDouble);
            }
        }

        private static bool isInSkipTimeMode = false;
        private static double timePassedMod = 0.0f;
    }
}
