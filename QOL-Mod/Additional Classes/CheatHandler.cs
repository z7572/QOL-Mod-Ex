using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using Steamworks;
using HarmonyLib;

namespace QOL
{
    class CheatHandler
    {
        public static void FirePackage(short positionX, short positionY, sbyte vectorX, sbyte vectorY,
            ushort fromPlayerID = ushort.MaxValue, ushort toPlayerID = ushort.MaxValue, int weaponIndex = -1 , bool isLocalDisplay = true, ushort syncIndex = 65535)
        {
            NetworkPlayer networkPlayer = null;
            Fighting fighting;
            var spawnID = ushort.MaxValue;
            if (MatchmakingHandler.IsNetworkMatch && fromPlayerID != ushort.MaxValue)
            {
                networkPlayer = Helper.GetNetworkPlayer(fromPlayerID);
                fighting = Traverse.Create(networkPlayer).Field("mFighting").GetValue<Fighting>();
                spawnID = Traverse.Create(networkPlayer).Field("mNetworkSpawnID").GetValue<ushort>();
                if (toPlayerID == Helper.localNetworkPlayer.NetworkSpawnID) isLocalDisplay = true;
            }
            else if (MatchmakingHandler.IsNetworkMatch && fromPlayerID == ushort.MaxValue)
            {
                networkPlayer = Helper.localNetworkPlayer;
                fighting = Traverse.Create(networkPlayer).Field("mFighting").GetValue<Fighting>();
                spawnID = Traverse.Create(networkPlayer).Field("mNetworkSpawnID").GetValue<ushort>();
                if (toPlayerID == Helper.localNetworkPlayer.NetworkSpawnID) isLocalDisplay = true;
            }
            else
            {
                fighting = Traverse.Create(Helper.controller).Field("fighting").GetValue<Fighting>();
                isLocalDisplay = true;
            }


            var weapons = Traverse.Create(fighting).Field("weapons").GetValue<Weapons>();
            var weapon = fighting.weapon;
            if (weaponIndex != -1)
            {
                weapon = weapons.transform.GetChild(weaponIndex).GetComponent<Weapon>();
            }

            Vector3 shootVector = new ByteVector2(vectorX, vectorY).ToVector3();
            Vector3 shootPosition = new ShortVector2(positionX, positionY).ToVector3();

            var projectilePackages = new ProjectilePackageStruct[1];
            projectilePackages[0] = new ProjectilePackageStruct
            {
                shootPosition = new ShortVector2(positionX, positionY),
                shootVector = new ByteVector2(vectorX, vectorY),
                syncIndex = syncIndex,
            };
            Debug.Log(string.Concat(new object[]
            {
                "Adding new projectile package: ",
                shootPosition.ToString(),
                " : ",
                shootVector,
                " syncIndex: ",
                syncIndex
            }));
            // Adding Projectile Package (local)
            if (weapon != null && isLocalDisplay)
            {
                try
                {
                    weapon.Shoot(null, true, shootVector, shootPosition, syncIndex);
                }
                catch (NullReferenceException) { } // Shooting weapon without attaching hand
            }


            // Sending Projectile Package (network)
            if (!MatchmakingHandler.IsNetworkMatch)
            {
                //Debug.Log("Not NetworkMatch, return!");
                return;
            }

            var positionPackage = Traverse.Create(networkPlayer).Field("mNetworkPositionPackage").GetValue<NetworkPlayer.NetworkPositionPackage>();
            var weaponPackage = Traverse.Create(networkPlayer).Field("mNetworkWeaponPackage").GetValue<NetworkPlayer.NetworkWeaponPackage>();

            byte[] array = new byte[positionPackage.Size + (2 + 6 + 2 * projectilePackages.Length) + 2];

            using (MemoryStream memoryStream = new MemoryStream(array))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
                {
                    binaryWriter.Write(positionPackage.Position.X);
                    binaryWriter.Write(positionPackage.Position.Y);
                    binaryWriter.Write(positionPackage.Rotation.X);
                    binaryWriter.Write(positionPackage.Rotation.Y);
                    binaryWriter.Write(positionPackage.YValue);
                    binaryWriter.Write(positionPackage.MovementType);

                    binaryWriter.Write(weaponPackage.FightState);

                    ushort num = (ushort)projectilePackages.Length;
                    binaryWriter.Write(num);

                    if (num > 0)
                    {
                        foreach (var projectilePackage in projectilePackages)
                        {

                            binaryWriter.Write(positionX);
                            binaryWriter.Write(positionY);
                            binaryWriter.Write(vectorX);
                            binaryWriter.Write(vectorY);
                            binaryWriter.Write(syncIndex);

                            Debug.Log("Sending: ProjectilePackage: " + projectilePackage.shootPosition.ToString() + " : " + projectilePackage.shootVector.ToString());
                        }
                    }

                    binaryWriter.Write(weaponPackage.WeaponType);
                }
            }

