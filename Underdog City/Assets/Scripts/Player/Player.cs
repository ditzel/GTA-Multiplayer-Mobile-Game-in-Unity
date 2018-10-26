﻿using Photon.Pun;
using UnityEngine;

namespace UnderdogCity
{
    public class Player : MonoBehaviourPun
    {
        [HideInInspector]
        public InputStr Input;
        public struct InputStr
        {
            public float LookX;
            public float LookZ;
            public float RunX;
            public float RunZ;
            public bool Jump;
        }

        public const float Speed = 10f;
        public const float JumpForce = 5f;

        protected Rigidbody Rigidbody;
        protected Quaternion LookRotation;
        protected Collider MainCollider;
        protected Animator CharacterAnimator;
        protected GameObject CharacterRagdoll;

        protected bool Grounded = true;

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            CharacterAnimator = GetComponentInChildren<Animator>();
            CharacterRagdoll = transform.Find("CharacterRagdoll").gameObject;
            MainCollider = GetComponent<Collider>();
        }

        private void Start()
        {
            SetRagdoll(false);
        }

        private void Update()
        {
            if (Rigidbody == null)
                return;

            CharacterAnimator.SetBool("Grounded", Grounded);

            var localVelocity = Quaternion.Inverse(transform.rotation) * (Rigidbody.velocity / Speed);
            CharacterAnimator.SetFloat("RunX", localVelocity.x);
            CharacterAnimator.SetFloat("RunZ", localVelocity.z);


        }

        void FixedUpdate()
        {
            if (Rigidbody == null)
                return;

            var inputRun = Vector3.ClampMagnitude(new Vector3(Input.RunX, 0, Input.RunZ), 1);
            var inputLook = Vector3.ClampMagnitude(new Vector3(Input.LookX, 0, Input.LookZ), 1);

            Rigidbody.velocity = new Vector3(inputRun.x * Speed, Rigidbody.velocity.y, inputRun.z * Speed);

            //rotation to go target
            if (inputLook.magnitude > 0.01f)
                LookRotation = Quaternion.AngleAxis(Vector3.SignedAngle(Vector3.forward, inputLook, Vector3.up), Vector3.up);

            transform.rotation = LookRotation;
            Grounded = Physics.OverlapSphere(transform.position, 0.3f, 1).Length > 1;

            if (Input.Jump)
            {
                if (Grounded)
                {
                    Rigidbody.velocity = new Vector3(Rigidbody.velocity.x, JumpForce, Rigidbody.velocity.z);
                }
            }
        }

        private void LateUpdate()
        {
            CharacterAnimator.transform.localPosition = Vector3.zero;
            CharacterAnimator.transform.localRotation = Quaternion.identity;
        }

        public void OnHit(Vector3 direction)
        {
            //Remote Procedure call
            photonView.RPC("OnHitRpc", RpcTarget.All, direction);
        }

        [PunRPC]
        void OnHitRpc(Vector3 direction, PhotonMessageInfo info)
        {
            SetRagdoll(true);
            if(GetComponent<Controller>() != null)
                Destroy(GetComponent<Controller>());

            //Properties
            PhotonNetwork.LocalPlayer.CustomProperties.Add("state", "dead");
            //access by: PhotonNetwork.PlayerListOthers[0].CustomProperties["state"]
        }

        public void SetRagdoll(bool on)
        {
            CharacterAnimator.gameObject.SetActive(!on);
            CharacterRagdoll.gameObject.SetActive(on);
            if (on)
            {
                Destroy(MainCollider);
                Destroy(Rigidbody);
            }
        }
    }
}