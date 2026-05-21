using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using KripakEngine; // 1. BRING IN YOUR ENGINE

namespace HopKripak
{
    // 2. INHERIT FROM YOUR ENGINE, NOT MONOGAME
    public class Game1 : EngineCore
    {
        public Game1()
        {
            // Set the vertical screen resolution for your Jump King game here
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 1000;
        }

        protected override void Initialize()
        {
            // Any game-specific setup goes here

            base.Initialize(); // This automatically calls the Engine's Initialize
        }

        protected override void LoadContent()
        {
            // 3. BOOT UP THE ENGINE'S RENDERER
            base.LoadContent();

            // TODO: Your Level Designer will eventually write code here to 
            // spawn the Player and Platform entities using the Engine!
        }

        // 4. NOTICE WHAT IS MISSING?
        // We completely deleted the Update() and Draw() methods from this file!
        // Why? Because your KripakEngine handles the Game Loop now. 
        // If you need specific game logic, you will write it in the Engine's Systems, 
        // not here in the main game file.
    }
}