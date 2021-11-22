using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class Utils
{
    public static bool TryGetNodePosition(XRNode node, out Vector3 position)
    {
        List<XRNodeState> nodeStates = new List<XRNodeState>();
        InputTracking.GetNodeStates(nodeStates);
        foreach (XRNodeState state in nodeStates)
        {
            if (state.nodeType == node)
            {
                return state.TryGetPosition(out position);
            }
        }

        position = Vector3.zero;
        return false;
    }

    public static Vector3 GetNodePosition(XRNode node)
    {
        TryGetNodePosition(node, out Vector3 resultPos);
        return resultPos;
    }

    public static bool TryGetNodeRotation(XRNode node, out Quaternion rotation)
    {
        List<XRNodeState> nodeStates = new List<XRNodeState>();
        InputTracking.GetNodeStates(nodeStates);
        foreach (XRNodeState state in nodeStates)
        {
            if (state.nodeType == node)
            {
                return state.TryGetRotation(out rotation);
            }
        }

        rotation = Quaternion.identity;
        return false;
    }

    public static Quaternion GetNodeRotation(XRNode node)
    {
        TryGetNodeRotation(node, out Quaternion resultRotation);
        return resultRotation;
    }

    public static Transform GetRootParentTransform(Transform t)
    {
        while (t.parent != null)
        {
            t = t.parent;
        }

        return t;
    }

    public static T GetOrAddComponent<T>(Transform t) where T : UnityEngine.Component
    {
        T result = t.GetComponent<T>();
        if (result == null)
        {
            result = t.gameObject.AddComponent<T>();
        }

        return result;
    }
}
