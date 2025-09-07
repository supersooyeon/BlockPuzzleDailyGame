// // ©2015 - 2025 Candy Smith
// // All rights reserved
// // Redistribution of this software is strictly not allowed.
// // Copy of this software can be obtained from unity asset store only.
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// // THE SOFTWARE.

using BlockPuzzleGameToolkit.Scripts.Enums;
using BlockPuzzleGameToolkit.Scripts.Gameplay;
using BlockPuzzleGameToolkit.Scripts.Popups;
using BlockPuzzleGameToolkit.Scripts.System;
using UnityEngine;

namespace BlockPuzzleGameToolkit.Scripts.GUI
{
    public class UIManager : MonoBehaviour
    {
        public CustomButton pauseButton;

        private void Awake()
        {
            pauseButton.onClick.AddListener(OnPauseButtonClicked);
        }

        private void OnPauseButtonClicked()
        {
            var fieldManager = FindObjectOfType<BlockPuzzleGameToolkit.Scripts.Gameplay.FieldManager>();
            if (fieldManager != null && fieldManager.IsAnimationPlaying)
                return;
            EventManager.GameStatus = EGameState.Pause;
            if (StateManager.instance.CurrentState == EScreenStates.MainMenu)
            {
                MenuManager.instance.ShowPopup<Popups.Settings>();
            }
            else
            {
                MenuManager.instance.ShowPopup("Popups/SettingsGame");
            }
        }
    }
}