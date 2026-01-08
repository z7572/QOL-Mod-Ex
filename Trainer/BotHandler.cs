using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using HarmonyLib;
using UnityEngine;

namespace QOL.Trainer;

// Adapted from https://github.com/alexcodito/StickFightTheGameTrainer/blob/master/StickFightTheGameTrainer/Trainer/TrainerLogic/TrainerManager.cs
class BotHandler : MonoBehaviour
{
    public static BotHandler Instance
    {
        get
        {
            return _instance;
        }
    }
    protected static BotHandler _instance;

    private static GameManager mGameManager => Traverse.Create(GameManager.Instance.mMultiplayerManager).Field("mGameManager").GetValue<GameManager>();
    private static HoardHandler hoardHandler => Traverse.Create(mGameManager).Field("hoardHandler").GetValue<HoardHandler>();

    private HoardHandler hoardHandlerBolt;
    private HoardHandler hoardHandlerPlayer;
    private HoardHandler hoardHandlerZombie;
    private IList<Weapon> _weaponComponents;

    private readonly float AiDamageMultiplier = 1f;
    private readonly float AiPunchForce = 100000f;
    private readonly float AiPunchTime = 0.15f;
    private readonly float AiMovementForceMultiplier = 3000f;
    private readonly float AiMovementJumpForceMultiplier = 25.0f;

    private void Awake()
    {
        _instance = this;
    }

    private void Start()
    {
        // Load hoard handlers for AI spawning
        var hoardHandlers = Resources.FindObjectsOfTypeAll<HoardHandler>();

        foreach (HoardHandler hoardHandler in hoardHandlers)
        {
            if (hoardHandler.name == "AI spawner")
            {
                hoardHandlerPlayer = hoardHandler;
            }
            if (hoardHandler.name == "AI spawner (1)")
            {
                hoardHandlerBolt = hoardHandler;
            }
            if (hoardHandler.name == "AI spawner (2)")
            {
                hoardHandlerZombie = hoardHandler;
            }
        }

        // Populate list of weapons to use as reference when resetting defaults.
        var playerObject = LevelEditor.ResourcesManager.Instance.CharacterObject;
        var weaponObjects = playerObject.transform.Find("Weapons");

        for (var i = 0; i < weaponObjects.childCount; i++)
        {
            var weaponComponent = weaponObjects.GetChild(i).GetComponent<Weapon>();
            _weaponComponents.Add(weaponComponent);
        }
    }

    private void SetBotStats()
    {
        var playerControllers = new List<Controller>();


        playerControllers.AddRange(Traverse.Create(hoardHandlerBolt).Field("charactersAlive").GetValue<List<Controller>>());
        playerControllers.AddRange(Traverse.Create(hoardHandlerPlayer).Field("charactersAlive").GetValue<List<Controller>>());
        playerControllers.AddRange(Traverse.Create(hoardHandlerZombie).Field("charactersAlive").GetValue<List<Controller>>());
        playerControllers.AddRange(Traverse.Create(mGameManager).Field("controllerHandler").GetValue<ControllerHandler>().ActivePlayers);

        foreach (var player in playerControllers)
        {
            var fighting = Traverse.Create(player).Field("fighting").GetValue<Fighting>();
            var movement = Traverse.Create(player).Field("movement").GetValue<Movement>();

            if (player.isAI)
            {
                //fighting.punchTime = AiPunchTime;
                //fighting.punchForce = AiPunchForce;
                //movement.forceMultiplier = AiMovementForceMultiplier;
                //movement.jumpForceMultiplier = AiMovementJumpForceMultiplier;
                Traverse.Create(fighting).Field("punchTime").SetValue(AiPunchTime);
                Traverse.Create(fighting).Field("punchForce").SetValue(AiPunchForce);
                Traverse.Create(movement).Field("forceMultiplier").SetValue(AiMovementForceMultiplier);
                Traverse.Create(movement).Field("jumpForceMultiplier").SetValue(AiMovementJumpForceMultiplier);

                // Set punch damage dealt by bots
                var punchForceComponents = player.gameObject.GetComponentsInChildren<PunchForce>();
                foreach (var punchForceComponent in punchForceComponents)
                {
                    //punchForceComponent.damageMultiplier = AiDamageMultiplier;
                    Traverse.Create(punchForceComponent).Field("damageMultiplier").SetValue(AiDamageMultiplier);
                }

                if (player.gameObject.name == "ZombieCharacterArms(Clone)")
                {
                    // Set grab damage dealt by Zombie bots
                    var reachForPlayerComponents = fighting.gameObject.GetComponentsInChildren<ReachForPlayer>();
                    foreach (var reachForPlayerComponent in reachForPlayerComponents)
                    {
                        //reachForPlayerComponent.damage = AiDamageMultiplier * 3f;
                        Traverse.Create(reachForPlayerComponent).Field("damage").SetValue(AiDamageMultiplier * 3f);
                    }
                }
            }
            else
            {
                // Set weapon damage received from bots
                var bodyPartComponents = player.gameObject.GetComponentsInChildren<BodyPart>();
                foreach (var bodyPartComponent in bodyPartComponents)
                {
                    //bodyPartComponent.multiplier = AiDamageMultiplier;
                    Traverse.Create(bodyPartComponent).Field("multiplier").SetValue(AiDamageMultiplier);
                }
            }
        }
    }

