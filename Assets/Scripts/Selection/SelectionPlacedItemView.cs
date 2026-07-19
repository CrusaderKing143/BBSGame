using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class SelectionPlacedItemView : MonoBehaviour,
    IPointerDownHandler,
    IBeginDragHandler,
    IDragHandler
{
    [SerializeField] private Image itemImage;

    private SelectionPanelController controller;
    private bool selected;

    public RectTransform RectTransform => transform as RectTransform;
    public SelectionCategoryType CategoryType { get; private set; }
    public string ItemId { get; private set; }
    public bool IsSelected => selected;

    public void Configure(
        SelectionPanelController owner,
        SelectionCategoryType categoryType,
        string itemId,
        Sprite sprite,
        float displayScale,
        bool selected)
    {
        ResolveReferences();
        controller = owner;
        CategoryType = categoryType;
        ItemId = itemId;

        if (itemImage != null)
        {
            itemImage.sprite = sprite;
            itemImage.enabled = sprite != null;
            itemImage.preserveAspect = true;
            itemImage.raycastTarget = true;
            if (sprite != null)
            {
                itemImage.SetNativeSize();
            }
        }

        RectTransform rectTransform = RectTransform;
        if (rectTransform != null)
        {
            float effectiveScale = displayScale > 0f ? displayScale : 1f;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.localScale = Vector3.one * effectiveScale;
        }

        SetSelected(selected);
        gameObject.SetActive(true);
    }

    public void SetSelected(bool selected)
    {
        this.selected = selected;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        controller?.HandlePlacedItemPointerDown(this, eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        controller?.HandlePlacedItemBeginDrag(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        controller?.HandlePlacedItemDrag(this, eventData);
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (itemImage == null)
        {
            itemImage = GetComponent<Image>();
        }

    }
}
