using KripakEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpKingClone.Components
{
    public class TransformComponent : Component
    {
        public Vector2 Position;
        public int Width;
        public int Height;
        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Width, Height);

        public TransformComponent(Vector2 pos, int w, int h)
        {
            Position = pos;
            Width = w;
            Height = h;
        }
    }

    public class RenderComponent : Component
    {
        public Color DefaultColor { get; set; }

        public RenderComponent(Color defaultColor)
        {
            DefaultColor = defaultColor;
        }
    }

    public class PhysicsComponent : Component
    {
        public Vector2 Velocity;
        public bool IsGrounded;
        public float Gravity = 0.35f;
        public float WalkSpeed = 1.8f;
    }

    public class JumpKingComponent : Component
    {
        public bool IsCharging;
        public float JumpCharge;
        public float MaxCharge = 5f;
        public float ChargeSpeed = 0.3f;
        public float BaseJumpForce = 4f;
    }

    public class SpriteComponent : Component
    {
        public Texture2D Texture;
        public Rectangle? SourceRectangle;
        public Color Color = Color.White;

        public SpriteEffects Effects = SpriteEffects.None;

        public SpriteComponent(Texture2D texture, Rectangle? source = null)
        {
            Texture = texture;
            SourceRectangle = source;
        }
    }

    public class AnimatorComponent : Component
    {
        public Texture2D SpriteSheet;
        public int FrameWidth;
        public int FrameHeight;
        public int CurrentFrame;

        public float TimeSinceLastFrame;
        public float FrameTime = 0.1f; //100 мс на кадр

        public bool FacingRight = true;

        public AnimatorComponent(Texture2D sheet, int frameWidth, int frameHeight)
        {
            SpriteSheet = sheet;
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
        }
    }
}