    // Spawn an NPC that deals and takes damage. 
    private bool SpawnBotPlayer(GameObject playerPrefab)
    {
        var controllerHandler = Traverse.Create(mGameManager).Field("controllerHandler").GetValue<ControllerHandler>();

        if (controllerHandler.ActivePlayers.Count >= 4)
        {
            return false;
        }

        var spawnPosition = Vector3.up * 8f;
        var spawnRotation = Quaternion.identity;
        var playerId = controllerHandler.ActivePlayers.Count;

        var playerColors = MultiplayerManagerAssets.Instance.Colors;

        var playerObject = Instantiate(playerPrefab, spawnPosition, spawnRotation);

        var playerController = playerObject.GetComponent<Controller>();

        var playerPrefabSetMovementAbilityComponent = MultiplayerManagerAssets.Instance.PlayerPrefab.GetComponent<SetMovementAbility>();

        if (playerObject.GetComponent<SetMovementAbility>() == null)
        {
            var hoardHandlerSetMovementAbilityComponent = playerObject.AddComponent<SetMovementAbility>();
            Traverse.Create(hoardHandlerSetMovementAbilityComponent).Field("abilities").SetValue(playerPrefabSetMovementAbilityComponent.abilities);
            Traverse.Create(hoardHandlerSetMovementAbilityComponent).Field("bossHealth").SetValue(playerPrefabSetMovementAbilityComponent.bossHealth);
        }

        var playerLineRenderers = playerObject.GetComponentsInChildren<LineRenderer>();
        for (var i = 0; i < playerLineRenderers.Length; i++)
        {
            if (playerId < playerColors.Length && playerColors[playerId] != null)
            {
                playerLineRenderers[i].sharedMaterial = playerColors[playerId];
            }
        }

        var playerSpriteRenderers = playerObject.GetComponentsInChildren<SpriteRenderer>();
        foreach (var spriteRenderer in playerSpriteRenderers)
        {
            if (spriteRenderer.transform.tag != "DontChangeColor")
            {
                if (playerId < playerColors.Length && playerColors[playerId] != null)
                {
                    spriteRenderer.color = playerColors[playerId].color;
                    if (!spriteRenderer.enabled)
                    {
                        spriteRenderer.enabled = true;
                    }
                }
            }
        }

        var characterInformation = playerObject.GetComponent<CharacterInformation>();
        if (characterInformation != null)
        {
            Traverse.Create(characterInformation).Field("myMaterial").SetValue(playerColors[playerId]);
            Traverse.Create(characterInformation).Field("enabled").SetValue(true);
        }

        if (playerController != null)
        {
            playerController.AssignNewDevice(null, false);

            Traverse.Create(playerController).Field("playerID").SetValue(playerId);
            Traverse.Create(playerController).Field("isAI").SetValue(true);
            Traverse.Create(playerController).Field("enabled").SetValue(true);
            Traverse.Create(playerController).Field("inactive").SetValue(false);

            playerController.SetCollision(true);
        }
        else
        {
            Debug.LogError("PlayerController is null");
            return false;
        }

        var startMethod = AccessTools.Method(typeof(Controller), "Start");
        if (startMethod != null)
        {
            startMethod.Invoke(playerController, null);
        }

        controllerHandler.players.Add(playerController);

        mGameManager.RevivePlayer(playerController, true);

        return true;
    }

    public bool SpawnBot(string botType, bool spawnPcEnabled = true)
    {
        GameObject prefabToUse = null;
        GameObject hoardCharToUse = null;

        switch (botType.ToLower())
        {
            case "player":
                prefabToUse = MultiplayerManagerAssets.Instance.PlayerPrefab;
                hoardCharToUse = hoardHandlerPlayer.character;
                break;
            case "bolt":
                prefabToUse = hoardHandlerBolt.character;
                hoardCharToUse = hoardHandlerBolt.character;
                break;
            case "zombie":
                prefabToUse = hoardHandlerZombie.character;
                hoardCharToUse = hoardHandlerZombie.character;
                break;
            default:
                Debug.LogWarning($"Unknown bot type: {botType}");
                return false;
        }

        var spawnAIMethod = AccessTools.Method(typeof(HoardHandler), "SpawnAI");

        if (spawnPcEnabled)
        {
            if (!SpawnBotPlayer(prefabToUse)) return false;
        }
        else
        {
            spawnAIMethod.Invoke(hoardHandler, [hoardCharToUse]);
        }

        SetBotStats();
        return true;
    }
}
