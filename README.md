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

| Command                                                                 | Description                                                  | Parameters                                                                         |
| ----------------------------------------------------------------------- | ------------------------------------------------------------ | ---------------------------------------------------------------------------------- |
| **Usage:**                                                              | ```/<command_name> <parameter> [optional parameter]```       |                                                                                    |
| /adv                                                                    | Outputs whatever you set it to in the config.                |                                                                                    |
| /alias `<command> [new_alias]`                                          | Add or remove aliases for commands.                          | command: target command name, new_alias: new alias (empty to remove all)           |
| /blacklist `<add\|remove\|list\|clear> [player\|id]`                    | Manage player blacklist.                                     | add/remove: player color or SteamID, list: show blacklist, clear: remove all       |
| /bossmusic `<blue\|red\|yellow\|rainbow\|stop>`                         | Play boss music or stop current music.                       |                                                                                    |
| /bulletcolor `<team\|battery\|random\|yellow\|blue\|red\|green\|white>` | Toggle bullet color mode with specified color option.        |                                                                                    |
| /config `<key> [value]`                                                 | Change config values. Empty value to reset to default.       | key: config entry name                                                             |
| /deathmsg                                                               | Toggle automatic death messages.                             |                                                                                    |
| /dm `<color> <message>`                                                 | Direct message a player.                                     | color: target player color                                                         |
| /fov `<value>`                                                          | Set the FOV for the game.                                    | value: field of view value                                                         |
| /fps `<value>`                                                          | Set the FPS for the game.                                    | value: target framerate (≥60)                                                      |
| /friend `<color>`                                                       | Open Steam friend add overlay for target player.             | color: target player color                                                         |
| /gg                                                                     | Toggle automatic "gg" upon death.                            |                                                                                    |
| /help                                                                   | Open Steam overlay to command documentation.                 |                                                                                    |
| /hikotoko                                                               | Toggle Hikotoko wintext (Simplified Chinese fonts required). |                                                                                    |
| /hp `[color]`                                                           | Output health percentage of target player or yourself.       | color: target player color (empty for self)                                        |
| /id `<color>`                                                           | Copy target player's SteamID to clipboard.                   | color: target player color                                                         |
| /invite                                                                 | Generate and copy "join game" link to clipboard.             |                                                                                    |
| /join `<lobby_id\|join_link>`                                           | Join specific lobby by ID or steam:// link.                  |                                                                                    |
| /lobhp                                                                  | Output lobby HP setting.                                     |                                                                                    |
| /lobregen                                                               | Output lobby regen setting.                                  |                                                                                    |
| /lowercase                                                              | Toggle lowercase-only chat messages.                         |                                                                                    |
| /nuky                                                                   | Toggle Nuky chat mode (splits messages word by word).        |                                                                                    |
| /maps `<preset\|save\|remove> [name]`                                   | Save/remove/load map presets.                                | preset: preset name, save/remove: preset name required                             |
| /mute `<color>`                                                         | Toggle mute for target player (client-side only).            | color: target player color                                                         |
| /music `<loop\|play\|skip\|randomize> [index]`                          | Control music playback.                                      | loop: [song index], play: song index, skip: next song, randomize: shuffle playlist |
| /ouchmsg                                                                | Toggle ouch mode.                                            |                                                                                    |
| /output `<public\|private> <command\|all>`                              | Set command output visibility.                               | public/private: visibility, command: target command or "all"                       |
| /ping `<color>`                                                         | Output target player's ping.                                 | color: target player color                                                         |
| /private                                                                | Make lobby private (host only).                              |                                                                                    |
| /profile `<color>`                                                      | Open Steam profile of target player.                         | color: target player color                                                         |
| /public                                                                 | Make lobby public (host only).                               |                                                                                    |
| /pumpkin                                                                | Toggle pumpkin accessory.                                    |                                                                                    |
| /rainbow                                                                | Toggle rainbow player color mode.                            |                                                                                    |
| /resolution `<width> <height>`                                          | Set game resolution.                                         | width: screen width, height: screen height                                         |
| /rich                                                                   | Toggle rich text for chat (client-side only).                |                                                                                    |
| /say `<message>`                                                        | Send chat message directly.                                  |                                                                                    |
| /shrug `[message]`                                                      | Append shrug emoticon to message.                            | message: text before shrug                                                         |
| /stat `[color] <stat_type>` or `<all> <stat_type>`                      | Output player statistics.                                    | color: target player, stat_type: statistic name, "all" for all players             |
| /suicide                                                                | Kill yourself with random death message.                     |                                                                                    |
| /translate                                                              | Toggle auto-translation for chat messages.                   |                                                                                    |
| /uncensor                                                               | Toggle chat censorship.                                      |                                                                                    |
| /uwu                                                                    | Toggle UwUifier for chat messages.                           |                                                                                    |
| /ver                                                                    | Output mod version.                                          |                                                                                    |
| /weapons `<preset\|save\|remove> [name]`                                | Save/remove/load weapon presets.                             | preset: preset name, save/remove: preset name required                             |
| /wings `<blue\|red\|yellow\|rainbow\|none> [add]`                       | Change wing type.                                            | add: keep existing wings                                                           |
| /winnerhp                                                               | Toggle winner HP announcer.                                  |                                                                                    |
| /winstreak                                                              | Toggle winstreak system.                                     |                                                                                    |

