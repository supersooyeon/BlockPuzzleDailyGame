# Play In-App Update 사용 가이드

## 개요
이 패키지는 Google Play In-App Update API를 사용하여 앱 내에서 자동 업데이트를 처리하는 Unity 스크립트입니다.

## 주요 기능
- **즉시 업데이트 (Immediate Update)**: 사용자가 앱을 사용할 수 없게 하고 업데이트를 강제로 진행
- **유연한 업데이트 (Flexible Update)**: 백그라운드에서 업데이트를 진행하고 사용자는 계속 앱을 사용 가능
- **자동 업데이트 감지**: 앱 시작 시 자동으로 업데이트 가능 여부 확인
- **진행률 모니터링**: 업데이트 진행 상황을 실시간으로 추적
- **이벤트 기반 시스템**: 업데이트 상태 변화를 이벤트로 알림

## 설치 및 설정

### 1. Google Play In-App Update 패키지 설치
Unity Package Manager에서 다음 패키지를 설치하세요:
- `com.google.play.appupdate`

### 2. 스크립트 추가
1. `InAppUpdateManager.cs`를 프로젝트에 추가
2. 씬의 빈 GameObject에 `InAppUpdateManager` 컴포넌트 추가

### 3. Android 설정
`Assets/Plugins/Android/AndroidManifest.xml`에 다음 권한이 있는지 확인:
```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
```

## 사용법

### 기본 사용법
```csharp
// InAppUpdateManager 참조 가져오기
InAppUpdateManager updateManager = FindObjectOfType<InAppUpdateManager>();

// 업데이트 확인 시작
updateManager.StartUpdateCheck();

// 즉시 업데이트 시작
updateManager.StartImmediateUpdate();

// 유연한 업데이트 시작
updateManager.StartFlexibleUpdate();
```

### 이벤트 구독
```csharp
// 업데이트 사용 가능 시
updateManager.OnUpdateAvailable += (updateInfo) => {
    Debug.Log("업데이트가 사용 가능합니다!");
};

// 업데이트 진행률
updateManager.OnUpdateProgress += (progress) => {
    Debug.Log($"진행률: {progress:P0}");
};

// 업데이트 완료
updateManager.OnUpdateCompleted += () => {
    Debug.Log("업데이트가 완료되었습니다!");
};

// 업데이트 실패
updateManager.OnUpdateFailed += (error) => {
    Debug.Log($"업데이트 실패: {error}");
};
```

### 자동 업데이트 설정
Inspector에서 다음 옵션을 설정할 수 있습니다:
- `Enable Immediate Update`: 즉시 업데이트 활성화 여부
- `Enable Flexible Update`: 유연한 업데이트 활성화 여부

## 업데이트 타입

### 즉시 업데이트 (Immediate Update)
- 사용자가 앱을 사용할 수 없음
- 업데이트가 완료될 때까지 대기
- 중요한 보안 업데이트나 필수 업데이트에 적합

### 유연한 업데이트 (Flexible Update)
- 백그라운드에서 업데이트 진행
- 사용자는 계속 앱을 사용 가능
- 업데이트 완료 후 앱 재시작 시 적용
- 일반적인 기능 업데이트에 적합

## 예제 UI 구현
`InAppUpdateExample.cs`를 참고하여 UI를 구현할 수 있습니다:

1. 업데이트 확인 버튼
2. 즉시 업데이트 버튼
3. 유연한 업데이트 버튼
4. 진행률 슬라이더
5. 상태 텍스트

## 주의사항

### 테스트 환경
- 실제 기기에서만 테스트 가능 (에뮬레이터에서는 작동하지 않음)
- Google Play Console에서 테스트 트랙 설정 필요
- 테스트용 APK를 업로드하여 테스트

### 제한사항
- Android 5.0 (API 레벨 21) 이상에서만 지원
- Google Play Store가 설치된 기기에서만 작동
- 인터넷 연결이 필요

### 에러 처리
- 네트워크 오류
- 저장 공간 부족
- 업데이트 권한 없음
- Google Play Store 연결 실패

## 문제 해결

### 일반적인 문제
1. **업데이트가 감지되지 않음**
   - Google Play Console에서 테스트 트랙 설정 확인
   - 테스트용 APK가 올바르게 업로드되었는지 확인

2. **업데이트 실패**
   - 기기의 저장 공간 확인
   - 인터넷 연결 상태 확인
   - Google Play Store 업데이트 확인

3. **권한 오류**
   - AndroidManifest.xml에 필요한 권한 추가
   - 런타임 권한 요청 구현

## 추가 리소스
- [Google Play In-App Update 공식 문서](https://developer.android.com/guide/playcore/in-app-updates)
- [Unity Google Play Games 플러그인](https://github.com/playgameservices/play-games-plugin-for-unity)
- [Google Play Console 도움말](https://support.google.com/googleplay/android-developer)

## 라이선스
이 스크립트는 MIT 라이선스 하에 제공됩니다.




