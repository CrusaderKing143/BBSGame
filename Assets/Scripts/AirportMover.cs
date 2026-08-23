using UnityEngine;

public class AirportMover : MonoBehaviour
{
    [Header("位置")]
    [SerializeField] private float startX = 569f;
    [SerializeField] private float showWeiqiX = 293f;
    [SerializeField] private float endX = -898f;

    [Header("移动")]
    [SerializeField, Min(0.01f)] private float moveSpeed = 50f;
    [SerializeField] private GameObject weiqi;

    private RectTransform airportRectTransform;

    private void Awake()
    {
        airportRectTransform = GetComponent<RectTransform>();

        if (weiqi == null)
        {
            Transform weiqiTransform = transform.Find("weiqi");
            if (weiqiTransform != null)
            {
                weiqi = weiqiTransform.gameObject;
            }
        }
    }

    private void OnEnable()
    {
        ResetAirport();
    }

    private void Update()
    {
        Vector2 position = airportRectTransform.anchoredPosition;
        position.x = Mathf.MoveTowards(position.x, endX, moveSpeed * Time.deltaTime);
        airportRectTransform.anchoredPosition = position;

        if (weiqi != null && position.x <= showWeiqiX)
        {
            weiqi.SetActive(true);
        }

        if (position.x <= endX)
        {
            ResetAirport();
        }
    }

    private void ResetAirport()
    {
        Vector2 position = airportRectTransform.anchoredPosition;
        position.x = startX;
        airportRectTransform.anchoredPosition = position;

        if (weiqi != null)
        {
            weiqi.SetActive(false);
        }
    }
}
