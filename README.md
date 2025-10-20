# QOL-Mod
<p style="text-align: center;">
  <a href="https://forthebadge.com">
    <img src="https://forthebadge.com/images/badges/made-with-c-sharp.svg" alt="">
  </a>
</p>
<p style="text-align: center;">
  <a href="https://github.com/Mn0ky/QOL-Mod/releases/latest">
    <img src="https://img.shields.io/github/downloads/Mn0ky/QOL-Mod/total?label=Github%20downloads&logo=github" alt="">
  </a>
  <a href="https://www.gnu.org/licenses/lgpl-3.0">
    <img src="https://img.shields.io/badge/License-LGPL_v3-blue.svg" alt="">
  </a>
</p>

A mod that offers quality-of-life improvements and additions to [Stick Fight: The Game](https://store.steampowered.com/app/674940/Stick_Fight_The_Game/).<br/>
This is accomplished through a GUI menu but alternative chat commands are listed below.<br/>
To open the menu, use the default keybind: <kbd>LeftShift</kbd> + <kbd>F1</kbd><br/>

A previous message system allows you to use the <kbd>↑</kbd> & <kbd>↓</kbd> keys to easily return to your previous messages.<br/>
There is a maximum of **``20``** messages stored before they start being overwritten.<br/>

This mod is a plugin for [BepInEx](https://github.com/BepInEx/BepInEx) which is required to load it. Everything is patched at runtime.<br/>

## Installation

To install the mod, watch the video below, or follow the written steps:<br/> 
  1)  Download [BepInEx](https://github.com/BepInEx/BepInEx/releases/download/v5.4.19/BepInEx_x86_5.4.19.0.zip).
  2)  Extract the newly downloaded zip into the ``StickFightTheGame`` folder.
  3)  Drag all contents from the folder into the ``StickFightTheGame`` folder (``winhttp.dll``, ``doorstop_config.ini``, the ``BepInEx`` folder etc.).
  4)  Launch the game and then exit (BepInEx will have generated new files and folders).
  5)  Download the latest version of the QOL mod from the [Releases](https://github.com/Mn0ky/QOL-Mod/releases/latest) section.
  6)  Put the mod zip into the newly generated folder located at ``BepInEx/plugins`` and **<ins>extract it</ins> to a folder named ``QOL-MOD``** for BepInEx to load.
  7)  Start the game, join a lobby, and enjoy!

#### Installation Tutorial Video:

https://user-images.githubusercontent.com/67206766/161408215-1f6e3d3e-5424-4942-8a4c-0543906c8557.mp4

## Caveats

The following are some general things to take note of:
  - Both the ``/private`` & ``/public`` commands require you to be the host in order to function.
  - The ``/rich`` command only enables rich text for you, a.k.a client-side only.
  - The auto-translation feature uses the Google Translate API and has a rate-limit of **``100``** requests per hour.
  - A custom player color only shows for you, a.k.a client-side only.

## QOL Menu

This menu is the primary way to use and enable/disable features.<br/>
It can be opened with the keybind: <kbd>LeftShift</kbd> + <kbd>F1</kbd><br/>
An image below shows a visual overview:<br/>
![Image of QOL Menu](https://i.ibb.co/pXhrfN7/menu-v14.png)<br/>
Alternative chat commands are listed directly below.

## Chat Commands

| Command                           | Description                                                                                                                                                    |
| --------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Usage:**                        | ```/<command_name> <parameter> [optional parameter]```                                                                                                         |
| /adv                              | Outputs whatever you set it to in the config.                                                                                                                  |
| /alias                            |
| /config `<keys> [value]`          | Allows you to change the values of the config file. Empty value to return default.                                                                             |
| /deathmsg                         |
| /dm `<color> <message>`           | Direct messages a player.                                                                                                                                      |
| /fov                              | Set the FOV for the game.                                                                                                                                      |
| /fps                              | Set the FPS for the game.                                                                                                                                      |
| /friend `<color>`                 |                                                                                                                                                                |
| /gg                               | Enables automatic sending of "gg" upon death of mod user.                                                                                                      |
| /help                             | Opens up the Steam overlay and takes you to this page.                                                                                                         |
| /hp	`<color>`                     | Outputs the percent based health of the target color to chat. Leave as ``/hp`` to always get your own.                                                         |
| /id	`<color>`                     | Copies the Steam ID of the target player to clipboard.                                                                                                         |
| /invite                           | Generates a "join game" link and copies it to clipboard.                                                                                                       |
| /lobhp                            | Outputs the health set for the whole lobby.                                                                                                                    |
| /lobregen                         | Outputs whether or not regen is enabled for the lobby.                                                                                                         |
| /logprivate                       |
| /logpublic                        |
| /lowercase                        | Enables/disables lowercase mode, which has your chat messages always sent in lowercase. Useful for those who keep pressing the caps-lock key.                  |
| /nuky                             | Lets you talk like Nuky. Splits up any message you send and outputs it word by word.                                                                           |
| /maps `<preset\|save\|remove>`    |
| /mute `<color>`                   | The targeted player's messages wont appear, making them "muted" for you (**client-side only**. A mute only lasts for the lobby you're currently in).           |
| /music skip                       |
| /music loop `[song_index]`        |
| /music play `<song_index>`        |
| /ouchmsg                          |
| /ping `<color>`                   | Outpus the ping for the targeted player.                                                                                                                       |
| /private                          | Privates the current lobby (**must be host**).                                                                                                                 |
| /public                           | Opens the current lobby to the public (**must be host**).                                                                                                      |
| /rainbow                          | Enables/disables rainbow mode. Dynamically shifts your player color through the color spectrum (the shifting speed of the colors is changeable in the config). |
| /resolution `<width> <height>`    | Sets the resolution of the game.                                                                                                                               |
| /rich                             | Enables rich text for chat (**client-side only**).                                                                                                             |
| /shrug `[message]`                | Appends ¯\\\_☹\_/¯ to the end of the typed message (changeable in config).                                                                                     |
| /stat `<color> <stat_type>`       | Gets the targeted stat of the targeted player. Open the stat menu to see a list of different stat names.                                                       |
| /suicide                          | Kills the user.                                                                                                                                                |
| /translate                        | Enables auto-translation for messages from others to English.                                                                                                  |
| /uncensor                         | Disables chat censorship.                                                                                                                                      |
| /uwu                              | *uwuifies* any message you send.                                                                                                                               |
| /ver                              | Outputs the mod version string.                                                                                                                                |
| /weapons `<preset>\|save\|remove` |
| /winnerhp                         | Outputs the winner's hp at the end of every round.                                                                                                             |
| /winstreak                        | Enables winstreak mode.                                                                                                                                        |

