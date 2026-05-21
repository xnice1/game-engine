using System.Collections.Generic;
using System.Linq;
using JumpKingClone.Core;
using JumpKingClone.Scenes;
using KripakEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class GameplayScene : Scene
{
    private Texture2D _pixel;
    private List<Entity> _sceneEntities = new List<Entity>();

    public GameplayScene(Texture2D pixel)
    {
        _pixel = pixel;

        // игрок из компонентов
        Entity player = new Entity();
        player.AddComponent(new TransformComponent(new Vector2(300, 200), 32, 32));
        player.AddComponent(new PhysicsComponent());
        player.AddComponent(new JumpKingComponent());
        player.AddComponent(new RenderComponent(Color.White));
        _sceneEntities.Add(player);

        // платформы
        AddPlatform(50, 450, 700, 40);
        AddPlatform(50, 200, 40, 250);
        AddPlatform(710, 200, 40, 250);
        AddPlatform(350, 330, 100, 20);
    }

    private void AddPlatform(int x, int y, int w, int h)
    {
        Entity plat = new Entity();
        plat.AddComponent(new TransformComponent(new Vector2(x, y), w, h));
        plat.AddComponent(new RenderComponent(Color.Gray));
        _sceneEntities.Add(plat);
    }

    public override void Update(GameTime gameTime)
    {
        var platforms = _sceneEntities
            .Where(e => e.HasComponent<TransformComponent>() && !e.HasComponent<JumpKingComponent>())
            .Select(e => e.GetComponent<TransformComponent>().Bounds)
            .ToList();

        foreach (var entity in _sceneEntities)
        {
            UpdateJumpKingInputSystem(entity);
            UpdateMovementAndCollisionSystem(entity, platforms);
        }
    }

    // обработка ввода героя
    private void UpdateJumpKingInputSystem(Entity entity)
    {
        if (!entity.HasComponent<JumpKingComponent>() || !entity.HasComponent<PhysicsComponent>()) return;

        var jk = entity.GetComponent<JumpKingComponent>();
        var phys = entity.GetComponent<PhysicsComponent>();

        if (phys.IsGrounded)
        {
            if (Input.Down(Keys.Space))
            {
                jk.IsCharging = true;
                phys.Velocity.X = 0;
                jk.JumpCharge += jk.ChargeSpeed;
                if (jk.JumpCharge > jk.MaxCharge) jk.JumpCharge = jk.MaxCharge;
            }
            else if (jk.IsCharging)
            {
                jk.IsCharging = false;
                phys.Velocity.Y = -(jk.BaseJumpForce + jk.JumpCharge);

                if (Input.Down(Keys.Left) || Input.Down(Keys.A)) phys.Velocity.X = -phys.WalkSpeed * 1.2f;
                else if (Input.Down(Keys.Right) || Input.Down(Keys.D)) phys.Velocity.X = phys.WalkSpeed * 1.2f;
                else phys.Velocity.X = 0;

                jk.JumpCharge = 0f;
                phys.IsGrounded = false;
            }
            else
            {
                if (Input.Down(Keys.Left) || Input.Down(Keys.A)) phys.Velocity.X = -phys.WalkSpeed;
                else if (Input.Down(Keys.Right) || Input.Down(Keys.D)) phys.Velocity.X = phys.WalkSpeed;
                else phys.Velocity.X = 0;
            }
        }
    }

    // физика (коллизии, гравитация и тд.)
    private void UpdateMovementAndCollisionSystem(Entity entity, List<Rectangle> platforms)
    {
        if (!entity.HasComponent<TransformComponent>() || !entity.HasComponent<PhysicsComponent>()) return;

        var transform = entity.GetComponent<TransformComponent>();
        var phys = entity.GetComponent<PhysicsComponent>();

        if (!phys.IsGrounded) phys.Velocity.Y += phys.Gravity;

        transform.Position += phys.Velocity;
        phys.IsGrounded = false;

        Rectangle bounds = transform.Bounds;

        foreach (var platform in platforms)
        {
            if (bounds.Intersects(platform))
            {
                if (phys.Velocity.Y > 0 && transform.Position.Y + transform.Height - phys.Velocity.Y <= platform.Top + 4)
                {
                    transform.Position.Y = platform.Top - transform.Height;
                    phys.Velocity = Vector2.Zero;
                    phys.IsGrounded = true;
                }
                else if (phys.Velocity.Y < 0 && transform.Position.Y - phys.Velocity.Y >= platform.Bottom - 4)
                {
                    transform.Position.Y = platform.Bottom;
                    phys.Velocity.Y = 0;
                }
                else
                {
                    if (phys.Velocity.X > 0)
                    {
                        transform.Position.X = platform.Left - transform.Width;
                        phys.Velocity.X = -phys.Velocity.X * 0.6f;
                    }
                    else if (phys.Velocity.X < 0)
                    {
                        transform.Position.X = platform.Right;
                        phys.Velocity.X = -phys.Velocity.X * 0.6f;
                    }
                }
            }
        }
    }

    // декоративная отрисовка
    public override void Draw(SpriteBatch spriteBatch)
    {
        foreach (var entity in _sceneEntities)
        {
            if (!entity.HasComponent<TransformComponent>() || !entity.HasComponent<RenderComponent>()) continue;

            var transform = entity.GetComponent<TransformComponent>();
            var render = entity.GetComponent<RenderComponent>();

            Color finalColor = render.DefaultColor;

            if (entity.HasComponent<JumpKingComponent>() && entity.HasComponent<PhysicsComponent>())
            {
                var jk = entity.GetComponent<JumpKingComponent>();
                var phys = entity.GetComponent<PhysicsComponent>();

                if (jk.IsCharging)
                    finalColor = Color.Lerp(Color.White, Color.Red, jk.JumpCharge / jk.MaxCharge);
                else if (!phys.IsGrounded)
                    finalColor = Color.LightSkyBlue;
            }

            spriteBatch.Draw(_pixel, transform.Bounds, finalColor);
        }
    }
}