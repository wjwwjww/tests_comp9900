using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PooledParticle : MonoBehaviour
{
    private const float ReturnBufferSeconds = 0.1f;

    private ParticlePoolManager _pool;
    private ParticleSystem _particleSystem;
    private Coroutine _returnRoutine;

    public ParticleSystem PrefabKey { get; private set; }

    public void Initialize(ParticlePoolManager pool, ParticleSystem prefabKey)
    {
        _pool = pool;
        PrefabKey = prefabKey;
        _particleSystem = GetComponent<ParticleSystem>();
        ResetState();
    }

    public void Rent(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;

        gameObject.SetActive(true);

        _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _particleSystem.Play(true);

        if (_returnRoutine != null)
        {
            StopCoroutine(_returnRoutine);
        }

        _returnRoutine = StartCoroutine(ReturnAfterLifetime());
    }

    public void ResetState()
    {
        if (_returnRoutine != null)
        {
            StopCoroutine(_returnRoutine);
            _returnRoutine = null;
        }

        if (_particleSystem != null)
        {
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        gameObject.SetActive(false);
    }

    private IEnumerator ReturnAfterLifetime()
    {
        yield return new WaitForSeconds(GetEstimatedLifetimeSeconds());

        _returnRoutine = null;
        _pool.ReturnToPool(this);
    }

    private float GetEstimatedLifetimeSeconds()
    {
        ParticleSystem.MainModule main = _particleSystem.main;
        float startLifetime = main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants
            ? main.startLifetime.constantMax
            : main.startLifetime.constant;

        return main.duration + startLifetime + ReturnBufferSeconds;
    }
}
