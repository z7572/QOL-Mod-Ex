using HarmonyLib;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace QOL;

public class CheatHelper
{
    public static int GetPlayerUpdateChannel(ushort playerID) => playerID * 2 + 2;

    public static int GetPlayerEventChannel(ushort playerID) => playerID * 2 + 3;

    public static void DamagePackage(float damage, bool killingBlow, ushort toPlayerID, DamageType dmgType = DamageType.Other,
        bool playParticles = false, Vector3 particlePosition = default, Vector3 particleDirection = default)
    {
        byte damager = (byte)Traverse.Create(Helper.networkPlayer).Field("mUpdateChannel").GetValue<int>(); // Me
        byte[] array = new byte[8 + ((!playParticles) ? 0 : (4 * 2))];
        using (MemoryStream memoryStream = new MemoryStream(array))
        {
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write(damager);
                if (killingBlow)
                {
                    binaryWriter.Write(666.666f);
                }
                else
                {
                    binaryWriter.Write(damage);
                }
                binaryWriter.Write(playParticles);
                if (playParticles)
                {
                    binaryWriter.Write(particleDirection.y);
                    binaryWriter.Write(particleDirection.z);
                }
                binaryWriter.Write((byte)dmgType);
            }
        }
        Helper.SendP2PPacketToUser(Helper.GetSteamID(toPlayerID), array, P2PPackageHandler.MsgType.PlayerTookDamage, channel:GetPlayerEventChannel(toPlayerID));
    }

    /// <summary> Adding projectile and packages to all or specified players, support multiple at same time </summary>
    /// <param name="fromPlayerID"> ushort.MaxValue means local game </param>
    /// <param name="toPlayerID"> ushort.MaxValue means all players </param>
    public static void FirePackage(short positionX, short positionY, sbyte vectorX, sbyte vectorY,
        ushort fromPlayerID = ushort.MaxValue, ushort toPlayerID = ushort.MaxValue, int weaponIndex = -1, bool isLocalDisplay = true, ushort syncIndex = 65535)
    {
        NetworkPlayer networkPlayer = null;
        Fighting fighting;
        var spawnID = ushort.MaxValue;

        if (MatchmakingHandler.IsNetworkMatch)
        {
            networkPlayer = fromPlayerID == ushort.MaxValue
                ? Helper.networkPlayer
                : Helper.GetNetworkPlayer(fromPlayerID);

            fighting = Traverse.Create(networkPlayer).Field("mFighting").GetValue<Fighting>();
            spawnID = Traverse.Create(networkPlayer).Field("mNetworkSpawnID").GetValue<ushort>();

            //if (toPlayerID == Helper.networkPlayer.NetworkSpawnID) isLocalDisplay = true; // Just for testing
        }
        else
        {
            fighting = Traverse.Create(Helper.controller).Field("fighting").GetValue<Fighting>();
            isLocalDisplay = true;
        }

        var weapon = fighting.weapon;
        if (weaponIndex != -1)
        {
            var weapons = Traverse.Create(fighting).Field("weapons").GetValue<Weapons>();
            weapon = weapons.transform.GetChild(weaponIndex).GetComponent<Weapon>();
        }

        var shootVector = new ByteVector2(vectorX, vectorY);
        var shootPosition = new ShortVector2(positionX, positionY);

        // Adding Projectile (local)
        if (weapon != null && isLocalDisplay)
        {
            try
            {
                weapon.Shoot(null, true, shootVector.ToVector3(), shootPosition.ToVector3(), syncIndex);
            }
            catch (NullReferenceException)
            {
                //Debug.LogWarning("NullReferenceException (Shooting weapon without attaching hand)");
            }
        }

        if (!MatchmakingHandler.IsNetworkMatch)
        {
            //Debug.LogWarning("Not NetworkMatch, return!");
            return;
        }

        {
            // Adding Projectile Package
            var projectilePackage = new ProjectilePackageStruct
            {
                shootPosition = shootPosition,
                shootVector = shootVector,
                syncIndex = syncIndex,
            };
            //Debug.Log($"[QOL] Adding new projectile package: Pos={shootPosition}, Vec={shootVector}, syncIndex={syncIndex}");

            cachedProjectilePackages.Enqueue(new CachedProjectilePackage
            {
                ProjectilePackage = projectilePackage,
                TargetPlayerID = toPlayerID,
                WeaponIndex = weaponIndex,
            });
            //Debug.Log($"[QOL] Cached new projectile package, target: {toPlayerID}, weaponIndex: {weaponIndex}");

            if (fighting != null)
            {
                Traverse.Create(fighting).Field("mJustAttacked").SetValue(true);
                Traverse.Create(networkPlayer).Field("mNetworkWeaponPackage").Field("FightState").SetValue((byte)1);
            }
        }

    }

    // Modify the position packages being sent to include custom projectiles from the cheat
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MultiplayerManager), "OnPlayerMoved")]
    private static bool OnPlayerMovedPrefix(MultiplayerManager __instance, ref byte[] data, int channel, ushort indexIgnore)
    {
        if (data.Length != 18) return true; // Skip if the position package includes projectiles

        if (cachedProjectilePackages.Count == 0)
        {
            CachedProjectilePackage.CurrentWeaponIndex = -1;
            return true;
        }

        var broadcastPackage = new List<ProjectilePackageStruct>();
        var singleTargetMap = new Dictionary<ushort, List<ProjectilePackageStruct>>();
        var unprocessedBullets = new Queue<CachedProjectilePackage>();

        while (cachedProjectilePackages.Count > 0)
        {
            var cache = cachedProjectilePackages.Dequeue();

            if (CachedProjectilePackage.CurrentWeaponIndex == -1) CachedProjectilePackage.CurrentWeaponIndex = cache.WeaponIndex;

            if (cache.WeaponIndex == CachedProjectilePackage.CurrentWeaponIndex)
            {
                if (cache.TargetPlayerID == ushort.MaxValue)
                {
                    broadcastPackage.Add(cache.ProjectilePackage);
                }
                else
                {
                    if (!singleTargetMap.ContainsKey(cache.TargetPlayerID)) singleTargetMap[cache.TargetPlayerID] = [];
                    singleTargetMap[cache.TargetPlayerID].Add(cache.ProjectilePackage);
                }
            }
            else
            {
                unprocessedBullets.Enqueue(cache);
            }
        }

        while (unprocessedBullets.Count > 0) cachedProjectilePackages.Enqueue(unprocessedBullets.Dequeue());
        Debug.Log($"[QOL] Unprocessed bullets count: {cachedProjectilePackages.Count}");

        // To all players
        if (broadcastPackage.Count > 0)
        {
            var ignoreUserID = (indexIgnore != ushort.MaxValue) ? Helper.ClientData[indexIgnore].ClientID.m_SteamID : 0UL;
            var broadcastData = AppendBulletsToData(data, broadcastPackage, CachedProjectilePackage.CurrentWeaponIndex);
            Helper.SendMessageToAllClients(broadcastData, P2PPackageHandler.MsgType.PlayerUpdate, false, ignoreUserID,
                EP2PSend.k_EP2PSendUnreliableNoDelay, channel);
            Debug.Log($"[QOL] Sending projectile packages[{broadcastPackage.Count}] to all players");
        }

        // To single player
        foreach (var kvp in singleTargetMap)
        {
            var targetPlayerID = kvp.Key;
            var targetBullets = kvp.Value;

            var targetClient = Helper.ClientData.FirstOrDefault(c => c != null && c.ClientID.m_SteamID == Helper.GetSteamID(targetPlayerID).m_SteamID);
            if (targetClient == null)
            {
                Debug.LogWarning($"[QOL] Player {targetPlayerID} is not connected, discarding packet");
                continue;
            }

            var singleTargetData = AppendBulletsToData(data, targetBullets, CachedProjectilePackage.CurrentWeaponIndex);
            Helper.SendP2PPacketToUser(targetClient.ClientID, singleTargetData, P2PPackageHandler.MsgType.PlayerUpdate,
                EP2PSend.k_EP2PSendUnreliableNoDelay, channel);

            Debug.Log($"[QOL] Sending projectile packages[{targetBullets.Count}] to player {targetPlayerID}, weaponIndex: {CachedProjectilePackage.CurrentWeaponIndex}");
        }

        CachedProjectilePackage.CurrentWeaponIndex = -1;
        return false;
    }

    private static byte[] AppendBulletsToData(byte[] originalData, List<ProjectilePackageStruct> bulletsToAppend, int weaponIndex)
    {
        using (var newMs = new MemoryStream())
        using (var newBw = new BinaryWriter(newMs))
        using (var originalMs = new MemoryStream(originalData))
        using (var originalBr = new BinaryReader(originalMs))
        {
            // Original Position Package
            newBw.Write(originalBr.ReadInt16()); // Position.X
            newBw.Write(originalBr.ReadInt16()); // Position.Y
            newBw.Write(originalBr.ReadSByte()); // Rotation.X
            newBw.Write(originalBr.ReadSByte()); // Rotation.Y
            newBw.Write(originalBr.ReadSByte()); // YValue
            newBw.Write(originalBr.ReadByte());  // MovementType

            originalBr.ReadByte(); // FightState (ignored)
            newBw.Write((byte)1); // FightState = 1 (attacking)
            // End of Position Package

            // Weapon Package
            ushort originalBulletCount = originalBr.ReadUInt16();
            ushort newBulletCount = (ushort)(originalBulletCount + bulletsToAppend.Count);
            newBw.Write(newBulletCount);

            // Original Projectile Package
            for (int i = 0; i < originalBulletCount; i++)
            {
                newBw.Write(originalBr.ReadInt16()); // shootPosition.X
                newBw.Write(originalBr.ReadInt16()); // shootPosition.Y
                newBw.Write(originalBr.ReadSByte()); // shootVector.X
                newBw.Write(originalBr.ReadSByte()); // shootVector.Y
                newBw.Write(originalBr.ReadUInt16()); // syncIndex
            }

            // New Projectile Package(s)
            foreach (var bullet in bulletsToAppend)
            {
                newBw.Write(bullet.shootPosition.X);
                newBw.Write(bullet.shootPosition.Y);
                newBw.Write(bullet.shootVector.X);
                newBw.Write(bullet.shootVector.Y);
                newBw.Write(bullet.syncIndex);
            }

            originalBr.ReadByte(); // WeaponIndex (ignored)
            newBw.Write((byte)weaponIndex); // WeaponIndex
            // End of Weapon Package

            Debug.Log($"[QOL] Ori data[{originalData.Length}]{(originalData.Length < 100 ? "\t: " + originalData.ToDecString() : "") }");
            Debug.Log($"[QOL] New data[{newMs.Length}]{(newMs.Length < 100 ? "\t: " + newMs.ToArray().ToDecString() : "")}");
            Debug.Log($"[QOL] Appended {bulletsToAppend.Count} bullet(s) to data");
            return newMs.ToArray();
        }
    }

    class CachedProjectilePackage
    {
        public ProjectilePackageStruct ProjectilePackage { get; set; }
        public ushort TargetPlayerID { get; set; }
        public int WeaponIndex { get; set; }
        public static int CurrentWeaponIndex { get; set; } = -1;
    }

    private static readonly Queue<CachedProjectilePackage> cachedProjectilePackages = new();

    public static void GetNetworkPosition(NetworkPlayer networkPlayer, out short x, out short y)
    {
        var positionPackage = Traverse.Create(networkPlayer).Field("mNetworkPositionPackage").GetValue<NetworkPlayer.NetworkPositionPackage>();
        x = positionPackage.Position.X;
        y = positionPackage.Position.Y;
    }

    public static ShortVector2 GetPlayerPosition(Controller controller)
    {
        var standing = controller.GetComponent<Standing>();
        var rigs = Traverse.Create(standing).Field("rigs").GetValue<Rigidbody[]>();
        var position = Vector3.zero;
        for (int i = 0; i < rigs.Length; i++)
        {
            position += rigs[i].transform.position;
        }
        position /= rigs.Length;
        return new ShortVector2(position);
    }

    // ~ to current position, ^ to mouse position
    public static float ParseCoordinate(string input, float currentValue = 0, float mouseValue = 0)
    {
        if (input.StartsWith("~"))
        {
            if (input == "~") return currentValue;
            if (float.TryParse(input.Substring(1), out float offset))
            {
                return currentValue + offset;
            }
        }
        else if (input.StartsWith("^"))
        {
            if (input == "^") return mouseValue;
            if (float.TryParse(input.Substring(1), out float offset))
            {
                return mouseValue + offset;
            }
        }
        else if (float.TryParse(input, out float value))
        {
            return value;
        }

        throw new ArgumentException($"Invalid coordinate: {input}");
    }

    public static Vector3 GetMouseWorldPosition()
    {
        var mousePos = (Input.mousePosition - new Vector3(Screen.width / 2f, Screen.height / 2f, 0f)) * 2f;
        return mousePos;
    }

    public static IEnumerator BulletHell(ushort playerID, ushort targetID, bool isLocalDisplay = false)
    {
        int i = 0;
        CoroutineRunner.Run(Horizonal());
        CoroutineRunner.Run(Vertical());
        yield return null;

        IEnumerator Vertical()
        {
            for (short y = 1800; y >= -1800; y -= 25)
            {
                FirePackage(1000, y, -1, 0, playerID, targetID, 39, isLocalDisplay); // Beam
                //if (i++ % 10 == 0) yield return null;
            }
            yield return null;
        }
        IEnumerator Horizonal()
        {
            for (short x = 1000; x >= -1000; x -= 25)
            {
                FirePackage(x, 1800, 0, -1, playerID, targetID, 39, isLocalDisplay);
                //if (i++ % 10 == 0) yield return null;
            }
            yield return null;
        }
    }

    public static IEnumerator BulletRing(ushort playerID, ushort targetID, int radius, int weaponIndex, bool isLocalDisplay)
    {
        var playerX = GetPlayerPosition(Helper.controller).X;
        var playerY = GetPlayerPosition(Helper.controller).Y;
        var bulletCount = 90;
        var bulletPerCircle = 18;
        var circleCount = bulletCount / bulletPerCircle;
        if (bulletCount % bulletPerCircle > 0)
        {
            circleCount++;
            bulletPerCircle = bulletCount / circleCount;
        }

        var angleStep = 360f / bulletPerCircle;

        for (int j = 0; j < circleCount; j++)
        {
            var angleOffset = (360f / circleCount) * j;

            for (int i = 0; i < bulletPerCircle; i++)
            {
                var angle = i * angleStep + angleOffset;
                var radian = angle * Mathf.Deg2Rad;

                var x = (short)Math.Round(playerX + radius * Mathf.Cos(radian));
                var y = (short)Math.Round(playerY + radius * Mathf.Sin(radian));
                var Vx = (sbyte)Math.Round(Mathf.Clamp((Mathf.Cos(radian) * 100), -100, 100));
                var Vy = (sbyte)Math.Round(Mathf.Clamp((Mathf.Sin(radian) * 100), -100, 100));

                FirePackage(x, y, Vx, Vy, playerID, targetID, weaponIndex, isLocalDisplay);
            }
            //yield return null;
        }
        yield return null;
    }
    public static int SwitcherWeaponIndex;
}