## Cheat Commands

| Command                                                                  | Description                                                                                                                                                 |
| ------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| /afk                                                                     | Toggle AFK mode. Assign AI to the player. Not completed.                                                                                                    |
| /pkg `<color\|all> <x> <y> <Vx> <Vy> [weaponIndex\|-1] [isLocalDisplay]` | Send a fire package to a specified player (or all) with bullet position, velocity, and weapon type (-1 to set current weapon).                              |
| /bullethell `<color\|all> [isLocalDisplay]`                              | Send all map of Beam ray to a specified player (or all).                                                                                                    |
| /bulletring `[weaponIndex] [radius]`                                     | Send a ring of bullets to all players. (default radius: 200)                                                                                                |
| /execute `<color> <command\|say> [parameter]`                            | Execute a command as the specified player.                                                                                                                  |
| /god                                                                     | Toggle god mode.                                                                                                                                            |
| /fullauto                                                                | Toggle full auto.                                                                                                                                           |
| /norecoil                                                                | Toggle no recoil.                                                                                                                                           |
| /infiniteammo                                                            | Toggle infinite ammo.                                                                                                                                       |
| /fastfire                                                                | Toggle fast firing.                                                                                                                                         |
| /fastpunch                                                               | Toggle fast punching.                                                                                                                                       |
| /blockall                                                                | Toggle all degrees and no cooldown blocking.                                                                                                                |
| /fly                                                                     | Toggle fly mode.                                                                                                                                            |
| /gun `[weaponIndex]`                                                     | Give the player the specified weapon (empty to clear, -1 to random).                                                                                        |
| /kick `<color> [type]`                                                   | Kick the specified player. Type: <br> `0`: Built-in <br> `1`: Client_Init <br> `2`: Workshop_Corruption_Kick <br> `3`: Workshop_Crash <br> `4`: Invalid_Map |
| /kill `<color>`                                                          | Kill the specified player.                                                                                                                                  |
| /revive                                                                  | Revive the player. (**client-side only**)                                                                                                                   |
| /sudo `<color> <message>`                                                | Say as the specified player.                                                                                                                                |
| /summon `<player\|bolt\|zombie> [spawnPcEnabled]`                        | Spawn a specified player prefab AI, not completed. (**only local**)                                                                                         |
| /switchweapon                                                            | Toggle switch weapon ability.                                                                                                                               |
| /win `[color] [mapIndex] `                                               | Set the specified player to win the round and load the specified map.                                                                                       |

## Stat Menu

The stat menu provides an easy way to view the statistics of certain player actions that the game tracks. By default it can be opened via the keybind <kbd>LeftShift</kbd> + <kbd>F2</kbd>, but also can be accessed through the QOL menu. The stats that are shown should **not** be taken as having absolute accuracy.<br/>
![Image of Stat Menu](https://i.ibb.co/txjYmP7/statmenu.png)

## Using The Config

A configuration file named ``monky.plugins.QOL.cfg`` can be found under ``BepInEx\config``.<br/>
Please note that you ___must run the mod at least once___ for it to be generated.<br/>
You can currently use it to set certain features to be enabled on startup.<br/>
Example: 
```cfg
## Enable rich text for chat on startup?
# Setting type: Boolean
# Default value: false
RichTextInChat = true
```
Changing ``RichTextInChat = false`` to ``RichTextInChat = true`` will enable it on startup without the need for doing ``/rich`` to enable it.<br/>

To change your player color to a custom value, please look in the config and replace the default value of ``FFFFFFFF`` to a [HEX color](https://g.co/kgs/qJMEDR).<br/>
An example is the color neon pink, which the HEX value is: ``FF10F0``<br/>
Please *do <ins>not</ins>* include a ``#`` character at the front of your HEX value.

Another important option to mention for the config is the ability to specify an API key for Google Translate.<br/>
In doing so, this will allow you to bypass the rate-limit that comes normally with ``/translate``.<br/> 
**You are responsible for creating the key, and any potential charges accrued.**<br/>
Instructions & documentation for all of that can be found [here](https://cloud.google.com/translate).<br/>

Simply delete the config file to have a new one generated with default settings.<br/>
Updating the mod ***does not*** require you to delete the config file.
