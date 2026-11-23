using System.Collections;
using UnityEngine;

public class PortaInterativa : MonoBehaviour
{
    public float anguloAbertura = 90f;
    public float duracao = 0.4f;

    private bool aberta;
    private bool animando;

    private Quaternion rotacaoFechada;

    void Awake()
    {
        rotacaoFechada = transform.localRotation;
    }

    public void Interagir()
    {
        if (animando)
            return;

        StartCoroutine(AlternarPorta());
    }

    IEnumerator AlternarPorta()
    {
        animando = true;

        Quaternion inicio = transform.localRotation;

        Quaternion destino = aberta
            ? rotacaoFechada
            : rotacaoFechada * Quaternion.Euler(0f, anguloAbertura, 0f);

        float tempo = 0f;
        float tempoTotal = Mathf.Max(0.01f, duracao);

        while (tempo < tempoTotal)
        {
            tempo += Time.deltaTime;

            float progresso = Mathf.Clamp01(tempo / tempoTotal);
            float suave = Mathf.SmoothStep(0f, 1f, progresso);

            transform.localRotation =
                Quaternion.Slerp(inicio, destino, suave);

            yield return null;
        }

        transform.localRotation = destino;

        aberta = !aberta;
        animando = false;
    }
}