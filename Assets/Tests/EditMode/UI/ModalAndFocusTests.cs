using System.Collections.Generic;
using NUnit.Framework;
using ProjectTwelve.UI;
using ProjectTwelve.UI.Controls;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTwelve.Tests.UI
{
    /// <summary>
    /// Modal stack input-blocking and focus-trapping: a blocking screen suppresses gameplay input while
    /// open, and a focus trap refuses to hand focus to controls outside the modal (or to disabled controls).
    /// </summary>
    public sealed class ModalAndFocusTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }

            spawned.Clear();
        }

        [Test]
        public void ModalStack_BlocksGameplayInput_WhileBlockingScreenOpen()
        {
            ModalStack stack = new ModalStack();
            Assert.IsFalse(stack.BlocksGameplayInput);

            FakeScreen blocking = new FakeScreen { BlocksGameplayInput = true, Layer = UiLayer.Modal };
            stack.Push(blocking);

            Assert.IsTrue(stack.BlocksGameplayInput, "Gameplay input must be suppressed while a modal is open.");
            Assert.AreSame(blocking, stack.Top);
            Assert.AreEqual(1, blocking.ShowCount);

            stack.Pop();
            Assert.IsFalse(stack.BlocksGameplayInput, "Closing the modal restores gameplay input.");
            Assert.AreEqual(1, blocking.HideCount);
        }

        [Test]
        public void ModalStack_NonBlockingScreen_DoesNotSuppressInput()
        {
            ModalStack stack = new ModalStack();
            stack.Push(new FakeScreen { BlocksGameplayInput = false });
            Assert.IsFalse(stack.BlocksGameplayInput);
        }

        [Test]
        public void FocusTrap_RejectsFocusOutsideTrapRoot()
        {
            GameObject trapRoot = new GameObject("Trap", typeof(RectTransform));
            spawned.Add(trapRoot);
            GameObject inside = new GameObject("Inside", typeof(RectTransform));
            inside.transform.SetParent(trapRoot.transform, false);
            GameObject outside = new GameObject("Outside", typeof(RectTransform));
            spawned.Add(outside);

            UiFocusController focus = new UiFocusController();
            focus.PushTrap(trapRoot.GetComponent<RectTransform>());

            Assert.IsTrue(focus.RequestFocus(inside), "Focus inside the trap is allowed.");
            Assert.AreSame(inside, focus.Focused);
            Assert.IsFalse(focus.RequestFocus(outside), "Focus outside the trap is rejected.");
            Assert.AreSame(inside, focus.Focused, "Focus stays on the trapped control.");

            focus.PopTrap(trapRoot.GetComponent<RectTransform>());
            Assert.IsTrue(focus.RequestFocus(outside), "After the trap ends, outside focus is allowed again.");
        }

        [Test]
        public void FocusController_RejectsDisabledControls()
        {
            GameObject go = new GameObject("Disabled", typeof(RectTransform), typeof(Image), typeof(Button));
            spawned.Add(go);
            go.GetComponent<Button>().interactable = false;

            UiFocusController focus = new UiFocusController();
            Assert.IsFalse(focus.RequestFocus(go), "A disabled control must not receive focus.");
        }

        [Test]
        public void ModalDialog_ReportsBlockingAndTrapsFocusWhileShown()
        {
            GameObject go = new GameObject("Modal", typeof(RectTransform));
            spawned.Add(go);
            UiModalDialog dialog = go.AddComponent<UiModalDialog>();
            dialog.Build();
            UiFocusController focus = new UiFocusController();
            dialog.SetFocusController(focus);

            Assert.IsTrue(dialog.BlocksGameplayInput);
            Assert.AreEqual(UiLayer.Modal, dialog.Layer);

            dialog.Show();
            Assert.IsTrue(focus.IsTrapped, "Showing the dialog traps focus.");

            dialog.Hide();
            Assert.IsFalse(focus.IsTrapped, "Hiding the dialog releases the trap.");
        }
    }
}
