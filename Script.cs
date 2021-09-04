using System.Collections;
using UnityEngine;
using TranCore;
using ModCommon;
using ModCommon.Util;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;

namespace BrokenVessel
{
    public class Script : MonoBehaviour
    {
        TranAttach TranAttach => gameObject.GetTranAttach();
        PlayMakerFSM control = null;
        tk2dSpriteAnimator animator = null;
        Rigidbody2D rig = null;
        GameObject overheadSlash = null;
        GameObject DS = null;
        GameObject HS = null;
        void SetRigX(float newV)
        {
            rig.velocity = new Vector2(newV, rig.velocity.y);
        }
        void SetRigY(float newV)
        {
            rig.velocity = new Vector2(rig.velocity.x, newV);
        }
        void Awake()
        {
            gameObject.AddComponent<TranAttach>();
            TranAttach.AutoDis = false;
            control = gameObject.LocateMyFSM("IK Control");

            DS = gameObject.GetFSMActionsOnState<SpawnObjectFromGlobalPool>("Dstab Land")[0].gameObject.Value;
            HS = gameObject.GetFSMActionOnState<SpawnObjectFromGlobalPool>("Spawn", "Shake Projectiles L").gameObject.Value;

            overheadSlash = control.FsmVariables.FindFsmGameObject("Overhead Slash").Value;
            overheadSlash.AddComponent<MPGetter>();
            overheadSlash.layer = (int)GlobalEnums.PhysLayers.HERO_ATTACK;
            if (overheadSlash.GetComponent<DamageHero>() != null)
            {
                Destroy(overheadSlash.GetComponent<DamageHero>());
            }

            Destroy(control);
            foreach (var v in GetComponents<PlayMakerFSM>()) Destroy(v);

            // DamageEnemies damageEnemies = overheadSlash.AddComponent<DamageEnemies>();
            // damageEnemies.magnitudeMult = 1;
            // damageEnemies.attackType = AttackTypes.Nail;
            // damageEnemies.specialType = SpecialTypes.None;
            // damageEnemies.circleDirection = true;
			overheadSlash.TranHeroAttack(AttacksType.Nail, 21);
            

            animator = gameObject.GetComponent<tk2dSpriteAnimator>();
            rig = gameObject.GetComponent<Rigidbody2D>();
            rig.gravityScale = 1;

            TranAttach.RegisterAction("ROAR", Roar, NoRoar);
            TranAttach.RegisterAction("JUMP", Jump, NoRoar , TranAttach.InvokeWithout("JUMP"),
                TranAttach.InvokeWithout("DASH"),
                TranAttach.InvokeWithout("DSTAB"));
            TranAttach.InvokeActionOn("JUMP", () => InputHandler.Instance.inputActions.jump.IsPressed);
            TranAttach.RegisterAction("DASH", Dash, NoRoar,
                TranAttach.InvokeWithout("DASH") ,
                TranAttach.InvokeWithout("DSTAB"));
            TranAttach.InvokeActionOn("DASH", () => InputHandler.Instance.inputActions.dash.IsPressed);
            TranAttach.RegisterAction("RUN", Run, NoRoar, 
                TranAttach.InvokeWithout("RUN"),
                TranAttach.InvokeWithout("DASH"), TranAttach.InvokeWithout("DSTAB"));
            TranAttach.InvokeActionOn("RUN", () => {
                return (InputHandler.Instance.inputActions.left.IsPressed
                || InputHandler.Instance.inputActions.right.IsPressed);
            });
            TranAttach.RegisterAction("ATTACK", Attack, NoRoar, TranAttach.InvokeWithout("DASH"),
                TranAttach.InvokeWithout("DSTAB"), TranAttach.InvokeWithout("JUMP"), TranAttach.InvokeWithout("ATTACK"));
            TranAttach.InvokeActionOn("ATTACK", () =>
            {
                return InputHandler.Instance.inputActions.attack.IsPressed;
            });


            TranAttach.RegisterAction("FALL", Fall, NoRoar, TranAttach.InvokeWithout("FALL"),
                TranAttach.InvokeWithout("DASH"),
                TranAttach.InvokeWithout("DSTAB")
                );
            TranAttach.InvokeActionOn("FALL", () =>
            {
                return rig.velocity.y < -0.1f;
            });

            TranAttach.RegisterAction("SHAKE", Shake, NoRoar,
                TranAttach.InvokeWithout("RUN"),
                TranAttach.InvokeWithout("JUMP"),
                TranAttach.InvokeWithout("DASH"),
                TranAttach.InvokeWithout("FALL"),
                TranAttach.InvokeWithout("DSTAB"),
                TranAttach.InvokeWithout("SHAKE"),
                TranAttach.EnoughMP(1)
                );
            TranAttach.InvokeActionOn("SHAKE", () => {
                return ((InputHandler.Instance.inputActions.quickCast.IsPressed ||
                InputHandler.Instance.inputActions.cast.IsPressed)
                && InputHandler.Instance.inputActions.up.IsPressed);
            });

            TranAttach.RegisterAction("DSTAB", Dstab, NoRoar, TranAttach.InvokeWithout("DSTAB"),
                TranAttach.EnoughMP(24),
                TranAttach.Or(TranAttach.InvokeWith("JUMP"), TranAttach.InvokeWith("FALL"))
                );
            TranAttach.InvokeActionOn("DSTAB", () => {
                return ((InputHandler.Instance.inputActions.quickCast.IsPressed || 
                InputHandler.Instance.inputActions.cast.IsPressed)
                && InputHandler.Instance.inputActions.down.IsPressed);
            });

            TranAttach.RegisterAction("STOP", Stop, NoRoar, TranAttach.InvokeWithout("RUN"),
                TranAttach.InvokeWithout("JUMP"),
                TranAttach.InvokeWithout("DASH"));
            TranAttach.InvokeActionOn("STOP", () => true);

            TranAttach.RegisterAction("IDLE", Idle, TranAttach.InvokeWithout("IDLE"));
            TranAttach.InvokeActionOn("IDLE", () => TranAttach.InvokeCount == 0);
        }

