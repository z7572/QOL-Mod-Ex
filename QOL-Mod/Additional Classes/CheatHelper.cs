using HarmonyLib;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace QOL;

public static class CheatHelper
{
    public static bool CheatEnabled
    {
        get
        {
#if DEBUG
            return true;
#else
            return !MatchmakingHandler.IsNetworkMatch;
#endif
        }
    }

    public static int GetPlayerUpdateChannel(ushort playerID) => playerID * 2 + 2;

    public static int GetPlayerEventChannel(ushort playerID) => playerID * 2 + 3;

    public static bool IsTrajectoryClear(Vector3 origin, Vector3 velocity, float gravity, float maxDist)
    {
        // 模拟参数与计算时保持一致
        float drag = 0.3f;
        float dt = 0.05f; // 检测步长可以稍大一点 (0.05s) 以节省性能

        Vector3 currentPos = origin;
        Vector3 currentVel = velocity;
        float traveledDist = 0f;

        // 这是一个用于遮挡检测的 LayerMask
        // 包含: Default(0), Map(23)
        // 必须排除: Player(玩家自己), Projectile(子弹), IgnoreRaycast
        int mask = (1 << 0) | (1 << 23);

        // 模拟 2 秒或到达目标距离
        int maxSteps = 40;

        for (int i = 0; i < maxSteps; i++)
        {
            Vector3 nextPos = currentPos;
            Vector3 nextVel = currentVel;

            // 物理积分
            nextVel.y -= gravity * dt;
            float dragFactor = Mathf.Clamp01(1f - drag * dt);
            nextVel *= dragFactor;
            nextPos += nextVel * dt;

            // 射线检测这一步 (currentPos -> nextPos)
            if (Physics.Linecast(currentPos, nextPos, out RaycastHit hit, mask))
            {
                // 如果撞到了非自己的东西，说明有遮挡
                // 忽略很小的物体或透过性物体
                if (!hit.collider.isTrigger)
                {
                    return false; // 路径被遮挡
                }
            }

            traveledDist += Vector3.Distance(currentPos, nextPos);
            if (traveledDist > maxDist) break; // 超出目标距离，不用测了

            currentPos = nextPos;
            currentVel = nextVel;
        }

        return true;
    }

    /// <summary>
    /// Calculates the exact throw direction with drag resistance (based on backtracking simulation)
    /// </summary>
    public static Vector3 CalculateSimulatedThrowDir(Vector3 origin, Vector3 target, float gravity)
    {
        Vector3 toTarget = target - origin;
        Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z);
        float targetDistXZ = toTargetXZ.magnitude;
        float targetY = toTarget.y;

        // 二分查找最佳仰角 (Pitch)
        float minAngle = -60f;
        float maxAngle = 80f;
        float bestAngle = 0f;

        for (int i = 0; i < 10; i++)
        {
            float midAngle = (minAngle + maxAngle) / 2f;
            float simulatedY = SimulateTrajectoryY(midAngle, targetDistXZ, gravity);

            if (simulatedY < targetY)
            {
                minAngle = midAngle; // 打低了，需要抬高
            }
            else
            {
                maxAngle = midAngle; // 打高了，压低
            }
            bestAngle = midAngle;
        }

        // [修复] 正确获取 Quaternion 的方向向量
        Quaternion lookRot = Quaternion.LookRotation(toTargetXZ);

        // 获取旋转后的右向量 (用于作为仰角的旋转轴)
        Vector3 rightDir = lookRot * Vector3.right;

        // 获取旋转后的前向量 (用于作为旋转的基础向量)
        Vector3 forwardDir = lookRot * Vector3.forward;

        // 计算最终方向：绕着右轴，向上(负角度)旋转 bestAngle 度
        // 注意：Unity中绕X轴(Right)正旋转通常是向下看，所以抬头需要负角度
        Vector3 finalDir = Quaternion.AngleAxis(-bestAngle, rightDir) * forwardDir;

