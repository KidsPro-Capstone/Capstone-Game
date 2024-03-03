﻿using System.Collections;
using UnityEngine;
using Utilities;

namespace GenericPopup
{
    public class PopupAdditive : MonoBehaviour
    {
        [SerializeField]
        private Animator animator;
        private readonly int exit = Animator.StringToHash("Exit");

        protected void ClosePopup()
        {
            if (animator != null)
            {
                animator.SetBool(exit, true);
                StartCoroutine(CloseDelay(0.5f));
            }

            PopupHelpers.Close(gameObject.scene);
        }

        private IEnumerator CloseDelay(float delayTime)
        {
            yield return new WaitForSeconds(delayTime);

            PopupHelpers.Close(gameObject.scene);
        }
    }
}