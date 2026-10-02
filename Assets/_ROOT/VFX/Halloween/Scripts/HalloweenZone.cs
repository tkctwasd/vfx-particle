using UnityEngine;
using UnityEngine.Events;

// Vùng đứng Halloween: Idle (pulse chậm) -> Active (player đứng trong, fill tiến trình) -> Completed (nổ hiệu ứng)
public class HalloweenZone : MonoBehaviour
{
    public enum State { Idle, Active, Completed }

    [SerializeField] Transform idleRoot;
    [SerializeField] ParticleSystem enterBurst;
    [SerializeField] ParticleSystem completeBurst;
    [SerializeField] Renderer progressRing;

    [Header("Pulse")]
    [SerializeField] float idlePulseSpeed = 2f;
    [SerializeField] float idlePulseAmount = 0.04f;
    [SerializeField] float activeScale = 1.12f;

    [Header("Trigger")]
    [SerializeField] bool useTrigger = true;
    [SerializeField] string playerTag = "Player";
    [SerializeField, Tooltip("> 0: đứng trong vùng đủ số giây này thì tự Complete")] float holdSeconds = 2f;
    [SerializeField] bool resetProgressOnExit = true;

    public UnityEvent onEnter;
    public UnityEvent onExit;
    public UnityEvent onCompleted;

    static readonly int FillId = Shader.PropertyToID("_Fill");

    MaterialPropertyBlock mpb;
    State state;
    float progress;
    float scaleK = 1f;
    int insideCount;

    public State CurrentState => state;
    public float Progress => progress;

    void OnEnable()
    {
        ResetZone();
    }

    void Update()
    {
        if (state == State.Completed) return;

        bool active = state == State.Active;
        scaleK = Mathf.Lerp(scaleK, active ? activeScale : 1f, Time.deltaTime * 8f);
        float pulse = 1f + Mathf.Sin(Time.time * idlePulseSpeed * (active ? 2.5f : 1f)) * idlePulseAmount;
        if (idleRoot) idleRoot.localScale = Vector3.one * (scaleK * pulse);

        if (active && holdSeconds > 0f)
        {
            SetProgress(progress + Time.deltaTime / holdSeconds);
            if (progress >= 1f) Complete();
        }
    }

    public void Enter()
    {
        if (state == State.Completed) return;
        state = State.Active;
        if (enterBurst) enterBurst.Play(true);
        onEnter?.Invoke();
    }

    public void Exit()
    {
        if (state != State.Active) return;
        state = State.Idle;
        if (resetProgressOnExit) SetProgress(0f);
        onExit?.Invoke();
    }

    public void Complete()
    {
        if (state == State.Completed) return;
        state = State.Completed;
        SetProgress(1f);
        if (completeBurst) completeBurst.Play(true);
        SetIdlePlaying(false);
        if (progressRing) progressRing.enabled = false;
        onCompleted?.Invoke();
    }

    public void ResetZone()
    {
        state = State.Idle;
        insideCount = 0;
        scaleK = 1f;
        SetProgress(0f);
        if (progressRing) progressRing.enabled = true;
        if (idleRoot) idleRoot.localScale = Vector3.one;
        SetIdlePlaying(true);
    }

    public void SetProgress(float value)
    {
        progress = Mathf.Clamp01(value);
        if (!progressRing) return;
        mpb ??= new MaterialPropertyBlock();
        progressRing.GetPropertyBlock(mpb);
        mpb.SetFloat(FillId, progress);
        progressRing.SetPropertyBlock(mpb);
    }

    void SetIdlePlaying(bool play)
    {
        if (!idleRoot) return;
        foreach (var ps in idleRoot.GetComponentsInChildren<ParticleSystem>())
        {
            if (play) ps.Play(false);
            else ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!useTrigger || !other.CompareTag(playerTag)) return;
        if (insideCount++ == 0) Enter();
    }

    void OnTriggerExit(Collider other)
    {
        if (!useTrigger || !other.CompareTag(playerTag)) return;
        insideCount = Mathf.Max(0, insideCount - 1);
        if (insideCount == 0) Exit();
    }

    [ContextMenu("Test/Enter")] void TestEnter() => Enter();
    [ContextMenu("Test/Exit")] void TestExit() => Exit();
    [ContextMenu("Test/Complete")] void TestComplete() => Complete();
    [ContextMenu("Test/Reset")] void TestReset() => ResetZone();
}
