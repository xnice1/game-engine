using JumpKingClone.Core;
using JumpKingClone.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace gugugaga
{
    public class gugumain : KripakEngine.EngineCore
    {
        private Scene currentScene;

        public gugumain() : base()
        {

        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            currentScene = new GameplayScene(_pixel);
        }

        protected override void Update(GameTime gameTime)
        {
            Input.Update();

            currentScene.Update(gameTime);

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Input.Down(Keys.Escape))
                Exit();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();

            currentScene.Draw(_spriteBatch);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
