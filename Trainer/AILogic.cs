using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QOL.Trainer.Patches;

namespace QOL.Trainer;

public class AILogic(AI aiInstance)
{
    public static float MapSizeMultiplier = 1f;
    private static HashSet<RayCastForward> _bulletCache = new HashSet<RayCastForward>();
    private ushort _lastTargetID = ushort.MaxValue;

    // Weak weapon IDs that bots may throw away
    private static readonly HashSet<int> _weakWeaponIDs =
    [
        1,  // Pistol
        3,  // Sword
        13, // Thruster
        22, // Time Bubble
        23, // RPG
        24, // Flame Thrower
        25, // Snake Gun
        26, // Snake Grenade
        27, // Snake Launcher
        28, // Glue Gun
        34, // Snake Minigun
        64, // Spear
        65, // Flying Snake
    ];

    public static void RegisterBullet(RayCastForward bullet)
    {
        if (bullet != null) _bulletCache.Add(bullet);
    }

    public static void UnregisterBullet(RayCastForward bullet)
    {
        if (bullet != null) _bulletCache.Remove(bullet);
    }

    public static void AttachTracker(GameObject bulletObj)
    {
        if (bulletObj != null && bulletObj.GetComponent<BulletTracker>() == null)
        {
            bulletObj.AddComponent<BulletTracker>();
        }
    }

    public bool Update(
        ref float reactionTime,
        ref float velocitySmoothnes,
        ref float preferredRange,
        ref float heightRange,
        ref bool canAttack,
        ref float range,
        ref float reactionHitReset,
        ref float jumpOffset,
        ref float targetingSmoothing,
        ref bool goForGuns,
        ref bool attacking,
        ref bool dontAimWhenAttacking,
        ref float startAttackDelay,
        ref Transform behaviourTarget,
        ref Rigidbody target,
        ref float velocity,
        ref ControllerHandler controllerHandler,
        ref Controller controller,
        ref Transform aimer,
        ref Fighting fighting,
        ref float reactionCounter,
        ref Movement movement,
        ref Transform head,
        ref CharacterInformation info,
        ref CharacterInformation targetInformation,
        ref float counter)
    {
        EnsureHealthHandlerRef(aiInstance, controller);

        startAttackDelay -= Time.deltaTime;

        var targetPosition = GetTargetPosition(behaviourTarget, target);

        if (counter > 1f)
        {
            counter = Random.Range(-0.5f, 0.5f);
            target = null;
        }

        if (targetPosition != Vector3.zero && (!targetInformation || !targetInformation.isDead))
        {
            FixFlyState(controller, info);

            // Handle Aiming
            HandleAiming(aimer, head, targetPosition, targetingSmoothing, dontAimWhenAttacking, fighting);

            counter += Time.deltaTime;

            // Move towards the target
            HandleMovement(controller, head, aimer, fighting, targetPosition, behaviourTarget, target, preferredRange, velocitySmoothnes, ref velocity, ref info);

            // Attack logic
            HandleCombat(controller, fighting, aimer, head, targetPosition, target, canAttack, range, jumpOffset, heightRange, behaviourTarget,
                         ref attacking, ref reactionTime, ref reactionCounter, ref startAttackDelay, velocity);
        }

        if (behaviourTarget) return false;

        // Move to guns
        if (HandleWeaponPickup(fighting, goForGuns, ref target))
        {
            return false;
        }

        // Scan logic
        ScanForTargets(aiInstance, controller, head, controllerHandler, ref target, ref targetInformation);

        // [Changed] Now uses instance method with internal state
        HandleTargetChange(controller, target, controllerHandler);

        HandleSmartBlocking(controller, fighting, aimer, head, target, info);

        return false;
    }

    private void EnsureHealthHandlerRef(AI instance, Controller controller)
    {
        var hh = controller.GetComponent<HealthHandler>();
        if (hh != null && controller.isAI)
        {
            var aiField = AccessTools.Field(typeof(HealthHandler), "ai");
            if (aiField.GetValue(hh) == null)
            {
                aiField.SetValue(hh, instance);
            }
        }
    }

