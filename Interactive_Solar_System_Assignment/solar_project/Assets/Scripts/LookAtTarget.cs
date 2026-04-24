using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LookAtTarget : MonoBehaviour
{
    [Header("Camera Targets")]
    public GameObject defaultTarget;   // 默认看向 Sun
    private GameObject currentTarget;

    [Header("Camera Settings")]
    public Vector3 mainViewPosition = new Vector3(0, 8, -12);
    public Vector3 mainViewRotation = new Vector3(35, 0, 0);
    public float selectedDistance = 4f;

    [Header("UI")]
    public TextMeshProUGUI factText;
    public Button backButton;

    // 记录上一个点击的物体（用于恢复大小）
    private GameObject lastSelectedObject;
    private Vector3 lastOriginalScale;

    void Start()
    {
        // 初始化相机位置
        transform.position = mainViewPosition;
        transform.eulerAngles = mainViewRotation;

        currentTarget = defaultTarget;

        // 初始化 UI
        if (factText != null)
        {
            factText.text = "Click Earth or the Moon to explore!";
        }

        if (backButton != null)
        {
            backButton.gameObject.SetActive(false);
            backButton.onClick.AddListener(ReturnToMainView);
        }
    }

    void Update()
    {
        // 左键点击检测
        if (Input.GetMouseButtonDown(0))
        {
            TrySelectObject();
        }

        // 相机始终看向目标
        if (currentTarget != null)
        {
            transform.LookAt(currentTarget.transform);
        }
    }

    void TrySelectObject()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject clickedObject = hit.collider.gameObject;

            // 只允许点击 Planet / Moon
            if (clickedObject.CompareTag("Planet") || clickedObject.CompareTag("Moon"))
            {
                SelectObject(clickedObject);
            }
        }
    }

    void SelectObject(GameObject selectedObject)
    {
        // 恢复上一个物体大小
        ResetLastSelectedScale();

        currentTarget = selectedObject;

        // 相机移动到目标附近
        Vector3 direction = (transform.position - selectedObject.transform.position).normalized;
        transform.position = selectedObject.transform.position + direction * selectedDistance;

        transform.LookAt(selectedObject.transform);

        // 显示文字
        SpaceFact fact = selectedObject.GetComponent<SpaceFact>();
        if (fact != null && factText != null)
        {
            factText.text = fact.factText;
        }

        // 显示返回按钮
        if (backButton != null)
        {
            backButton.gameObject.SetActive(true);
        }

        // 放大选中物体
        lastSelectedObject = selectedObject;
        lastOriginalScale = selectedObject.transform.localScale;
        selectedObject.transform.localScale = lastOriginalScale * 1.15f;

        AudioSource audio = selectedObject.GetComponent<AudioSource>();

        if (audio == null)
        {
            audio = selectedObject.GetComponentInParent<AudioSource>();
        }

        if (audio != null && audio.clip != null)
        {
            audio.Stop();
            audio.Play();
            Debug.Log("Playing audio on: " + selectedObject.name);
        }
        else
        {
            Debug.LogWarning("No AudioSource or AudioClip found on: " + selectedObject.name);
        }
    }

    public void ReturnToMainView()
    {
        ResetLastSelectedScale();

        currentTarget = defaultTarget;

        transform.position = mainViewPosition;
        transform.eulerAngles = mainViewRotation;

        if (factText != null)
        {
            factText.text = "Click Earth or the Moon to explore!";
        }

        if (backButton != null)
        {
            backButton.gameObject.SetActive(false);
        }
    }

    void ResetLastSelectedScale()
    {
        if (lastSelectedObject != null)
        {
            lastSelectedObject.transform.localScale = lastOriginalScale;
            lastSelectedObject = null;
        }
    }
}