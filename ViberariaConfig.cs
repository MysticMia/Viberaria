using System;
using System.ComponentModel;
using System.Linq;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace Viberaria;

public class ViberariaConfig : ModConfig
{
    public static ViberariaConfig Instance;
    public override ConfigScope Mode => ConfigScope.ClientSide;

    public static string IntifaceConnectionAddress
    {
        get
        {
            string ip;
            if (Instance.IntifaceAddress.UseLocalhost)
            {
                ip = "localhost";
            }
            else
            {
                // convert [192, 168, 0, 0] to "192.168.0.0"
                ip = String.Join(".",
                    Instance.IntifaceAddress.IntifaceIpAddress.Select(x => x.ToString()).ToArray()
                );
            }
            string port = Instance.IntifaceAddress.IntifaceIpPort.ToString();
            ModContent.GetInstance<Viberaria>().Logger.InfoFormat("Connecting to Intiface IP:");
            return $"{ip}:{port}";
        }
    }


    private const int MinTime = 10;
    private const int MaxTime = 3000;
    private const int IncrementTime = 10;
    private const float MinIntensity = 0.05f;
    private const float MaxIntensity = 1f;
    private const float IncrementIntensity = 0.01f;

    #region Main Configuration
    [Header("MainConfiguration")]
    [DefaultValue(true)] public bool ViberariaEnabled;
    [Range(0f,1f)] [Increment(0.01f)] [DefaultValue(1f)] public float VibratorMaxIntensity;
    public IntifaceIpSubpage IntifaceAddress = new();

    [SeparatePage]
    public class IntifaceIpSubpage
    {
        [Header("IntifaceIP")] [DefaultValue(true)]
        public bool UseLocalhost;
        [Range(0,255)] public int[] IntifaceIpAddress = [192, 168, 0, 0];
        public int IntifaceIpPort = 12345;

        public override string ToString()
        {
            return IntifaceConnectionAddress;
        }

        // "Implementing Equals and GetHashCode are critical for any classes you use."
        //   - tModLoader/CustomDataTypes/Pair
        public override bool Equals(object obj) {
            if (obj is IntifaceIpSubpage other)
                return UseLocalhost == other.UseLocalhost && IntifaceIpAddress == other.IntifaceIpAddress && IntifaceIpPort == other.IntifaceIpPort;
            // ReSharper disable once BaseObjectEqualsIsObjectEquals
            return base.Equals(obj);
        }

        public override int GetHashCode() {
            // ReSharper disable thrice NonReadonlyMemberInGetHashCode
            return new { UseLocalhost, IntifaceIpAddress, IntifaceIpPort }.GetHashCode();
        }
    }
    #endregion


    #region Health config
    [Header("HealthVibrationScaling")]
    [DefaultValue(false)] public bool HealthVibratationScalingEnabled;
    [Range(MinIntensity,MaxIntensity)] [Increment(IncrementIntensity)] [DefaultValue(1f)] public float HealthMaxIntensity;
    [Range(0,MaxIntensity)] [Increment(IncrementIntensity)] [DefaultValue(0.2f)] public float HealthMinIntensity;
    #endregion

    #region Damage config
    [Header("DamageVibration")]
    [DefaultValue(true)] public bool DamageVibrationEnabled;
    [DefaultValue(true)] public bool StaticDamageVibration;
    [Range(MinIntensity,MaxIntensity)] [Increment(IncrementIntensity)] [DefaultValue(.5f)] public float DamageVibrationIntensity;
    [Range(MinTime,MaxTime)] [Increment(IncrementTime)] [DefaultValue(600)] public int DamageVibrationDurationMsec;
    #endregion

    #region Death config
    [Header("DeathVibration")]
    [DefaultValue(true)] public bool DeathVibrationEnabled;
    [Range(MinIntensity,MaxIntensity)] [Increment(IncrementIntensity)] [DefaultValue(.7f)] public float DeathVibrationIntensity;
    [DefaultValue(true)] public bool StaticDeathVibrationLength;
    [Range(MinTime,MaxTime)] [Increment(IncrementTime)] [DefaultValue(1000)] public int DeathVibrationDurationMsec;
    #endregion

    #region Potion Use config
    [Header("PotionUseVibration")]
    [DefaultValue(true)] public bool PotionUseVibrationEnabled;
    [Range(MinIntensity,MaxIntensity), Increment(IncrementIntensity), DefaultValue(.4f)] public float PotionVibrationIntensity;
    [Range(MinTime,MaxTime), Increment(IncrementTime), DefaultValue(400)] public int PotionVibrationDurationMsec;
    #endregion

    #region Debuff config
    [Header("DebuffVibration")]

    [DefaultValue(true)] public bool DebuffVibrationEnabled;
    [Range(MinIntensity,MaxIntensity), Increment(IncrementIntensity), DefaultValue(.45f)] public float DebuffMaxIntensity;
    [Range(MinIntensity,MaxIntensity), Increment(IncrementIntensity), DefaultValue(.2f)] public float DebuffMinIntensity;
    [Range(MinTime,MaxTime), Increment(IncrementTime), DefaultValue(500)] public int DebuffDelayMsec;
    #endregion

    #region Other
    [Header("OtherConfigs")]
    [DefaultValue(false)] public bool DebugChatMessages;
    #endregion
}