    private Vector3 GetTargetPosition(Transform behaviourTarget, Rigidbody target)
    {
        if (behaviourTarget) return behaviourTarget.position;
        if (target) return target.position;
        return Vector3.zero;
    }

    private void FixFlyState(Controller controller, CharacterInformation info)
    {
        if (controller.canFly)
            info.paceState = 1;
        else
            info.paceState = 0;
    }

    private void HandleAiming(Transform aimer, Transform head, Vector3 targetPosition, float smoothing, bool dontAimWhenAttacking, Fighting fighting)
    {
        if (!dontAimWhenAttacking || !fighting.isSwinging)
        {
            if (smoothing == 0f)
            {
                aimer.rotation = Quaternion.LookRotation(targetPosition - head.position);
            }
            else
            {
                aimer.rotation = Quaternion.Lerp(aimer.rotation, Quaternion.LookRotation(targetPosition - head.position), Time.deltaTime * (5f / smoothing));
            }
        }
    }

    private void HandleMovement(Controller controller, Transform head, Transform aimer, Fighting fighting, Vector3 targetPosition,
        Transform behaviourTarget, Rigidbody target, float preferredRange, float smoothness, ref float velocity, ref CharacterInformation info)
    {
        // Anti-Falling
        var rb = controller.GetComponentInChildren<Torso>().GetComponent<Rigidbody>();
        if (rb != null && rb.velocity.y < -3f && CheckDeathPit(controller))
        {
            HandleFallingRecovery(controller, aimer, fighting, target);

            Vector3 moveDir = (targetPosition - head.position);
            moveDir.y = 0;
            moveDir.Normalize();

            float forwardDot = Vector3.Dot(controller.transform.forward, moveDir);
            float moveInput = forwardDot > 0 ? 1f : -1f;

            controller.Move(moveInput);

            return;
        }

        float dist = Vector3.Distance(head.position, targetPosition);

        if (dist > preferredRange)
        {
            float desiredInput = 0f;

            if (targetPosition.z < head.position.z)
            {
                desiredInput = -1f;
                velocity = smoothness == 0f ? -1f
                    : Mathf.Lerp(velocity, -1f, Time.deltaTime * (5f / smoothness));
            }
            else if (targetPosition.z > head.position.z)
            {
                desiredInput = 1f;
                velocity = smoothness == 0f ? 1f
                    : Mathf.Lerp(velocity, 1f, Time.deltaTime * (5f / smoothness));
            }

            // 1. Calculate Move Direction Vector (Z-axis based on velocity sign)
            var moveDirectionVector = new Vector3(0f, 0f, velocity >= 0 ? 1f : -1f);

            var desiredDirectionVector = new Vector3(0f, 0f, desiredInput);

            // 2. Punch Move (Ground Acceleration)
            if (dist > 3f && info.isGrounded)
            {
                PunchMove(controller, aimer, fighting, moveDirectionVector);
            }

            // 3. Execute JumpOverWall logic
            Transform currentTargetTransform = (target != null) ? target.transform : behaviourTarget;

            // Pass desiredDirectionVector to be not stuck on a wall
            JumpOverWall(controller, currentTargetTransform, velocity, aimer, fighting, desiredDirectionVector);

            controller.Move(velocity);
        }
    }

