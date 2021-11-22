using UnityEngine;

public class PerEyeShaderRenderingSupport : MonoBehaviour
{
    private int currentEye;
    private int isStereoscopic;

    private void Awake()
    {
        // Check for stereo
        HeadCameraLeft hdLeft = FindObjectOfType<HeadCameraLeft>();
        HeadCameraRight hdRight = FindObjectOfType<HeadCameraRight>();
        if (hdLeft != null && hdRight != null)
        {
            isStereoscopic = 1;
        }
    }

    private void OnPreRender()
    {
        Shader.SetGlobalInt("RenderingEye", currentEye);
        Shader.SetGlobalInt("IsStereoscopic", isStereoscopic);
        currentEye = 1 - currentEye;
    }
}
