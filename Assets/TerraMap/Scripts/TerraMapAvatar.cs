using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace TerraMap
{
    [DefaultExecutionOrder(100)]
    public sealed class TerraMapAvatar : MonoBehaviour
    {
        public TerraMapPlayer Player;
        static readonly string[] Names = { "Idle_Loop", "Walk_Loop", "Sprint_Loop", "Crouch_Idle_Loop", "Crouch_Fwd_Loop", "Jump_Loop", "Punch_Jab", "Punch_Cross" };
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable[] clips;
        float[] lengths, weights;
        readonly Dictionary<string, int> indices = new Dictionary<string, int>();
        Transform visual, head;
        Vector3 headScale;
        int punch = -1;
        float punchUntil;

        void Start()
        {
            var prefab = Resources.Load<GameObject>("Player/Male_Peasant");
            if (prefab == null) { Debug.LogError("terra-map: player model is missing."); return; }
            visual = new GameObject("Visual").transform;
            var body = Instantiate(prefab);
            body.name = "Armature";
            body.transform.SetParent(visual, false);
            foreach (var name in new[] { "Head_Male", "Hair_Buzzed" })
            {
                var part = Resources.Load<GameObject>("Player/" + name);
                if (part != null) TerraWalk.WalkDress.Attach(body, Instantiate(part));
            }
            foreach (var animator in body.GetComponentsInChildren<Animator>()) { animator.enabled = false; Destroy(animator); }
            var bounds = new Bounds();
            var first = true;
            foreach (var renderer in body.GetComponentsInChildren<Renderer>())
            {
                if (first) { bounds = renderer.bounds; first = false; } else bounds.Encapsulate(renderer.bounds);
            }
            var scale = bounds.size.y > 0f ? 1.8f / bounds.size.y : 1f;
            body.transform.localScale = Vector3.one * scale;
            body.transform.localPosition = Vector3.up * (-bounds.min.y * scale);
            visual.SetParent(transform, false);
            foreach (var t in visual.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = 2;
                if (t.name == "Head") { head = t; headScale = t.localScale; }
            }
            var imported = Resources.LoadAll<AnimationClip>("Player/UAL1_Standard");
            var selected = new List<AnimationClip>();
            foreach (var name in Names)
            {
                var clip = System.Array.Find(imported, c => c.name == name);
                if (clip == null) continue;
                indices[name] = selected.Count;
                selected.Add(clip);
            }
            if (selected.Count == 0) { Debug.LogError("terra-map: animation clips are missing."); return; }
            var target = visual.gameObject.AddComponent<Animator>();
            target.applyRootMotion = false;
            graph = PlayableGraph.Create("terra-map player");
            mixer = AnimationMixerPlayable.Create(graph, selected.Count);
            clips = new AnimationClipPlayable[selected.Count];
            lengths = new float[selected.Count];
            weights = new float[selected.Count];
            for (var i = 0; i < clips.Length; i++)
            {
                clips[i] = AnimationClipPlayable.Create(graph, selected[i]);
                lengths[i] = Mathf.Max(0.01f, selected[i].length);
                graph.Connect(clips[i], 0, mixer, i);
            }
            weights[0] = 1f;
            mixer.SetInputWeight(0, 1f);
            AnimationPlayableOutput.Create(graph, "Player", target).SetSourcePlayable(mixer);
            graph.Play();
        }

        public void Punch(bool cross)
        {
            if (!indices.TryGetValue(cross ? "Punch_Cross" : "Punch_Jab", out punch)) { punch = -1; return; }
            clips[punch].SetTime(0);
            punchUntil = Time.time + Mathf.Min(lengths[punch], 0.45f);
        }

        void Update()
        {
            if (!graph.IsValid()) return;
            var moving = Player.Move.sqrMagnitude > 0.01f;
            var name = Player.Flying || !Player.Grounded ? "Jump_Loop" : Player.Crouch ? (moving ? "Crouch_Fwd_Loop" : "Crouch_Idle_Loop")
                : moving ? (Player.Sprint ? "Sprint_Loop" : "Walk_Loop") : "Idle_Loop";
            indices.TryGetValue(name, out var selected);
            if (punch >= 0 && Time.time < punchUntil) selected = punch;
            var total = 0f;
            for (var i = 0; i < clips.Length; i++)
            {
                weights[i] = Mathf.MoveTowards(weights[i], selected == i ? 1f : 0f, Time.deltaTime * 10f);
                total += weights[i];
                if (i != punch && clips[i].GetTime() >= lengths[i]) clips[i].SetTime(clips[i].GetTime() % lengths[i]);
            }
            for (var i = 0; i < clips.Length; i++) mixer.SetInputWeight(i, weights[i] / Mathf.Max(total, 0.0001f));
        }

        void LateUpdate()
        {
            if (head != null) head.localScale = Player.Perspective == 0 ? Vector3.zero : headScale;
            if (visual == null) return;
            // shortcut: prone rotates the idle rig; replace when a prone animation is supplied.
            visual.localRotation = Player.Prone ? Quaternion.Euler(85f, 0f, 0f) : Quaternion.identity;
            visual.localPosition = Player.Prone ? Vector3.up * 0.2f : Vector3.zero;
        }

        void OnDestroy() { if (graph.IsValid()) graph.Destroy(); }
    }
}
