using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MyGame;

public class Game1 : Game
{
    GraphicsDeviceManager graphics;
    SpriteBatch spriteBatch;

    Texture2D pixel;

    // player
    Vector2 playerPos = new Vector2(100, 250);
    float playerSpeed = 5f;
    int paddleSize = 80;

    // enemy
    Vector2 enemyPos = new Vector2(700, 250);

    // puck
    Vector2 puckPos = new Vector2(400, 240);
    Vector2 puckVelocity = new Vector2(4, 4);
    int puckSize = 30;

    int screenWidth = 800;
    int screenHeight = 500;

    public Game1()
    {
        graphics = new GraphicsDeviceManager(this);

        graphics.PreferredBackBufferWidth = screenWidth;
        graphics.PreferredBackBufferHeight = screenHeight;

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);

        // white pixel texture
        pixel = new Texture2D(GraphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState k = Keyboard.GetState();

        // EXIT
        if (k.IsKeyDown(Keys.Escape))
            Exit();

        // PLAYER MOVEMENT
        if (k.IsKeyDown(Keys.W))
            playerPos.Y -= playerSpeed;

        if (k.IsKeyDown(Keys.S))
            playerPos.Y += playerSpeed;

        if (k.IsKeyDown(Keys.A))
            playerPos.X -= playerSpeed;

        if (k.IsKeyDown(Keys.D))
            playerPos.X += playerSpeed;

        // screen borders
        playerPos.X = MathHelper.Clamp(playerPos.X, 0, screenWidth - paddleSize);
        playerPos.Y = MathHelper.Clamp(playerPos.Y, 0, screenHeight - paddleSize);

        // SIMPLE AI
        if (enemyPos.Y + paddleSize / 2 < puckPos.Y)
            enemyPos.Y += 3;

        if (enemyPos.Y + paddleSize / 2 > puckPos.Y)
            enemyPos.Y -= 3;

        // PUCK MOVEMENT
        puckPos += puckVelocity;

        // WALL COLLISION
        if (puckPos.Y <= 0 || puckPos.Y >= screenHeight - puckSize)
            puckVelocity.Y *= -1;

        // GOAL RESET
        if (puckPos.X <= 0 || puckPos.X >= screenWidth - puckSize)
        {
            puckPos = new Vector2(400, 240);
            puckVelocity = new Vector2(4, 4);
        }

        Rectangle playerRect = new Rectangle(
            (int)playerPos.X,
            (int)playerPos.Y,
            paddleSize,
            paddleSize);

        Rectangle enemyRect = new Rectangle(
            (int)enemyPos.X,
            (int)enemyPos.Y,
            paddleSize,
            paddleSize);

        Rectangle puckRect = new Rectangle(
            (int)puckPos.X,
            (int)puckPos.Y,
            puckSize,
            puckSize);

        // COLLISION
        if (playerRect.Intersects(puckRect))
        {
            puckVelocity.X = Math.Abs(puckVelocity.X);
        }

        if (enemyRect.Intersects(puckRect))
        {
            puckVelocity.X = -Math.Abs(puckVelocity.X);
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkSlateGray);

        spriteBatch.Begin();

        // center line
        spriteBatch.Draw(pixel,
            new Rectangle(screenWidth / 2 - 2, 0, 4, screenHeight),
            Color.White);

        // player
        spriteBatch.Draw(pixel,
            new Rectangle(
                (int)playerPos.X,
                (int)playerPos.Y,
                paddleSize,
                paddleSize),
            Color.Blue);

        // enemy
        spriteBatch.Draw(pixel,
            new Rectangle(
                (int)enemyPos.X,
                (int)enemyPos.Y,
                paddleSize,
                paddleSize),
            Color.Red);

        // puck
        spriteBatch.Draw(pixel,
            new Rectangle(
                (int)puckPos.X,
                (int)puckPos.Y,
                puckSize,
                puckSize),
            Color.White);

        spriteBatch.End();

        base.Draw(gameTime);
    }
}