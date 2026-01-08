using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace QOL.Trainer.Patches;

[HarmonyPatch(typeof(AI))]
public static class AIPatch
{
    private static readonly Dictionary<AI, ushort> _aiTargetIDMap = new Dictionary<AI, ushort>();

    // Just rewrite the whole method
    [HarmonyPatch("Update")]
    [HarmonyPrefix]
    public static bool UpdateMethodPrefix(AI __instance, 
        ref float ___reactionTime, 
        ref float ___velocitySmoothnes, 
        ref float ___preferredRange,
        ref float ___heightRange, 
        ref bool ___canAttack, 
        ref float ___range, 
        ref float ___reactionHitReset, 
        ref float ___jumpOffset,
        ref float ___targetingSmoothing, 
        ref bool ___goForGuns, 
        ref bool ___attacking, 
        ref bool ___dontAimWhenAttacking, 
        ref float ___startAttackDelay,
        ref Transform ___behaviourTarget, 
        ref Rigidbody ___target, 
        ref float ___velocity, 
        ref ControllerHandler ___controllerHandler,
        ref Controller ___controller, 
        ref Transform ___aimer, 
        ref Fighting ___fighting, 
        ref float ___reactionCounter, 
        ref Movement ___movement,
        ref Transform ___head, 
        ref CharacterInformation ___info, 
        ref CharacterInformation ___targetInformation, 
        ref float ___counter)
    {
        // GC
        var keysToRemove = new List<AI>();
        foreach (var kvp in _aiTargetIDMap) if (kvp.Key == null) keysToRemove.Add(kvp.Key);
        foreach (var ai in keysToRemove) _aiTargetIDMap.Remove(ai);

        if (___info.isDead) return false;

        var hh = ___controller.GetComponent<HealthHandler>();
        if (hh != null && ___controller.isAI)
        {
            var aiField = AccessTools.Field(typeof(HealthHandler), "ai");
            if (aiField.GetValue(hh) == null)
            {
                aiField.SetValue(hh, __instance);
            }
        }

        ushort lastTargetID = ushort.MaxValue;
        if (!_aiTargetIDMap.ContainsKey(__instance))
        {
            _aiTargetIDMap[__instance] = ushort.MaxValue;
        }
        else
        {
            lastTargetID = _aiTargetIDMap[__instance];
        }

        ___startAttackDelay -= Time.deltaTime;
        var targetPosition = Vector3.zero;

        if (___behaviourTarget)
        {
            targetPosition = ___behaviourTarget.position;
        }
        else if (___target)
        {
            targetPosition = ___target.position;
        }

        // Reset target (presumably to prevent being stuck in certain sittuations)
        if (___counter > 1f)
        {
            ___counter = Random.Range(-0.5f, 0.5f);
            ___target = null;
            //_aiTargetIDMap[__instance] = ushort.MaxValue;
        }

        if (targetPosition != Vector3.zero && (!___targetInformation || !___targetInformation.isDead))
        {
            // Fix fly falling issue
            if (___controller.canFly)
            {
                ___info.paceState = 1;
            }
            else
            {
                ___info.paceState = 0;
            }

            if (!___dontAimWhenAttacking || !___fighting.isSwinging)
            {
                if (___targetingSmoothing == 0f)
                {
                    ___aimer.rotation = Quaternion.LookRotation(targetPosition - ___head.position);
                }
                else
                {
                    ___aimer.rotation = Quaternion.Lerp(___aimer.rotation, Quaternion.LookRotation(targetPosition - ___head.position), Time.deltaTime * (5f / ___targetingSmoothing));
                }
            }

            ___counter += Time.deltaTime;

            // Move towards the target
            if (Vector3.Distance(___head.position, targetPosition) > ___preferredRange)
            {
                if (targetPosition.z < ___head.position.z)
                {
                    ___velocity = ___velocitySmoothnes == 0f ? -1f
                        : Mathf.Lerp(___velocity, -1f, Time.deltaTime * (5f / ___velocitySmoothnes));
                }
                else if (targetPosition.z > ___head.position.z)
                {
                    ___velocity = ___velocitySmoothnes == 0f ? 1f
                        : Mathf.Lerp(___velocity, 1f, Time.deltaTime * (5f / ___velocitySmoothnes));
                }

                JumpOverWall(___controller, (___target != null) ? ___target.transform : ___behaviourTarget, ___velocity);

                ___controller.Move(___velocity);
            }

            // Jump if the target's head is above
            if (targetPosition.y > ___head.position.y + ___jumpOffset)
            {
                ___controller.Jump(false, false);
            }

            ___attacking = false;

            if (!___behaviourTarget && ___canAttack && ___startAttackDelay < 0f)
            {
                var currentAttackRange = ___range;
                ___attacking = true;

                //if (Singleton<TrainerOptions>.Instance.AiAggressiveEnabled)
                //{
                //    ___reactionTime = 0.0f;
                //}

                ___reactionTime = 0.4f;

                if (___fighting.weapon)
                {
                    if (___fighting.weapon.isGun)
                    {
                        currentAttackRange = 25f;
                        ___reactionTime = 0.25f;
                    }
                    else
                    {
                        currentAttackRange = 2f;
                        ___reactionTime = 0.25f;
                    }
                }

                var cubeLayerBitMask = 1 << 23; // Cube bitmask
                // Check that the target is in direct line of sight and not obstructed by a wall (cube)
                if (Physics.Linecast(___head.position, targetPosition, out var cubeHit, cubeLayerBitMask) == false)
                {
                    // Perform an attack if the target is still present and is within range
                    if (___target && Vector3.Distance(___head.position, targetPosition) < currentAttackRange && targetPosition.y - ___head.position.y < ___heightRange)
                    {
                        ___reactionCounter += Time.deltaTime;

                        if (___reactionCounter > ___reactionTime)
                        {
                            ___reactionCounter = 0f;
                            ___controller.Attack();
                        }
                    }
                    else if (___reactionCounter > 0f)
                    {
                        ___reactionCounter -= Time.deltaTime;
                    }
                }
            }
        }

        if (___behaviourTarget) return false;

        // Move to guns
        var closestTargetDistance = 100f;
        WeaponPickUp weaponPickUp = null;
        var weapons = Traverse.Create(___fighting).Field("weapons").GetValue<Weapons>();

        if (___goForGuns)
        {
            weaponPickUp = Object.FindObjectOfType<WeaponPickUp>();
        }

        if (weaponPickUp && weaponPickUp.transform.position.y < 10f && ___fighting.weapon == null)
        {
            // Ensure that the AI has this type of weapon and can pick it up (some AI prefabs have less weapons than others).
            if (weaponPickUp.id < weapons.transform.childCount)
            {
                ___target = weaponPickUp.GetComponent<Rigidbody>();
                return false;
            }
        }

        // Find a PC / player to attack
        foreach (var playerController in ___controllerHandler.players)
        {
            if (playerController == null) continue;

            // Set target that is not itself and not dead.
            var playerCharacterInformation = playerController.GetComponent<CharacterInformation>();
            if (playerController == ___controller || playerCharacterInformation.isDead) continue;
                
            var torsoTransform = playerController.GetComponentInChildren<Torso>().transform;
            var targetDistance = Vector3.Distance(___head.position, torsoTransform.position);

            if (targetDistance < closestTargetDistance)
            {
                closestTargetDistance = targetDistance;
                ___target = torsoTransform.GetComponent<Rigidbody>();
                ___targetInformation = playerCharacterInformation;

                var newTargetID = (ushort)___target.transform.root.GetComponentInChildren<Controller>().playerID;
                if (newTargetID != lastTargetID)
                {
                    var chat = ___controller.GetComponentInChildren<ChatManager>();
                    chat.Talk($"Targeting: {Helper.GetColorFromID(newTargetID)}");
                    lastTargetID = newTargetID;
                    _aiTargetIDMap[__instance] = newTargetID;
                }
            }
        }

        // Find an NPC (black) to attack
        // Todo: The choice between attempting to first target a Player/PC or NPC could be randomized.
        // (Done) Todo: The below works, however the NPCs need to be on different GameObject layers for them to be able to collide (refer to Controller.SetCollision)

        if (___target == null)
        {
            var charactersAlive = Traverse.Create(GameManager.Instance).Field("hoardHandler").Field("charactersAlive").GetValue<List<Controller>>();
            foreach (var characterAlive in charactersAlive)
            {
                if (characterAlive == null || characterAlive == ___controller) continue;

                var characterInformation = characterAlive.GetComponent<CharacterInformation>();
                if (characterInformation.isDead) continue;

                var torsoTransform = characterAlive.GetComponentInChildren<Torso>().transform;
                var targetDistance = Vector3.Distance(___head.position, torsoTransform.position);
                if (targetDistance < closestTargetDistance)
                {
                    closestTargetDistance = targetDistance;
                    ___target = torsoTransform.GetComponent<Rigidbody>();
                    ___targetInformation = characterInformation;

                    var newTargetID = (ushort)___target.transform.root.GetComponentInChildren<Controller>().playerID;
                    if (newTargetID != lastTargetID)
                    {
                        var chat = ___controller.GetComponentInChildren<ChatManager>();
                        chat.Talk($"Targeting NPC");
                        lastTargetID = newTargetID;
                        _aiTargetIDMap[__instance] = newTargetID;
                    }
                }
            }
        }

        return false;
    }

    // Jump if a wall is in the way of the move direction
    private static void JumpOverWall(Controller controller, Transform target, float velocity)
    {
        var torso = controller.GetComponentInChildren<Torso>();
        var torsoPos = torso.transform.position;

        if (target != null)
        {
            // Avoid being stuck on a wall when the target is below
            var toTarget = target.position - torsoPos;
            if (Vector3.Angle(Vector3.down, toTarget) <= 5f)
            {
                controller.Down();
                return;
            }
        }

        var cubeLayerBitMask = 1 << 23;
        var moveDirection = new Vector3(0f, 0f, velocity >= 0 ? 1f : -1f);
        var rayEnd = torsoPos + moveDirection * 0.5f;

        if (!Physics.Linecast(torsoPos, rayEnd, cubeLayerBitMask)) return;

        controller.Jump(false, false);
    }
}