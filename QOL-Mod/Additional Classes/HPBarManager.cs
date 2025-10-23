using HarmonyLib;
using TMPro;
using UnityEngine;

namespace QOL;

public class HPBarManager : MonoBehaviour
{
    private int playerID;
    private Transform targetRig;
    private HealthHandler healthHandler;
    private SetMovementAbility setMovementAbility;
    private CodeStateAnimation bossBar;
    private OnlinePlayerUI onlinePlayerUI;
    private Canvas canvas;
    private Material colorMat;
    private TextMeshProUGUI TMPtext;
    private Transform playerNameObj;

    private void Awake()
    {
        playerID = gameObject.GetComponent<Controller>().playerID;
        targetRig = gameObject.GetComponentInChildren<Torso>().transform;
        healthHandler = gameObject.GetComponent<HealthHandler>();
        setMovementAbility = gameObject.GetComponent<SetMovementAbility>();
        canvas = transform.Find("GameCanvas").GetComponent<Canvas>();
    }

    private void Start()
    {
        if (MultiplayerManagerAssets.Instance != null)
        {
            colorMat = MultiplayerManagerAssets.Instance.Colors[playerID];
        }
        else
        {
            colorMat = transform.Find("Renderers/handRenderer").GetComponent<LineRenderer>().material;
        }
        onlinePlayerUI = FindObjectOfType<OnlinePlayerUI>();
        playerNameObj = onlinePlayerUI?.transform.GetChild(playerID);

        var newObj = new GameObject($"Player{playerID + 1}HPText");
        TMPtext = newObj.AddComponent<TextMeshProUGUI>();
        newObj.SetActive(ChatCommands.CmdDict["showhp"].IsEnabled);
        Helper.HpBars[playerID] = newObj;

        // TextMeshProUGUI texts must under the canvas to display
        newObj.transform.SetParent(canvas.transform);
        newObj.transform.localRotation = Quaternion.identity;
        newObj.transform.localScale = Vector3.one;
        TMPtext.alignment = TextAlignmentOptions.Center;
        TMPtext.fontSize = 40f;
        TMPtext.color = colorMat.color;
        TMPtext.font = Resources.Load<TMP_FontAsset>("fonts & materials/MiSans SDF") ?? // SFTGCNText
                       Resources.Load<TMP_FontAsset>("fonts & materials/Anton SDF") ?? TMPtext.font;
        bossBar = Traverse.Create(setMovementAbility).Field("anim").GetValue<CodeStateAnimation>();
    }

    private void Update()
    {
        if (playerNameObj == null || playerNameObj.localScale == Vector3.zero)
        {
            TMPtext.transform.position = targetRig.position + Vector3.up * 1.5f;
        }
        else
        {
            TMPtext.transform.position = targetRig.position + Vector3.up * 2.1f;
        }

        var health = healthHandler.health * (OptionsHolder.HP / 100f);

        if (bossBar != null && bossBar.state1)
        {
            health *= healthHandler.bossHealthZ / 100f;
        }

        if (healthHandler.health > 0f)
        {
            TMPtext.text = health.ToString("0.0");
        }
        else
        {
            TMPtext.text = string.Empty;
        }
    }

    private void OnDestroy()
    {
        if (TMPtext != null) Destroy(TMPtext.gameObject);
        Helper.HpBars[playerID] = null;
    }
}
