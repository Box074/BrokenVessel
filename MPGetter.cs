using System.Collections;
using UnityEngine;

namespace BrokenVessel
{
    public class MPGetter : MonoBehaviour
    {
        void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.GetComponent<HealthManager>() != null)
            {
                int mp = 12;
                if (PlayerData.instance.equippedCharm_20) mp += 4;
                if (PlayerData.instance.equippedCharm_21) mp += 8;
                HeroController.instance.AddMPCharge(mp);
            }
        }
    }
}