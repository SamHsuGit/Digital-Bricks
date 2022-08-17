using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PPFXSetValues : MonoBehaviour
{
    public World world;
    [HideInInspector] public DepthOfField depthOfField;

    VolumeProfile volumeProfile;
    private void Awake()
    {
        volumeProfile = GetComponent<Volume>()?.profile;
    }
    void FixedUpdate()
    {
        if (!volumeProfile) throw new System.NullReferenceException(nameof(VolumeProfile));

        // You can leave this variable out of your function, so you can reuse it throughout your class.
        //UnityEngine.Rendering.Universal.DepthOfField depthOfField;

        if (!volumeProfile.TryGet(out depthOfField)) throw new System.NullReferenceException(nameof(depthOfField));
    }
}