using System.Linq;
using UnityEngine;
using HarmonyLib;

namespace QOL;

public class EdgeArrowManager : MonoBehaviour
{
    protected float horizonalPadding = 2f;
    protected float verticalPadding = 1f;
    protected float visibilityThreshold = 0.1f;

    protected float baseArrowSize = 0.3f;
    protected float minScale = 0.6f;
    protected float maxScale = 1.4f;
    protected float maxDistance = 15f;

    private int playerID;
    private Standing standing;
    private Camera mainCamera;
    private Rigidbody[] rigs;

    private GameObject parentObject;
    private GameObject spriteObject;

    private void Awake()
    {
        playerID = gameObject.GetComponent<Controller>().playerID;
        standing = gameObject.GetComponent<Standing>();
        mainCamera = Camera.main;
    }

    private void Start()
    {
        rigs = Traverse.Create(standing).Field("rigs").GetValue<Rigidbody[]>();

        parentObject = new GameObject("EdgeArrow");
        parentObject.transform.SetParent(transform);
        parentObject.SetActive(false);

        spriteObject = new GameObject("Sprite");
        spriteObject.transform.SetParent(parentObject.transform);
        spriteObject.transform.localPosition = new Vector3(-0.2f, 0f, 0f);
        spriteObject.transform.rotation = Quaternion.Euler(0f, 0f, -90f);
        spriteObject.transform.localScale = new Vector3(baseArrowSize, baseArrowSize * Mathf.Sqrt(1f / 3f), baseArrowSize);

        var spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
        var arrowSprite = Resources.FindObjectsOfTypeAll<Sprite>().Where(s => s.name == "triangle-xxl").FirstOrDefault();
        var materials = MultiplayerManagerAssets.Instance.Colors;

        if (arrowSprite == null || materials == null) return;
        spriteRenderer.sprite = arrowSprite;
        spriteRenderer.color = Color.white;
        spriteRenderer.material = materials[playerID];
    }

    private void Update()
    {
        if (rigs == null)
        {
            Debug.LogError("rigidbodies is null!!!");
            rigs = Traverse.Create(gameObject.GetComponent<Standing>()).Field("rigs").GetValue<Rigidbody[]>();
        }
        if (parentObject == null)
        {
            Debug.LogError("parentObject is null!!!");
            parentObject = transform.Find("EdgeArrow").gameObject;
        }

        var isPlayerVisible = AreAnyRigidbodiesOnScreen();

        if (!isPlayerVisible)
        {
            PositionArrowAtScreenEdge();
        }

        parentObject.SetActive(!isPlayerVisible);
    }

    private bool AreAnyRigidbodiesOnScreen()
    {
        int visibleRigidbodies = 0;

        foreach (var rb in rigs)
        {
            Vector3 screenPos = mainCamera.WorldToScreenPoint(rb.position);

            if (screenPos.z > 0 &&
                screenPos.x > 0 && screenPos.x < Screen.width &&
                screenPos.y > 0 && screenPos.y < Screen.height)
            {
                visibleRigidbodies++;
            }
        }

        float visibleRatio = (float)visibleRigidbodies / rigs.Length;
        return visibleRatio >= visibilityThreshold;
    }

    private void PositionArrowAtScreenEdge()
    {
        Vector3 playerWorldPos = Vector3.zero;

        foreach (var rb in rigs)
        {
            playerWorldPos += rb.position;
        }

        playerWorldPos /= rigs.Length;

        Vector3 playerScreenPos = mainCamera.WorldToScreenPoint(playerWorldPos);

        float arrowX = Mathf.Clamp(playerScreenPos.x, horizonalPadding, Screen.width - horizonalPadding);
        float arrowY = Mathf.Clamp(playerScreenPos.y, verticalPadding, Screen.height - verticalPadding);

        Vector3 arrowWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(arrowX, arrowY, mainCamera.nearClipPlane + 5f));

        Vector2 arrowScreenPos = new Vector2(arrowX, arrowY);
        Vector2 direction = ((Vector2)playerScreenPos - arrowScreenPos).normalized;

        Vector2 arrowPosYZ = new Vector2(arrowWorldPos.z, arrowWorldPos.y);
        Vector2 playerPosYZ = new Vector2(playerWorldPos.z, playerWorldPos.y);

        float distance = Vector2.Distance(arrowPosYZ, playerPosYZ);
        float scaleRatio = 1f - Mathf.Clamp01(distance / maxDistance);
        float currentScale = Mathf.Lerp(minScale, maxScale, scaleRatio);

        parentObject.transform.position = arrowWorldPos;
        parentObject.transform.rotation = Quaternion.Euler(0f, 90f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        parentObject.transform.localScale = Vector3.one * currentScale;
    }
}