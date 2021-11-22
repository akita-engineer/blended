using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class RealityPortal : MonoBehaviour
{
    [Header("Global")]
    [SerializeField]
    private bool willBeSetByOther = default;

    [Header("VR-only Settings")]
    [SerializeField]
    private bool isInVirtualWorld = default;

    [Header("Target Setup")]
    [SerializeField]
    private RealityPortal pairPortal = default;

    [SerializeField]
    private VirtualSceneSetup sourceSceneSetup = default;

    [SerializeField]
    private VirtualSceneSetup destinationSceneSetup = default;

    [SerializeField]
    private PortalSide enterSide = default;

    [Header("Rendering")]
    [SerializeField]
    private LayerMask portalCameraCullingMask = default;

    [SerializeField]
    private LayerMask userCameraCullingMaskAfterTeleport = default;

    [Header("Gameplay References")]
    [SerializeField]
    private RealityPortalSide sideA = default;

    [SerializeField]
    private RealityPortalSide sideB = default;

    // Properties

    public bool IsStereoMode => headCameraLeft != null && headCameraRight != null;

    // Private fields
    private PlayerRig playerRig;

    private Camera headCameraLeft;
    private Camera headCameraCenter;
    private Camera headCameraRight;

    private Camera portalCameraLeft;
    private Camera portalCameraCenter;
    private Camera portalCameraRight;

    private MeshRenderer renderingPlane;

    private int materialLeftEyeTexProperty;
    private int materialRightEyeTexProperty;
    private int materialCenterEyeTexProperty;

    private MaterialPropertyBlock portalPropertyBlock;

    private bool allowTeleport = true;
    private bool isInit;

    //// SETTERS

    public void SetPortalTarget(RealityPortal anotherPortal)
    {
        this.pairPortal = anotherPortal;
    }

    public void SetAllowTeleport(bool allow)
    {
        this.allowTeleport = allow;
    }

    public void SetSourceScene(VirtualSceneSetup sceneSetup)
    {
        this.sourceSceneSetup = sceneSetup;
    }

    public void SetEnterSide(PortalSide side)
    {
        enterSide = side;

        Collider sideACollider = sideA.GetComponentInChildren<Collider>();
        Collider sideBCollider = sideB.GetComponentInChildren<Collider>();

        MeshRenderer sideARenderer = sideA.RenderingPlane;
        MeshRenderer sideBRenderer = sideB.RenderingPlane;

        // Just in case
        sideACollider.isTrigger = true;
        sideBCollider.isTrigger = true;

        // Depending on which entrance is active, enable/disable colliders. Only one of them will be sending the event OnTriggerEnter.
        sideACollider.enabled = side == PortalSide.SideA;
        sideBCollider.enabled = side == PortalSide.SideB;

        // Renderers
        sideARenderer.enabled = side == PortalSide.SideA;
        sideBRenderer.enabled = side == PortalSide.SideB;

        sideA.gameObject.SetActive(side == PortalSide.SideA);
        sideB.gameObject.SetActive(side == PortalSide.SideB);

        // Save references to the sides for future
        renderingPlane = side == PortalSide.SideA ? sideA.RenderingPlane : sideB.RenderingPlane;
    }

    //// SETUP

    private Camera SetupPortalCamera(Camera headCamera, int shaderProperty)
    {
        // Create the camera
        Camera portalCamera = new GameObject().AddComponent<Camera>();
        portalCamera.targetTexture = new RenderTexture(Screen.width, Screen.height, 24);
        portalCamera.cullingMask = portalCameraCullingMask.value;
        portalCamera.clearFlags = CameraClearFlags.Skybox;

        if (destinationSceneSetup != null)
        {
            portalCamera.gameObject.AddComponent<Skybox>().material = destinationSceneSetup.SkyboxMaterial;
        }

        // Add render texture
        renderingPlane.GetPropertyBlock(portalPropertyBlock);
        portalPropertyBlock.SetTexture(shaderProperty, portalCamera.targetTexture);
        renderingPlane.SetPropertyBlock(portalPropertyBlock);

        // Setup to mirror settings of the original
        portalCamera.fieldOfView = headCamera.fieldOfView;
        portalCamera.aspect = headCamera.aspect;
        portalCamera.nearClipPlane = headCamera.nearClipPlane;
        portalCamera.farClipPlane = headCamera.farClipPlane;

        return portalCamera;
    }

    public void Init()
    {
        portalPropertyBlock = new MaterialPropertyBlock();
        materialLeftEyeTexProperty = Shader.PropertyToID("_LeftEyeTex");
        materialRightEyeTexProperty = Shader.PropertyToID("_RightEyeTex");
        materialCenterEyeTexProperty = Shader.PropertyToID("_CenterEyeTex");

        if (Camera.main == null)
        {
            Debug.LogWarning("No main camera detected. This might lead to certain portal-related crashes down the line.");
        }

        // First make sure that the rig is defined
        playerRig = FindObjectOfType<PlayerRig>();
        if (playerRig == null)
        {
            RealityPortalLogWarning("Couldn't find the root rig. Will make the rig whatever is the root transform of the camera");
            Transform rootTransform = Utils.GetRootParentTransform(Camera.main.transform);
            PlayerRig newRig = Utils.GetOrAddComponent<PlayerRig>(rootTransform);
            playerRig = newRig;
        }

        // Check for stereo
        HeadCameraLeft hdLeft = FindObjectOfType<HeadCameraLeft>();
        HeadCameraRight hdRight = FindObjectOfType<HeadCameraRight>();
        if (hdLeft != null && hdRight != null)
        {
            RealityPortalLog("Found left and right eye cameras, will be using stereo mode.");
            headCameraLeft = hdLeft.GetComponent<Camera>();
            headCameraRight = hdRight.GetComponent<Camera>();
        }

        // Check for single
        RealityPortalLogWarning("One of the eye cameras is not present. Will try to use the single camera mode.");
        HeadCameraCenter hdCenter = FindObjectOfType<HeadCameraCenter>();
        if (hdCenter == null)
        {
            RealityPortalLogWarning("Couldn't find a center camera. Will make the main camera a center camera.");
            HeadCameraCenter cameraCenter = Utils.GetOrAddComponent<HeadCameraCenter>(Camera.main.transform);
            headCameraCenter = cameraCenter.GetComponent<Camera>();
        }
        else
        {
            headCameraCenter = hdCenter.GetComponent<Camera>();
        }

        portalCameraCenter = SetupPortalCamera(headCameraCenter, materialCenterEyeTexProperty);
        if (IsStereoMode)
        {
            portalCameraLeft = SetupPortalCamera(headCameraLeft, materialLeftEyeTexProperty);
            portalCameraRight = SetupPortalCamera(headCameraRight, materialRightEyeTexProperty);
        }

        isInit = true;
    }

    //// CAMERA AND TELEPORT MATH
   
    private void SetStereoProjectionMatrix(Camera.StereoscopicEye stereoscopicEye, Camera headCamera, Camera portalCamera)
    {
        // Projection
        Matrix4x4 projection = headCamera.GetStereoProjectionMatrix(stereoscopicEye);
        portalCamera.projectionMatrix = projection;
    }

    private void UpdateXRCamera(XRNode node, Camera portalCamera)
    {
        // Position
        Vector3 eyePos = playerRig.transform.TransformPoint(Utils.GetNodePosition(node));
        Vector3 eyeLocalSpace = transform.InverseTransformPoint(eyePos);
        portalCamera.transform.position = pairPortal.transform.TransformPoint(eyeLocalSpace);

        // Rotation
        Quaternion eyeRotation = playerRig.transform.rotation * Utils.GetNodeRotation(node);
        Quaternion portalToEyeRotation = Quaternion.Inverse(transform.rotation) * eyeRotation;
        portalCamera.transform.rotation = pairPortal.transform.rotation * portalToEyeRotation;
    }

    private void UpdateCenterPortalCamera(Camera headCamera, Camera portalCamera)
    {
        Vector3 eyeLocalSpace = transform.InverseTransformPoint(headCamera.transform.position);
        portalCamera.transform.position = pairPortal.transform.TransformPoint(eyeLocalSpace);

        Quaternion portalToEyeRotation = Quaternion.Inverse(transform.rotation) * headCamera.transform.rotation;
        portalCamera.transform.rotation = pairPortal.transform.rotation * portalToEyeRotation;
    }

    private void Teleport(Camera headCamera, Camera portalCamera)
    {
        Quaternion difference = portalCamera.transform.rotation * Quaternion.Inverse(headCamera.transform.rotation);
        playerRig.transform.rotation = difference * playerRig.transform.rotation;

        Vector3 positionDiff = portalCamera.transform.position - headCamera.transform.position;
        playerRig.transform.position = playerRig.transform.position + positionDiff;
    }

    //// UNITY EVENTS

    private void Awake()
    {
        if (willBeSetByOther)
        {
            return;
        }


        // Setup itself and target portal
        SetEnterSide(enterSide);
        Init();

        pairPortal.SetPortalTarget(this);
        pairPortal.SetEnterSide(enterSide == PortalSide.SideA ? PortalSide.SideB : PortalSide.SideA);
        pairPortal.SetSourceScene(destinationSceneSetup);
        pairPortal.Init();
    }

    private void Start()
    {
        if (IsStereoMode)
        {
            SetStereoProjectionMatrix(Camera.StereoscopicEye.Left, headCameraLeft, portalCameraLeft);
            SetStereoProjectionMatrix(Camera.StereoscopicEye.Right, headCameraRight, portalCameraRight);
        }
    }

    private void OnEnable()
    {
        Application.onBeforeRender += OnBeforeRender;
    }

    private void OnDisable()
    {
        Application.onBeforeRender -= OnBeforeRender;
    }

    private void OnBeforeRender()
    {
        if (!isInit)
        {
            return;
        }

        if (IsStereoMode)
        {
            UpdateXRCamera(XRNode.CenterEye, portalCameraCenter);
            UpdateXRCamera(XRNode.LeftEye, portalCameraLeft);
            UpdateXRCamera(XRNode.RightEye, portalCameraRight);
        } else
        {
            UpdateCenterPortalCamera(headCameraCenter, portalCameraCenter);
        }
    }

    private void Update()
    {
        // TODO: hack for now - this means there must be ONE real world portal
        if (isInVirtualWorld)
        {
            return;
        }

        if (OVRInput.GetDown(OVRInput.Button.One) || Input.GetKeyDown(KeyCode.Space))
        {
            RealityPortal[] portals = FindObjectsOfType<RealityPortal>();
            List<RealityPortal> destinations = new List<RealityPortal>();

            foreach (RealityPortal portal in portals)
            {
                if (portal.isInVirtualWorld)
                {
                    destinations.Add(portal);
                }
            }

          
            int currentIndex = destinations.IndexOf(pairPortal);
            int nextIndex = (currentIndex + 1) % destinations.Count;
            Debug.Log(currentIndex);
            Debug.Log(nextIndex);
            RealityPortal nextDestination = destinations[nextIndex];

            pairPortal = nextDestination;
            destinationSceneSetup = nextDestination.sourceSceneSetup;

            nextDestination.SetEnterSide(enterSide == PortalSide.SideA ? PortalSide.SideB : PortalSide.SideA);
            nextDestination.SetPortalTarget(this);

            if (!nextDestination.isInit)
            {
                nextDestination.Init();
            }

            portalCameraCenter.GetComponent<Skybox>().material = nextDestination.sourceSceneSetup.SkyboxMaterial;

            if (IsStereoMode)
            {
                portalCameraLeft.GetComponent<Skybox>().material = nextDestination.sourceSceneSetup.SkyboxMaterial;
                portalCameraRight.GetComponent<Skybox>().material = nextDestination.sourceSceneSetup.SkyboxMaterial;
            }  
        }
    }

    // It is teleporting once - but really high. Why?
    private void OnTriggerEnter(Collider other)
    {
        if (!allowTeleport)
        {
            return;
        }

        if (!enabled)
        {
            return;
        }

        bool isHead = other.GetComponent<HeadCameraCenter>() != null || other.GetComponent<HeadCameraLeft>() != null || other.GetComponent<HeadCameraRight>() != null;

        if (!isHead)
        {
            return;
        }

        Teleport(headCameraCenter, portalCameraCenter);

        // Set skyboxes
        if (!isInVirtualWorld)
        {
            headCameraCenter.GetComponent<Skybox>().material = destinationSceneSetup.SkyboxMaterial;
            destinationSceneSetup.StartScene();
        }
        else
        {
            headCameraCenter.GetComponent<Skybox>().material = null;
            sourceSceneSetup.StopScene();
        }

        headCameraCenter.cullingMask = userCameraCullingMaskAfterTeleport;

        // Make sure that it doesn't teleport us back
        pairPortal.SetAllowTeleport(false);

        //Passthrough changes for VR only
        OVRPassthroughLayer passthroughLayer = FindObjectOfType<OVRPassthroughLayer>();
        if (passthroughLayer == null)
        {
            return;
        }

        if (!isInVirtualWorld)
        {
            passthroughLayer.projectionSurfaceType = OVRPassthroughLayer.ProjectionSurfaceType.UserDefined;
            passthroughLayer.overlayType = OVROverlay.OverlayType.Underlay;

            // Restart
            passthroughLayer.enabled = false;
            passthroughLayer.enabled = true;

            // The plane of the target portal must become a cutout
            passthroughLayer.AddSurfaceGeometry(pairPortal.renderingPlane.gameObject); //any of the planes will do
        }
        else
        {
            // We are AT the virtual portal, so OUR central plane is currently a cutout which we remove
            passthroughLayer.RemoveSurfaceGeometry(renderingPlane.gameObject); //any of the planes will do

            passthroughLayer.projectionSurfaceType = OVRPassthroughLayer.ProjectionSurfaceType.Reconstructed;
            passthroughLayer.overlayType = OVROverlay.OverlayType.Underlay;

            // Restart
            passthroughLayer.enabled = false;
            passthroughLayer.enabled = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (allowTeleport)
        {
            return;
        }

        if (!enabled)
        {
            return;
        }

        bool isHead = other.GetComponent<HeadCameraCenter>() != null || other.GetComponent<HeadCameraLeft>() != null || other.GetComponent<HeadCameraRight>() != null;

        if (!isHead)
        {
            return;
        }

        SetAllowTeleport(true);
    }

    //// LOGGING

    private string logTag = "[REALITY PORTAL]";

    private void RealityPortalLog(string msg)
    {
        Debug.Log($"{logTag} {msg}");
    }

    private void RealityPortalLogWarning(string msg)
    {
        Debug.LogWarning($"{logTag} {msg}");
    }

    private void RealityPortalLogError(string msg)
    {
        Debug.LogError($"{logTag} {msg}");
    }
}