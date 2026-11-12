using UnityEngine;
using UnityEngine.InputSystem;

public class DeliveryChute : MonoBehaviour
{
    [Header("Testing only")]
    [Tooltip("Drag a package prefab here to test the chute with a key.")]
    public GameObject testPackage;
    public bool useTestKey = false;
    public Key testKey = Key.V;

    // The last package that came out (other scripts can read it)
    public GameObject LastDelivered { get; private set; }

    void Update()
    {
        if (!useTestKey || testPackage == null || Keyboard.current == null) return;
        if (Keyboard.current[testKey].wasPressedThisFrame)
            Deliver(testPackage);
    }

    // Spawns the prefab at the chute mouth and lets physics slide it down
    public GameObject Deliver(GameObject prefab)
    {
        if (prefab == null) return null;

        GameObject package = Instantiate(prefab, transform.position, Quaternion.identity);

        // A little random turn so packages do not always look the same
        package.transform.rotation = Quaternion.Euler(0f, Random.Range(-20f, 20f), 0f);

        LastDelivered = package;
        Debug.Log("Delivered: " + package.name);
        return package;
    }
}