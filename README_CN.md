# QOL-Mod-Ex

![GitHub Stars](https://img.shields.io/github/stars/z7572/QOL-Mod-Ex?style=social)
[![GitHub Release](https://img.shields.io/github/v/release/z7572/QOL-Mod-Ex)](https://github.com/z7572/QOL-Mod-Ex/releases)
[![Github Downloads](https://img.shields.io/github/downloads/z7572/QOL-Mod-Ex/total?label=Github%20downloads&logo=github)](https://github.com/z7572/QOL-Mod-Ex/releases/latest)

[English](./README.md) | 中文

这是 [QOL-Mod](https://github.com/Mn0ky/QOL-Mod) 的一个分支版本。

## 特性：

### 命令
- 命令多参数补全支持
  - 支持多种参数定义方式
    - 以索引分隔的参数列表 `List<List<string>>`
    - 树状参数 `Dictionary<string, object>`
    - 以索引分隔的参数列表 + 树状参数混合 `HybridAutoParams`
    - 纯混合参数 `List<object>`
      - 默认以索引分隔，可用 `Dictionary<string, object>` 充当分叉器，匹配失败时进入下一参数
      - 任意输入参数 `AnyInputParams`
      - 无限参数 `InfiniteAutoParams`
        - 可选择是否去重
      - 无限任意输入参数 `InfiniteAnyInputParams`
      - 动态参数 `DynamicAutoParams`
        - 当前的命令列表
        - 链式子命令调用
        - 坐标 —— `~`为玩家位置，`^`为鼠标指针位置
        - 方向 —— `~`为玩家速度方向，`^`为鼠标指针速度方向
        - 欧拉角 —— `~`为玩家瞄准方向，`^`为鼠标指针速度方向
        - 配置项
        - 地图名称
        - 音乐名称
- 命令、参数切换（使用 `Tab` / `Ctrl` + `Tab` 或 滚动鼠标滚轮 以正反向切换）
- 按 `/` 以直接键入命令
- 在本地游戏（和关卡编辑器）中启用聊天输入框，以随时输入命令
- [更多命令](#更多命令)

### 单人游戏
- 移植和优化了 [Trainer](https://github.com/alexcodito/StickFightTheGameTrainer) 的Bot功能
  - 使用 `/summon <player|bolt|zombie> [isNPC] [dummy|legacyAI]` 生成Bot
- Bot现在拥有更先进的AI
  - 移动
  - 近战
  - 盾反
  - 扔枪
    - 近战武器有概率扔
    - 所有武器在近距离有概率扔
  - 后坐起飞
  - 鸣龙掷
  - 无偏
  - ...
  - \* ***不能** 且 **做不到** 寻路*
- [更多命令](#更多命令)


## 更改 | 修复 | 优化：

- 现在可以在地图/武器选择界面按Q/E或左右箭头来快速翻页
- 每50局自动清除场景中残留的箱子
- 优化了音乐导入的性能问题，修改了音乐导入逻辑：现在可以在文件名前加上 `01.` 等数字索引来指定导入顺序
- 修复了当渲染如 Boss翅膀 等粒子效果时，使用 Blink Dagger 瞬移时导致的粒子渲染冻结并残留的问题
- 修复了在高帧率下，重力子弹的轨迹抖动的问题
- 修复了自定义玩家颜色导致材质污染的问题
- 新增配置项： 
  - `EnableCustomColor` - 设置是否启用自定义玩家颜色（而不是与默认值比较）
  - `CustomNPCColor` - 设置NPC的颜色
  - `ChatFieldProportion` - 调整聊天输入框的宽度
  - `EnableChatFieldInLevelEditor` - 设置是否在关卡编辑器中启用聊天输入框
  - `DisableStatsMap` - 设置是否禁用本地游戏的“统计”地图跳转（每30局跳转一次）
- 修改了关卡编辑器场景的渲染设置以和主游戏场景的相对应（为了让 `/bulletcolor` 命令正常显示修改后的子弹颜色）
- 移除了 `LoadingScreen` 的 16:9 边框，使不同长宽比的屏幕都能显示完整内容
  - 同时导致在显示加载屏幕时能显示粒子特效（圆圈/叉号等）
- 修改了聊天气泡的限位逻辑，使其始终保持在屏幕内（使用边距，确保支持不同屏幕长宽比/视角大小）
- 固定了聊天框和胜利文本UI的位置，使其始终跟随相机
- 现在单人在主界面死亡时不会进入游戏，只有存在多个玩家时才会进入游戏
- 现在在本地游戏中胜利时会清除所有生成的NPC
- 修复了本地游戏中的玩家列表索引问题（从不定长列表改为定长列表，空位用 `null` 占位），以避免 热插拔手柄 或 用命令添加/移除玩家 时导致的玩家分数以及玩家颜色索引错位的问题
  - 同时修复了因此改动导致在装有 [MoreWinText](https://github.com/Buwwet/StickFightMoreWinText) 插件时本地游戏无法胜利的问题
- 渐变色TMP文本
  - 使用 `<link=xxx></link>` 定义需要进行颜色渐变的文本
  - 自带一些渐变色名称预设，存储在 `GradientColorPresets.json` 文件中，可自行修改或添加
  - 不支持的预设不会生效，且不会显示此富文本标签

## 更多命令

### 非作弊命令

#### 添加的命令
| Command      | Parameters                                                   | Comment                                                                                                |
| :----------- | :----------------------------------------------------------- | :----------------------------------------------------------------------------------------------------- |
| /afk         |                                                              | Can be disabled by others                                                                              |
| /bossmusic   | `<blue\|red\|yellow\|rainbow\|stop>`                         |                                                                                                        |
| /bulletcolor | `[team\|battery\|random\|yellow\|blue\|red\|green\|white]`   | Enable or disables bullet color that correspond to player's                                            |
| /edgearrow   |                                                              | To be or not to be (a cheat), that is the question                                                     |
| /emoji       | `<emoji_name>`                                               |                                                                                                        |
| /hikotoko    | `[c]`                                                        | [API](https://developer.hitokoto.cn/sentence/#%E5%8F%A5%E5%AD%90%E7%B1%BB%E5%9E%8B-%E5%8F%82%E6%95%B0) |
| /join        | `<lobby_id_or_url>`                                          |                                                                                                        |
| /lobbyinfo   |                                                              |                                                                                                        |
| /lobbytype   | `[public\|friends\|private\|invisible]`                      | Gets or sets current lobby's lobby type (public, private, friends-only, invisible)                     |
| /output      | `<public\|private>` `<command>`                              |                                                                                                        |
| /pumpkin     |                                                              |                                                                                                        |
| /repeat      | `<count\|stop>` `<interval_ms>` `<command>` `[args...]`      | Repeats a command for a specified number of times with an optional interval                            |
| /say         | `<message>`                                                  | Equals to chat directly, or use /execute to say as specified player(s)                                 |
| /summon      | `<player\|bolt\|zombie>` `[true\|false]` `[dummy\|legacyAI]` | Local only                                                                                             |
| /suddendeath |                                                              | Local only                                                                                             |
| /wings       | `<blue\|red\|yellow\|white\|none>` `[add]`                   |                                                                                                        |

#### 移除的命令
| Command     | Parameters  | Comment               |
| :---------- | :---------- | :-------------------- |
| /logprivate | `<command>` | 被 `/output` 命令替代 |
| /logpublic  | `<command>` | 被 `/output` 命令替代 |

#### 更改的命令
| Command  | Parameters                                           | Comment                                                        |
| :------- | :--------------------------------------------------- | :------------------------------------------------------------- |
| /config  | `<key> [value]`                                      | 自动补全参数列表进行了更新以支持新结构                         |
| /maps    | `<load\|save\|remove>` `<presetName>`               | 参数数量从1个变更为2个，操作逻辑更新（分为了load/save/remove） |
| /music   | `<loop\|play\|skip\|randomize>` `[musicName\|Index]` | 自动补全更改为字典树结构，并新增了 `randomize`（随机）选项     |
| /weapons | `<load\|save\|remove>` `<presetName>`               | 参数数量从1个变更为2个，操作逻辑更新（分为了load/save/remove） |


---


### 作弊命令

> *好孩子不应该看这些* <br>
> *仅****单人模式****生效* <br>
> *\* 永远不要尝试在多人模式下执行这些命令*

#### 添加的命令
| Command         | Parameters                                                                                 | Comment                                                              |
| :-------------- | :----------------------------------------------------------------------------------------- | :------------------------------------------------------------------- |
| /afk            | `[canBeDisabledByOthers]`                                                                  |                                                                      |
| /dmgpkt         | `<target>`                                                                                 |                                                                      |
| /objpkt         | `<target>` `<objectType\|Index>` `<x>` `<y>` `<rotX>` `<rotY>` `<rotZ>` `[isLocalDisplay]` | Sending object spawn packets to specified player                     |
| /firepkt        | `<target>` `<x>` `<y>` `<Vx>` `<Vy>` `[weaponName\|Index]` `[isLocalDisplay]`              | Sending fire packets to specified player                             |
| /bullethell     | `<target>` `[isLocalDisplay]` `[sendInSegments]`                                           |                                                                      |
| /bulletring     | `<target>` `<weaponIndex>` `[isLocalDisplay]` `[radius]`                                   |                                                                      |
| /execute        | `<target>` `<command>` `[args...]`                                                         | Execute commands as specified player                                 |
| /antichat       | `<target>` `[replacementText]`                                                             |                                                                      |
| /boss           | `<blue\|red\|yellow\|rainbow\|none>`                                                       |                                                                      |
| /blockall       |                                                                                            |                                                                      |
| /drag           |                                                                                            |                                                                      |
| /esp            |                                                                                            |                                                                      |
| /throwesp       |                                                                                            |                                                                      |
| /weaponesp      |                                                                                            |                                                                      |
| /aimesp         |                                                                                            |                                                                      |
| /weaponspawnesp |                                                                                            |                                                                      |
| /hitbox         |                                                                                            |                                                                      |
| /god            |                                                                                            |                                                                      |
| /fullauto       |                                                                                            |                                                                      |
| /quickdraw      |                                                                                            | For Deagle/Revolver/M1, etc.                                         |
| /perfectaim     |                                                                                            | Aims from the actual shoot position to the mouse instead of the neck |
| /norecoil       | `[all\|weapon\|torso]`                                                                     |                                                                      |
| /nospread       |                                                                                            |                                                                      |
| /infiniteammo   |                                                                                            |                                                                      |
| /invisible      |                                                                                            |                                                                      |
| /fastblock      |                                                                                            |                                                                      |
| /fastfire       |                                                                                            |                                                                      |
| /fastpunch      |                                                                                            |                                                                      |
| /fly            |                                                                                            |                                                                      |
| /gun            | `<weaponName\|Index\|spawnrandom>`                                                         |                                                                      |
| /kick           | `<target>` `[method]`                                                                      |                                                                      |
| /kill           | `[target]`                                                                                 |                                                                      |
| /revive         |                                                                                            |                                                                      |
| /scrollattack   |                                                                                            |                                                                      |
| /showhp         |                                                                                            |                                                                      |
| /sayas          | `<target>` `<visible\|invisible>` `<message>`                                              | Say as specified player                                              |
| /spec           |                                                                                            | Be a spectator and skip spawning the own player at joining the match |
| /switchweapon   |                                                                                            |                                                                      |
| /throwcalc      |                                                                                            |                                                                      |
| /tp             | `<x>` `<y>`                                                                                |                                                                      |
| /visualbar      | `<target>`                                                                                 |                                                                      |
| /win            | `[target]` `[MapName\|Index]`                                                              | Set the selected player win and switch to selected or next map       |

---

*以下为原来的README.md文件内容:*

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

| Command                                    | Description                                                                                                                                                    |
|--------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Usage:**		                               | ```/<command_name> [<additional parameter>]```                                                                                                                 |
| /adv		                                     | Outputs whatever you set it to in the config.                                                                                                                  |
| /fov                                       | Set the FOV for the game.                                                                                                                                      |
| /gg		                                      | Enables automatic sending of "gg" upon death of mod user.                                                                                                      |
| /help		                                    | Opens up the Steam overlay and takes you to this page.                                                                                                         |
| /hp	```[<player_color>]```	                | Outputs the percent based health of the target color to chat. Leave as ``/hp`` to always get your own.                                                         |
| /id	```[<player_color>]```		               | Copies the Steam ID of the target player to clipboard.                                                                                                         |
| /invite		                                  | Generates a "join game" link and copies it to clipboard.                                                                                                       |
| /lobhp		                                   | Outputs the health set for the whole lobby.                                                                                                                    |
| /lobregen		                                | Outputs whether or not regen is enabled for the lobby.                                                                                                         |
| /lowercase		                               | Enables/disables lowercase mode, which has your chat messages always sent in lowercase. Useful for those who keep pressing the caps-lock key.                  |
| /nuky		                                    | Lets you talk like Nuky. Splits up any message you send and outputs it word by word.                                                                           |
| /mute ```[<player_color>]```		             | The targeted player's messages wont appear, making them "muted" for you (**client-side only**. A mute only lasts for the lobby you're currently in).           |
| /ping ```[<player_color>]```		             | Outpus the ping for the targeted player.                                                                                                                       |
| /private		                                 | Privates the current lobby (**must be host**).                                                                                                                 |
| /public		                                  | Opens the current lobby to the public (**must be host**).                                                                                                      |
| /rainbow		                                 | Enables/disables rainbow mode. Dynamically shifts your player color through the color spectrum (the shifting speed of the colors is changeable in the config). |
| /rich		                                    | Enables rich text for chat (**client-side only**).                                                                                                             |
| /shrug ```[<message>]```		                 | Appends ¯\\\_☹\_/¯ to the end of the typed message (changeable in config).                                                                                     |
| /stat ```[<player_color> <stat_type>]```		 | Gets the targeted stat of the targeted player. Open the stat menu to see a list of different stat names.                                                       |
| /suicide                                   | Kills the user.                                                                                                                                                |
| /translate		                               | Enables auto-translation for messages from others to English.                                                                                                  |
| /uncensor		                                | Disables chat censorship.                                                                                                                                      |
| /uwu		                                     | *uwuifies* any message you send.                                                                                                                               |
| /ver		                                     | Outputs the mod version string.                                                                                                                                | /winnerhp                                    | Outputs the winner's hp at the end of every round.                                                                                                                                |
| /winnerhp                                  | Outputs the winner's hp at the end of every round.                                                                                                             |                                                                                                                                                                |
| /winstreak		                               | Enables winstreak mode.                                                                                                                                        |

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