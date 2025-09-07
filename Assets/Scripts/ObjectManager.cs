using Unity.VisualScripting;
using UnityEngine;

public abstract class ObjectManager : MonoBehaviour
{
    //All objects such as Towers and Enemies will inherit from this class

    [Header("Object Components")]
    public Rigidbody rigidBody;
    public BoxCollider hitBox;
    public SphereCollider rangeCollider;
    public GameObject objectUI;
    public ObjectUIManager uiManager;
    public Health health;
    public Attack attack;

    public GameObject objectUiPrefab;  //assign in inspector

    public string objectTag;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Setup()
    {
        Debug.Log($"ObjectManager Start() called for {gameObject.name}");

        // Acquire or create object UI safely
        if (objectUI == null)
        {
            var uiManagerComponent = GetComponentInChildren<ObjectUIManager>();
            if (uiManagerComponent != null)
            {
                uiManager = uiManagerComponent;
                objectUI = uiManagerComponent.gameObject; // assign the GameObject itself
            }
            else
            {
                Vector3 uiPosition = transform.position + new Vector3(0, 2, 0);
                objectUI = Instantiate(objectUiPrefab, uiPosition, Quaternion.identity);
            }
        }
        uiManager = objectUI.GetComponent<ObjectUIManager>();
        if(uiManager == null)
        {
            Debug.LogError("ObjectUIManager component not found on the instantiated objectUI for " + name);
        }
        uiManager.nameText.text = objectTag;
        uiManager.UpdateHealthBar(1, 1); // Initialize health bar to full

        //Get or add required components
        rigidBody = GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
        hitBox = GetComponent<BoxCollider>() ?? gameObject.AddComponent<BoxCollider>();
        rangeCollider = GetComponent<SphereCollider>() ?? gameObject.AddComponent<SphereCollider>();

        health = GetComponent<Health>() ?? gameObject.AddComponent<Health>();
        health.Setup(uiManager);
        attack = GetComponent<Attack>() ?? gameObject.AddComponent<Attack>();
        attack.Setup(rigidBody, hitBox, rangeCollider);

        this.tag = objectTag;  //set tag to tower
    }

    public abstract void OnDeath();
}
