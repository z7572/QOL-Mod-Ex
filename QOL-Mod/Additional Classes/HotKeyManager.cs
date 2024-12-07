using System;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using QOL;

public class HotKeyManager : MonoBehaviour
{
    // TODO: Add keybinds to config
    private KeyCode nextWeaponKey = KeyCode.E;
    private KeyCode prevWeaponKey = KeyCode.Q;
    private float keyHoldTime = 0f;
    private const float HoldThreshold = 0.5f;
    public int CurrentWeaponIndex = 0;

    private ControllerHandler controllerHandler;

    private void Start()
    {
        controllerHandler = FindObjectOfType<ControllerHandler>();
        if (controllerHandler == null)
        {
            Debug.LogError("ControllerHandler not found!");
        }
    }

    private void Update()
    {
        // Switch weapon
        if (!ChatManager.isTyping && controllerHandler != null)
        {
            foreach (Controller controller in controllerHandler.ActivePlayers)
            {
                if (!controller.HasControl) continue;

                var fighting = Utils.GetFieldValue<Fighting>(controller, "fighting");
                if (fighting == null) continue;

                if (Input.GetKey(prevWeaponKey))
                {
                    keyHoldTime += Time.deltaTime;
                    if (Input.GetKeyDown(prevWeaponKey) || keyHoldTime > HoldThreshold)
                    {
                        if (fighting.CurrentWeaponIndex != 0)
                        {
                            CurrentWeaponIndex = fighting.CurrentWeaponIndex; // Update current weapon index(except for punch)
                        }
                        if (CurrentWeaponIndex != 0 && fighting.CurrentWeaponIndex == 0)
                        {
                            SwitchWeapon(0, fighting); // Switch to thrown weapon
                        }
                        else
                        {
                            SwitchWeapon(-1, fighting);
                        }
                    }
                }
                else if (Input.GetKeyUp(prevWeaponKey))
                {
                    keyHoldTime = 0f;
                }

                if (Input.GetKey(nextWeaponKey))
                {
                    keyHoldTime += Time.deltaTime;
                    if (Input.GetKeyDown(nextWeaponKey) || keyHoldTime > HoldThreshold)
                    {
                        if (fighting.CurrentWeaponIndex != 0)
                        {
                            CurrentWeaponIndex = fighting.CurrentWeaponIndex;
                        }
                        if (CurrentWeaponIndex != 0 && fighting.CurrentWeaponIndex == 0)
                        {
                            SwitchWeapon(0, fighting);
                        }
                        else
                        {
                            SwitchWeapon(1, fighting);
                        }
                        CurrentWeaponIndex = fighting.CurrentWeaponIndex;
                    }
                }
                else if (Input.GetKeyUp(nextWeaponKey))
                {
                    keyHoldTime = 0f;
                }
            }
        }
    }

    /// <param name="direction">1 for next, -1 for previous</param>
    private void SwitchWeapon(int direction, Fighting fighting)
    {
        var weapons = Utils.GetFieldValue<Weapons>(fighting, "weapons").transform;

        int weaponCount = weapons.childCount + 1; // +1 for the empty slot(punch)

        CurrentWeaponIndex += direction;

        if (CurrentWeaponIndex < 0)
        {
            CurrentWeaponIndex = weaponCount - 1;
        }
        else if (CurrentWeaponIndex >= weaponCount)
        {
            CurrentWeaponIndex = 0;
        }

        fighting.Dissarm();
        fighting.NetworkPickUpWeapon((byte)CurrentWeaponIndex);

        var weapon = Utils.GetFieldValue<Weapon>(fighting, "weapon");
        if (weapon == null || string.IsNullOrEmpty(weapon.name)) return;

        var weaponName = weapon.name;
        if (weaponName.Contains(" "))
        {
            weaponName = weaponName.Insert(weaponName.IndexOf(" "), ".");
        }

        Helper.SendModOutput(weaponName, Command.LogType.Success, false);
    }
}
