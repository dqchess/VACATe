﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueRoom3 : MonoBehaviour
{
    public Animator Anim;

    // Start is called before the first frame update
    void Start()
    {
        Anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            Anim.SetTrigger("EnterRoom3");
        }
    }
}

