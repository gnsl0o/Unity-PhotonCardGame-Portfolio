using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject[] uiPanels;

    public void ShowUIPanel(int panelIndex)
    {
        // 모든 UI 패널 비활성화
        foreach (GameObject panel in uiPanels)
        {
            panel.SetActive(false);
        }
        // 선택한 UI 패널만 활성화
        if (panelIndex >= 0 && panelIndex < uiPanels.Length)
        {
            uiPanels[panelIndex].SetActive(true);
        }
    }
}