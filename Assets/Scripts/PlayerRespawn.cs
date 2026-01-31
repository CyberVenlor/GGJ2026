using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerRespawn : MonoBehaviour
{
    [Header("References")]
    public Health health;
    public PlayerController playerController;
    public Rigidbody rb;
    public Image fadeImage;

    [Header("Timing")]
    public float deathFadeDuration = 2f;
    public float respawnFadeDuration = 0.35f;

    [Header("Visuals")]
    public Color fadeColor = Color.black;

    private bool _isRespawning;
    private bool _cachedKinematic;

    private void Awake()
    {
        if (health == null)
        {
            health = GetComponentInParent<Health>();
        }

        if (playerController == null)
        {
            playerController = GetComponentInParent<PlayerController>();
        }

        if (rb == null)
        {
            rb = GetComponentInParent<Rigidbody>();
        }

        if (fadeImage == null)
        {
            fadeImage = CreateFadeOverlay();
        }

        SavePoint.SetDefaultRespawnPosition(transform.position);
    }

    private void Update()
    {
        if (_isRespawning || health == null)
        {
            return;
        }

        if (health.CurrentHealth <= 0)
        {
            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        _isRespawning = true;

        if (playerController != null)
        {
            playerController.enabled = false;
        }

        if (rb != null)
        {
            _cachedKinematic = rb.isKinematic;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        yield return FadeTo(1f, deathFadeDuration);

        transform.position = SavePoint.GetRespawnPosition();

        if (health != null)
        {
            health.CurrentHealth = health.MaxHealth;
        }

        if (rb != null)
        {
            rb.isKinematic = _cachedKinematic;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (playerController != null)
        {
            playerController.enabled = true;
        }

        yield return FadeTo(0f, respawnFadeDuration);

        _isRespawning = false;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadeImage == null)
        {
            yield break;
        }

        Color color = fadeColor;
        color.a = fadeImage.color.a;
        fadeImage.color = color;

        float startAlpha = fadeImage.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            color.a = alpha;
            fadeImage.color = color;
            yield return null;
        }

        color.a = targetAlpha;
        fadeImage.color = color;
    }

    private Image CreateFadeOverlay()
    {
        GameObject canvasObject = new GameObject("DeathFadeCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        canvasObject.AddComponent<CanvasScaler>();

        GameObject imageObject = new GameObject("DeathFade");
        imageObject.transform.SetParent(canvasObject.transform, false);

        Image image = imageObject.AddComponent<Image>();
        image.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return image;
    }
}
