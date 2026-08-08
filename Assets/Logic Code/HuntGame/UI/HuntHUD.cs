using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HuntHUD : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text matchResultText;
    [SerializeField] private TMP_Text noiseText;
    private NetworkCharacterBase localCharacter;
    private float hideNoiseAt;

    private void OnEnable() => NoiseSystem.NoiseHeard += OnNoiseHeard;
    private void OnDisable() => NoiseSystem.NoiseHeard -= OnNoiseHeard;

    private void Update()
    {
        if (localCharacter == null)
        {
            foreach (NetworkCharacterBase candidate in FindObjectsByType<NetworkCharacterBase>(FindObjectsSortMode.None))
                if (candidate.IsOwner) { localCharacter = candidate; break; }
        }

        if (localCharacter != null)
        {
            healthSlider.maxValue = localCharacter.MaxHealth.Value;
            healthSlider.value = localCharacter.Health.Value;
            healthText.text = $"HP {localCharacter.Health.Value}/{localCharacter.MaxHealth.Value}";
            roleText.text = localCharacter.Role.Value.ToString();
        }

        HuntMatchManager match = HuntMatchManager.Instance;
        if (match != null)
        {
            int total = Mathf.CeilToInt(match.TimeRemaining.Value);
            timerText.text = $"{total / 60:00}:{total % 60:00}";
            matchResultText.gameObject.SetActive(!match.MatchRunning.Value && match.WinReason.Value != HuntWinReason.None);
            if (matchResultText.gameObject.activeSelf)
                matchResultText.text = $"{match.WinningTeam.Value} thắng\n{match.WinReason.Value}";
        }
        if (noiseText != null && Time.time >= hideNoiseAt) noiseText.gameObject.SetActive(false);
    }

    private void OnNoiseHeard(Vector3 position, float radius, NoiseKind kind, HuntTeam sourceTeam)
    {
        if (localCharacter == null || sourceTeam == localCharacter.Team.Value || noiseText == null) return;
        float distance = Vector3.Distance(localCharacter.transform.position, position);
        if (distance > radius) return;
        Vector3 localDirection = localCharacter.transform.InverseTransformDirection((position - localCharacter.transform.position).normalized);
        string direction = Mathf.Abs(localDirection.x) > Mathf.Abs(localDirection.z)
            ? (localDirection.x > 0 ? "bên phải" : "bên trái")
            : (localDirection.z > 0 ? "phía trước" : "phía sau");
        noiseText.text = $"Nghe thấy {kind} ở {direction}";
        noiseText.gameObject.SetActive(true);
        hideNoiseAt = Time.time + 3f;
    }
}