    private void HandleCombat(Controller controller, Fighting fighting, Transform aimer, Transform head, Vector3 targetPosition, Rigidbody target,
        bool canAttack, float range, float jumpOffset, float heightRange, Transform behaviourTarget,
        ref bool attacking, ref float reactionTime, ref float reactionCounter, ref float startAttackDelay, float velocity)
    {
        // Jump if the target's head is above
        if (targetPosition.y > head.position.y + jumpOffset)
        {
            var moveDirection = new Vector3(0f, 0f, velocity >= 0 ? 1f : -1f);
            PunchJump(controller, aimer, fighting, moveDirection);
        }

        attacking = false;

        // Only attack if not following a dummy behaviour point, allowed to attack, and delay is over
        if (!behaviourTarget && canAttack && startAttackDelay < 0f)
        {
            var currentAttackRange = range;
            attacking = true;

            // Default reaction settings
            reactionTime = 0.4f;
            bool isGun = false;

            if (fighting.weapon)
            {
                if (fighting.weapon.isGun)
                {
                    isGun = true;
                    currentAttackRange = 25f; // Gun range
                    reactionTime = 0.1f; // Faster reaction for guns
                }
                else
                {
                    currentAttackRange = 2f; // Melee range
                    reactionTime = 0.25f;
                }

                if (target != null)
                {
                    bool shouldThrow = false;
                    float dist = Vector3.Distance(head.position, targetPosition);

                    // Melee Throw
                    if (dist < 3.0f)
                    {
                        if (Random.value < 0.02f)
                        {
                            shouldThrow = true;
                            controller.GetComponentInChildren<ChatManager>()?.Talk("!Melee Throw!");
                        }
                    }

                    // Discard Weak Weapon
                    if (_weakWeaponIDs.Contains(fighting.CurrentWeaponIndex))
                    {
                        if (Random.value < 0.03f)
                        {
                            shouldThrow = true;
                            controller.GetComponentInChildren<ChatManager>()?.Talk("!Throwing Weak Weapon!");
                        }
                    }

                    if (shouldThrow)
                    {
                        HandleThrowWeapon(controller, aimer, target);
                        return;
                    }
                }
            }

            // Check that the target is in direct line of sight and not obstructed by a wall (cube) or map objects
            var mask = (1 << 0) | (1 << 23); // Default + Map layers

            if (Physics.Linecast(head.position, targetPosition, out _, mask) == false)
            {
                float dist = Vector3.Distance(head.position, targetPosition);
                float heightDiff = targetPosition.y - head.position.y;

                // Check ranges
                if (target && dist < currentAttackRange && heightDiff < heightRange)
                {
                    // Smart Shooting Logic based on Weapon Type
                    if (isGun && fighting.weapon != null)
                    {
                        Weapon w = fighting.weapon;
                        Vector3 toTarget = (targetPosition - head.position).normalized;

                        // Check if we are actually aiming at the target (prevent shooting walls while turning)
                        bool isAimingAtTarget = Vector3.Dot(aimer.forward, toTarget) > 0.95f;

                        if (isAimingAtTarget)
                        {
                            if (w.fullAuto)
                            {
                                controller.Attack();
                                reactionCounter = 0f;
                            }
                            else if (w.isCharged)
                            {
                                if (w.currentCharge < w.maxChargeTime)
                                {
                                    controller.Attack();
                                }
                                reactionCounter = 0f;
                            }
                            else
                            {
                                if (w.sinceShot > w.cd)
                                {
                                    reactionCounter += Time.deltaTime;
                                    if (reactionCounter > reactionTime)
                                    {
                                        controller.Attack();
                                        reactionCounter = 0f;
                                    }
                                }
                            }
                        }
                    }
                    // Melee Logic
                    else
                    {
                        reactionCounter += Time.deltaTime;
                        if (reactionCounter > reactionTime)
                        {
                            reactionCounter = 0f;
                            controller.Attack();
                        }
                    }
                }
                else if (reactionCounter > 0f)
                {
                    reactionCounter -= Time.deltaTime;
                }
            }
        }
    }

    private bool HandleWeaponPickup(Fighting fighting, bool goForGuns, ref Rigidbody target)
    {
        if (!goForGuns) return false;

        if (fighting.weapon != null) return false;

        var weapons = Traverse.Create(fighting).Field("weapons").GetValue<Weapons>();
        var weaponPickUp = Object.FindObjectOfType<WeaponPickUp>();

        if (weaponPickUp)
        {
            Vector3 pos = weaponPickUp.transform.position;
            float limitY = 10f * MapSizeMultiplier;
            float limitZ = 18f * MapSizeMultiplier;

            bool isSafeToPick = Mathf.Abs(pos.y) < limitY && Mathf.Abs(pos.z) < limitZ;

            if (!isSafeToPick)
            {
                return false;
            }

            if (weaponPickUp.id < weapons.transform.childCount)
            {
                target = weaponPickUp.GetComponent<Rigidbody>();
                return true;
            }
        }

        return false;
    }

