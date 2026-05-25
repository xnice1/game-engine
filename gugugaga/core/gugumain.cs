using JumpKingClone.Core;
using JumpKingClone.Scenes;
using KripakEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace gugugaga
{
    public class gugumain : KripakEngine.EngineCore
    {
        private RenderTarget2D _virtualRenderTarget;
        private Scene _currentScene;
        private GameSfx _sfx;

        public const int TargetWidth = 480;
        public const int TargetHeight = 270;

        public gugumain() : base()
        {
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            base.Initialize();

            _virtualRenderTarget = new RenderTarget2D(GraphicsDevice, TargetWidth, TargetHeight);
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            _sfx = new GameSfx(Content);
            _sfx.StartBgm();
            _currentScene = new GameplayScene(_pixel, Content, _sfx);
        }

        protected override void Update(GameTime gameTime)
        {
            JumpKingClone.Core.Input.Update();

            base.Update(gameTime);

            _currentScene?.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(_virtualRenderTarget);
            GraphicsDevice.Clear(Color.Black);

            _currentScene?.Draw(_spriteBatch);

            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            _spriteBatch.Draw(_virtualRenderTarget, new Rectangle(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height), Color.White);
            _spriteBatch.End();
        }
    }
}