        void Update()
        {
            HeroController.instance.GetComponent<MeshRenderer>().enabled = false;
            if (!HeroController.instance.cState.transitioning && !HeroController.instance.cState.hazardRespawning
                && !HeroController.instance.cState.dead)
            {
                HeroController.instance.transform.position = transform.position;
            }
            else
            {
                transform.position = HeroController.instance.transform.position;
            }
            transform.localScale = HeroController.instance.transform.localScale * new Vector2(-1, 1);
            if (InputHandler.Instance.inputActions.left.IsPressed)
            {
                HeroController.instance.FaceLeft();
            }
            if (InputHandler.Instance.inputActions.right.IsPressed)
            {
                HeroController.instance.FaceRight();
            }
        }

        void OnEnable()
        {
            On.HeroController.CanTalk += HeroController_CanTalk;
            On.HeroController.CanFocus += HeroController_CanFocus;
            On.HeroController.CanQuickMap += HeroController_CanQuickMap;
            On.HeroController.CanNailCharge += HeroController_CanNailCharge;
            TranAttach.InvokeAction("ROAR");
        }

        private bool HeroController_CanNailCharge(On.HeroController.orig_CanNailCharge orig, HeroController self)
        {
            return false;
        }

        private bool HeroController_CanQuickMap(On.HeroController.orig_CanQuickMap orig, HeroController self)
        {
            if (!HeroController.instance.cState.dead) return true;
            return false;
        }

        private bool HeroController_CanFocus(On.HeroController.orig_CanFocus orig, HeroController self)
        {
            if (!HeroController.instance.cState.dead) return true;
            return false;
        }

        private bool HeroController_CanTalk(On.HeroController.orig_CanTalk orig, HeroController self)
        {
            if (!HeroController.instance.cState.dead) return true;
            return false;
        }

