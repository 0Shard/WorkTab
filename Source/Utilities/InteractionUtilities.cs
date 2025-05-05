// InteractionUtilities.cs
// Copyright Karel Kroeze, 2017-2020

using System;
using UnityEngine;
using Verse;

namespace WorkTab {
    public enum ScrollDirection {
        Up,
        Down
    }

    public static class InteractionUtilities {
        public static bool Alt => Input.GetKey(KeyCode.AltGr) || Input.GetKey(KeyCode.LeftAlt) ||
                                  Input.GetKey(KeyCode.RightAlt) || Event.current.alt;

        public static bool Ctrl => Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.LeftControl) ||
                                   Input.GetKey(KeyCode.RightCommand) || Input.GetKey(KeyCode.RightControl) ||
                                   Event.current.control;

        public static bool Shift => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Event.current.shift;

        public static void ButtonImageToggle(Func<bool> getter, Action<bool> setter, Rect canvas,
                                              string tipOn, Texture2D texOn,
                                              string tipOff, Texture2D texOff) {
            if (setter == null || getter == null) {
                throw new NullReferenceException("getter and setter must not be null");
            }

            TooltipHandler.TipRegion(canvas, getter() ? tipOff : tipOn);
            if (Widgets.ButtonImage(canvas, getter() ? texOff : texOn, Color.white, GenUI.MouseoverColor)) {
                setter(!getter());
            }
        }

        private static bool Clicked(Rect rect, int button = 0) {
            return Event.current.type == EventType.MouseDown && Clicked(button) && Mouse.IsOver(rect);
        }

        private static bool Clicked(int button = 0) {
            return Event.current.button == button;
        }

        public static bool LeftClicked(Rect rect) {
            return Clicked(rect);
        }

        public static bool LeftClicked() {
            return Clicked();
        }

        public static bool RightClicked(Rect rect) {
            return Clicked(rect, 1);
        }

        public static bool RightClicked() {
            return Clicked(1);
        }

        private static bool Scrolled(Rect rect, ScrollDirection direction, bool stopPropagation) {
            if (Settings.disableScrollwheel) {
                return false;
            }

            var scrolled = Event.current.type == EventType.ScrollWheel && Scrolled(direction) && Mouse.IsOver(rect);

            if (scrolled && stopPropagation) {
                Event.current.Use();
            }

            return scrolled;
        }

        private static bool Scrolled(ScrollDirection direction) {
            if (Settings.disableScrollwheel) {
                return false;
            }

            var scrolled = Event.current.delta.y > 0 && direction == ScrollDirection.Up ||
                           Event.current.delta.y < 0 && direction == ScrollDirection.Down;

            return scrolled;
        }

        public static bool ScrolledDown(Rect rect, bool stopPropagation = false) {
            return Scrolled(rect, ScrollDirection.Down, stopPropagation);
        }

        public static bool ScrolledDown() {
            return Scrolled(ScrollDirection.Down);
        }

        public static bool ScrolledUp(Rect rect, bool stopPropagation = false) {
            return Scrolled(rect, ScrollDirection.Up, stopPropagation);
        }

        public static bool ScrolledUp() {
            return Scrolled(ScrollDirection.Up);
        }
    }
}
