using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("Tool Buttons")]
    [SerializeField] private Button createDome;
    [SerializeField] private Button createVolume;

    [Header("Tool Managers")]
    [SerializeField] private UITool domeManager;
    [SerializeField] private UITool volumeManager;

    [Header("Rope UI")]
    [SerializeField] private GameObject ropeSettingsPanel;
    [SerializeField] private Slider ropeLengthSlider;
    [SerializeField] private Toggle tipWeightToggle;
    [SerializeField] private Toggle balancedWeightToggle;
    [SerializeField] private Slider weightSlider;
    [SerializeField] private Button generateMesh;

    [Header("Volume UI")]
    [SerializeField] private GameObject volumeSettingsPanel;
    [SerializeField] private Slider volumeHeightSlider;
    [SerializeField] private Toggle sailRoofToggle;
    [SerializeField] private Toggle tentRoofToggle;

    private UITool currentTool;
    private GF_Rope2 selectedRope;
    private GF_Volume selectedVolume;

    private void Start()
    {
        createDome.onClick.AddListener(() => ActivateTool(domeManager));
        createVolume.onClick.AddListener(() => ActivateTool(volumeManager));

        ropeSettingsPanel.SetActive(false);

        // Setup UI callbacks
        ropeLengthSlider.onValueChanged.AddListener(OnRopeLengthChanged);
        tipWeightToggle.onValueChanged.AddListener(OnTipWeightChanged);
        balancedWeightToggle.onValueChanged.AddListener(OnBalancedWeightChanged);
        weightSlider.onValueChanged.AddListener(OnWeightSliderChanged);
        generateMesh.onClick.AddListener(() => GenerateDome());

        //Volume
        volumeHeightSlider.onValueChanged.AddListener(OnVolumeHeightChanged);
        sailRoofToggle.onValueChanged.AddListener(OnSailVaultChanged);
        tentRoofToggle.onValueChanged.AddListener(OnTentRoofChanged);
    }

    private void ActivateTool(UITool newTool)
    {
        GF_GridPoint[] allPoints = FindObjectsOfType<GF_GridPoint>();
        List<GF_GridPoint> selectedPoints = new List<GF_GridPoint>();

        foreach (var point in allPoints)
        {
            if (point.toggled)
                selectedPoints.Add(point);
        }

        if (!newTool.ValidatePoints(selectedPoints))
        {
            Debug.LogWarning($"{newTool.name} rejected activation: selected points invalid.");
            return;
        }

        if (currentTool != null)
        {
            currentTool.OnToolDeactivated();
            currentTool.enabled = false;
        }

        currentTool = newTool;
        currentTool.enabled = true;

        currentTool.OnToolActivated(selectedPoints);
    }

    // Called by GF_Rope2.OnMouseDown()
    public void ShowRopeSettings(GF_Rope2 rope)
    {
        volumeSettingsPanel.SetActive(false);
        selectedRope = rope;

        ropeLengthSlider.SetValueWithoutNotify(rope.RopeLength);
        tipWeightToggle.SetIsOnWithoutNotify(rope.TipWeight);
        balancedWeightToggle.SetIsOnWithoutNotify(rope.BalancedWeights);
        weightSlider.SetValueWithoutNotify(rope.RopeLength);

        ropeSettingsPanel.SetActive(true);
    }

    private void OnRopeLengthChanged(float newLength)
    {
        if (selectedRope == null) return;

        selectedRope.RopeLength = newLength;
        if(selectedRope.drawPerpendicularLine){
            selectedRope.GetComponent<MeshRenderer>().enabled = false;
        }
    }

    private void OnTipWeightChanged(bool enabled)
    {
        if (selectedRope == null) return;

        selectedRope.TipWeight = enabled;
        if(selectedRope.drawPerpendicularLine){
            selectedRope.GetComponent<MeshRenderer>().enabled = false;
        }
    }

    private void OnBalancedWeightChanged(bool enabled)
    {
        if (selectedRope == null) return;

        selectedRope.BalancedWeights = enabled;
        if(selectedRope.drawPerpendicularLine){
            selectedRope.GetComponent<MeshRenderer>().enabled = false;
        }
    }

    private void OnWeightSliderChanged(float newWeight)
    {
        if (selectedRope == null) return;

        selectedRope.BalancedWeightT = newWeight;
        if(selectedRope.drawPerpendicularLine){
            selectedRope.GetComponent<MeshRenderer>().enabled = false;
        }
    }

    private void GenerateDome()
    {
        if(selectedRope.drawPerpendicularLine){
            selectedRope.gameObject.GetComponent<GF_DomeMesh>().GenerateDomeMesh();
            selectedRope.GetComponent<MeshRenderer>().enabled = true;
        }
    }

    public void ShowVolumeSettings(GF_Volume volume)
    {
        selectedVolume = volume;
        volumeHeightSlider.SetValueWithoutNotify(volume.VolumeHeight);
        sailRoofToggle.SetIsOnWithoutNotify(volume.IsSailRoofActive);
        tentRoofToggle.SetIsOnWithoutNotify(volume.IsTentRoofActive);

        ropeSettingsPanel.SetActive(false);
        volumeSettingsPanel.SetActive(true);
    }

    private void OnVolumeHeightChanged(float newLength)
    {
        if (selectedVolume == null) return;

        selectedVolume.VolumeHeight = (int)newLength;
        Debug.Log("Height changed");
    }

    private void OnSailVaultChanged(bool enabled)
    {
        if (selectedVolume == null) return;

        if(enabled){
            selectedVolume.EnableSailRoof();
        }
        else{
            selectedVolume.DisableSailRoof();
        }

        sailRoofToggle.SetIsOnWithoutNotify(selectedVolume.IsSailRoofActive);
        tentRoofToggle.SetIsOnWithoutNotify(selectedVolume.IsTentRoofActive);
    }

    private void OnTentRoofChanged(bool enabled)
    {
        if (selectedVolume == null) return;

        if(enabled){
            selectedVolume.EnableTentRoof();
        }
        else{
            selectedVolume.DisableTentRoof();
        }

        sailRoofToggle.SetIsOnWithoutNotify(selectedVolume.IsSailRoofActive);
        tentRoofToggle.SetIsOnWithoutNotify(selectedVolume.IsTentRoofActive);
    }
}
