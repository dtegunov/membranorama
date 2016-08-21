using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Membranogram.Helpers
{
    public static class KeyboardHelper
    {
        public static bool CtrlDown()
        {
            return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        }

        public static bool ShiftDown()
        {
            return Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        }

        public static bool AltDown()
        {
            return Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
        }

        public static bool MouseLeftDown()
        {
            return Mouse.LeftButton == MouseButtonState.Pressed;
        }

        public static bool MouseRightDown()
        {
            return Mouse.RightButton == MouseButtonState.Pressed;
        }
    }
}
