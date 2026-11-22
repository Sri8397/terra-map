using UnityEngine;

namespace TerraMap
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class TerraMapPlayer : MonoBehaviour
    {
        public TerraMapGame Game;
        public Vector2 Move;
        public float Lift;
        public bool Sprint, Crouch, Prone, Flying;
        public bool PointerFree;
        public bool FreeLook;
        public Camera View { get; private set; }
        public TerraMapAvatar Avatar { get; private set; }
        public float Speed { get; private set; }
        public bool Grounded => controller.isGrounded;
        public int Perspective { get; private set; } = 1;
        public bool CanAct => Game != null && Game.MapReady && !Game.Paused;
        CharacterController controller;
        Transform pack;
        Vector3 flightHome;
        float yaw = 29f, pitch = 15f, vertical, punchAt;
        bool cross;
        const int WORLD_MASK = ~(1 << 2);
        // Helsinki map: X east and Z north in [-1000, 1000] m; spawn (X, Z) on Senate Square.
        const float LIMIT = 999.6f;
        static readonly Vector2 SPAWN = new Vector2(363f, -57f);

        void Awake()
        {
            gameObject.layer = 2;
            controller = GetComponent<CharacterController>();
            controller.height = 1.8f;
            controller.center = Vector3.up * 0.9f;
            controller.radius = 0.3f;
            controller.stepOffset = 0.4f;
            controller.slopeLimit = 50f;
            View = new GameObject("Main Camera").AddComponent<Camera>();
            View.tag = "MainCamera";
            View.fieldOfView = 65f;
            View.nearClipPlane = 0.05f;
            View.farClipPlane = 2200f;
            View.allowHDR = true;
            View.allowMSAA = true;
            View.allowDynamicResolution = false;
            View.gameObject.AddComponent<AudioListener>();
            Avatar = gameObject.AddComponent<TerraMapAvatar>();
            Avatar.Player = this;
            pack = new GameObject("Jetpack").transform;
            pack.SetParent(transform, false);
            pack.localPosition = new Vector3(0f, 1.15f, -0.27f);
            var material = new Material(Shader.Find("Standard")) { color = new Color(0.24f, 0.27f, 0.3f) };
            material.SetFloat("_Metallic", 0.7f);
            for (var side = -1; side <= 1; side += 2)
            {
                var tank = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Destroy(tank.GetComponent<Collider>());
                tank.layer = 2;
                tank.transform.SetParent(pack, false);
                tank.transform.localPosition = new Vector3(side * 0.12f, 0f, 0f);
                tank.transform.localScale = new Vector3(0.18f, 0.28f, 0.18f);
                tank.GetComponent<Renderer>().sharedMaterial = material;
            }
            pack.gameObject.SetActive(false);
        }

        public void Look(Vector2 delta)
        {
            yaw = Mathf.Repeat(yaw + delta.x, 360f);
            pitch = Mathf.Clamp(pitch - delta.y, -85f, 85f);
        }

        public void Jump()
        {
            if (CanAct && !Flying && Grounded && !Prone) vertical = 5.6f;
        }

        public void ToggleJet()
        {
            if (!CanAct) return;
            Flying = !Flying;
            if (Flying) flightHome = transform.position;
            Crouch = Prone = false;
            vertical = 0f;
            pack.gameObject.SetActive(Flying && Perspective != 0);
        }

        public void Return()
        {
            if (CanAct && Flying)
            {
                MoveTo(flightHome);
                ToggleJet();
            }
        }

        public void ChangeView()
        {
            Perspective = (Perspective + 1) % 3;
            pack.gameObject.SetActive(Flying && Perspective != 0);
        }

        public void Punch()
        {
            if (!CanAct || Flying || Prone || Time.time < punchAt) return;
            punchAt = Time.time + 0.45f;
            cross = !cross;
            Avatar.Punch(cross);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var direction = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            if (Physics.SphereCast(transform.position + Vector3.up * 1.2f, 0.18f, direction,
                out var hit, 1.5f, WORLD_MASK, QueryTriggerInteraction.Ignore))
            {
                if (hit.rigidbody != null && !hit.rigidbody.isKinematic)
                    hit.rigidbody.AddForceAtPosition(direction * 12f, hit.point, ForceMode.Impulse);
                hit.collider.SendMessageUpwards("TakePunch", 25f, SendMessageOptions.DontRequireReceiver);
            }
        }

        public void ClearInput() { Move = Vector2.zero; Lift = 0f; Sprint = false; }

        public bool ResetToStreet()
        {
            // Senate Square, on the open square south of the statue (checked against the mesh).
            if (!Physics.Raycast(new Vector3(SPAWN.x, 1500f, SPAWN.y), Vector3.down, out var hit,
                3000f, WORLD_MASK, QueryTriggerInteraction.Ignore)) return false;
            Flying = Crouch = Prone = false;
            pack.gameObject.SetActive(false);
            vertical = 0f;
            ClearInput();
            MoveTo(hit.point + Vector3.up * 0.1f);
            return true;
        }

        void MoveTo(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
        }

        void Update()
        {
            if (!CanAct) return;
            var height = Prone ? 0.6f : Crouch ? 1.1f : 1.8f;
            // Standing up must not put the capsule through a ceiling.
            if (height <= controller.height || !Physics.CheckCapsule(transform.position + Vector3.up * 0.35f,
                transform.position + Vector3.up * (height - 0.3f), 0.28f, WORLD_MASK, QueryTriggerInteraction.Ignore))
            {
                controller.height = height;
                controller.center = Vector3.up * (height * 0.5f);
            }
            var rotation = Quaternion.Euler(Flying ? pitch : 0f, FreeLook ? transform.eulerAngles.y : yaw, 0f);
            var input = Vector2.ClampMagnitude(Move, 1f);
            var direction = rotation * new Vector3(input.x, 0f, input.y);
            Speed = Flying ? 18f : Prone ? 0.6f : Crouch ? 1.6f : Sprint ? 10f : 4f;
            var velocity = direction * Speed;
            if (Flying) velocity += Vector3.up * Lift * 12f;
            else
            {
                if (Grounded && vertical < 0f) vertical = -2f;
                vertical = Mathf.Max(vertical - 9.81f * Time.deltaTime, -55f);
                velocity.y = vertical;
            }
            var next = transform.position + velocity * Time.deltaTime;
            next.x = Mathf.Clamp(next.x, -LIMIT, LIMIT);
            next.z = Mathf.Clamp(next.z, -LIMIT, LIMIT);
            next.y = Mathf.Min(next.y, 1200f);
            controller.Move(next - transform.position);
            if (input.sqrMagnitude > 0.01f && !FreeLook)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0f, yaw, 0f), 720f * Time.deltaTime);
            if (transform.position.y < -200f) ResetToStreet();
        }

        void LateUpdate()
        {
            var pivot = transform.position + Vector3.up * (Prone ? 0.4f : Crouch ? 0.95f : 1.65f);
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            var distance = Perspective == 0 ? 0f : Perspective == 1 ? 3.5f : 6f;
            if (distance > 0f && Physics.SphereCast(pivot, 0.2f, rotation * Vector3.back,
                out var hit, distance, WORLD_MASK, QueryTriggerInteraction.Ignore)) distance = Mathf.Max(0f, hit.distance - 0.1f);
            View.transform.SetPositionAndRotation(pivot + rotation * Vector3.back * distance, rotation);
        }

        void OnDestroy()
        {
            if (pack != null) Destroy(pack.GetComponentInChildren<Renderer>(true)?.sharedMaterial);
            if (View != null) Destroy(View.gameObject);
        }
    }
}
