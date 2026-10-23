// AI use: Simplified by Claude (Anthropic).
// Owner: (add name)

using UnityEngine;

// Spawns the weapon a character is holding and keeps track of its hitbox and muzzle.
// The player's weapon goes through EquipmentManager so the stat bonuses stay in sync.
// Enemies just equip their starting weapon.
public class WeaponInventory : MonoBehaviour
{
    [SerializeField] Transform weaponHolder;
    [SerializeField] WeaponItem startingWeapon;

    public WeaponItem CurrentWeapon { get; private set; }
    public GameObject WeaponObject { get; private set; }
    public WeaponHitbox Hitbox { get; private set; }
    public Transform Muzzle { get; private set; }

    bool isPlayer;
    bool UseManager => isPlayer && EquipmentManager.instance != null;

    void Awake()
    {
        isPlayer = GetComponent<PlayerStats>() != null;
    }

    void OnEnable()
    {
        if (UseManager)
            EquipmentManager.instance.onEquipmentChanged += HandleEquipmentChanged;
    }

    void OnDisable()
    {
        if (UseManager)
            EquipmentManager.instance.onEquipmentChanged -= HandleEquipmentChanged;
    }

    void Start()
    {
        if (startingWeapon != null)
            Equip(startingWeapon);
    }

    public void Equip(WeaponItem weapon)
    {
        if (UseManager)
            EquipmentManager.instance.Equip(weapon, false);
        else
            SetWeapon(weapon);
    }

    void HandleEquipmentChanged(Equipment newItem, Equipment oldItem)
    {
        // Only weapons matter here (null means it was taken off)
        if (newItem == null || newItem is WeaponItem)
            SetWeapon(newItem as WeaponItem);
    }

    void SetWeapon(WeaponItem weapon)
    {
        if (WeaponObject != null)
            Destroy(WeaponObject);

        CurrentWeapon = weapon;
        WeaponObject = null;
        Hitbox = null;
        Muzzle = weaponHolder;

        if (weapon == null || weapon.weaponPrefab == null)
            return;

        WeaponObject = Instantiate(weapon.weaponPrefab, weaponHolder);
        WeaponObject.transform.localPosition = Vector3.zero;
        WeaponObject.transform.localRotation = Quaternion.identity;

        Hitbox = WeaponObject.GetComponentInChildren<WeaponHitbox>(true);
        if (Hitbox != null)
            Hitbox.Setup(GetComponent<CharacterCombat>());

        // Ranged weapons have a WeaponMuzzle where the projectiles come out
        WeaponMuzzle muzzle = WeaponObject.GetComponentInChildren<WeaponMuzzle>(true);
        if (muzzle != null)
            Muzzle = muzzle.transform;
    }
}
