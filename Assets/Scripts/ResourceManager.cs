using UnityEngine;

public class ResourceManager : MonoBehaviour
{
   
    public int currentPower = 3; 
    public int currentBudget = 6;
    public int currentTime = 15;

   
    public void ResetPower()
    {
        currentPower = 3;
    }
}