    private void ScanForTargets(AI instance, Controller controller, Transform head, ControllerHandler controllerHandler,
        ref Rigidbody target, ref CharacterInformation targetInformation)
    {
        var closestTargetDistance = 100f;

        // Find a PC / player to attack
        foreach (var playerController in controllerHandler.players)
        {
            if (playerController == null) continue;

            // Set target that is not itself and not dead.
            var playerCharacterInformation = playerController.GetComponent<CharacterInformation>();
            if (playerController == controller || playerCharacterInformation.isDead) continue;

            CheckAndSetTarget(instance, controller, head, playerController, playerCharacterInformation, ref target, ref targetInformation, ref closestTargetDistance);
        }

        // Find an NPC (black) to attack
        if (target == null)
        {
            var charactersAlive = Traverse.Create(GameManager.Instance).Field("hoardHandler").Field("charactersAlive").GetValue<List<Controller>>();
            foreach (var characterAlive in charactersAlive)
            {
                if (characterAlive == null || characterAlive == controller) continue;

                var characterInformation = characterAlive.GetComponent<CharacterInformation>();
                if (characterInformation.isDead) continue;

                CheckAndSetTarget(instance, controller, head, characterAlive, characterInformation, ref target, ref targetInformation, ref closestTargetDistance);
            }
        }
    }

    private void CheckAndSetTarget(AI instance, Controller controller, Transform head, Controller potentialTarget, CharacterInformation pInfo,
        ref Rigidbody target, ref CharacterInformation targetInfo, ref float closestDist)
    {
        var torso = potentialTarget.GetComponentInChildren<Torso>();
        if (torso == null) return;

        var dist = Vector3.Distance(head.position, torso.transform.position);

        if (dist < closestDist)
        {
            closestDist = dist;
            target = torso.GetComponent<Rigidbody>();
            targetInfo = pInfo;
        }
    }

    private void HandleTargetChange(Controller controller, Rigidbody target, ControllerHandler controllerHandler)
    {
        if (target == null) return;
        var targetController = target.transform.root.GetComponentInChildren<Controller>();
        if (targetController == null) return;

        var newTargetID = (ushort)targetController.playerID;

        // Only check internal state
        if (newTargetID != _lastTargetID)
        {
            _lastTargetID = newTargetID;

            var chat = controller.GetComponentInChildren<ChatManager>();
            if (chat != null)
            {
                bool isPlayer = controllerHandler.players.Contains(targetController);

                if (!isPlayer)
                {
                    chat.Talk($"Targeting NPC");
                }
                else
                {
                    chat.Talk($"Targeting: {Helper.GetColorFromID(newTargetID)}");
                }
            }
        }
    }

    private void JumpOverWall(Controller controller, Transform target, float velocity, Transform aimer, Fighting fighting, Vector3 moveDirection)
    {
        var torso = controller.GetComponentInChildren<Torso>();
        if (torso == null) return;
        var torsoPos = torso.transform.position;

        if (target != null)
        {
            var isCharacter = target.GetComponentInParent<CharacterInformation>() != null;
            if (isCharacter)
            {
                var toTarget = target.position - torsoPos;
                if (Vector3.Angle(Vector3.down, toTarget) <= 5f)
                {
                    controller.Down();
                    return;
                }
            }
        }

        var mask = (1 << 0) | (1 << 23); // Default + Map layers
        var rayEnd = torsoPos + moveDirection * 0.5f;

        if (Physics.Linecast(torsoPos, rayEnd, mask))
        {
            PunchJump(controller, aimer, fighting, moveDirection);
        }
    }

    private void PunchMove(Controller controller, Transform aimer, Fighting fighting, Vector3 moveDirection)
    {
        if (fighting.weapon != null) return;

        // Aim horizontally in the direction of movement
        aimer.rotation = Quaternion.LookRotation(moveDirection);

        // Punch to gain forward momentum
        controller.Attack();
    }

