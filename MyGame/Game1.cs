using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace MyGame;

public class Game1 : KripakEngine.EngineCore
{
    private Scene _currentScene;

    public Game1() : base()
    {
        _graphics.PreferredBackBufferWidth = 800;
        _graphics.PreferredBackBufferHeight = 500;
    }

    protected override void LoadContent()
    {
        base.LoadContent();
        _currentScene = new PongScene(_pixel, 800, 500);
    }

    protected override void Update(GameTime gameTime)
    {
        Input.Update();

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Input.Down(Keys.Escape))
            Exit();

        _currentScene.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkSlateGray);

        _spriteBatch.Begin();
        _currentScene.Draw(_spriteBatch);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
