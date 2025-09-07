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
using System.Text.RegularExpressions;
using BlockPuzzleGameToolkit.Scripts.Settings;
using BlockPuzzleGameToolkit.Scripts.System;
using UnityEngine;

namespace BlockPuzzleGameToolkit.Scripts.Localization
{
    public class LocalizationManager : SingletonBehaviour<LocalizationManager>
    {
        private static DebugSettings _debugSettings;
        public static Dictionary<string, string> _dic;
        private static SystemLanguage _currentLanguage;

        public override void Awake()
        {
            InitializeLocalization();
        }

        public static void InitializeLocalization()
        {
            _debugSettings = Resources.Load("Settings/DebugSettings") as DebugSettings;
            
            // SystemLanguage enum 값들 디버깅
            Debug.Log($"SystemLanguage.Korean.ToString() = '{SystemLanguage.Korean.ToString()}'");
            Debug.Log($"SystemLanguage.English.ToString() = '{SystemLanguage.English.ToString()}'");
            
            LoadLanguage(GetSystemLanguage());
        }

        public static void LoadLanguage(SystemLanguage language)
        {
            Debug.Log($"LoadLanguage called with: {language}");
            _currentLanguage = language;
            var txt = Resources.Load<TextAsset>($"Localization/{language}");
            Debug.Log($"Trying to load: Localization/{language}, Result: {(txt != null ? "Success" : "Failed")}");
            
            if (txt == null)
            {
                Debug.LogWarning($"Localization file for {language} not found. Falling back to English.");
                txt = Resources.Load<TextAsset>("Localization/English");
                _currentLanguage = SystemLanguage.English;
            }

            _dic = new Dictionary<string, string>();
            var lines = txt.text.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            Debug.Log($"Loaded {lines.Length} lines from {_currentLanguage} localization file");
            
            foreach (var inp_ln in lines)
            {
                var l = inp_ln.Split(new[] { ':' }, 2);
                if (l.Length == 2)
                {
                    var key = l[0].Trim();
                    var text = l[1].Trim();
                    _dic[key] = text;
                }
            }
            
            Debug.Log($"Dictionary loaded with {_dic.Count} entries. Sample: SETTINGS = {(_dic.ContainsKey("SETTINGS") ? _dic["SETTINGS"] : "NOT FOUND")}");
        }

        public static SystemLanguage GetSystemLanguage()
        {
            if (_debugSettings == null)
            {
                _debugSettings = Resources.Load("Settings/DebugSettings") as DebugSettings;
            }

            // 에디터에서는 테스트 언어 사용
            if (Application.isEditor)
            {
                return _debugSettings.TestLanguage;
            }

            // 사용자가 선택한 언어가 있는지 확인
            if (PlayerPrefs.HasKey("SelectedLanguage"))
            {
                var savedLanguage = PlayerPrefs.GetString("SelectedLanguage");
                Debug.Log($"Found saved language in PlayerPrefs: {savedLanguage}");
                if (Enum.TryParse<SystemLanguage>(savedLanguage, out var language))
                {
                    Debug.Log($"Successfully parsed saved language: {language}");
                    return language;
                }
                else
                {
                    Debug.LogWarning($"Failed to parse saved language: {savedLanguage}");
                }
            }
            else
            {
                Debug.Log("No saved language found in PlayerPrefs");
            }

            return Application.systemLanguage;
        }

        public static string GetText(string key, string defaultText)
        {
            // 딕셔너리가 초기화되지 않았거나 비어있으면 현재 언어로 로드
            if (_dic == null || _dic.Count == 0)
            {
                // 현재 언어가 설정되지 않았으면 시스템 언어로 초기화
                if (_currentLanguage == default(SystemLanguage))
                {
                    var systemLanguage = GetSystemLanguage();
                    Debug.Log($"Current language not set, initializing with system language: {systemLanguage}");
                    LoadLanguage(systemLanguage);
                }
                else
                {
                    Debug.Log($"Reloading current language: {_currentLanguage}");
                    LoadLanguage(_currentLanguage);
                }
            }

            // 키가 없으면 현재 언어로 다시 로드 시도
            if (!_dic.ContainsKey(key))
            {
                Debug.Log($"Key '{key}' not found, reloading language: {_currentLanguage}");
                LoadLanguage(_currentLanguage);
            }

            if (_dic.TryGetValue(key, out var localizedText) && !string.IsNullOrEmpty(localizedText))
            {
                var processedText = PlaceholderManager.ReplacePlaceholders(localizedText);
                // 이스케이프 문자 처리
                processedText = ProcessEscapeCharacters(processedText);
                return processedText;
            }

            var processedDefault = PlaceholderManager.ReplacePlaceholders(defaultText);
            // 이스케이프 문자 처리
            processedDefault = ProcessEscapeCharacters(processedDefault);
            return processedDefault;
        }

        public static SystemLanguage GetCurrentLanguage()
        {
            Debug.Log($"GetCurrentLanguage called, returning: {_currentLanguage}");
            return _currentLanguage;
        }

