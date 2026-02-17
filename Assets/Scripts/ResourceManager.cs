using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    [cite_start]// Starting values from documentation [cite: 55, 56, 57]
    public int currentPower = 3; 
    public int currentBudget = 6;
    public int currentTime = 15;

    [cite_start]// Call this at the start of every encounter [cite: 71]
    public void ResetPower()
    {
        currentPower = 3;
    }
}