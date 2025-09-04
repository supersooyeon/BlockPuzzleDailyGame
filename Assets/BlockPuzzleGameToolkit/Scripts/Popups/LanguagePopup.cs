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

using System;
using System.Collections.Generic;
using System.Linq;
using BlockPuzzleGameToolkit.Scripts.GUI;
using BlockPuzzleGameToolkit.Scripts.Localization;
using UnityEngine;

namespace BlockPuzzleGameToolkit.Scripts.Popups
{
    [Serializable]
    public class LanguageButtonInfo
    {
        public SystemLanguage language;
        public string displayName;
        public CustomButton button;
        public GameObject checkIcon;
    }

    public class LanguagePopup : PopupWithCurrencyLabel
    {
        [Header("Language Selection")]
        [SerializeField] private LanguageButtonInfo[] languageButtons;
        
        private SystemLanguage currentSelectedLanguage;

        protected override void Awake()
        {
            base.Awake();
            
            // 현재 언어를 가져옴 (시스템 언어 우선, 없으면 English)
            currentSelectedLanguage = GetDefaultLanguage();
            
            // 닫기 버튼 설정 (부모 클래스의 closeButton 사용)
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(ClosePopup);
            }
        }

        private void OnEnable()
        {
            // 팝업이 활성화될 때마다 언어 버튼들을 초기화
            InitializeLanguageButtons();
        }

        private SystemLanguage GetDefaultLanguage()
        {
            // 시스템 언어 먼저 확인
            SystemLanguage systemLanguage = LocalizationManager.GetSystemLanguage();
            
            // 지원하는 언어인지 확인
            if (languageButtons != null && languageButtons.Any(lb => lb.language == systemLanguage))
            {
                // 해당 언어 파일이 있는지 확인
                var languageFile = Resources.Load<TextAsset>($"Localization/{systemLanguage}");
                if (languageFile != null)
                {
                    return systemLanguage;
                }
            }
            
            // 기본값으로 English 반환
            return SystemLanguage.English;
        }

        private void InitializeLanguageButtons()
        {
            if (languageButtons == null || languageButtons.Length == 0)
            {
                Debug.LogError("Language buttons are not assigned in the inspector!");
                return;
            }

            // 현재 선택된 언어 업데이트 (최신 상태 반영)
            currentSelectedLanguage = LocalizationManager.GetCurrentLanguage();
            Debug.Log($"Current selected language during initialization: {currentSelectedLanguage}");

            // 각 언어 버튼 초기화
            foreach (var languageButtonInfo in languageButtons)
            {
                if (languageButtonInfo.button == null)
                {
                    Debug.LogError($"Language button for {languageButtonInfo.language} is not assigned!");
                    continue;
                }

                // 언어 파일이 존재하는지 확인 (로그용)
                var languageFile = Resources.Load<TextAsset>($"Localization/{languageButtonInfo.language}");
                bool hasLanguageFile = languageFile != null || languageButtonInfo.language == SystemLanguage.English;

                // 버튼 초기화 (모든 언어 동일하게 처리)
                InitializeLanguageButton(languageButtonInfo, hasLanguageFile);
                
                // 모든 버튼 활성화
                languageButtonInfo.button.gameObject.SetActive(true);
            }

            // 현재 선택된 언어 업데이트
            UpdateSelectedLanguage(currentSelectedLanguage);
        }

        private void InitializeLanguageButton(LanguageButtonInfo buttonInfo, bool hasLanguageFile)
        {
            // 언어 텍스트 설정
            var languageText = buttonInfo.button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (languageText != null)
            {
                // 모든 언어에 동일하게 displayName 사용
                languageText.text = buttonInfo.displayName;
            }

            // 클릭 이벤트 설정 - 언어 파일 유무에 상관없이 동일하게 처리
            buttonInfo.button.onClick.RemoveAllListeners();
            buttonInfo.button.onClick.AddListener(() => OnLanguageSelected(buttonInfo.language));
        }

        private void OnLanguageSelected(SystemLanguage selectedLanguage)
        {
            Debug.Log($"Language selected: {selectedLanguage}");
            Debug.Log($"Language as string: {selectedLanguage.ToString()}");
            
            currentSelectedLanguage = selectedLanguage;
            
            // 언어 설정을 즉시 PlayerPrefs에 저장
            PlayerPrefs.SetString("SelectedLanguage", selectedLanguage.ToString());
            PlayerPrefs.Save();
            Debug.Log($"Saved language to PlayerPrefs: {selectedLanguage}");
            
            // 언어 변경 적용
            Debug.Log("Calling LocalizationManager.LoadLanguage...");
            LocalizationManager.LoadLanguage(selectedLanguage);
            
            // 체크 아이콘 업데이트
            Debug.Log("Updating selected language UI...");
            UpdateSelectedLanguage(selectedLanguage);
            
            // 모든 LocalizedTextMeshProUGUI 컴포넌트 업데이트
            Debug.Log("Updating all localized texts...");
            UpdateAllLocalizedTexts();
            
            Debug.Log("Language selection completed.");
        }



        private void UpdateSelectedLanguage(SystemLanguage selectedLanguage)
        {
            Debug.Log($"Updating selected language to: {selectedLanguage}");
            
            foreach (var languageButtonInfo in languageButtons)
            {
                if (languageButtonInfo.button != null)
                {
                    bool isSelected = languageButtonInfo.language == selectedLanguage;
                    bool hasLanguageFile = HasLanguageFile(languageButtonInfo.language);
                    
                    Debug.Log($"Button {languageButtonInfo.language}: selected={isSelected}, hasFile={hasLanguageFile}");
                    
                    SetButtonSelected(languageButtonInfo.button, languageButtonInfo.checkIcon, isSelected, hasLanguageFile);
                }
            }
        }

        private bool HasLanguageFile(SystemLanguage language)
        {
            var languageFile = Resources.Load<TextAsset>($"Localization/{language}");
            return languageFile != null || language == SystemLanguage.English;
        }



        private void SetButtonSelected(CustomButton button, GameObject checkIcon, bool selected, bool hasLanguageFile)
        {
            // 체크 아이콘 처리 - 선택된 경우만 체크 아이콘 표시
            if (checkIcon != null)
            {
                checkIcon.SetActive(selected);
                Debug.Log($"Button {button.name}: CheckIcon({checkIcon.name}) set to {selected} (selected={selected})");
            }
            else
            {
                Debug.LogWarning($"CheckIcon not assigned for button {button.name}. Please assign it in the inspector.");
            }
            
            // 선택된 상태에 따라 버튼 색상 변경 - 언어 파일 유무에 상관없이 동일하게 처리
            var colors = button.colors;
            colors.normalColor = selected ? Color.yellow : Color.white;
            button.colors = colors;
        }

        private void UpdateAllLocalizedTexts()
        {
            // 씬의 모든 LocalizedTextMeshProUGUI 컴포넌트 찾아서 업데이트
            var localizedTexts = FindObjectsOfType<LocalizedTextMeshProUGUI>();
            foreach (var localizedText in localizedTexts)
            {
                localizedText.UpdateText();
            }
        }

        private void ClosePopup()
        {
            Close();
        }

        public override void Close()
        {
            base.Close();
        }
    }


}

