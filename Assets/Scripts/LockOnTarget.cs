using System.Collections.Generic;
using UnityEngine;

//put this on anything the player should be able to lock onto (enemies)
public class LockOnTarget : MonoBehaviour
{
    [SerializeField] private Vector3 lockPointOffset = Vector3.zero; //move the lock point up/down if the middle isnt right

    //static = ONE shared list for the whole game, not one per enemy
    //every lock on target adds itself when it appears so the player can just look through this list
    //much faster than searching the whole scene every time
    public static readonly List<LockOnTarget> All = new List<LockOnTarget>();

    private Collider col;
    private Health health;

    //the point the camera looks at and the lock icon sits on, the middle of the collider if there is one
    public Vector3 LockPoint => (col != null ? col.bounds.center : transform.position) + lockPointOffset;

    //dead things or switched off things cant be locked onto
    public bool CanBeTargeted => isActiveAndEnabled && (health == null || !health.IsDead);

    private void Awake()
    {
        col = GetComponentInChildren<Collider>();
        health = GetComponentInParent<Health>();
    }

    //OnEnable / OnDisable run when the object is switched on/off (and when its created/destroyed)
    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);
}
