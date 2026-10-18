using UnityEngine;

public enum FruitKind
{
    RedApple,
    GreenApple,
    Lemon
}

[RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class FruitPickup : MonoBehaviour
{
    private FruitMinigameController owner;
    private Rigidbody2D body;
    private float fallSpeed;
    private bool resolved;

    public FruitKind Kind { get; private set; }

    public void Initialize(
        FruitMinigameController controller,
        FruitKind fruitKind,
        Rigidbody2D rigidbody2D,
        float configuredFallSpeed)
    {
        owner = controller;
        Kind = fruitKind;
        body = rigidbody2D;
        fallSpeed = configuredFallSpeed;
    }

    private void FixedUpdate()
    {
        if (resolved || body == null)
            return;

        body.MovePosition(body.position + Vector2.down * (fallSpeed * Time.fixedDeltaTime));
    }

    public void Catch()
    {
        if (resolved)
            return;

        resolved = true;
        owner?.RegisterCaught(this);
    }

    public void Miss()
    {
        if (resolved)
            return;

        resolved = true;
        owner?.RegisterMissed(this);
    }
}
