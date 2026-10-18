using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class FruitPlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float minimumX = -8.1f;
    [SerializeField] private float maximumX = 8.1f;

    private Rigidbody2D body;
    private float horizontalInput;
    private bool movementEnabled = true;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!movementEnabled || Keyboard.current == null)
        {
            horizontalInput = 0f;
            return;
        }

        bool leftPressed = Keyboard.current.leftArrowKey.isPressed;
        bool rightPressed = Keyboard.current.rightArrowKey.isPressed;
        horizontalInput = (rightPressed ? 1f : 0f) - (leftPressed ? 1f : 0f);
    }

    private void FixedUpdate()
    {
        if (!movementEnabled || body == null)
            return;

        float targetX = Mathf.Clamp(
            body.position.x + horizontalInput * moveSpeed * Time.fixedDeltaTime,
            minimumX,
            maximumX);
        body.MovePosition(new Vector2(targetX, body.position.y));
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
        if (!enabled)
            horizontalInput = 0f;
    }

    public void Configure(float speed, float minX, float maxX)
    {
        moveSpeed = speed;
        minimumX = minX;
        maximumX = maxX;
    }
}
