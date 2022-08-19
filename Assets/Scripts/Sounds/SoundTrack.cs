using System.Collections;
using UnityEngine;

public class SoundTrack : MonoBehaviour
{
    public AudioSource[] audioClips;
    int trackNumber;
    int previousTrackNumber;

    void Start()
    {
        trackNumber = 0;
        previousTrackNumber = trackNumber;
        StartCoroutine(PlayMusic());
    }

    IEnumerator PlayMusic()
    {
        while (true) // loop forever
        {
            PlayRandomTrack();
            yield return new WaitWhile(() => audioClips[trackNumber - 1].isPlaying);
        }
    }

    private void PlayRandomTrack()
    {
        // avoid repeating the same track twice
        while(trackNumber == previousTrackNumber)
            trackNumber = Random.Range(0, audioClips.Length) + 1; // pick a new track number randomly
        previousTrackNumber = trackNumber; // save the track number for future use to compare to next random track number

        //Debug.Log(trackNumber + " out of " + audioClips.Length);
        audioClips[trackNumber - 1].Play();
    }
}
