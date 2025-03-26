using System.Collections;
using UnityEngine;

public class EquipSystem : MonoBehaviour {
    private readonly int sheathWeaponHash = Animator.StringToHash("SheathWeapon");
    private readonly int drawWeaponHash = Animator.StringToHash("DrawWeapon");
    
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private Transform sheathHolder;
    [SerializeField] private GameObject dummyWeapon;
    [SerializeField] private float layerTransition = 1.0f;

    private float _currentLayerTransition = 0.0f;
    private static bool WantsToEquipWeapon => Input.GetKeyDown(KeyCode.E);

    private GameObject _currentWeaponInHand;
    private GameObject _currentWeaponInSheath;
    
    private bool _isEquipped;
    private Animator _animator;
    private int _currentHash;

    private Coroutine _smoothTransition;
    
    public bool WeaponEquipped { get; private set; }

    private void Awake() {
        _animator = GetComponent<Animator>();
        _currentHash = sheathWeaponHash;
        _currentWeaponInSheath = sheathHolder.GetChild(0).gameObject;
    }

    private void Update() {
        if (!WantsToEquipWeapon) return;
        
        _currentLayerTransition = 0.0f;
        
        if (!WeaponEquipped && _animator.GetLayerWeight(1) < 1.0f) {
            _animator.SetLayerWeight(1, 1);
        }
        
        int newHash = _currentHash == sheathWeaponHash ? drawWeaponHash : sheathWeaponHash;
        _currentHash = newHash;
        _animator.SetTrigger(newHash);
    }

    public void EquipWeapon() {
        WeaponEquipped = true;
        _currentWeaponInHand = Instantiate(dummyWeapon, weaponHolder);
        
        if(_currentWeaponInSheath != null)
            Destroy(_currentWeaponInSheath);
    }
    
    public void SheathWeapon() {
        WeaponEquipped = false;
        _currentWeaponInSheath = Instantiate(dummyWeapon, sheathHolder);
        
        if(_currentWeaponInHand != null)
            Destroy(_currentWeaponInHand);
        
        _animator.SetLayerWeight(1, 0);
    }
    
    private void SetCombatLayerWeight(int weight) {
        if (_smoothTransition != null) {
            StopCoroutine(_smoothTransition);
            _smoothTransition = null;
        }
        
        _smoothTransition = StartCoroutine(SmoothTransitionAnimationLayerWeight(weight));
    }
    
    private IEnumerator SmoothTransitionAnimationLayerWeight(float weight) {
        while(Mathf.Abs(_animator.GetLayerWeight(1) - weight) > 0.001f) {
            _currentLayerTransition += Time.deltaTime;
            float t = _currentLayerTransition / layerTransition;
            
            _animator.SetLayerWeight(1, Mathf.Lerp(_animator.GetLayerWeight(1), weight, t));
            yield return null;
        }
    }
}