        /// <summary>
        /// 이스케이프 문자들을 실제 문자로 변환하고 색상 태그를 처리합니다.
        /// </summary>
        /// <param name="text">처리할 텍스트</param>
        /// <returns>이스케이프 문자가 변환되고 색상 태그가 적용된 텍스트</returns>
        private static string ProcessEscapeCharacters(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // 이스케이프 문자 처리
            text = text
                .Replace("\\n", "\n")      // 줄바꿈
                .Replace("\\r", "\r")      // 캐리지 리턴
                .Replace("\\t", "\t")      // 탭
                .Replace("\\\"", "\"")     // 큰따옴표
                .Replace("\\'", "'")       // 작은따옴표
                .Replace("\\\\", "\\");    // 백슬래시 (마지막에 처리)

            // 색상 태그 처리
            text = ProcessColorTags(text);

            return text;
        }

        /// <summary>
        /// 간단한 색상 태그를 TextMeshPro Rich Text 태그로 변환합니다.
        /// </summary>
        /// <param name="text">처리할 텍스트</param>
        /// <returns>색상 태그가 적용된 텍스트</returns>
        private static string ProcessColorTags(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var originalText = text;
            
            // 미리 정의된 색상 태그 처리 (먼저 처리)
            text = ProcessPredefinedColorTags(text);

            // 기본 색상 태그 처리: [red]텍스트[/red] -> <color=red>텍스트</color>
            text = Regex.Replace(text, @"\[(\w+)\](.*?)\[/\1\]", "<color=$1>$2</color>");

            // 헥스 색상 태그 처리: [#FF0000]텍스트[/#FF0000] -> <color=#FF0000>텍스트</color>
            text = Regex.Replace(text, @"\[(#[0-9A-Fa-f]{6})\](.*?)\[/\1\]", "<color=$1>$2</color>");

            // 변환 결과 디버깅
            if (originalText != text)
            {
                Debug.Log($"Color tag conversion: '{originalText}' -> '{text}'");
            }

            return text;
        }

        /// <summary>
        /// 미리 정의된 색상 태그들을 처리합니다.
        /// </summary>
        /// <param name="text">처리할 텍스트</param>
        /// <returns>미리 정의된 색상이 적용된 텍스트</returns>
        private static string ProcessPredefinedColorTags(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // 미리 정의된 색상들 (한국어 + 영어)
            var colorMappings = new Dictionary<string, string>
            {
                // 한국어 태그
                { "강조", "#FF6B6B" },      // 빨간색 강조
                { "중요", "#FF4757" },      // 진한 빨간색
                { "성공", "#2ED573" },      // 초록색
                { "경고", "#FFA502" },      // 주황색
                { "정보", "#3742FA" },      // 파란색
                { "보상", "#FFD700" },      // 금색
                { "코인", "#F1C40F" },      // 노란색
                { "레벨", "#9B59B6" },      // 보라색
                { "점수", "#E74C3C" },      // 빨간색
                { "시간", "#3498DB" },      // 파란색
                { "생명", "#E91E63" },      // 핑크색
                { "힌트", "#17A2B8" },      // 시안색
                { "특별", "#6F42C1" },      // 진한 보라색
                { "무료", "#28A745" },      // 진한 초록색
                { "프리미엄", "#FD7E14" },  // 오렌지색
                { "새로운", "#20C997" },    // 터쿠아즈색
                { "완료", "#6C757D" },      // 회색
                { "실패", "#DC3545" },      // 진한 빨간색
                { "도전", "#FF6B35" },      // 주황빨간색
                { "보너스", "#FFB700" },    // 황금색
                
                // 영어 태그
                { "highlight", "#FF6B6B" }, // 빨간색 강조
                { "important", "#FF4757" }, // 진한 빨간색
                { "success", "#2ED573" },   // 초록색
                { "warning", "#FFA502" },   // 주황색
                { "info", "#3742FA" },      // 파란색
                { "reward", "#FFD700" },    // 금색
                { "coin", "#F1C40F" },      // 노란색
                { "level", "#9B59B6" },     // 보라색
                { "score", "#E74C3C" },     // 빨간색
                { "time", "#3498DB" },      // 파란색
                { "life", "#E91E63" },      // 핑크색
                { "hint", "#17A2B8" },      // 시안색
                { "special", "#6F42C1" },   // 진한 보라색
                { "free", "#28A745" },      // 진한 초록색
                { "premium", "#FD7E14" },   // 오렌지색
                { "new", "#20C997" },       // 터쿠아즈색
                { "complete", "#6C757D" },  // 회색
                { "failed", "#DC3545" },    // 진한 빨간색
                { "challenge", "#FF6B35" }, // 주황빨간색
                { "bonus", "#FFB700" }      // 황금색
            };

            foreach (var mapping in colorMappings)
            {
                var pattern = $@"\[{mapping.Key}\](.*?)\[/{mapping.Key}\]";
                text = Regex.Replace(text, pattern, $"<color={mapping.Value}>$1</color>");
            }

            return text;
        }
    }
}