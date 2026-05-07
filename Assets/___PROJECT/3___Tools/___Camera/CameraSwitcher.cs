using System;
using UnityEngine;
using Unity.Cinemachine;

//구역에 따라 카메라 위치 전환
public class CameraSwitcher : MonoBehaviour
{
    public CinemachineCamera targetCamera;

    private void Awake()
    {
        targetCamera = GetComponentInParent<CinemachineCamera>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            targetCamera.Priority = 10;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            targetCamera.Priority = 0;
        }
    }
}