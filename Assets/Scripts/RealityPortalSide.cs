using UnityEngine;

public enum PortalSide
{
    SideA, SideB
}

public class RealityPortalSide : MonoBehaviour
{
    [SerializeField]
    private PortalSide side = default;
    public PortalSide Side => side;

    [Header("Rendering Dependencies")]
    [SerializeField]
    private MeshRenderer renderingPlane = default;
    public MeshRenderer RenderingPlane => renderingPlane;
}
