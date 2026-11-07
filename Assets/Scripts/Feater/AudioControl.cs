using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioControl : MonoBehaviour
{
    public AudioSource audioSource;
    public float delayStart;
    public AudioClip[] clips;
    public float[] delays;
    public bool playMainAffterStack;
    public bool loop;

    private void OnValidate()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        //audioSource.PlayDelayed(delayed);
        StartCoroutine(PlayStack());
    }

    IEnumerator PlayStack()
    {
        yield return new WaitForSeconds(delayStart);
        int i = 0;
        while(i < clips.Length)
        {
            audioSource.PlayOneShot(clips[i]);
            i++;
            if (loop && i >= clips.Length)
            {
                i = 0;
            }
            yield return new WaitForSeconds(clips[i].length);
            if (delays != null && delays.Length > i)
                yield return new WaitForSeconds(delays[i]);
        }

        if (playMainAffterStack)
            audioSource.Play();
    }
}
