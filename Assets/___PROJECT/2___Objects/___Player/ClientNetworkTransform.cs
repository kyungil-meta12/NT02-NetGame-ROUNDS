using Unity.Netcode.Components;
using UnityEngine;

[DisallowMultipleComponent]
/// <summary>
/// 소유권을 가진 클라이언트가 자신의 위치/회전/스케일을 직접 서버로 전송할 수 있게 해주는 컴포넌트
/// </summary>
public class ClientNetworkTransform : NetworkTransform
{
    /// <summary>
    /// 이 함수가 false를 반환하면 서버 권한이 아닌 클라이언트(소유자) 권한으로 동작.
    /// </summary>
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
