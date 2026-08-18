using UnityEngine;

public enum RouletteEffectType
{
    Success,
    GreatSuccess,
    ExtraSpin,
    SelfBuff,
    Heal,
    Failure,
    SelfDebuff,
    Special
}

[CreateAssetMenu(fileName = "New Roulette Effect", menuName = "Game Data/Roulette Effect")]
public class RouletteEffectData : ScriptableObject
{
    [Tooltip("룰렛 칸에 표시하고 효과 데이터를 찾을 때 사용할 색상입니다.")]
    [SerializeField] private Color color = Color.white;
    [Tooltip("이 색상에 당첨됐을 때 적용할 룰렛 결과입니다.")]
    [SerializeField] private RouletteEffectType effect;
    [Tooltip("룰렛 결과 수치의 최소값(X)과 최대값(Y)입니다.")]
    [SerializeField] private Vector2Int statRange;

    public Color Color => color;
    public RouletteEffectType Effect => effect;
    public Vector2Int StatRange => statRange;

    public int RollValue()
    {
        int min = Mathf.Min(statRange.x, statRange.y);
        int max = Mathf.Max(statRange.x, statRange.y);
        return Random.Range(min, max + 1);
    }
}
