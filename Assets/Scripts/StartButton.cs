using UnityEngine;
using UnityEngine.UI;

public class StartButton : MonoBehaviour
{

    [SerializeField] private GameObject LoginPanel;

    [SerializeField] private GameObject MainPanel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameObject.GetComponent<Button>().onClick.AddListener(OnClick);
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnClick()
    {
        MainPanel.SetActive(true);
        LoginPanel.SetActive(false);
    }
}