        return finalDir;
    }

    /// <summary>
    /// 模拟弹道，返回到达指定水平距离时的相对高度 (Y)
    /// 完全复用游戏内的物理步进逻辑
    /// Simulate the trajectory, return the relative height (Y) when reaching the specified horizontal distance
    /// Completely reuse the game's physical step logic
    /// </summary>
    private static float SimulateTrajectoryY(float pitchAngleDeg, float targetDistXZ, float gravityAbs)
    {
        float v0 = 35f;
        float drag = 0.3f;
        float dt = Time.fixedDeltaTime;

        float pitchRad = pitchAngleDeg * Mathf.Deg2Rad;
        // 分解速度
        float velXZ = v0 * Mathf.Cos(pitchRad);
        float velY = v0 * Mathf.Sin(pitchRad);

        // [精确修正] 初始位置偏移
        // 源码: startPos = aimPos - forward * 0.5f
        // 也就是在瞄准方向的反方向偏移 0.5m
        // 分解这个偏移量：
        float startOffsetX = -0.5f * Mathf.Cos(pitchRad); // 水平回退
        float startOffsetY = -0.5f * Mathf.Sin(pitchRad); // 垂直回退

        // 初始状态
        float currentDistXZ = startOffsetX;
        float currentY = startOffsetY;

        int maxSteps = 200;

        for (int i = 0; i < maxSteps; i++)
        {
            velY -= gravityAbs * dt;

            float dragFactor = Mathf.Clamp01(1f - drag * dt);
            velXZ *= dragFactor;
            velY *= dragFactor;

            currentDistXZ += velXZ * dt;
            currentY += velY * dt;

            if (currentDistXZ >= targetDistXZ)
            {
                return currentY;
            }
        }

        return -9999f;
    }

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

    // Check and block kick packets and auto blacklist
    public static void CheckPacket(CSteamID kickPacketSender, bool isKickPacket)
    {
        var senderPlayerColor = Helper.GetColorFromID(Helper.ClientData
            .First(data => data.ClientID == kickPacketSender)
            .PlayerObject.GetComponent<NetworkPlayer>()
            .NetworkSpawnID);
        var senderPlayerID = kickPacketSender.ToString();
        var senderPlayerName = Helper.GetPlayerName(kickPacketSender);

        if (Blacklist.IsPlayerBlacklisted(senderPlayerID)) // In case non-host lobby
        {
            Helper.TrustedKicker = false;
            Helper.LastPacketSender = kickPacketSender;
            Helper.SendModOutput($"Blocked kick sent by: {senderPlayerColor} (Blacklisted)", Command.LogType.Warning, false);
            Debug.LogWarning($"Blocked kick sent by: {senderPlayerName}, SteamID: {senderPlayerID} (Blacklisted)");
            return;
        }

        // SteamID's are Monky and Rexi and z7572
        if (isKickPacket && kickPacketSender.m_SteamID is not
            (76561198202108442 or 76561198870040513 or 76561198840554147))
        {
            Helper.TrustedKicker = false;
            Helper.LastPacketSender = kickPacketSender;
            Helper.SendModOutput($"Blocked kick sent by: {senderPlayerColor}, blacklisted!", Command.LogType.Warning, false);
            Debug.LogWarning($"Blocked kick sent by: {senderPlayerName}, SteamID: {senderPlayerID}");

            // Auto blacklist
            if (!Blacklist.IsPlayerBlacklisted(senderPlayerID))
            {
                Blacklist.AddToBlacklist(senderPlayerID, senderPlayerName);
                Debug.LogWarning($"Added {senderPlayerID}({senderPlayerName}) to blacklist!");
            }
            else
            {
                Debug.LogWarning($"{senderPlayerID}({senderPlayerName}) is already blacklisted!");
            }
            return;
        }

        Helper.TrustedKicker = true;
    }

    // Modify the position packages being sent to include custom projectiles from the cheat
    public static bool ProcessCustomProjectiles(ref byte[] data, int channel, ushort indexIgnore)
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
            Debug.Log($"[QOL] Sending projectile packages[{broadcastPackage.Count}] to all players");
            Helper.SendMessageToAllClients(broadcastData, P2PPackageHandler.MsgType.PlayerUpdate, false, ignoreUserID,
                EP2PSend.k_EP2PSendReliable, channel);
            Debug.Log("[QOL] Sended!");
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
            Debug.Log($"[QOL] Sending projectile packages[{targetBullets.Count}] to player {targetPlayerID}, weaponIndex: {CachedProjectilePackage.CurrentWeaponIndex}");
            Helper.SendP2PPacketToUser(targetClient.ClientID, singleTargetData, P2PPackageHandler.MsgType.PlayerUpdate,
                EP2PSend.k_EP2PSendReliable, channel);
            Debug.Log("[QOL] Sended!");
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

    public static IEnumerator BulletHell(ushort playerID, ushort targetID, bool isLocalDisplay = false, bool sendInSegments = false)
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
                if (sendInSegments) if (i++ % 10 == 0) yield return null;
            }
            yield return null;
        }
        IEnumerator Horizonal()
        {
            for (short x = 1000; x >= -1000; x -= 25)
            {
                FirePackage(x, 1800, 0, -1, playerID, targetID, 39, isLocalDisplay);
                if (sendInSegments) if (i++ % 10 == 0) yield return null;
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
