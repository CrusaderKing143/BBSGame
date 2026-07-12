using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class SelectionItemView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject selectedMark;

    private UnityAction clickAction;

    public Button Button => button;
    public Image IconImage => iconImage;
    public bool IsSelected => selectedMark != null && selectedMark.activeSelf;

    public void Configure(Sprite icon, bool selected, UnityAction onClick)
    {
        ResolveReferences();
        RemoveClickAction();

        clickAction = onClick;
        if (button != null && clickAction != null)
        {
            button.onClick.AddListener(clickAction);
        }

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.preserveAspect = true;
        }

        SetSelected(selected);
        gameObject.SetActive(true);
    }

    public void SetSelected(bool selected)
    {
        if (selectedMark != null)
        {
            selectedMark.SetActive(selected);
        }
    }

    public void Clear()
    {
        RemoveClickAction();
        SetSelected(false);
        gameObject.SetActive(false);
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnDestroy()
    {
        RemoveClickAction();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (iconImage == null)
        {
            Transform icon = transform.Find("Icon");
            if (icon != null)
            {
                iconImage = icon.GetComponent<Image>();
            }
        }

        if (selectedMark == null)
        {
            Transform mark = transform.Find("SelectedMark");
            if (mark != null)
            {
                selectedMark = mark.gameObject;
            }
        }
    }

    private void RemoveClickAction()
    {
        if (button != null && clickAction != null)
        {
            button.onClick.RemoveListener(clickAction);
        }

        clickAction = null;
    }
}
