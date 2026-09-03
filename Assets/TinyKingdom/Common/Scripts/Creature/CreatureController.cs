using UnityEngine;

namespace TinyKingdom.Common.Scripts.Creature
{
    public class CreatureController : MonoBehaviour
    {
        public GameObject Front;
        public GameObject Back;
        public Animator Animator;
        public float Speed;
        public AudioSource Footsteps;

        private static readonly int IsFrontHash = Animator.StringToHash("IsFront");
        private static readonly int StateHash = Animator.StringToHash("State");

        public void Start()
        {
            Front.SetActive(true);
            Back.SetActive(false);
        }

        public void Update()
        {
            var movement = Vector3.zero;
            var dead = Animator.GetInteger("State") == (int)CreatureState.Dead;
            var state = dead ? CreatureState.Dead : CreatureState.Idle;
            var direction = 0;

            if (Input.GetKey(KeyCode.LeftArrow))
            {
                movement.x -= Speed;
                direction = -1;
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                movement.x += Speed;
                direction = 1;
            }

            if (direction != 0)
            {
                var scale = transform.localScale;

                scale.x = direction * Mathf.Abs(scale.x);
                transform.localScale = scale;
            }

            if (Input.GetKey(KeyCode.UpArrow))
            {
                movement.y += Speed * 0.7f;
                Front.SetActive(true);
                Back.SetActive(false);
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                movement.y -= Speed * 0.7f;
                Front.SetActive(false);
                Back.SetActive(true);
            }

            if (movement.magnitude > 0)
            {
                Front.SetActive(movement.y <= 0);
                Back.SetActive(movement.y > 0);
            }

            if (movement.x != 0 || movement.y != 0)
            {
                if (state == CreatureState.Dead)
                {
                    GetComponent<FaceExpressions>().SetFace("Normal");
                }

                state = CreatureState.Run;
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                Animator.SetTrigger("Attack");
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                Animator.SetTrigger("BowShot");
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                state = CreatureState.Dead;
            }

            transform.position += movement * Time.deltaTime;
            Animator.SetBool(IsFrontHash, Front.activeSelf);
            Animator.SetInteger(StateHash, (int)state);

            if (Footsteps)
            {
                if (state == CreatureState.Run)
                {
                    if (!Footsteps.isPlaying)
                    {
                        Footsteps.Play();
                    }
                }
                else
                {
                    if (Footsteps.isPlaying)
                    {
                        Footsteps.Stop();
                    }
                }
            }
        }
    }
}