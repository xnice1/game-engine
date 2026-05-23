using JumpKingClone.Components;
using JumpKingClone.Core;
using KripakEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
// ƒÓ‰‡ÌÓ ‰Îˇ Ó·ÓÚË Á Ù‡ÈÎ‡ÏË Ì‡ÔˇÏÛ
using System;
using System.IO;
using System.Diagnostics;
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

            // ‘≤ — ◊Œ–ÕŒØ —Ã”√»: ÕÂ ‰ÓÁ‚ÓÎˇ∫ÏÓ Í‡ÏÂ≥ ÓÔÛÒÍ‡ÚËÒˇ ÌËÊ˜Â ÌÛÎˇ (Ô≥‰ Ô≥‰ÎÓ„Û)
            if (Y > 0)
            {
                Y = 0;
            }
        }
    }

    public class GameplayScene : Scene
    {
        private Texture2D _pixel;

        private List<Entity> _backgroundEntities = new List<Entity>();
        private List<Entity> _gameEntities = new List<Entity>();

        private Camera _camera = new Camera();
        private Entity _playerEntity;

        // == œŒ –¿Ÿ≈Õ»… Õ≈œ–Œ¡»¬Õ»… Ã≈“Œƒ ƒÀﬂ «¿¬¿Õ“¿∆≈ÕÕﬂ PNG Õ¿œ–ﬂÃ” ==
        private Texture2D LoadPngDirect(GraphicsDevice graphicsDevice, string fileName)
        {
            var possiblePaths = new List<string>
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Content", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../Content", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gugugaga/Content", fileName)
            };

            string finalPath = null;

            foreach (var currentPath in possiblePaths)
            {
                Debug.WriteLine($"[Content] Checking path: {currentPath}");

                if (File.Exists(currentPath))
                {
                    finalPath = currentPath;
                    Debug.WriteLine($"[Content] File FOUND: {finalPath}");
                    break;
                }
            }

            if (finalPath != null)
            {
                try
                {
                    using (var stream = new FileStream(finalPath, FileMode.Open, FileAccess.Read))
                    {
                        return Texture2D.FromStream(graphicsDevice, stream);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Content] Error loading Texture2D from stream: {ex.Message}");
                }
            }

            Debug.WriteLine($"[Content] FAILED to load: {fileName}. Using default pixel texture.");
            return _pixel;
        }

        public GameplayScene(Texture2D pixel, ContentManager content)
        {
            _pixel = pixel;

            GraphicsDevice graphicsDevice = pixel.GraphicsDevice;

            int totalSections = 3;
            int sectionHeight = 270;

            for (int i = 0; i < totalSections; i++)
            {
                Texture2D sectionTex = LoadPngDirect(graphicsDevice, $"bg_layer_{i}.png");

                int yPosition = -(i * sectionHeight);
                AddBackgroundSection(sectionTex, yPosition);
            }

            _playerEntity = new Entity();
            _playerEntity.AddComponent(new TransformComponent(new Vector2(240, 200), 16, 20));
            _playerEntity.AddComponent(new PhysicsComponent());
            _playerEntity.AddComponent(new JumpKingComponent());

            Texture2D playerSheet = LoadPngDirect(graphicsDevice, "player_sheet.png");

            if (playerSheet == _pixel)
            {
                _playerEntity.AddComponent(new RenderComponent(Color.White));
            }
            else
            {
                _playerEntity.AddComponent(new AnimatorComponent(playerSheet, 16, 20));
            }

            _gameEntities.Add(_playerEntity);

            // “‚Óø Ì‡Î‡¯ÚÓ‚‡Ì≥ ≥‚Ì≥ Ú‡ ÔÎ‡ÚÙÓÏË
            AddPlatform(0, 250, 480, 5);
            AddPlatform(195, 200, 25, 10);
            AddPlatform(250, 150, 22, 20);
            AddPlatform(240, 70, 5, 48);
            AddPlatform(273, 70, 10, 48);
            AddPlatform(276, 108, 45, 7);
            AddPlatform(315, 98, 5, 20);
            AddPlatform(363, 90, 32, 10);
            AddPlatform(428, 105, 27, 7);
            AddPlatform(454, 47, 27, 7);
            AddPlatform(357, 15, 32, 10);
            AddPlatform(299, 18, 32, 10);
            AddPlatform(288, -38, 10, 70);
            AddPlatform(390, -52, 10, 80);
            AddPlatform(400, -28, 25, 7);
            AddPlatform(341, -308, 10, 286);
            AddPlatform(200, -50, 37, 7);
            AddPlatform(390, -100, 90, 13);
            AddPlatform(35, -136, 80, 18);
            AddPlatform(0, -200, 29, 18);
            AddPlatform(73, -203, 35, 25);
            AddPlatform(65, -265, 105, 30);
            AddPlatform(237, -268, 35, 30);
            AddPlatform(303, -309, 35, 30);
            AddPlatform(198, -226, 10, 17);
            AddPlatform(198, -309, 10, 26);
            AddPlatform(156, -340, 108, 30);
            AddPlatform(188, -397, 125, 25);
            AddPlatform(188, -443, 9, 45);
            AddPlatform(128, -395, 27, 90);
            AddPlatform(314, -396, 112, 10);
            AddPlatform(115, -125, 10, 8);

            AddPlatform(0, -545, 2, 800);
            AddPlatform(479, -545, 2, 800);
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

            plat.AddComponent(new RenderComponent(Color.Red));

            _gameEntities.Add(plat);
        }

        public override void Update(GameTime gameTime)
        {
            if (_playerEntity != null)
            {
                var playerTransform = _playerEntity.GetComponent<TransformComponent>();
                _camera.Follow(playerTransform.Position, 0.1f);
            }

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

                if (screenBounds.Bottom < -200 || screenBounds.Top > 500) continue;

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