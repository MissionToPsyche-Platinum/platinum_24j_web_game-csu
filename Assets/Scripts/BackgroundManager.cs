using UnityEngine;
using UnityEngine.UI;

public class BackgroundManager : MonoBehaviour
{
public Image backgroundImage;

public Sprite floor1BG;
public Sprite floor2BG;
public Sprite floor3BG;
public Sprite floor4BG;
    void Start()
    {
        if (GameManager.Instance != null)
            SetFloor(GameManager.Instance.currentFloor);
        else
            Debug.LogWarning("[BackgroundManager] GameManager.Instance is null on Start — background not set.");
    }

public void SetFloor(int floor)
{
    if (backgroundImage == null)
    {
        Debug.LogWarning("[BackgroundManager] backgroundImage is not assigned.");
        return;
    }
    switch (floor)
    {
        case 1:
            backgroundImage.sprite = floor1BG;
            break;
        case 2:
            backgroundImage.sprite = floor2BG;
            break;
        case 3:
            backgroundImage.sprite = floor3BG;
            break;
        case 4:
            backgroundImage.sprite = floor4BG;
            break;
    }
}

}