    private void PunchJump(Controller controller, Transform aimer, Fighting fighting, Vector3 moveDirection)
    {
        if (Random.Range(0, 10) < 3)
        {
            PunchBlockJump(controller, aimer, fighting, moveDirection);
            return;
        }

        if (fighting.weapon != null)
        {
            controller.Jump(false, false);
            return;
        }

        var rb = controller.GetComponent<Rigidbody>();
        if (rb != null && rb.velocity.y < -0.1f) return; // Don't punch if falling

        Vector3 aimDir = Vector3.up + moveDirection.normalized;
        aimer.rotation = Quaternion.LookRotation(aimDir);
        controller.Attack();
        controller.Jump(false, false);
    }

    private void PunchBlockJump(Controller controller, Transform aimer, Fighting fighting, Vector3 moveDirection)
    {
        if (fighting.weapon != null)
        {
            controller.Jump(false, false);
            return;
        }

        var rb = controller.GetComponent<Rigidbody>();
        if (rb != null && rb.velocity.y < -0.1f) return; // Don't punch if falling

        CoroutineRunner.Run(PunchBlockJumpCoroutine(controller, aimer, fighting, moveDirection));
    }

    private IEnumerator PunchBlockJumpCoroutine(Controller controller, Transform aimer, Fighting fighting, Vector3 moveDirection)
    {
        Vector3 aimDir = Vector3.up + moveDirection.normalized;
        aimer.rotation = Quaternion.LookRotation(aimDir);

        controller.Attack();

        var blockHandler = controller.GetComponent<BlockHandler>();
        var c = Traverse.Create(blockHandler).Field("c").GetValue<float>();

        if ((fighting.isSwinging || fighting.counter > 0f) && c > 0.3f)
        {
            controller.StartBlock();
            controller.Jump(false, false);
            yield return new WaitForSeconds(0.1f);
            controller.EndBlock();
        }
        else
        {
            controller.Jump(false, false);
        }
    }

    private void HandleSmartBlocking(Controller controller, Fighting fighting, Transform aimer, Transform head, Rigidbody target, CharacterInformation info)
    {
        if (info.isDead) return;

        var blockHandler = controller.GetComponent<BlockHandler>();
        if (blockHandler == null) return;

        if (fighting.weapon != null)
        {
            if (blockHandler.isBlocking)
            {
                controller.EndBlock();
            }
            return;
        }

        var chat = controller.GetComponentInChildren<ChatManager>();
        bool shouldBlock = false;
        Vector3 targetBlockLookDir = Vector3.zero;

        foreach (var rcf in _bulletCache)
        {
            if (rcf == null || !rcf.enabled || !rcf.gameObject.activeInHierarchy) continue;

            if (Vector3.Distance(head.position, rcf.transform.position) > 15.0f) continue;

            var projCol = Traverse.Create(rcf).Field("projectileCollision").GetValue<ProjectileCollision>();
            if (projCol == null) projCol = rcf.GetComponent<ProjectileCollision>();

            // Ignore own bullets
            if (projCol.damager == controller) continue;

            Vector3 bulletForward = rcf.transform.forward;
            Vector3 logicDir = bulletForward;

            // Apply Gravity correction if present
            var gravityComp = rcf.GetComponent<Gravity>();
            if (gravityComp != null)
            {
                logicDir = (rcf.speed * bulletForward + gravityComp.force * Vector3.down).normalized;
            }

            float currentSpeed = rcf.speed;
            if (currentSpeed < 1f) continue;

            Vector3 vectorToMe = head.position - rcf.transform.position;
            float distance = vectorToMe.magnitude;

            if (Vector3.Dot(logicDir, vectorToMe.normalized) > 0.9f)
            {
                if (Vector3.Dot(bulletForward, vectorToMe) > 0)
                {
                    float timeToImpact = distance / currentSpeed;

                    if (timeToImpact < 0.25f)
                    {
                        targetBlockLookDir = -logicDir;
                        shouldBlock = true;
                        chat?.Talk($"!PARRY! T:{timeToImpact:F2}");
                        break;
                    }
                }
            }
        }

        if (!shouldBlock && target != null)
        {
            var enemyFighting = target.GetComponent<Fighting>();
            if (enemyFighting == null) enemyFighting = target.transform.root.GetComponent<Fighting>();

            if (enemyFighting != null)
            {
                float distToEnemy = Vector3.Distance(head.position, target.position);
                if (distToEnemy < 4.0f && (enemyFighting.isSwinging || enemyFighting.counter > 0.05f))
                {
                    var enemyAimTarget = target.transform.root.GetComponentInChildren<AimTarget>();
                    if (enemyAimTarget != null)
                    {
                        Vector3 enemyAttackDir = enemyAimTarget.transform.forward;
                        Vector3 dirToMe = (head.position - target.position).normalized;
                        if (Vector3.Dot(enemyAttackDir, dirToMe) > 0.4f)
                        {
                            shouldBlock = true;
                            targetBlockLookDir = -enemyAttackDir;
                        }
                    }
                }
            }
        }

        if (shouldBlock)
        {
            if (targetBlockLookDir != Vector3.zero)
            {
                aimer.rotation = Quaternion.LookRotation(targetBlockLookDir);
            }
            if (!blockHandler.isBlocking)
            {
                controller.StartBlock();
            }
        }
        else
        {
            if (blockHandler.isBlocking && blockHandler.sinceBlockStart > 0.25f)
            {
                controller.EndBlock();
            }
        }
    }

