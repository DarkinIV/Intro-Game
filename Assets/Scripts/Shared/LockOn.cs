using System.Reflection;
using UnityEngine;

//the lock on for all three classes middle click = lock on and also let go
//i havent made a separate targeting code: this script uses Asim's targeting from PlayerController, so there is only one pls remember that before renaming anything - kc 
//lock on rule in the whole game (the living enemy closest to the middle of the screen, within his lockOnRange).
//thing is that the existing lock on lives inside Update, as private, list of things that r priv  (ToggleLockOn, lockedTarget, lockOnRange, cam),
//and none of the classes run That Update anymore (the acrobat runs AcrobatController, the other two run DashJumpController)
//so we reach  those private things by  name using "reflection" = asking "give me the method called ToggleLockOn on this object" even tho its priv
//really cool actually, took some time to understand as i had ever used it before but worth checking out
// did not want to change from priv to public bcz i didnt know what sort of conseqeunces might follow and did not want to mess with the code that was alr made by us previously 
// same trick DashJumpController uses for Dash().
//
// !SUPERIMPORTANTPLS! DO NOT RENAME these in PlayerController.cs, or lock on stops working for every class (found out the hard way)
//List of things to not be renamed: ToggleLockOn()   lockedTarget   lockOnRange   cam                                    !!!
//if one of them is renamed anyway, u get a red error at start saying exactly which one 
//
//how it works, step by step, every frame (see Update):
//  1. make sure his private "cam" is filled in. that Start() normally fills it, but the acrobat's own Start() replaces that,
//     so for the acrobat it would be empty and that ToggleLockOn would crash looking at the screen
//  2. if middle click was pressed, call that ToggleLockOn(). it either picks the best enemy or lets go of the current one
//  3. read that lockedTarget to see what he picked
//  4. his drop rule: let go if the target died or is further than thag lockOnRange (this is copied from that Update,
//     the 4 lines right after key check). we also write the null back into field so that toggle sees it
//  5. tell the camera and the combat what we are locked onto (the same 2 lines that Update does)
[DefaultExecutionOrder(-10)] //run before the controllers so they face the right target in the same frame
public class LockOn : MonoBehaviour
{
    [SerializeField] KeyCode lockKey = KeyCode.Mouse2; //middle click
    //the locked on enemy, null = not locked. the controllers read this to face the enemy
    public Transform LockTarget { get; private set; }
    //private members, looked up by name once
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly MethodInfo ToggleMethod = typeof(PlayerController).GetMethod("ToggleLockOn", Private);
    static readonly FieldInfo TargetField = typeof(PlayerController).GetField("lockedTarget", Private);
    static readonly FieldInfo RangeField = typeof(PlayerController).GetField("lockOnRange", Private);
    static readonly FieldInfo CamField = typeof(PlayerController).GetField("cam", Private);

    PlayerController controller;   //previously established controller (they are subclasses so this finds the others  too)
    PlayerCombat combat;
    Camera cam;
    CameraController camCtrl;
    void Awake()
    {
        controller = GetComponent<PlayerController>();
        combat = GetComponent<PlayerCombat>();

        if (ToggleMethod == null || TargetField == null || RangeField == null || CamField == null)
        {
            Debug.LogError("LockOn: a private member of PlayerController was renamed "
                + "(ToggleLockOn=" + (ToggleMethod != null)
                + ", lockedTarget=" + (TargetField != null)
                + ", lockOnRange=" + (RangeField != null)
                + ", cam=" + (CamField != null) + "). Lock on will not work until the name is put back.");
        }
    }
    void Update()
    {
        if (controller == null) return;
        //the camera belongs to the scene, so find it the first time we need it
        if (cam == null)
        {
            cam = Camera.main;
            if (cam != null) camCtrl = cam.GetComponent<CameraController>();
        }

        //step 1: that ToggleLockOn uses private cam to work out whats on screen, fill it if its empty
        if (CamField != null && cam != null && CamField.GetValue(controller) == null)
        {
            CamField.SetValue(controller, cam);
        }
        //step 2:the key thatpreviously estgablished toggle (picks the best enemy, or lets go if already locked)
        if (Input.GetKeyDown(lockKey) && ToggleMethod != null)
        {
            ToggleMethod.Invoke(controller, null); //no need for args 
        }
        //step 3: read what was picked
        LockTarget = null;
        if (TargetField != null)
        {
            LockTarget = (Transform)TargetField.GetValue(controller);
        }
        //step 4: drop rule, let go if the target died or ran too far
        if (LockTarget != null)
        {
            CharacterStats s = LockTarget.GetComponentInParent<CharacterStats>();
            bool tooFar = Vector3.Distance(transform.position, LockTarget.position) > Range();
            if (s == null || s.IsDead || tooFar)
            {
                LockTarget = null;
                if (TargetField != null) TargetField.SetValue(controller, null); //so toggle knows we let go
            }
        }
        //step 5: camera looks at it, combat aims at it
        if (camCtrl != null) camCtrl.lockOnTarget = LockTarget;
        if (combat != null) combat.Target = LockTarget;
    }

    //lock on range (12 if the lookup failed)
    float Range()
    {
        if (RangeField == null) return 12f;
        return (float)RangeField.GetValue(controller);
    }
}
