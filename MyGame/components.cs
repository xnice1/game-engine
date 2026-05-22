using KripakEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

public class SpriteComponent : Component
{
    public Texture2D Texture;
    public SpriteComponent(Texture2D texture) { Texture = texture; }
}
