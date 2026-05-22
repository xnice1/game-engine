using KripakEngine;
using Microsoft.Xna.Framework;

namespace MyGame;

public class PaddleComponent : Component
{
    public float Speed;
    public bool IsPlayer;
    public Vector2 Velocity; // last-frame movement, used for puck impulse
    public PaddleComponent(float speed, bool isPlayer) { Speed = speed; IsPlayer = isPlayer; }
}

public class PuckComponent : Component
{
    public Vector2 Velocity;
    public PuckComponent(Vector2 velocity) { Velocity = velocity; }
}
