using HarmonyLib;
using UnityEngine;

namespace QOL.Trainer.Patches;

public static class AIPatch
{
    public static void Patch(Harmony harmonyInstance)
    {
        var updateMethod = AccessTools.Method(typeof(AI), "Update");
        var updateMethodPrefix = new HarmonyMethod(typeof(AIPatch).GetMethod(nameof(UpdateMethodPrefix)));
        harmonyInstance.Patch(updateMethod, prefix: updateMethodPrefix);
    }

    public static bool UpdateMethodPrefix(AI __instance, ref float ___reactionTime, ref float ___velocitySmoothnes, ref float ___preferredRange,
        ref float ___heightRange, ref bool ___canAttack, ref float ___range, ref float ___reactionHitReset, ref float ___jumpOffset,
        ref float ___targetingSmoothing, ref bool ___goForGuns, ref bool ___attacking, ref bool ___dontAimWhenAttacking, ref float ___startAttackDelay,
        ref Transform ___behaviourTarget, ref Rigidbody ___target, ref float ___velocity, ref ControllerHandler ___controllerHandler,
        ref Controller ___controller, ref Transform ___aimer, ref Fighting ___fighting, ref float ___reactionCounter, ref Movement ___movement,
        ref Transform ___head, ref CharacterInformation ___info, ref CharacterInformation ___targetInformation, ref float ___counter)
    {
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
        }

        if (targetPosition != Vector3.zero && (!___targetInformation || !___targetInformation.isDead))
        {
            ___info.paceState = 0;

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

                // (Work-in-progress)
                // Jump if a wall is in the way of the move direction

                //var cubeLayerBitMask = 1 << 23; // Cube bitmask
                //RaycastHit cubeHit;
                //var transformDirection = ___head.transform.TransformDirection(___velocity * Vector3.forward);
                //if (transformDirection != Vector3.up && transformDirection != Vector3.down && Physics.Linecast(___head.position, transformDirection, out cubeHit, cubeLayerBitMask) == false)
                //{
                //  Gizmos.DrawLine(___head.position, transformDirection, Color.red);
                //  ___controller.Jump(false, false);
                //}

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
                RaycastHit cubeHit;
                // Check that the target is in direct line of sight and not obstructed by a wall (cube)
                if (Physics.Linecast(___head.position, targetPosition, out cubeHit, cubeLayerBitMask) == false)
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
        else if (!___behaviourTarget)
        {
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
                if (playerController != null)
                {
                    var playerCharacterInformation = playerController.GetComponent<CharacterInformation>();

                    // Set target that is not itself and not dead.
                    if (playerController != ___controller && !playerCharacterInformation.isDead)
                    {
                        var torsoTransform = playerController.GetComponentInChildren<Torso>().transform;
                        var targetDistance = Vector3.Distance(___head.position, torsoTransform.position);

                        if (targetDistance < closestTargetDistance)
                        {
                            closestTargetDistance = targetDistance;
                            ___target = torsoTransform.GetComponent<Rigidbody>();
                            ___targetInformation = playerCharacterInformation;
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

        return false;
    }
}