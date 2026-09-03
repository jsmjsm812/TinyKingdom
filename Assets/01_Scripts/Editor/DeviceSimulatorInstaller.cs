#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace WitchHour.EditorTools
{
    /// <summary>
    /// Device Simulator 패키지(에디터에서 실제 폰 화면 모양 — 노치 포함 — 을 미리보기)를 설치한다.
    /// 버전을 하드코딩하지 않고 Package Manager 레지스트리가 이 에디터 버전과 맞는 버전을
    /// 알아서 고르게 한다.
    /// </summary>
    public static class DeviceSimulatorInstaller
    {
        private static AddRequest _request;

        [MenuItem("WitchHour/Install Device Simulator Package")]
        public static void Install()
        {
            _request = Client.Add("com.unity.device-simulator");
            EditorApplication.update += Progress;
            Debug.Log("[DeviceSimulatorInstaller] 설치 요청 보냄, 잠시 기다려주세요...");
        }

        private static void Progress()
        {
            if (_request == null || !_request.IsCompleted) return;

            EditorApplication.update -= Progress;

            if (_request.Status == StatusCode.Success)
                Debug.Log($"[DeviceSimulatorInstaller] 설치 완료: {_request.Result.packageId}. " +
                          "Window > Device Simulator 메뉴에서 열 수 있습니다.");
            else
                Debug.LogError($"[DeviceSimulatorInstaller] 설치 실패: {_request.Error.message}");
        }
    }
}
#endif