            //Debug.Log("Sending PackageFire!!!");
            var updateChannel = Traverse.Create(networkPlayer).Field("mUpdateChannel").GetValue<int>();

            if (toPlayerID == ushort.MaxValue) // To all
            {
                //var networkManager = Traverse.Create(networkPlayer).Field("mNetworkManager").GetValue<MultiplayerManager>();
                //var multiplayerManager = Traverse.Create(networkManager).Field("mMultiplayerManager").GetValue<MultiplayerManager>();
                //multiplayerManager.SendMessageToAllClients(array, P2PPackageHandler.MsgType.PlayerUpdate,
                //    false, spawnID, EP2PSend.k_EP2PSendUnreliableNoDelay, updateChannel);
                foreach (var color in new ushort[] { 0, 1, 2, 3 })
                {
                    if (color == spawnID) continue;
                    try
                    {
                        P2PPackageHandler.Instance.SendP2PPacketToUser(Helper.GetSteamID(color), array, P2PPackageHandler.MsgType.PlayerUpdate,
                            EP2PSend.k_EP2PSendUnreliableNoDelay, updateChannel);
                        //Debug.Log("Sended! array length: " + array.Length);
                    }
                    catch (Exception) { continue; }
                }
            }
            else
            {
                P2PPackageHandler.Instance.SendP2PPacketToUser(Helper.GetSteamID(toPlayerID), array, P2PPackageHandler.MsgType.PlayerUpdate,
                    EP2PSend.k_EP2PSendUnreliableNoDelay, updateChannel);
                //Debug.Log("Sended! array length: " + array.Length);
            }

        }

        public static void GetNetworkPosition(NetworkPlayer networkPlayer, out short x, out short y)
        {
            var positionPackage = Traverse.Create(networkPlayer).Field("mNetworkPositionPackage").GetValue<NetworkPlayer.NetworkPositionPackage>();
            x = positionPackage.Position.X;
            y = positionPackage.Position.Y;
        }

        public static IEnumerator BulletHell(ushort playerID, ushort targetID, bool isLocalDisplay = false)
        {
            int i = 0;
            GameManager.Instance.StartCoroutine(Horizonal());
            GameManager.Instance.StartCoroutine(Vertical());
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
                    if (i++ % 10 == 0) yield return null;
                }
                //yield return null;
            }
        }

        public static IEnumerator BulletRing(ushort playerID, ushort targetID, short radius, int weaponIndex)
        {
            short playerX, playerY;
            if (MatchmakingHandler.IsNetworkMatch)
            {
                GetNetworkPosition(Helper.localNetworkPlayer, out playerX, out playerY);
            }
            else
            {
                playerX = (short)Helper.controller.transform.position.x;
                playerY = (short)Helper.controller.transform.position.y;
            }
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

                    FirePackage(x, y, Vx, Vy, playerID, targetID, weaponIndex, true);
                }
                yield return null;
            }
        }
    }
}
