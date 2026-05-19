using System;
using System.Collections.Generic;
using UltimateClean;
using UnityEngine;
using UnityEngine.Events;

public class BGMController : MonoBehaviour
{
    public LoopableSelectionSlider bgmSlider;
    public List<AudioClip> bgmClips;
    
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (bgmSlider != null)
        {
            bgmSlider.Options = new List<string>(bgmClips.Count);
            for(int i = 0; i < bgmClips.Count; i++)
            {
                bgmSlider.Options[i] = bgmClips[i].name;
            }
        } 
    }

    public void OnBGMChangeButtonClick()
    {
        audioSource.clip = bgmClips[bgmSlider.Index];
        audioSource.Play();
    }
}
