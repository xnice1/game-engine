using System;
using System.Collections.Generic;
using KripakEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MyGame;

public class PongScene : Scene
{
    private Texture2D _pixel;
    private List<Entity> _entities = new List<Entity>();
    private int _screenWidth;
    private int _screenHeight;

    private const int PaddleSize = 80;
    private const int PuckSize = 30;

    public PongScene(Texture2D pixel, int screenWidth, int screenHeight)
    {
        _pixel = pixel;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        Entity player = new Entity();
        player.AddComponent(new TransformComponent(new Vector2(100, 250), PaddleSize, PaddleSize));
        player.AddComponent(new PaddleComponent(5f, isPlayer: true));
        player.AddComponent(new RenderComponent(Color.Blue));
        _entities.Add(player);

        Entity enemy = new Entity();
        enemy.AddComponent(new TransformComponent(new Vector2(700, 250), PaddleSize, PaddleSize));
        enemy.AddComponent(new PaddleComponent(3f, isPlayer: false));
        enemy.AddComponent(new RenderComponent(Color.Red));
        _entities.Add(enemy);

        Entity puck = new Entity();
        puck.AddComponent(new TransformComponent(new Vector2(400, 240), PuckSize, PuckSize));
        puck.AddComponent(new PuckComponent(new Vector2(4, 4)));
        puck.AddComponent(new RenderComponent(Color.White));
        _entities.Add(puck);
    }

    public override void Update(GameTime gameTime)
    {
        Entity puckEntity = GetPuck();
        var puckTransform = puckEntity.GetComponent<TransformComponent>();
        var puckPhysics = puckEntity.GetComponent<PuckComponent>();

        UpdatePlayerSystem();
        UpdateAiSystem(puckTransform);

        puckTransform.Position += puckPhysics.Velocity;

        if (puckTransform.Position.Y <= 0 || puckTransform.Position.Y >= _screenHeight - PuckSize)
            puckPhysics.Velocity.Y *= -1;

        if (puckTransform.Position.X <= 0 || puckTransform.Position.X >= _screenWidth - PuckSize)
        {
            puckTransform.Position = new Vector2(400, 240);
            puckPhysics.Velocity = new Vector2(4, 4);
        }

        UpdateCollisionSystem(puckTransform, puckPhysics);

        foreach (var entity in _entities)
        {
            if (!entity.HasComponent<PaddleComponent>()) continue;
            var t = entity.GetComponent<TransformComponent>();
            t.Position.X = MathHelper.Clamp(t.Position.X, 0, _screenWidth - PaddleSize);
            t.Position.Y = MathHelper.Clamp(t.Position.Y, 0, _screenHeight - PaddleSize);
        }
    }

    private void UpdatePlayerSystem()
    {
        foreach (var entity in _entities)
        {
            var paddle = entity.GetComponent<PaddleComponent>();
            if (paddle == null || !paddle.IsPlayer) continue;

            var t = entity.GetComponent<TransformComponent>();
            if (Input.Down(Microsoft.Xna.Framework.Input.Keys.W)) t.Position.Y -= paddle.Speed;
            if (Input.Down(Microsoft.Xna.Framework.Input.Keys.S)) t.Position.Y += paddle.Speed;
            if (Input.Down(Microsoft.Xna.Framework.Input.Keys.A)) t.Position.X -= paddle.Speed;
            if (Input.Down(Microsoft.Xna.Framework.Input.Keys.D)) t.Position.X += paddle.Speed;
        }
    }

    private void UpdateAiSystem(TransformComponent puckTransform)
    {
        foreach (var entity in _entities)
        {
            var paddle = entity.GetComponent<PaddleComponent>();
            if (paddle == null || paddle.IsPlayer) continue;

            var t = entity.GetComponent<TransformComponent>();
            if (t.Position.Y + PaddleSize / 2 < puckTransform.Position.Y)
                t.Position.Y += paddle.Speed;
            else if (t.Position.Y + PaddleSize / 2 > puckTransform.Position.Y)
                t.Position.Y -= paddle.Speed;
        }
    }

    private void UpdateCollisionSystem(TransformComponent puckTransform, PuckComponent puckPhysics)
    {
        Rectangle puckRect = puckTransform.Bounds;

        foreach (var entity in _entities)
        {
            if (!entity.HasComponent<PaddleComponent>()) continue;

            var t = entity.GetComponent<TransformComponent>();
            var paddle = entity.GetComponent<PaddleComponent>();

            if (!t.Bounds.Intersects(puckRect)) continue;

            if (paddle.IsPlayer)
                puckPhysics.Velocity.X = Math.Abs(puckPhysics.Velocity.X);
            else
                puckPhysics.Velocity.X = -Math.Abs(puckPhysics.Velocity.X);
        }
    }

    private Entity GetPuck()
    {
        foreach (var entity in _entities)
            if (entity.HasComponent<PuckComponent>()) return entity;
        return null;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_pixel, new Rectangle(_screenWidth / 2 - 2, 0, 4, _screenHeight), Color.White);

        foreach (var entity in _entities)
        {
            if (!entity.HasComponent<TransformComponent>() || !entity.HasComponent<RenderComponent>()) continue;

            var t = entity.GetComponent<TransformComponent>();
            var r = entity.GetComponent<RenderComponent>();
            spriteBatch.Draw(_pixel, t.Bounds, r.DefaultColor);
        }
    }
}
