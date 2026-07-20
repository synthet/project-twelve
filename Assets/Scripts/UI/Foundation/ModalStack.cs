using System;
using System.Collections.Generic;

namespace ProjectTwelve.UI
{
    /// <summary>
    /// Ordered stack of open screens. The topmost dismissible screen is what the cancel action closes,
    /// and <see cref="BlocksGameplayInput"/> answers "should world input be suppressed right now?" so the
    /// input router has a single source of truth instead of each screen guessing.
    /// </summary>
    public sealed class ModalStack
    {
        private readonly List<IUiScreen> stack = new List<IUiScreen>();

        /// <summary>Raised whenever the stack changes (push/pop), so the input router can re-evaluate.</summary>
        public event Action Changed;

        /// <summary>Number of screens currently open.</summary>
        public int Count => stack.Count;

        /// <summary>The topmost open screen, or null when the stack is empty.</summary>
        public IUiScreen Top => stack.Count > 0 ? stack[stack.Count - 1] : null;

        /// <summary>True when any open screen blocks gameplay input.</summary>
        public bool BlocksGameplayInput
        {
            get
            {
                for (int i = 0; i < stack.Count; i++)
                {
                    if (stack[i].BlocksGameplayInput)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>Pushes a screen onto the stack and shows it.</summary>
        public void Push(IUiScreen screen)
        {
            if (screen == null)
            {
                throw new ArgumentNullException(nameof(screen));
            }

            stack.Add(screen);
            screen.Show();
            Changed?.Invoke();
        }

        /// <summary>Hides and removes the topmost screen. Returns the popped screen, or null when empty.</summary>
        public IUiScreen Pop()
        {
            if (stack.Count == 0)
            {
                return null;
            }

            IUiScreen screen = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            screen.Hide();
            Changed?.Invoke();
            return screen;
        }

        /// <summary>Hides and removes a specific screen wherever it sits in the stack.</summary>
        public bool Remove(IUiScreen screen)
        {
            int index = stack.IndexOf(screen);
            if (index < 0)
            {
                return false;
            }

            stack.RemoveAt(index);
            screen.Hide();
            Changed?.Invoke();
            return true;
        }

        /// <summary>True when the given screen is anywhere on the stack.</summary>
        public bool Contains(IUiScreen screen) => stack.Contains(screen);
    }
}
