using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TMPController : MonoBehaviour
{
    public PlayerMovement script;
    double vel;
    [SerializeField] private TextMeshProUGUI VelocityIndicator;

    public void FixedUpdate()
    {
        vel = script.vel;
        VelocityIndicator.text = vel.ToString("0.#");
    }

}