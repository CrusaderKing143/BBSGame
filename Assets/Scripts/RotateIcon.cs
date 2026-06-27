using System.Collections;
using UnityEngine;

public class RotateIcon : MonoBehaviour
{
    [SerializeField] private float rotationDuration = 2f;
    [SerializeField] private float rotationSpeed = 360f;

    [SerializeField] private GameObject gameTitlePanel;
    [SerializeField] private GameObject loginPanel;

    private void Start()
    {
        StartCoroutine(RotateForDuration());
    }

    private IEnumerator RotateForDuration()
    {
        float elapsed = 0f;

        while (elapsed < rotationDuration)
        {
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime, Space.Self);
            elapsed += Time.deltaTime;
            yield return null;
        }

        OnRotationFinished();
    }

    private void OnRotationFinished()
    {
        if (gameTitlePanel != null)
        {
            gameTitlePanel.SetActive(false);
        }

        if (loginPanel != null)
        {
            loginPanel.SetActive(true);
        }
    }
}
