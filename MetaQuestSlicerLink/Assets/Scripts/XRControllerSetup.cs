using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRControllerSetup : MonoBehaviour
{
    [Header("Controller Prefabs")]
    public GameObject leftControllerPrefab;
    public GameObject rightControllerPrefab;

    [Header("Attach To")]
    public Transform attachUnder; // e.g. XR Rig or any parent object

    private void Start()
    {
        if (leftControllerPrefab && rightControllerPrefab && attachUnder)
        {
            CreateController(leftControllerPrefab, "LeftHand Controller");
            CreateController(rightControllerPrefab, "RightHand Controller");
        }
        else
        {
            Debug.LogError("[XRControllerSetup] Please assign both controller prefabs and attachUnder.");
        }
    }

    private void CreateController(GameObject prefab, string name)
    {
        GameObject controller = Instantiate(prefab, attachUnder);
        controller.name = name;

        XRRayInteractor rayInteractor = controller.GetComponent<XRRayInteractor>();
        if (rayInteractor == null)
        {
            rayInteractor = controller.AddComponent<XRRayInteractor>();
        }

        XRInteractorLineVisual lineVisual = controller.GetComponent<XRInteractorLineVisual>();
        if (lineVisual == null)
        {
            lineVisual = controller.AddComponent<XRInteractorLineVisual>();
        }

        ActionBasedController xrController = controller.GetComponent<ActionBasedController>();
        if (xrController == null)
        {
            xrController = controller.AddComponent<ActionBasedController>();
        }

        Debug.Log("[XRControllerSetup] Created: " + name);
    }
}
