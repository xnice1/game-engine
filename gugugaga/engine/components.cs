using KripakEngine;
using Microsoft.Xna.Framework;

public class TransformComponent : Component
{
    public Vector2 Position;
    public int Width;
    public int Height;
    public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Width, Height);

    public TransformComponent(Vector2 pos, int w, int h)
    {
        Position = pos; Width = w; Height = h;
    }
}

public class PhysicsComponent : Component
{
    public Vector2 Velocity;
    public bool IsGrounded;
    public float Gravity = 0.35f;
    public float WalkSpeed = 2.5f;
}

public class JumpKingComponent : Component
{
    public bool IsCharging;
    public float JumpCharge;
    public float MaxCharge = 15f;
    public float ChargeSpeed = 0.3f;
    public float BaseJumpForce = 4f;
}

public class RenderComponent : Component
{
    public Color DefaultColor;
    public RenderComponent(Color color) => DefaultColor = color;
}