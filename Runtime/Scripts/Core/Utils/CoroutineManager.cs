using UnityEngine;
using System.Collections;
using NobunAtelier;

public class CoroutineManager : SingletonMonoBehaviour<CoroutineManager>
{
    public static void Start(IEnumerator routine)
    {
        if (!IsSingletonValid)
        {
            var gao = new GameObject("CoroutineManager");
            gao.AddComponent<CoroutineManager>();
        }

        Instance.Execute(routine);
    }

    private void Execute(IEnumerator coroutine)
    {
        StartCoroutine(coroutine);
    }
}