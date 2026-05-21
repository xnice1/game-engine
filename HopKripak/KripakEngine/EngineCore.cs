using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace KripakEngine
{
    // By inheriting from Game, YOUR engine controls the Game Loop, not just MonoGame
    public abstract class EngineCore : Game
    {
        protected GraphicsDeviceManager _graphics;
        protected SpriteBatch _spriteBatch;

        // The core Entity Component System list
        protected List<object> _entities = new List<object>();

        public EngineCore()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            // This is where your Audio and Render engineers will initialize their systems
        }

        protected override void Update(GameTime gameTime)
        {
            // 1. Input Manager checks keys here
            // 2. Physics System updates all entities here

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue); // Default background

            _spriteBatch.Begin();
            // 3. Render System draws all active entities here
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}