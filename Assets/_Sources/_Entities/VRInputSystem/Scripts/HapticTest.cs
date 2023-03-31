using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HapticTest : MonoBehaviour
{
    [SerializeField] private ActionBasedController _leftController;
    [SerializeField] private ActionBasedController _rightController;
    [SerializeField] private VRInputSystem _vrInputSystem;

    private void Start()
    {
        StartCoroutine(HapticCoroutine(_leftController));
        StartCoroutine(HapticCoroutine(_rightController));
    }

    private IEnumerator HapticCoroutine(ActionBasedController controller)
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            _vrInputSystem.SendHapticImpulse(0.1f, 0.1f, controller);
        }
    }
}
