using UnityEngine;
using UnityEngine.UI;

public class BallSpinner : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float switchInterval = 0.4f;
    [SerializeField] private Image lightImage;
    [SerializeField] private Image darkImage;

    private float elapsed;
    private bool showingLight;

    private void Awake()
    {
        SetImageState(true);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed < switchInterval)
        {
            return;
        }

        elapsed %= switchInterval;
        SetImageState(!showingLight);
    }

    private void SetImageState(bool showLight)
    {
        showingLight = showLight;

        if (lightImage != null)
        {
            lightImage.enabled = showLight;
        }

        if (darkImage != null)
        {
            darkImage.enabled = !showLight;
        }
    }
}
