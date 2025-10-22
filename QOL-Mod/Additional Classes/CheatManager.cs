using System;
using System.Reflection;
using UnityEngine;
using HarmonyLib;

namespace QOL;

public class CheatManager : MonoBehaviour
{
    // TODO: Add keybinds to config
    private const KeyCode prevWeaponKey = KeyCode.Q;
    private const KeyCode nextWeaponKey = KeyCode.E;
    private float keyHoldTime = 0f;
    private const float HoldThreshold = 0.5f;

    private MapWrapper _currentMap;
    private Controller controller;
    private Fighting fighting;

    private void Start()
    {
        controller = gameObject.GetComponent<Controller>();
        fighting = Traverse.Create(controller).Field("fighting").GetValue<Fighting>();
    }

    private void Update()
    {
        if (controller != null && !ChatManager.isTyping && !PauseManager.isPaused)
        {
            if (ChatCommands.CmdDict["scrollattack"].IsEnabled)
            {
                if (Input.GetAxis("Mouse ScrollWheel") != 0f)
                {
                    if (fighting.weapon == null || !fighting.weapon.isEnergyBased)
                    {
                        controller.Attack();
                    }
                }
            }

            if (ChatCommands.CmdDict["switchweapon"].IsEnabled)
            {
                if (Input.GetKey(prevWeaponKey))
                {
                    keyHoldTime += Time.deltaTime;
                    if (Input.GetKeyDown(prevWeaponKey) || keyHoldTime > HoldThreshold)
                    {
                        if (fighting.CurrentWeaponIndex != 0)
                        {
                            CheatHelper.SwitcherWeaponIndex = fighting.CurrentWeaponIndex; // Update current weapon index(except for punch)
                        }
                        if (CheatHelper.SwitcherWeaponIndex != 0 && fighting.CurrentWeaponIndex == 0)
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
                            CheatHelper.SwitcherWeaponIndex = fighting.CurrentWeaponIndex;
                        }
                        if (CheatHelper.SwitcherWeaponIndex != 0 && fighting.CurrentWeaponIndex == 0)
                        {
                            SwitchWeapon(0, fighting);
                        }
                        else
                        {
                            SwitchWeapon(1, fighting);
                        }
                        CheatHelper.SwitcherWeaponIndex = fighting.CurrentWeaponIndex;
                    }
                }
                else if (Input.GetKeyUp(nextWeaponKey))
                {
                    keyHoldTime = 0f;
                }
            }
        }

        // Keep track of map / level changes
        var currentMap = GameManager.Instance.GetCurrentMap();
        if (_currentMap != currentMap)
        {
            _currentMap = currentMap;
            ReapplyToggleOptions();
        }

    }

    /// <param name="direction">1 for next, -1 for previous</param>
    private void SwitchWeapon(int direction, Fighting fighting)
    {
        var weapons = Traverse.Create(fighting).Field("weapons").GetValue<Weapons>().transform;

        int weaponCount = weapons.childCount + 1; // +1 for the empty slot(punch)

        CheatHelper.SwitcherWeaponIndex += direction;

        if (CheatHelper.SwitcherWeaponIndex < 0)
        {
            CheatHelper.SwitcherWeaponIndex = weaponCount - 1;
        }
        else if (CheatHelper.SwitcherWeaponIndex >= weaponCount)
        {
            CheatHelper.SwitcherWeaponIndex = 0;
        }

        fighting.Dissarm();
        fighting.NetworkPickUpWeapon((byte)CheatHelper.SwitcherWeaponIndex);

        var weapon = Traverse.Create(fighting).Field("weapon").GetValue<Weapon>();
        if (weapon == null || string.IsNullOrEmpty(weapon.name)) return;

        var weaponName = weapon.name;
        if (weaponName.Contains(" "))
        {
            weaponName = weaponName.Insert(weaponName.IndexOf(" "), ".");
        }

        Helper.SendModOutput(weaponName, Command.LogType.Success, false);
    }

    private void ToggleFly()
    {
        controller.canFly = ChatCommands.CmdDict["fly"].IsEnabled;
    }

    private void ReapplyToggleOptions()
    {
        ToggleFly();
    }
}