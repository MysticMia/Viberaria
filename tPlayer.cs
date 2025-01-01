using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

using static Viberaria.bVibration;
using static Viberaria.VibrationManager.VibrationManager;
using static Viberaria.bClient;
using static Viberaria.tSystem;
using static Viberaria.ViberariaConfig;


namespace Viberaria;

public class tPlayer : ModPlayer
{
    private readonly int[] _debuffs = { 20, 24, 44, 70 };
    // 20 = Poisoned
    // 24 = On Fire!
    // 44 = Frostburn
    // 70 = Acid venom

    public override void OnEnterWorld()
        => ClientConnect();

    public override void Load()
        => ClientHandles();

    public override void Unload()
        => ClientDisconnect();

    public override void NaturalLifeRegen(ref float regen)
    {
        if (Player != Main.LocalPlayer) return;
        HealthUpdated(Player.statLife, Player.statLifeMax2);
    }

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        if (Player != Main.LocalPlayer) return;
        Died(damageSource, Player.respawnTimer );
    }

    public override void OnHurt(Player.HurtInfo hurtInfo)
    {
        if (Player != Main.LocalPlayer) return;
        Damaged(hurtInfo, Player.statLifeMax2);
    }

    public override void OnConsumeAmmo(Item weapon, Item ammo)
    {
        if (Player != Main.LocalPlayer) return;
        SoIStartedBlasting(weapon, ammo);
    }
    
    public override void OnRespawn()
    {
        if (Player != Main.LocalPlayer) return;
        Reset(); // first reset to prevent _busy from blocking, then rerun health update
        HealthUpdated(Player.statLife, Player.statLifeMax);
    }

    public override async void PreUpdateBuffs()
    {
        if (Player != Main.LocalPlayer) return;
        foreach (var buffId in _debuffs)
        {
            int index = Player.FindBuffIndex(buffId);
            if (index != -1)
                await DamageOverTimeVibration(Player.buffTime[index]);
        }
    }

    public override void PostUpdate()
    {
        if(!Instance.ViberariaEnabled)
        {
            Reset();
            Halt();
            return;
        }
        if (tSys.WorldLoaded && _client.Connected != true)
        {
            ClientConnect();
        }
    }
}