## Cheat Commands

| Command                                                              | Description                                             | Parameters                                                                                                 |
| -------------------------------------------------------------------- | ------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| /afk                                                                 | Toggle AFK mode (assigns AI to player).                 |                                                                                                            |
| /logpkg `[types]`                                                    | Toggle P2P package logging for specified message types. | types: P2P message type names (empty for all)                                                              |
| /dmgpkg `<target>`                                                   | Send damage packages to target player.                  | target: player color                                                                                       |
| /firepkg `<target> <x> <y> <Vx> <Vy> [weaponIndex] [isLocalDisplay]` | Send fire package to target.                            | target: player color, x/y: position, Vx/Vy: velocity                                                       |
| /bullethell `<target> [isLocalDisplay]`                              | Send bullet hell to target player.                      | target: player color or "all"                                                                              |
| /bulletring `[target] [weaponIndex] [isLocalDisplay] [radius]`       | Create bullet ring around player.                       | target: player color, radius: ring size (default: 200)                                                     |
| /execute `<target> <command> [parameters]`                           | Execute command as target player.                       | target: player color or "all", command: command to execute                                                 |
| /boss `<blue\|red\|yellow\|rainbow\|none>`                           | Transform into boss with special abilities and music.   |                                                                                                            |
| /blockall                                                            | Toggle block all attacks mode.                          |                                                                                                            |
| /god                                                                 | Toggle god mode.                                        |                                                                                                            |
| /fullauto                                                            | Toggle full auto firing.                                |                                                                                                            |
| /quickdraw                                                           | Toggle auto quick draw for revolvers.                   |                                                                                                            |
| /norecoil `<all\|notorso>`                                           | Toggle no recoil mode.                                  | all: complete no recoil, notorso: no torso recoil only                                                     |
| /nospread                                                            | Toggle no bullet spread.                                |                                                                                                            |
| /infiniteammo                                                        | Toggle infinite ammo.                                   |                                                                                                            |
| /invisible                                                           | Toggle invisibility.                                    |                                                                                                            |
| /fastfire                                                            | Toggle fast firing.                                     |                                                                                                            |
| /fastpunch                                                           | Toggle fast punching.                                   |                                                                                                            |
| /fly                                                                 | Toggle fly mode.                                        |                                                                                                            |
| /gun `[weapon_index]`                                                | Give specified weapon.                                  | weapon_index: weapon ID (-1 to clear, -2 for random)                                                       |
| /kick `<color> [method]`                                             | Kick target player using specified method.              | color: target player, method: Built-in, Client_Init, Workshop_Corruption_Kick, Workshop_Crash, Invalid_Map |
| /kill `[color]`                                                      | Kill target player or yourself.                         | color: target player (empty for self)                                                                      |
| /revive                                                              | Revive yourself.                                        |                                                                                                            |
| /scrollattack                                                        | Toggle scroll wheel attack.                             |                                                                                                            |
| /showhp                                                              | Toggle HP bars display.                                 |                                                                                                            |
| /sayas `<color> <visible\|invisible> <message>`                      | Send message as target player.                          | color: target player, visible/invisible: message visibility to target                                      |
| /summon `<player\|bolt\|zombie> [spawnPcEnabled]`                    | Spawn AI enemy (local only).                            | spawnPcEnabled: enable spawn effects                                                                       |
| /switchweapon                                                        | Toggle weapon switching ability.                        |                                                                                                            |
| /tp `<x> <y>`                                                        | Teleport to coordinates.                                | x/y: coordinates or relative values                                                                        |
| /win `[color] [map_index]`                                           | Set round winner and change map.                        | color: winner player, map_index: next map ID                                                               |

## Test Commands

| Command     | Description                                | Usage                                          |
| ----------- | ------------------------------------------ | ---------------------------------------------- |
| /testmulti  | Test multiple parameter completion.        | ```/testmulti option1 any_value sub1 final1``` |
| /testtree   | Test tree-structured parameter completion. | ```/testtree branch1 leaf1 value1```           |
| /testhybrid | Test hybrid parameter completion.          | ```/testhybrid option1 sub1 branch1 leaf1```   |
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
