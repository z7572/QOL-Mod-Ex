using HarmonyLib;
using UnityEngine;

namespace QOL.Patches.BotTest;

public static class AIPatch
{
    public static void Patch(Harmony harmonyInstance)
    {
        var updateMethod = AccessTools.Method(typeof(AI), "Update");
        var updateMethodPrefix = new HarmonyMethod(typeof(AIPatch).GetMethod(nameof(UpdateMethodPrefix)));
        harmonyInstance.Patch(updateMethod, prefix: updateMethodPrefix);
    }

    public static bool UpdateMethodPrefix(AI __instance)
    {
        SetNewAILogic(__instance);
        return false;
    }

    private static void SetNewAILogic(AI __instance)
    {
        float reactionTime = Traverse.Create(__instance).Field("reactionTime").GetValue<float>();
        float velocitySmoothnes = Traverse.Create(__instance).Field("velocitySmoothnes").GetValue<float>();
        float preferredRange = Traverse.Create(__instance).Field("preferredRange").GetValue<float>();
        float heightRange = Traverse.Create(__instance).Field("heightRange").GetValue<float>();
        bool canAttack = Traverse.Create(__instance).Field("canAttack").GetValue<bool>();
        float range = Traverse.Create(__instance).Field("range").GetValue<float>();
        float reactionHitReset = Traverse.Create(__instance).Field("reactionHitReset").GetValue<float>();
        float jumpOffset = Traverse.Create(__instance).Field("jumpOffset").GetValue<float>();
        float targetingSmoothing = Traverse.Create(__instance).Field("targetingSmoothing").GetValue<float>();
        bool goForGuns = Traverse.Create(__instance).Field("goForGuns").GetValue<bool>();
        bool attacking = Traverse.Create(__instance).Field("attacking").GetValue<bool>();
        bool dontAimWhenAttacking = Traverse.Create(__instance).Field("dontAimWhenAttacking").GetValue<bool>();
        float startAttackDelay = Traverse.Create(__instance).Field("startAttackDelay").GetValue<float>();

        var behaviourTarget = Traverse.Create(__instance).Field("behaviourTarget").GetValue<Transform>();
        var target = Traverse.Create(__instance).Field("target").GetValue<Rigidbody>();
        float velocity = Traverse.Create(__instance).Field("velocity").GetValue<float>();
        var controllerHandler = Traverse.Create(__instance).Field("controllerHandler").GetValue<ControllerHandler>();
        var controller = Traverse.Create(__instance).Field("controller").GetValue<Controller>();
        var aimer = Traverse.Create(__instance).Field("aimer").GetValue<Transform>();
        var fighting = Traverse.Create(__instance).Field("fighting").GetValue<Fighting>();
        float reactionCounter = Traverse.Create(__instance).Field("reactionCounter").GetValue<float>();
        var movement = Traverse.Create(__instance).Field("movement").GetValue<Movement>();
        var head = Traverse.Create(__instance).Field("head").GetValue<Transform>();
        var info = Traverse.Create(__instance).Field("info").GetValue<CharacterInformation>();
        var targetInformation = Traverse.Create(__instance).Field("targetInformation").GetValue<CharacterInformation>();
        float counter = Traverse.Create(__instance).Field("counter").GetValue<float>();


        startAttackDelay -= Time.deltaTime;
        Traverse.Create(__instance).Field("startAttackDelay").SetValue(startAttackDelay);
        var targetPosition = Vector3.zero;

        if (behaviourTarget)
        {
            targetPosition = behaviourTarget.position;
        }
        else if (target)
        {
            targetPosition = target.position;
        }

        // Reset target (presumably to prevent being stuck in certain sittuations)
        if (counter > 1f)
        {
            counter = UnityEngine.Random.Range(-0.5f, 0.5f);
            target = null;
            Traverse.Create(__instance).Field("counter").SetValue(counter);
            Traverse.Create(__instance).Field("target").SetValue(target);
        }

        if (targetPosition != Vector3.zero && (!targetInformation || !targetInformation.isDead))
        {
            info.paceState = 0;
            Traverse.Create(__instance).Field("info").SetValue(info);

            if (!dontAimWhenAttacking || !fighting.isSwinging)
            {
                if (targetingSmoothing == 0f)
                {
                    aimer.rotation = Quaternion.LookRotation(targetPosition - head.position);
                }
                else
                {
                    aimer.rotation = Quaternion.Lerp(aimer.rotation, Quaternion.LookRotation(targetPosition - head.position), Time.deltaTime * (5f / targetingSmoothing));
                }
            }

            counter += Time.deltaTime;
            Traverse.Create(__instance).Field("counter").SetValue(counter);

            // Move towards the target
            if (Vector3.Distance(head.position, targetPosition) > preferredRange)
            {
                if (targetPosition.z < head.position.z)
                {
                    velocity = velocitySmoothnes == 0f ? -1f 
                        : Mathf.Lerp(velocity, -1f, Time.deltaTime * (5f / velocitySmoothnes));
                }
                else if (targetPosition.z > head.position.z)
                {
                    velocity = velocitySmoothnes == 0f ? 1f
                        : Mathf.Lerp(velocity, 1f, Time.deltaTime * (5f / velocitySmoothnes));
                }

                Traverse.Create(__instance).Field("velocity").SetValue(velocity);

                // (Work-in-progress)
                // Jump if a wall is in the way of the move direction

                //var cubeLayerBitMask = 1 << 23; // Cube bitmask
                //RaycastHit cubeHit;
                //var transformDirection = head.transform.TransformDirection(velocity * Vector3.forward);
                //if (transformDirection != Vector3.up && transformDirection != Vector3.down && Physics.Linecast(head.position, transformDirection, out cubeHit, cubeLayerBitMask) == false)
                //{
                //  Gizmos.DrawLine(head.position, transformDirection, Color.red);
                //  controller.Jump(false, false);
                //}

                controller.Move(velocity);
            }

            // Jump if the target's head is above
            if (targetPosition.y > head.position.y + jumpOffset)
            {
                controller.Jump(false, false);
            }

            attacking = false;
            Traverse.Create(__instance).Field("attacking").SetValue(attacking);

            if (!behaviourTarget && canAttack && startAttackDelay < 0f)
            {
                var currentAttackRange = range;
                attacking = true;
                Traverse.Create(__instance).Field("attacking").SetValue(attacking);

                //if (Singleton<TrainerOptions>.Instance.AiAggressiveEnabled)
                //{
                //    reactionTime = 0.0f;
                //}

                reactionTime = 0.4f;

                if (fighting.weapon)
                {
                    if (fighting.weapon.isGun)
                    {
                        currentAttackRange = 25f;
                        reactionTime = 0.25f;
                    }
                    else
                    {
                        currentAttackRange = 2f;
                        reactionTime = 0.25f;
                    }
                }
                Traverse.Create(__instance).Field("reactionTime").SetValue(reactionTime);

                var cubeLayerBitMask = 1 << 23; // Cube bitmask
                RaycastHit cubeHit;
                // Check that the target is in direct line of sight and not obstructed by a wall (cube)
                if (Physics.Linecast(head.position, targetPosition, out cubeHit, cubeLayerBitMask) == false)
                {
                    // Perform an attack if the target is still present and is within range
                    if (target && Vector3.Distance(head.position, targetPosition) < currentAttackRange && targetPosition.y - head.position.y < heightRange)
                    {
                        reactionCounter += Time.deltaTime;

                        if (reactionCounter > reactionTime)
                        {
                            reactionCounter = 0f;
                            controller.Attack();
                        }
                    }
                    else if (reactionCounter > 0f)
                    {
                        reactionCounter -= Time.deltaTime;
                    }
                    Traverse.Create(__instance).Field("reactionCounter").SetValue(reactionCounter);
                }
            }
        }
        else if (!behaviourTarget)
        {
            var closestTargetDistance = 100f;
            WeaponPickUp weaponPickUp = null;
            var weapons = Traverse.Create(fighting).Field("weapons").GetValue<Weapons>();

            if (goForGuns)
            {
                weaponPickUp = UnityEngine.Object.FindObjectOfType<WeaponPickUp>();
            }

            if (weaponPickUp && weaponPickUp.transform.position.y < 10f && fighting.weapon == null)
            {
                // Ensure that the AI has this type of weapon and can pick it up (some AI prefabs have less weapons than others).
                if (weaponPickUp.id < weapons.transform.childCount)
                {
                    target = weaponPickUp.GetComponent<Rigidbody>();
                    Traverse.Create(__instance).Field("target").SetValue(target);
                    return;
                }
            }

            // Find a PC / player to attack
            foreach (var playerController in controllerHandler.players)
            {
                if (playerController != null)
                {
                    var playerCharacterInformation = playerController.GetComponent<CharacterInformation>();

                    // Set target that is not itself and not dead.
                    if (playerController != controller && !playerCharacterInformation.isDead)
                    {
                        var torsoTransform = playerController.GetComponentInChildren<Torso>().transform;
                        var targetDistance = Vector3.Distance(head.position, torsoTransform.position);

                        if (targetDistance < closestTargetDistance)
                        {
                            closestTargetDistance = targetDistance;
                            target = torsoTransform.GetComponent<Rigidbody>();
                            targetInformation = playerCharacterInformation;
                            Traverse.Create(__instance).Field("target").SetValue(target);
                            Traverse.Create(__instance).Field("targetInformation").SetValue(targetInformation);
                        }
                    }
                }
            }

            // Find an NPC to attack
            // Todo: The choice between attempting to first target a Player/PC or NPC could be randomized.
            // Todo: The below works, however the NPCs need to be on different GameObject layers for them to be able to collide (refer to Controller.SetCollision)

            //if (this.target == null)
            //{
            //    foreach (var characterAlive in MultiplayerManager.mGameManager.hoardHandler.charactersAlive)
            //    {
            //        if (characterAlive != null && characterAlive != this.controller)
            //        {
            //            var characterInformation = characterAlive.GetComponent<CharacterInformation>();
            //            if (!characterInformation.isDead)
            //            {
            //                var torsoTransform = characterAlive.GetComponentInChildren<Torso>().transform;
            //                var targetDistance = Vector3.Distance(this.head.position, torsoTransform.position);
            //                if (targetDistance < closestTargetDistance)
            //                {
            //                    closestTargetDistance = targetDistance;
            //                    this.target = torsoTransform.GetComponent<Rigidbody>();
            //                    this.targetInformation = characterInformation;
            //                }
            //            }
            //        }
            //    }
            //}

        }
    }
}
