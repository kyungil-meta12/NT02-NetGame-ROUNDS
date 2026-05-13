using UnityEngine;

/// <summary>
/// 각 플레이어가 가지는 스탯 데이터
/// </summary>
public struct StatData
{
    // 예시
    // 추가 이동 속도, 추가 연사 딜레이 감소 등...
    // 이 값들은 카드 선택 시 변경할 수 있음 // 각 능력치 마다 메서드를 추가하여 변경하는 코드를 작성
    public float moveSpeedMultiply;
    // float fireIntervalMultiply;
}

/// <summary>
/// 플레이어의 외모 스프라이트 인덱스
/// </summary>
public struct AppearanceData
{
    public int bodyIndex;
    public int faceIndex;
    public Color particleColor;
}

/// <summary>
/// 플레이어 관련 데이터를 관리하는 싱글톤 모듈 // 씬 전환 시 인스턴스 유지됨
/// </summary>
public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Inst;

    /// <summary>
    /// 나의 스탯
    /// </summary>
    public StatData Stat = new();
    public AppearanceData Appearance = new();
    private bool appearanceCreated = false;

    void Awake()
    {
        if(Inst && Inst != this)
        {
            DestroyImmediate(this);
            return;
        }
        Inst = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 플레이어 매니저 객체 및 인스턴스 삭제
    /// </summary>
    public void Destroy()
    {
        Inst = null;
        Destroy(gameObject);
    }

    /// <summary>
    /// 플레이어의 외모 데이터를 저장한다.
    /// 초기에 한 번만 설정되고, 이후의 호출은 무시된다.
    /// </summary>
    /// <param name="bodyIndex"></param>
    /// <param name="faceIndex"></param>
    public void SaveAppearance(int bodyIndex, int faceIndex, Sprite bodySprite)
    {
        if(appearanceCreated)
        {
            return;
        }
        Appearance.bodyIndex = bodyIndex;
        Appearance.faceIndex = faceIndex;
        Appearance.particleColor = GetBodyColor(bodySprite);
        appearanceCreated = true;
    }

    private Color GetBodyColor(Sprite sprite)
    {
        var texture = sprite.texture;
        int x = (texture.width / 2) - 5;
        int y = (texture.height / 2) - 5;

        // 중앙 10 x 10 픽셀을 샘플링하여 PlayerDeath 파티클 생성 시 해당 색상으로 설정
        Color[] pixels = sprite.texture.GetPixels(x, y, 10, 10);
        Color sumColor = new Color(0, 0, 0, 0);

        foreach (Color pixel in pixels)
        {
            sumColor += pixel;
        }

        float totalPixels = pixels.Length;
        Color avgColor = sumColor / totalPixels;

        return avgColor;
    }
}