        void OnDisable()
        {
            On.HeroController.CanTalk -= HeroController_CanTalk;
            On.HeroController.CanFocus -= HeroController_CanFocus;
            On.HeroController.CanQuickMap -= HeroController_CanQuickMap;
            On.HeroController.CanNailCharge -= HeroController_CanNailCharge;
        }
        void CancelAttack()
        {
            overheadSlash.GetComponent<tk2dSpriteAnimator>().StopAndResetFrame();
            overheadSlash.SetActive(false);
            animator.Stop();
        }
        void CancelShake()
        {
            isShake = false;
        }
        bool NoRoar() => !TranAttach.IsActionInvoking("ROAR");
        IEnumerator Run()
        {
            CancelShake();
            if (!TranAttach.IsActionInvoking("FALL") && !TranAttach.IsActionInvoking("JUMP")
                && !TranAttach.IsActionInvoking("ATTACK"))
            {
                animator.Play("Walk");
            }
            float speed = HeroController.instance.cState.facingRight ?
                HeroController.instance.RUN_SPEED_CH_COMBO : -HeroController.instance.RUN_SPEED_CH_COMBO;
            SetRigX(speed);
            yield return null;
        }
        IEnumerator Idle()
        {
            animator.Play("Idle");
            On.HeroController.TakeDamage -= _NoTakeDamage;
            yield return null;
        }
        IEnumerator Stop()
        {
            SetRigX(0);
            yield return null;
        }
        IEnumerator Roar()
        {
            On.HeroController.TakeDamage -= _NoTakeDamage;
            On.HeroController.TakeDamage += _NoTakeDamage;
            rig.gravityScale = 0;
            animator.Play("Wake 1");
            yield return new WaitForSeconds(0.15f);
            animator.Play("Wake 2");
            yield return new WaitForSeconds(0.25f);
            animator.Play("Wake 3");
            yield return new WaitForSeconds(0.5f);
            yield return animator.PlayAnimWait("Roar Start");
            animator.Play("Roar Loop");
            yield return new WaitForSeconds(1);
            yield return animator.PlayAnimWait("Roar End");
            rig.gravityScale = 1;
            foreach (var v in GetComponents<Collider2D>())
            {
                v.enabled = true;
                v.isTrigger = false;
            }
            rig.isKinematic = false;
            rig.bodyType = RigidbodyType2D.Dynamic;
            On.HeroController.TakeDamage -= _NoTakeDamage;
        }

        IEnumerator Jump()
        {
            CancelShake();
            CancelAttack();
            SetRigY(HeroController.instance.JUMP_SPEED * 1.5f);
            animator.Play("Jump");
            yield return new WaitForSeconds(0.25f);
        }

        IEnumerator Fall()
        {
            CancelShake();
            rig.gravityScale = 1;
            animator.Play("Fall");
            while (!(rig.velocity.y < 0.1f && rig.velocity.y > -0.1f)) yield return null;
            if (animator.IsPlaying("Fall") && !TranAttach.IsActionInvoking("DSTAB"))
            {
                yield return animator.PlayAnimWait("Land");
            }
        }

