using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectTwelve.UI
{
    /// <summary>
    /// Single owner of UI focus. Tracks the currently focused control and the most-recently-used input
    /// method so mouse hover and directional (keyboard/controller) focus do not conflict: the focus ring
    /// is only shown for keyboard/controller. Focus-trapping (for modals) is enforced by refusing to move
    /// focus outside the active trap root.
    /// </summary>
    public sealed class UiFocusController
    {
        private GameObject focused;
        private RectTransform trapRoot;

        /// <summary>Raised when the focused control changes. Argument is the newly focused object (may be null).</summary>
        public event Action<GameObject> FocusChanged;

        /// <summary>The input method last used to interact with the UI.</summary>
        public UiInputMethod LastInputMethod { get; private set; } = UiInputMethod.Mouse;

        /// <summary>The currently focused control, or null when nothing is focused.</summary>
        public GameObject Focused => focused;

        /// <summary>True when the focus ring should be drawn (keyboard/controller navigation, not mouse).</summary>
        public bool ShouldShowFocusRing => LastInputMethod != UiInputMethod.Mouse;

        /// <summary>True while focus is trapped inside a modal/dialog root.</summary>
        public bool IsTrapped => trapRoot != null;

        /// <summary>Records the input method the player just used (called by the input router).</summary>
        public void NotifyInputMethod(UiInputMethod method)
        {
            LastInputMethod = method;
        }

        /// <summary>
        /// Requests focus for <paramref name="target"/>. Rejected (returns false) when a focus trap is
        /// active and the target lies outside the trap root, or when the target is a disabled control.
        /// </summary>
        public bool RequestFocus(GameObject target)
        {
            if (target != null && IsDisabled(target))
            {
                return false;
            }

            if (!IsWithinTrap(target))
            {
                return false;
            }

            if (ReferenceEquals(focused, target))
            {
                return true;
            }

            focused = target;
            FocusChanged?.Invoke(focused);
            return true;
        }

        /// <summary>Begins trapping focus inside <paramref name="root"/> (modal open).</summary>
        public void PushTrap(RectTransform root)
        {
            trapRoot = root;
        }

        /// <summary>Ends focus trapping if <paramref name="root"/> is the active trap (modal close).</summary>
        public void PopTrap(RectTransform root)
        {
            if (ReferenceEquals(trapRoot, root))
            {
                trapRoot = null;
            }
        }

        /// <summary>True when <paramref name="target"/> is allowed to receive focus under the current trap.</summary>
        public bool IsWithinTrap(GameObject target)
        {
            if (trapRoot == null)
            {
                return true;
            }

            if (target == null)
            {
                return false;
            }

            Transform t = target.transform;
            while (t != null)
            {
                if (ReferenceEquals(t, trapRoot))
                {
                    return true;
                }

                t = t.parent;
            }

            return false;
        }

        private static bool IsDisabled(GameObject target)
        {
            Selectable selectable = target.GetComponent<Selectable>();
            return selectable != null && !selectable.IsInteractable();
        }
    }
}
