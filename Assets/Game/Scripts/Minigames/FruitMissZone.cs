using UnityEngine;

public sealed class FruitMissZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        FruitPickup fruit = other.GetComponent<FruitPickup>();
        if (fruit != null)
            fruit.Miss();
    }
}