        IEnumerator Dash()
        {
            CancelShake();
            CancelAttack();
            rig.gravityScale = 0;
            SetRigY(0);
            HeroController.instance.SetDamageMode(1);
            SetRigX(HeroController.instance.cState.facingRight ?
                HeroController.instance.DASH_SPEED : -HeroController.instance.DASH_SPEED);
            animator.Play("Dash Antic 2");
            yield return new WaitForSeconds(0.25f);
            yield return animator.PlayAnimWait("Dash Antic 3");
            SetRigX(0);
            yield return animator.PlayAnimWait("Dash Recover");
            HeroController.instance.SetDamageMode(0);
            
            rig.gravityScale = 1;
        }
        IEnumerator Dstab()
        {
            CancelAttack();
            HeroController.instance.TakeMP(24);
            HeroController.instance.SetDamageMode(2);
            yield return animator.PlayAnimWait("Downstab Antic");
            animator.Play("Downstab");

            while (!(rig.velocity.y < 0.1f && rig.velocity.y > -0.1f))
			{
				SetRigY(-40);
				yield return null;
			}
            DS.Clone().SetPos(transform.position + new Vector3(0, -0.5f, 0)).TranHeroAttack(AttackTypes.Spell, 1)
                .GetComponent<Rigidbody2D>().velocity = new Vector2(10, 0);
            DS.Clone().SetPos(transform.position + new Vector3(0, -0.5f, 0)).TranHeroAttack(AttackTypes.Spell, 1)
                .GetComponent<Rigidbody2D>().velocity = new Vector2(19, 0);
            if (PlayerData.instance.equippedCharm_19)
            {
                DS.Clone().SetPos(transform.position + new Vector3(0, -0.5f, 0)).TranHeroAttack(AttackTypes.Spell, 1)
                    .GetComponent<Rigidbody2D>().velocity = new Vector2(28, 0);
                DS.Clone().SetPos(transform.position + new Vector3(0, -0.5f, 0)).TranHeroAttack(AttackTypes.Spell, 1)
                    .GetComponent<Rigidbody2D>().velocity = new Vector2(37, 0);
            }
            DS.Clone().SetPos(transform.position + new Vector3(0, -0.5f, 0)).TranHeroAttack(AttackTypes.Spell, 1)
                .GetComponent<Rigidbody2D>().velocity = new Vector2(-10, 0);
            DS.Clone().SetPos(transform.position + new Vector3(0, -0.5f, 0)).TranHeroAttack(AttackTypes.Spell, 1)
                .GetComponent<Rigidbody2D>().velocity = new Vector2(-19, 0);
            if (PlayerData.instance.equippedCharm_19)
            {
                DS.Clone().SetPos(transform.position + new Vector3(0, -0.5f, 0)).TranHeroAttack(AttackTypes.Spell, 1)
                    .GetComponent<Rigidbody2D>().velocity = new Vector2(-28, 0);
                DS.Clone().SetPos(transform.position + new Vector3(0, -0.5f, 0)).TranHeroAttack(AttackTypes.Spell, 1)
                    .GetComponent<Rigidbody2D>().velocity = new Vector2(-37, 0);
            }
            animator.Play("Downstab Land");
            
            rig.gravityScale = 1;
            HeroController.instance.SetDamageMode(0);
			HeroController.instance.QuakeInvuln();
        }
        
        bool isShake = false;
        IEnumerator Shake()
        {
            isShake = true;
            yield return animator.PlayAnimWait("Shake Antic");
            animator.Play("Shake Loop");
            int count = 0;
            while (isShake && PlayerData.instance.MPCharge >= 1)
            {
                if (PlayerData.instance.equippedCharm_19)
                {
                    HS.Clone().SetPos(transform.position).TranHeroAttack(AttackTypes.Spell, 4)
                    .GetComponent<Rigidbody2D>().velocity = new Vector2(Random.Range(-25, 25), 0);
                }
                else
                {
                    HS.Clone().SetPos(transform.position).TranHeroAttack(AttackTypes.Spell, 1)
                    .GetComponent<Rigidbody2D>().velocity = new Vector2(Random.Range(-25, 25), 0);
                }
                count++;
                if (count == 10)
                {
                    count = 0;
                    HeroController.instance.TakeMP(4);
                }
                yield return new WaitForSeconds(0.1f);
            }
            animator.PlayAnimWait("Shake End");
        }
        IEnumerator Attack()
        {
            CancelShake();
			overheadSlash.tag = "Nail Attack";
            overheadSlash.GetComponent<DE>().hit.DamageDealt = PlayerData.instance.nailDamage;
            overheadSlash.SetActive(true);
            overheadSlash.GetComponent<tk2dSpriteAnimator>().Play("Overhead Slash");
            yield return animator.PlayAnimWait("Overhead Slashing");
            overheadSlash.SetActive(false);
            yield return animator.PlayAnimWait("Overhead Recover");
        }
        
        private void _NoTakeDamage(On.HeroController.orig_TakeDamage orig, HeroController self, GameObject go,
            GlobalEnums.CollisionSide damageSide, int damageAmount, int hazardType)
        {
            
        }
    }
}