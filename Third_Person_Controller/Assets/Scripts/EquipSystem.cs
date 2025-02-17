using UnityEngine;

public class EquipSystem : MonoBehaviour
{
    private static readonly int EquippedHash = Animator.StringToHash("Equipped");
    private readonly int sheathWeaponHash = Animator.StringToHash("SheathWeapon");
    private readonly int drawWeaponHash = Animator.StringToHash("DrawWeapon");
    private readonly int cancelWeaponInteractionHash = Animator.StringToHash("CancelWeaponInteraction");
    
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private Transform sheathHolder;
    [SerializeField] private GameObject dummyWeapon;
    private static bool WantsToEquipWeapon => Input.GetKeyDown(KeyCode.E);

    private GameObject _currentWeaponInHand;
    private GameObject _currentWeaponInSheath;
    
    private bool _isEquipped;
    
    private Animator _animator;

    private int _currentHash;

    private void Awake() {
        _animator = GetComponent<Animator>();
        _currentHash = sheathWeaponHash;
        _currentWeaponInSheath = sheathHolder.GetChild(0).gameObject;
    }

    private void Update() {
        if (!WantsToEquipWeapon) return;
        int newHash = _currentHash == sheathWeaponHash ? drawWeaponHash : sheathWeaponHash;
        _animator.ResetTrigger(_currentHash);
        _currentHash = newHash;
        _animator.SetTrigger(newHash);
    }

    public bool WeaponEquipped { get; private set; }

    public void EquipWeapon() {
        WeaponEquipped = true;
        _currentWeaponInHand = Instantiate(dummyWeapon, weaponHolder);
        
        if(_currentWeaponInSheath != null)
            Destroy(_currentWeaponInSheath);
        
        _animator.SetBool(EquippedHash, true);
    }

    public void SetCombatLayerWeight(int weight) {
        _animator.SetLayerWeight(1, weight);
    }

    public void SheathWeapon() {
        WeaponEquipped = false;
        _currentWeaponInSheath = Instantiate(dummyWeapon, sheathHolder);
        
        if(_currentWeaponInHand != null)
            Destroy(_currentWeaponInHand);
        
        _animator.SetBool(EquippedHash, false);
    }
}