    private void HandleThrowWeapon(Controller controller, Transform aimer, Rigidbody targetEnemy)
    {
        if (targetEnemy != null)
        {
            Vector3 targetPos = targetEnemy.position;
            var headPart = targetEnemy.GetComponentInChildren<Head>();
            if (headPart) targetPos = headPart.transform.position;

            float g = Mathf.Abs(Physics.gravity.y);

            // Use internal logic (assuming CheatHelper logic is moved here or available externally)
            // For completeness, I'll call the external helper as per previous context
            Vector3 throwDir = CheatHelper.CalculateSimulatedThrowDir(aimer.position, targetPos, g);
            Vector3 initialVel = throwDir * 35f;
            float distToTarget = Vector3.Distance(aimer.position, targetPos);

            if (!CheatHelper.IsTrajectoryClear(aimer.position, initialVel, g, distToTarget))
            {
                return;
            }

            aimer.rotation = Quaternion.LookRotation(throwDir);
            controller.Throw();
        }
        else
        {
            // Blind throw
            Vector3 forwardThrow = controller.transform.forward + Vector3.up * 0.2f;
            aimer.rotation = Quaternion.LookRotation(forwardThrow);
            controller.Throw();
        }
    }

    private void HandleFallingRecovery(Controller controller, Transform aimer, Fighting fighting, Rigidbody targetEnemy)
    {
        if (fighting.weapon != null)
        {
            controller.GetComponent<ChatManager>()?.Talk("!Falling Throw!");
            HandleThrowWeapon(controller, aimer, targetEnemy);
            return;
        }

        controller.GetComponent<ChatManager>()?.Talk("!I'm Falling!");
        aimer.rotation = Quaternion.LookRotation(Vector3.up);
        controller.Attack();
        if (fighting.isSwinging || fighting.counter > 0f)
        {
            var bh = controller.GetComponent<BlockHandler>();
            if (bh && !bh.isBlocking) controller.StartBlock();
        }
    }

    private bool CheckDeathPit(Controller controller)
    {
        var torso = controller.GetComponentInChildren<Torso>();
        if (!torso) return false;

        Vector3 rayDir = Vector3.down;
        RaycastHit hit;

        if (Physics.Raycast(torso.transform.position, rayDir, out hit, 20f))
        {
            if (hit.collider.gameObject.name == "Death")
            {
                return true;
            }
        }
        return false;
    }
}