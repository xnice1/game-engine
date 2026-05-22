using JumpKingClone.Components;
using JumpKingClone.Core;
using KripakEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JumpKingClone.Scenes
{
    public class Camera
    {
        public float Y;
        public int TargetWidth = 480;
        public int TargetHeight = 270;

        public void Follow(Vector2 targetPosition, float lerpSpeed)
        {
                float targetY = targetPosition.Y - (TargetHeight / 2);
                Y = MathHelper.Lerp(Y, targetY, lerpSpeed);
        }
    }

    public class GameplayScene : Scene
    {
        private Texture2D _pixel;

        private List<Entity> _backgroundEntities = new List<Entity>();
        private List<Entity> _gameEntities = new List<Entity>();

        private Camera _camera = new Camera();
        private Entity _playerEntity;

        public GameplayScene(Texture2D pixel, ContentManager content)
        {
            _pixel = pixel;

            int totalSections = 3;
            int sectionHeight = 270;

            for (int i = 0; i < totalSections; i++)
            {
                Texture2D sectionTex = content.Load<Texture2D>($"bg_layer_{i}");

                int yPosition = -(i * sectionHeight);
                AddBackgroundSection(sectionTex, yPosition);
            }

            _playerEntity = new Entity();
            _playerEntity.AddComponent(new TransformComponent(new Vector2(240, 200), 16, 20));
            _playerEntity.AddComponent(new PhysicsComponent());
            _playerEntity.AddComponent(new JumpKingComponent());
            _playerEntity.AddComponent(new AnimatorComponent(content.Load<Texture2D>("player_sheet"), 16, 20));
            _gameEntities.Add(_playerEntity);

            AddPlatform(0, 250, 480, 20);

            AddPlatform(0, 0, 20, 270);
            AddPlatform(460, 0, 20, 270);
            AddPlatform(140, 160, 100, 15);
            AddPlatform(280, 110, 80, 15);

            // /\ /\ /\ /\ /\
            // || || || || ||
            //ÑÎÇÄÀÍÈÅ ÏËÀÒÔÎÐÌ ÏÐÎÈÑÕÎÄÈÒ ÇÄÅÑÜ !!!
        }

        private void AddBackgroundSection(Texture2D tex, int yPosition)
        {
            Entity bg = new Entity();
            bg.AddComponent(new TransformComponent(new Vector2(0, yPosition), 480, 270));
            bg.AddComponent(new SpriteComponent(tex));
            _backgroundEntities.Add(bg);
        }

        private void AddPlatform(int x, int y, int w, int h)
        {
            Entity plat = new Entity();
            plat.AddComponent(new TransformComponent(new Vector2(x, y), w, h));

            //ÏÐÈ ÑÎÇÄÀÍÈÈ ÏËÀÒÔÎÐÌ ÆÅËÀÒÅËÜÍÎ ÐÀÑÊÎÌÌÅÍÒÈÐÎÂÀÒÜ ÑÒÐÎÊÓ ÍÈÆÅ,
            //ÁÅÇ ÍÅ¨ ÏËÀÒÔÎÐÌÛ ÁÓÄÓÒ ÍÅÂÈÄÈÌÛÌÈ, ÊÀÊ ÑÎÁÑÒÂÅÍÍÎ È ÄÎËÆÍÛ ÁÛÒÜ !!!
            // || || || || ||
            // \/ \/ \/ \/ \/ 


            //plat.AddComponent(new RenderComponent(Color.Red));

            // /\ /\ /\ /\ /\
            // || || || || ||

            _gameEntities.Add(plat);
        }

        public override void Update(GameTime gameTime)
        {
            var platforms = _gameEntities
                .Where(e => e.HasComponent<TransformComponent>() && !e.HasComponent<JumpKingComponent>())
                .Select(e => e.GetComponent<TransformComponent>().Bounds)
                .ToList();

            foreach (var entity in _gameEntities)
            {
                UpdateJumpKingInputSystem(entity);
                UpdateMovementAndCollisionSystem(entity, platforms);
                UpdateAnimationSystem(entity, gameTime);
            }

            if (_playerEntity != null)
            {
                var playerTransform = _playerEntity.GetComponent<TransformComponent>();
                _camera.Follow(playerTransform.Position, 0.1f);
            }
        }

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

                    if (Input.Down(Keys.Left) || Input.Down(Keys.A)) phys.Velocity.X = -phys.WalkSpeed * 1.5f;
                    else if (Input.Down(Keys.Right) || Input.Down(Keys.D)) phys.Velocity.X = phys.WalkSpeed * 1.5f;
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

        private void UpdateAnimationSystem(Entity entity, GameTime gameTime)
        {
            if (!entity.HasComponent<AnimatorComponent>() || !entity.HasComponent<PhysicsComponent>() || !entity.HasComponent<JumpKingComponent>()) return;

            var anim = entity.GetComponent<AnimatorComponent>();
            var phys = entity.GetComponent<PhysicsComponent>();
            var jk = entity.GetComponent<JumpKingComponent>();

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (!phys.IsGrounded)
            {
                anim.CurrentFrame = 0;
            }
            else if (jk.IsCharging)
            {
                anim.CurrentFrame = 2;
            }
            else if (Math.Abs(phys.Velocity.X) > 0.1f)
            {
                anim.TimeSinceLastFrame += dt;
                if (anim.TimeSinceLastFrame >= anim.FrameTime)
                {
                    anim.TimeSinceLastFrame = 0;
                    anim.CurrentFrame = anim.CurrentFrame == 0 ? 1 : 0;
                }
            }
            else
            {
                anim.CurrentFrame = 0;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

            DrawEntityList(spriteBatch, _backgroundEntities);

            DrawEntityList(spriteBatch, _gameEntities);

            spriteBatch.End();
        }

        private void DrawEntityList(SpriteBatch spriteBatch, List<Entity> entities)
        {
            foreach (var entity in entities)
            {
                if (!entity.HasComponent<TransformComponent>()) continue;

                var transform = entity.GetComponent<TransformComponent>();

                Rectangle screenBounds = new Rectangle(
                    transform.Bounds.X,
                    transform.Bounds.Y - (int)_camera.Y,
                    transform.Bounds.Width,
                    transform.Bounds.Height
                );

                if (screenBounds.Bottom < -50 || screenBounds.Top > 320) continue;

                if (entity.HasComponent<SpriteComponent>())
                {
                    var sprite = entity.GetComponent<SpriteComponent>();
                    spriteBatch.Draw(sprite.Texture, screenBounds, sprite.SourceRectangle, sprite.Color);
                }
                else if (entity.HasComponent<AnimatorComponent>())
                {
                    var anim = entity.GetComponent<AnimatorComponent>();
                    Rectangle sourceRect = new Rectangle(anim.CurrentFrame * anim.FrameWidth, 0, anim.FrameWidth, anim.FrameHeight);
                    spriteBatch.Draw(anim.SpriteSheet, screenBounds, sourceRect, Color.White);
                }
                else if (entity.HasComponent<RenderComponent>())
                {
                    var render = entity.GetComponent<RenderComponent>();
                    spriteBatch.Draw(_pixel, screenBounds, render.DefaultColor);
                }
            }
        }
    }
}