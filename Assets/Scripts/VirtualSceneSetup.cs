using UnityEngine;

public class VirtualSceneSetup : MonoBehaviour
{
    [SerializeField]
    private bool startOnAwake = default;

    [SerializeField]
    private AudioSource music = default;

    [SerializeField]
    private Material skyboxMaterial = default;
    public Material SkyboxMaterial => skyboxMaterial;

    private bool paused;

    private void Awake()
    {
        if (music == null)
        {
            return;
        }

        music.playOnAwake = false;
        music.loop = true;

        if (startOnAwake)
        {
            StartScene();
        } else
        {
            StopScene();
        }
    }

    public void StartScene()
    {
        if (music == null)
        {
            return;
        }

        if (paused)
        {
            paused = false;
            music.UnPause();
        } else
        {
            music.Play();
        }
    }

    public void StopScene()
    {
        if (music == null)
        {
            return;
        }

        if (music.isPlaying)
        {
            paused = true;
            music.Pause();
        }
    }
}
