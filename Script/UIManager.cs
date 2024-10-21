using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject[] uiPanels;

    public void ShowUIPanel(int panelIndex)
    {
        // ��� UI �г� ��Ȱ��ȭ
        foreach (GameObject panel in uiPanels)
        {
            panel.SetActive(false);
        }
        // ������ UI �гθ� Ȱ��ȭ
        if (panelIndex >= 0 && panelIndex < uiPanels.Length)
        {
            uiPanels[panelIndex].SetActive(true);
        }
    }
}