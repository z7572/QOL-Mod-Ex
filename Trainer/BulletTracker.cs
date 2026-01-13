using UnityEngine;

namespace QOL.Trainer;

public class BulletTracker : MonoBehaviour
{
    private RayCastForward _rcf;

    private void Awake()
    {
        _rcf = GetComponent<RayCastForward>();
    }

    private void OnEnable()
    {
        if (_rcf) AILogic.RegisterBullet(_rcf);
    }

    private void OnDisable()
    {
        if (_rcf) AILogic.UnregisterBullet(_rcf);
    }
}