using Microsoft.Xna.Framework.Input;

namespace JumpKingClone.Core
{
    public static class Input
    {
        public static KeyboardState CurrentKeyboard;
        public static KeyboardState PreviousKeyboard;

        public static void Update()
        {
            PreviousKeyboard = CurrentKeyboard;
            CurrentKeyboard = Keyboard.GetState();
        }

        public static bool Down(Keys key)
        {
            return CurrentKeyboard.IsKeyDown(key);
        }

        public static bool Pressed(Keys key)
        {
            return CurrentKeyboard.IsKeyDown(key) &&
                   PreviousKeyboard.IsKeyUp(key);
        }

        public static bool Released(Keys key)
        {
            return CurrentKeyboard.IsKeyUp(key) &&
                   PreviousKeyboard.IsKeyDown(key);
        }
    }
}