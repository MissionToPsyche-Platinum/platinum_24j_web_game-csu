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
        SetFloor(GameManager.Instance.currentFloor); 
    }

public void SetFloor(int floor)
{
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
